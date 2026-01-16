#!/usr/bin/env python3
"""
WebSocket Server with TRUE Event-by-Event Streaming
Streams MIDI tokens as they're generated - no waiting for complete chunks!
"""
import asyncio
import websockets
import json
import base64
import argparse
import sys
import os
import numpy as np
import onnxruntime as rt
from pathlib import Path
from pathlib import PurePosixPath
from concurrent.futures import ThreadPoolExecutor
import queue
import threading

# Add src directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

# Import from app_onnx.py
import app_onnx
from app_onnx import generate, get_tokenizer, apply_io_binding, sample_top_p_k, softmax
import MIDI

# Global model state
model_base = None
model_token = None
tokenizer = None
device = "cuda"

# Inject device into app_onnx module so generate() can access it
app_onnx.device = device


number2drum_kits = {
    -1: "None",
    0: "Standard",
    8: "Room",
    16: "Power",
    24: "Electric",
    25: "TR-808",
    32: "Jazz",
    40: "Blush",
    48: "Orchestra",
}

key_signatures = [
    'C♭', 'A♭m', 'G♭', 'E♭m', 'D♭', 'B♭m', 'A♭', 'Fm', 'E♭', 'Cm', 'B♭', 'Gm', 'F', 'Dm',
    'C', 'Am', 'G', 'Em', 'D', 'Bm', 'A', 'F♯m', 'E', 'C♯m', 'B', 'G♯m', 'F♯', 'D♯m', 'C♯', 'A♯m'
]


def _parse_time_sig(time_sig):
    """Return (nn, dd_code) for tokenizer v2 time_signature event, or None."""
    if time_sig is None:
        return None
    if isinstance(time_sig, str):
        if time_sig.strip().lower() in ("", "auto", "none"):
            return None
        try:
            nn_s, dd_s = time_sig.split('/')
            nn = int(nn_s)
            dd = int(dd_s)
            dd_code = {2: 1, 4: 2, 8: 3}.get(dd)
            if dd_code is None:
                return None
            return nn, dd_code
        except Exception:
            return None
    return None


def _parse_key_sig(key_sig):
    """Return (sf, mi) for tokenizer v2 key_signature event, or None.

    Supports:
    - None / "auto" / "none" => None
    - int (1..len(key_signatures)) as app/app_onnx UI index => maps to list
    - string in key_signatures list => maps directly
    """
    if key_sig is None:
        return None
    if isinstance(key_sig, str):
        ks = key_sig.strip()
        if ks.lower() in ("", "auto", "none"):
            return None
        if ks in key_signatures:
            idx = key_signatures.index(ks)
        else:
            # Allow plain 'C'/'Am' style by matching exact entries if present
            matches = [i for i, v in enumerate(key_signatures) if v == ks]
            if not matches:
                return None
            idx = matches[0]
        # app/app_onnx uses: idx -> sf = idx//2 - 7 ; mi = idx % 2
        sf = idx // 2 - 7
        mi = idx % 2
        return sf, mi
    if isinstance(key_sig, (int, float)):
        # app/app_onnx UI uses 0 => auto, else 1..N
        idx0 = int(key_sig)
        if idx0 <= 0:
            return None
        idx = idx0 - 1
        if idx < 0 or idx >= len(key_signatures):
            return None
        sf = idx // 2 - 7
        mi = idx % 2
        return sf, mi
    return None

def log(msg):
    print(f"[WS-STREAM] {msg}", flush=True)


def _resolve_model_path(path_str: str) -> str:
    """Resolve a model path, optionally relative to MODEL_PATH.

    If MODEL_PATH is set and the provided path is relative, treat it as relative
    to MODEL_PATH instead of CWD. This makes docker-compose env MODEL_PATH
    effective while keeping the existing defaults working.
    """
    if not path_str:
        return path_str

    p = Path(path_str)
    if p.is_absolute():
        return str(p)

    model_root = os.environ.get("MODEL_PATH")
    if model_root:
        # Interpret common conventions:
        # - If path is like "models/default/...", treat it as relative to MODEL_PATH
        #   (dropping the leading "models/") so MODEL_PATH="/app/models" maps to
        #   "/app/models/default/...".
        # - Otherwise treat it as relative to MODEL_PATH directly.
        posix_parts = PurePosixPath(path_str.replace("\\", "/")).parts
        if len(posix_parts) > 0 and posix_parts[0] == "models":
            rel_posix = PurePosixPath(*posix_parts[1:])
        else:
            rel_posix = PurePosixPath(*posix_parts)

        candidate = Path(model_root) / Path(str(rel_posix))

        # Backward-compat for a previously buggy resolution that produced
        # /app/models/models/...; if that already exists, prefer it over re-downloading.
        legacy = Path(model_root) / p
        if candidate.exists() or not legacy.exists():
            return str(candidate)
        return str(legacy)

    return str(p)


