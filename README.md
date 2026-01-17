# BclPro AI Ed - Generative Adaptive Music for Games

Real-time AI-generated adaptive soundtracks for Unity games using transformer-based MIDI generation.

---

## What this repo contains

An adaptive music system that generates MIDI in response to game state and plays it inside Unity.

- Unity project under `Assets/` for runtime integration and demo scenes.
- A Python-based MIDI generation model in `midi-model/src/` (Gradio app / model tools).
- A Dockerized WebSocket MIDI generation server (CPU and CUDA) under `midi-model/docker/`.

This repository is primarily a research & integration project for generative adaptive music.


---

docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
## Quick Start (Run the Unity demo)

The Unity project expects a MIDI generator server reachable via WebSocket (default):

- `ws://localhost:8766`

Pick ONE of the server options below (Docker CPU, Docker GPU, or Local Python), then run the Unity scene.

### 0) Unity prerequisites

- Unity version: see `ProjectSettings/ProjectVersion.txt`
- Import `BclPackage.unitypackage` (required): Unity → `Assets -> Import Package -> Custom Package...` → select `BclPackage.unitypackage` from the repo root.

### 1) Start a MIDI server (choose one)

#### Option A — Docker (CPU) (recommended)

Size small models, no GPU needed slow generation.

```bash
docker compose -f midi-model/docker/docker-compose.yml up -d --build midi-server-cpu
docker logs -f midi-server-cpu
```

Stop:

```bash
docker compose -f midi-model/docker/docker-compose.yml stop midi-server-cpu
```

#### Option B — Docker (CUDA / GPU)

Larger models, faster generation using NVIDIA GPU.

Prereqs:

- NVIDIA GPU + driver
- Docker Desktop using WSL2 backend

GPU sanity check:

```bash
nvidia-smi
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
```

Run:

```bash
docker compose -f midi-model/docker/docker-compose.yml --profile cuda up -d --build midi-server-cuda
docker logs -f midi-server-cuda
```

Stop:

```bash
docker compose -f midi-model/docker/docker-compose.yml stop midi-server-cuda
```

#### Option C — Local Python (no Docker)

smaller models, no GPU needed slow generation.

This runs the WebSocket server directly on your machine.

Windows (PowerShell) example:

```powershell
py -m venv .venv-ws
.\.venv-ws\Scripts\Activate.ps1
python -m pip install -U pip
pip install -r midi-model/websocket/requirements-websocket.txt

# CPU
python midi-model/websocket/ws_server_true_streaming.py --device cpu --host 127.0.0.1 --port 8766
```

GPU note:

- If you want ONNX Runtime CUDA, install `onnxruntime-gpu` and run with `--device cuda`.
- If CUDA provider isn’t available, the server will fall back to CPU.

### 2) Run the Unity demo scene

- Open the Unity project.
- Open the scene `Assets/Scenes/AdaptiveMusic.unity`.
- Press Play.

If you don’t hear music immediately:

- Confirm the server is listening on port `8766`.
- Check the Unity Console for `[AdaptiveMusicSystem]` / `[WebSocketClient]` logs.

---

## Architecture (high level)

Unity <--> MIDI Server (WebSocket or Named Pipe)

- `Assets/Scripts/AdaptiveMusic/AdaptiveMusicSystem.cs`: orchestrates generation and playback
- `NamedPipeMusicClient.cs`: high-performance IPC client
- `WebSocketMusicClient.cs`: network client (default)
- `midi-model/src/`: Python model, tokenizer, and small Gradio app for testing

The Docker WebSocket server uses ONNX Runtime and loads models from `models/default/` (mounted into the container at `/app/models`).

---

## Project structure (high level)

```
BclPro_AI_Ed/
├── Assets/                 # Unity project (Scenes, Scripts, Audio)
├── midi-model/            # Python model & tools
│   └── src/               # Model code, app.py, requirements.txt
├── docs/                  # Documentation and integration guides
├── models/                # (Optional) ONNX or model files
└── README.md
```

---

## Running & testing notes

- The Unity side supports two client types: WebSocket and Named Pipe. WebSocket is the simplest to run across platforms; Named Pipes provide lower latency on supported platforms.
- The Python model entrypoint for local testing is `midi-model/src/app.py`. There is also `midi-model/src/app_onnx.py` for ONNX runtime usage.
- Use `midi-model/src/check_server.py` for a simple health-check of a running server.

### Test bench (optional)

For automated runs/validation there is a separate compose setup under `midi-model/test_bench/`.

---

## Requirements

- Unity: see `ProjectSettings/ProjectVersion.txt`
- Python: 3.10+
- For GPU: CUDA + appropriate drivers and ONNX Runtime / PyTorch as needed by the model

---
