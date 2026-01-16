# BclPro AI Ed - Generative Adaptive Music for Games

Real-time AI-generated adaptive soundtracks for Unity games using transformer-based MIDI generation.

---

## What this repo contains

An adaptive music system that generates MIDI in response to game state and plays it inside Unity.

- Unity project under `Assets/` for runtime integration and demo scenes.
- A Python-based MIDI generation model in `midi-model/src/` (Gradio app / model tools).
- A Dockerized WebSocket MIDI generation server (CPU and CUDA) under `midi-model/docker/`.
- Documentation under `docs/` for integration and research guidance.

This repository is primarily a research & integration project for generative adaptive music.


---

## Quick Start (Docker MIDI generator + Unity)

The easiest way to run the MIDI generator is via Docker Compose. The Unity project connects to the WebSocket server on:

- `ws://localhost:8766`

### 1) Prerequisites

- Unity (project version is defined in `ProjectSettings/ProjectVersion.txt`)
- Docker Desktop

Unity package dependency:

- Import `BclPackage.unitypackage` (required). In Unity: `Assets -> Import Package -> Custom Package...` and select `BclPackage.unitypackage` from the repo root.

Recommended:

```bash
copy .env.example .env
```

For CUDA/GPU mode additionally:

- NVIDIA GPU + driver
- WSL2 backend enabled in Docker Desktop

GPU sanity check:

```bash
nvidia-smi
docker run --rm --gpus all nvidia/cuda:12.1.0-base-ubuntu22.04 nvidia-smi
```

### 2) Start the MIDI server (CPU)

From the repo root:

```bash
docker compose -f midi-model/docker/docker-compose.yml up -d --build midi-server-cpu
```

Tail logs:

```bash
docker logs -f midi-server-cpu
```

Stop:

```bash
docker compose -f midi-model/docker/docker-compose.yml stop midi-server-cpu
```

### 3) Start the MIDI server (CUDA / GPU)

From the repo root:

```bash
docker compose -f midi-model/docker/docker-compose.yml --profile cuda up -d --build midi-server-cuda
```

Optional GPU selection:

```bash
set CUDA_VISIBLE_DEVICES=0
```

Tail logs:

```bash
docker logs -f midi-server-cuda
```

Stop:

```bash
docker compose -f midi-model/docker/docker-compose.yml stop midi-server-cuda
```

### 4) Run the Unity demo scene

- Open the Unity project.
- Open the scene `Assets/Scenes/AdaptiveMusic.unity`.
- Press Play.

If you don’t hear music immediately, check the Unity Console for `[AdaptiveMusicSystem]` / `[WebSocketClient]` logs and ensure the container is up and listening on port `8766`.

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