def _ensure_model_files(args, *, force: bool = False):
    """Download required model artifacts if missing.

    If `force` is True, existing files are removed and re-downloaded.
    """
    if getattr(args, "no_download", False):
        return

    def _maybe_remove(path: str, *, min_bytes: int | None = None):
        if not path or not os.path.exists(path):
            return
        if force:
            try:
                os.remove(path)
            except OSError:
                pass
            return
        if min_bytes is not None:
            try:
                if os.path.getsize(path) < min_bytes:
                    os.remove(path)
            except OSError:
                pass

    # Only auto-download when we were given URLs (defaults are provided).
    # Reuse midi-model's downloader so we don't duplicate logic.
    try:
        if args.model_config and args.model_config.endswith(".json") and args.model_config_url:
            _maybe_remove(args.model_config, min_bytes=32)
            if not os.path.exists(args.model_config):
                log(f"⬇️  Downloading model config → {args.model_config}")
            app_onnx.download_if_not_exit(args.model_config_url, args.model_config)

        if args.model_base_url:
            # Heuristic: a valid ONNX model will be far larger than 1MB.
            _maybe_remove(args.model_base, min_bytes=1_000_000)
            if not os.path.exists(args.model_base):
                log(f"⬇️  Downloading model base → {args.model_base}")
            app_onnx.download_if_not_exit(args.model_base_url, args.model_base)

        if args.model_token_url:
            _maybe_remove(args.model_token, min_bytes=1_000_000)
            if not os.path.exists(args.model_token):
                log(f"⬇️  Downloading model token → {args.model_token}")
            app_onnx.download_if_not_exit(args.model_token_url, args.model_token)

    except Exception as e:
        log(f"❌ Failed to download model files: {e}")
        raise

def generate_in_thread(model, prompt, params, event_queue):
    """Run generate() in a background thread and put events in queue."""
    try:
        generator = np.random.RandomState(params['seed'])
        prompt_len = int(getattr(prompt, "shape", [0])[0]) if prompt is not None else 0
        max_len = int(params['gen_events']) + prompt_len
        for token_seq in generate(
            model,
            prompt=prompt,
            batch_size=1,
            max_len=max_len,
            temp=params['temp'],
            top_p=params['top_p'],
            top_k=params['top_k'],
            disable_patch_change=bool(params.get('disable_patch_change', False)),
            disable_control_change=bool(params.get('disable_control_change', False)),
            disable_channels=params.get('disable_channels', None),
            generator=generator
        ):
            # Put event in queue for async handler to consume
            event_queue.put(('event', token_seq))
        
        # Signal completion
        event_queue.put(('complete', None))
    except Exception as e:
        event_queue.put(('error', str(e)))

def build_initial_prompt(
    tokenizer,
    bpm: int = 0,
    instruments: list = None,
    drum_kit: str = "None",
    time_sig: str = None,
    key_sig=None,
):
    """Build initial MIDI prompt."""
    mid = [[tokenizer.bos_id] + [tokenizer.pad_id] * (tokenizer.max_token_seq - 1)]

    # v2 supports time/key signature meta events in the prompt
    if getattr(tokenizer, "version", "v1") == "v2":
        ts = _parse_time_sig(time_sig)
        if ts is not None:
            nn, dd_code = ts
            mid.append(tokenizer.event2tokens(["time_signature", 0, 0, 0, nn - 1, dd_code - 1]))

        ks = _parse_key_sig(key_sig)
        if ks is not None:
            sf, mi = ks
            mid.append(tokenizer.event2tokens(["key_signature", 0, 0, 0, sf + 7, mi]))
    
    if bpm and int(bpm) != 0:
        mid.append(tokenizer.event2tokens(["set_tempo", 0, 0, 0, int(bpm)]))

    patch2number = {v: k for k, v in MIDI.Number2patch.items()}
    drum_kits2number = {v: k for k, v in number2drum_kits.items()}
    
    patches = {}
    if instruments:
        i = 0
        for instr in instruments:
            if instr not in patch2number:
                continue
            patches[i] = patch2number[instr]
            i = (i + 1) if i != 8 else 10

    if drum_kit and isinstance(drum_kit, str) and drum_kit != "None" and drum_kit in drum_kits2number:
        patches[9] = drum_kits2number[drum_kit]
    
    for idx, (c, p) in enumerate(patches.items()):
        mid.append(tokenizer.event2tokens(["patch_change", 0, 0, idx + 1, c, p]))
    
    return np.array(mid, dtype=np.int64)


async def handler(websocket, path):
    """WebSocket handler with true event streaming."""
    global model_base, model_token, tokenizer, device
    client_addr = websocket.remote_address
    log(f"Client connected: {client_addr}")
    
    try:
        async for message in websocket:
            try:
                request = json.loads(message)
                action = request.get("action")
                
                if action == "stream-events":
                    # TRUE EVENT STREAMING MODE
                    params = request.get("params", {})
                    
                    seed = int(params.get("seed", 999))
                    gen_events = int(params.get("gen_events", params.get("max_len", 200)))
                    temp = float(params.get("temp", 0.85))
                    top_p = float(params.get("top_p", 0.95))
                    top_k = int(params.get("top_k", 50))
                    bpm = int(params.get("bpm", 120))
                    instruments = params.get("instruments", ["Acoustic Grand"])
                    drum_kit = params.get("drum_kit", "None")
                    time_sig = params.get("time_sig", None)
                    key_sig = params.get("key_sig", None)

                    allow_cc = params.get("allow_cc", True)
                    disable_control_change = bool(params.get("disable_control_change", not allow_cc))

                    disable_patch_change = bool(params.get("disable_patch_change", False)) if "disable_patch_change" in params else None
                    disable_channels = params.get("disable_channels", None) if "disable_channels" in params else None
                    
                    log(f"Starting event stream: {gen_events} events, seed={seed}, temp={temp}")
                    
                    # Send start message
                    await websocket.send(json.dumps({
                        "type": "start",
                        "params": {
                            "seed": seed,
                            "gen_events": gen_events,
                            "temp": temp,
                            "instruments": instruments
                        }
                    }))
                    
                    # Build prompt
                    prompt = build_initial_prompt(
                        tokenizer,
                        bpm=bpm,
                        instruments=instruments,
                        drum_kit=drum_kit,
                        time_sig=time_sig,
                        key_sig=key_sig,
                    )

                    # If the client didn't explicitly choose decoding constraints, mirror app.py defaults:
                    # - when instruments/drums are specified, prevent patch changes and restrict channels.
                    if disable_patch_change is None or disable_channels is None:
                        used_channels = []
                        if isinstance(instruments, list) and len(instruments) > 0:
                            # Channels assigned in build_initial_prompt: 0..8 then 10.. as needed
                            ch = 0
                            for _ in instruments:
                                used_channels.append(ch)
                                ch = (ch + 1) if ch != 8 else 10
                        if isinstance(drum_kit, str) and drum_kit != "None":
                            used_channels.append(9)

                        if len(used_channels) > 0:
                            if disable_patch_change is None:
                                disable_patch_change = True
                            if disable_channels is None:
                                disable_channels = [c for c in range(16) if c not in set(used_channels)]

                    disable_patch_change = bool(disable_patch_change) if disable_patch_change is not None else False
                    
                    # Create model tuple
                    model = (model_base, model_token, tokenizer)
                    
                    # Create queue and params for thread
                    event_queue = queue.Queue()
                    gen_params = {
                        'seed': seed,
                        'gen_events': gen_events,
                        'temp': temp,
                        'top_p': top_p,
                        'top_k': top_k,
                        'disable_patch_change': disable_patch_change,
                        'disable_control_change': disable_control_change,
                        'disable_channels': disable_channels,
                    }
                    
                    # Start generation in background thread
                    thread = threading.Thread(
                        target=generate_in_thread,
                        args=(model, prompt, gen_params, event_queue),
                        daemon=True
                    )
                    thread.start()
                    log("🔄 Generation thread started")
                    
                    # Stream events as they arrive from queue
                    event_count = 0
                    # Initialize buffer with prompt tokens (as list for detokenize)
                    events_buffer = prompt.tolist()
                    running = True
                    
                    while running:
                        try:
                            # Non-blocking check with small timeout for async friendliness
                            msg_type, data = event_queue.get(timeout=0.01)
                            
                            if msg_type == 'event':
                                # Got one event token sequence!
                                token_seq = data
                                event_count += 1
                                
                                # Convert token sequence to event
                                try:
                                    # token_seq is (1, max_token_seq) - get the first row
                                    token_list = token_seq[0].tolist() if token_seq.ndim > 1 else token_seq.tolist()
                                    event = tokenizer.tokens2event(token_list)
                                    events_buffer.append(token_list)
                                    
                                    # Send event immediately
                                    event_msg = {
                                        "type": "event",
                                        "index": event_count,
                                        "event": event,
                                        "tokens": token_list
                                    }
                                    await websocket.send(json.dumps(event_msg))
                                    
                                    if event_count % 10 == 0:  # Log every 10 events to reduce spam
                                        log(f"→ Sent event #{event_count}: {event}")
                                    
                                    # Every 20 events, send a MIDI snapshot
                                    if event_count % 20 == 0 or event_count == gen_events:
                                        try:
                                            # Convert accumulated events to MIDI
                                            # detokenize expects a list of token sequences
                                            mid_seq = tokenizer.detokenize(events_buffer)
                                            midi_bytes = MIDI.score2midi(mid_seq)
                                            midi_b64 = base64.b64encode(midi_bytes).decode('utf-8')
                                            
                                            # Send MIDI snapshot
                                            snapshot_msg = {
                                                "type": "snapshot",
                                                "index": event_count,
                                                "total_events": len(events_buffer),
                                                "midi_b64": midi_b64,
                                                "size_bytes": len(midi_bytes)
                                            }
                                            await websocket.send(json.dumps(snapshot_msg))
                                            
                                            log(f"📦 Sent snapshot at {event_count} events: {len(midi_bytes)} bytes")
                                        except Exception as e:
                                            log(f"Snapshot error at {event_count}: {e}")
                                
                                except Exception as e:
                                    log(f"Event decode error: {e}")
                                    continue
                            
                            elif msg_type == 'complete':
                                # Generation finished
                                running = False
                                
                                # Send completion message
                                complete_msg = {
                                    "type": "complete",
                                    "total_events": event_count
                                }
                                await websocket.send(json.dumps(complete_msg))
                                
                                log(f"✅ Stream complete: {event_count} events sent")
                                log(f"   - Total snapshots: {event_count // 20}")
                                log(f"   - Buffer final size: {len(events_buffer)}")
                            
                            elif msg_type == 'error':
                                # Error in generation thread
                                running = False
                                error_msg = str(data)
                                log(f"❌ Generation error: {error_msg}")
                                await websocket.send(json.dumps({
                                    "status": "error",
                                    "error": error_msg
                                }))
                        
                        except queue.Empty:
                            # No event ready yet, yield to event loop
                            await asyncio.sleep(0.001)
                
                elif action == "generate-midi":
                    # STANDARD GENERATION (wait for all events)
                    params = request.get("params", {})
                    
                    seed = int(params.get("seed", 999))
                    gen_events = int(params.get("gen_events", params.get("max_len", 80)))
                    temp = float(params.get("temp", 0.85))
                    top_p = float(params.get("top_p", 0.95))
                    top_k = int(params.get("top_k", 50))
                    bpm = int(params.get("bpm", 120))
                    instruments = params.get("instruments", ["Acoustic Grand"])
                    drum_kit = params.get("drum_kit", "None")
                    time_sig = params.get("time_sig", None)
                    key_sig = params.get("key_sig", None)

                    allow_cc = params.get("allow_cc", True)
                    disable_control_change = bool(params.get("disable_control_change", not allow_cc))

                    disable_patch_change = bool(params.get("disable_patch_change", False)) if "disable_patch_change" in params else None
                    disable_channels = params.get("disable_channels", None) if "disable_channels" in params else None
                    
                    log(f"Generating MIDI: {gen_events} events, seed={seed}")
                    
                    # Build prompt
                    generator = np.random.RandomState(seed)
                    prompt = build_initial_prompt(
                        tokenizer,
                        bpm=bpm,
                        instruments=instruments,
                        drum_kit=drum_kit,
                        time_sig=time_sig,
                        key_sig=key_sig,
                    )

                    if disable_patch_change is None or disable_channels is None:
                        used_channels = []
                        if isinstance(instruments, list) and len(instruments) > 0:
                            ch = 0
                            for _ in instruments:
                                used_channels.append(ch)
                                ch = (ch + 1) if ch != 8 else 10
                        if isinstance(drum_kit, str) and drum_kit != "None":
                            used_channels.append(9)

                        if len(used_channels) > 0:
                            if disable_patch_change is None:
                                disable_patch_change = True
                            if disable_channels is None:
                                disable_channels = [c for c in range(16) if c not in set(used_channels)]

                    disable_patch_change = bool(disable_patch_change) if disable_patch_change is not None else False
                    
                    # Create model tuple
                    model = (model_base, model_token, tokenizer)
                    
                    # Collect all events
                    prompt_len = int(getattr(prompt, "shape", [0])[0]) if prompt is not None else 0
                    max_len = gen_events + prompt_len

                    events_buffer = prompt.tolist() if prompt is not None else [[tokenizer.bos_id] + [tokenizer.pad_id] * (tokenizer.max_token_seq - 1)]

                    for token_seq in generate(
                        model,
                        prompt=prompt,
                        batch_size=1,
                        max_len=max_len,
                        temp=temp,
                        top_p=top_p,
                        top_k=top_k,
                        disable_patch_change=disable_patch_change,
                        disable_control_change=disable_control_change,
                        disable_channels=disable_channels,
                        generator=generator
                    ):
                        token_list = token_seq[0].tolist() if getattr(token_seq, "ndim", 1) > 1 else token_seq.tolist()
                        events_buffer.append(token_list)
                    
                    # Convert to MIDI
                    mid_seq = tokenizer.detokenize(events_buffer)
                    midi_bytes = MIDI.score2midi(mid_seq)
                    
                    # Send response
                    await websocket.send(json.dumps({
                        "status": "ok",
                        "events": gen_events,
                        "midi_b64": base64.b64encode(midi_bytes).decode('utf-8'),
                        "size_bytes": len(midi_bytes)
                    }))
                    
                    log(f"Generated {gen_events} events (+prompt {prompt_len}) ({len(midi_bytes)} bytes)")
                
                else:
                    await websocket.send(json.dumps({
                        "status": "error",
                        "error": f"Unknown action: {action}"
                    }))
            
            except json.JSONDecodeError as e:
                log(f"JSON decode error: {e}")
                await websocket.send(json.dumps({
                    "status": "error",
                    "error": f"Invalid JSON: {str(e)}"
                }))
            
            except Exception as e:
                import traceback
                log(f"Error: {type(e).__name__}: {e}")
                log(f"Traceback:\n{traceback.format_exc()}")
                try:
                    await websocket.send(json.dumps({
                        "status": "error",
                        "error": str(e)
                    }))
                except:
                    pass
    
    except websockets.exceptions.ConnectionClosed:
        log(f"Client disconnected: {client_addr}")


def main():
    global model_base, model_token, tokenizer, device
    
    parser = argparse.ArgumentParser(description="WebSocket server with true event streaming")
    parser.add_argument("--host", type=str, default="0.0.0.0")
    parser.add_argument("--port", type=int, default=8766)
    parser.add_argument("--model-config", type=str, default="models/default/config.json")
    parser.add_argument("--model-base", type=str, default="models/default/model_base.onnx")
    parser.add_argument("--model-token", type=str, default="models/default/model_token.onnx")

    # Auto-download defaults match the upstream midi-model ONNX demo.
    # Can be overridden via CLI or env vars.
    parser.add_argument(
        "--model-config-url",
        type=str,
        default=os.environ.get(
            "MODEL_CONFIG_URL",
            "https://huggingface.co/skytnt/midi-model-tv2o-medium/resolve/main/config.json",
        ),
        help="Download config.json to --model-config if missing",
    )
    parser.add_argument(
        "--model-base-url",
        type=str,
        default=os.environ.get(
            "MODEL_BASE_URL",
            "https://huggingface.co/skytnt/midi-model-tv2o-medium/resolve/main/onnx/model_base.onnx",
        ),
        help="Download model_base.onnx to --model-base if missing",
    )
    parser.add_argument(
        "--model-token-url",
        type=str,
        default=os.environ.get(
            "MODEL_TOKEN_URL",
            "https://huggingface.co/skytnt/midi-model-tv2o-medium/resolve/main/onnx/model_token.onnx",
        ),
        help="Download model_token.onnx to --model-token if missing",
    )
    parser.add_argument(
        "--no-download",
        action="store_true",
        default=os.environ.get("MODEL_NO_DOWNLOAD", "").lower() in ("1", "true", "yes"),
        help="Disable auto-download of model files",
    )
    parser.add_argument("--device", type=str, default="cuda", choices=["cuda", "cpu"])
    args = parser.parse_args()

    # Make MODEL_PATH effective for relative paths.
    args.model_config = _resolve_model_path(args.model_config)
    args.model_base = _resolve_model_path(args.model_base)
    args.model_token = _resolve_model_path(args.model_token)
    
    device = args.device
    app_onnx.device = device  # Update app_onnx module's device variable
    
    log("="*60)
    log("WebSocket Server with TRUE Event Streaming")
    log("="*60)
    log(f"Host: {args.host}:{args.port}")
    log(f"Device: {device}")
    log(f"Model config: {args.model_config}")
    log(f"Model base: {args.model_base}")
    log(f"Model token: {args.model_token}")
    log("="*60)
    
    # Load models
    log("Loading ONNX models...")
    try:
        if device == "cuda":
            providers = [("CUDAExecutionProvider", {"cudnn_conv_algo_search": "DEFAULT"})]
        else:
            providers = ["CPUExecutionProvider"]

        rt.set_default_logger_severity(3)

        _ensure_model_files(args)

        try:
            model_base = rt.InferenceSession(args.model_base, providers=providers)
            model_token = rt.InferenceSession(args.model_token, providers=providers)
        except Exception as e:
            # Common when a previous run was interrupted mid-download leaving a partial file.
            msg = str(e)
            if (not args.no_download) and (
                "INVALID_PROTOBUF" in msg
                or "Protobuf parsing failed" in msg
                or "NO_SUCHFILE" in msg
                or "File doesn't exist" in msg
            ):
                log(f"⚠️  Model load failed ({e}); forcing re-download and retrying once...")
                _ensure_model_files(args, force=True)
                model_base = rt.InferenceSession(args.model_base, providers=providers)
                model_token = rt.InferenceSession(args.model_token, providers=providers)
            else:
                raise

        tokenizer = get_tokenizer(args.model_config)

        log("✅ Models loaded successfully")
        log(f"Tokenizer version: {tokenizer.version}")
        log(f"Vocab size: {tokenizer.vocab_size}")

    except Exception as e:
        log(f"❌ Failed to load models: {e}")
        import traceback
        log(traceback.format_exc())
        sys.exit(1)
    
    # Start WebSocket server
    log("")
    log("🚀 Starting WebSocket server...")
    log(f"📡 Connect to: ws://{args.host}:{args.port}")
    log("")
    log("Available actions:")
    log("  • stream-events  - Stream events one-by-one as generated")
    log("  • generate-midi  - Standard generation (wait for all events)")
    log("")
    log("Press Ctrl+C to stop")
    log("="*60)
    
    async def start():
        async with websockets.serve(
            handler,
            args.host,
            args.port,
            ping_interval=20,
            ping_timeout=20,
            close_timeout=5,
        ):
            await asyncio.Future()  # Run forever
    
    try:
        asyncio.run(start())
    except KeyboardInterrupt:
        log("\n⚠️  Server stopped by user")


if __name__ == "__main__":
    main()
