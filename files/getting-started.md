# Getting started

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- `ffmpeg` and `ffprobe` on your `PATH` (only needed to run the worker against real video)

Check your setup:

```bash
dotnet --version   # expect 8.x
ffmpeg -version    # any recent build
```

## Build

```bash
dotnet restore
dotnet build
```

## Run the API

The API ships with an in-memory job store, so it runs with zero infrastructure:

```bash
dotnet run --project src/StreamForge.Api
```

By default it listens on the URLs printed at startup (typically `http://localhost:5xxx`).

## Submit a job

```bash
curl -X POST http://localhost:5000/api/jobs \
  -H "Content-Type: application/json" \
  -d '{
    "sourceUrl": "https://example.com/input.mp4",
    "outputs": [
      { "name": "1080p", "width": 1920, "height": 1080, "videoBitrateKbps": 4500 },
      { "name": "720p",  "width": 1280, "height": 720,  "videoBitrateKbps": 2500 }
    ],
    "callbackUrl": "https://client.example.com/job-callback"
  }'
```

Response:

```json
{ "jobId": "…", "status": "Queued" }
```

## Poll status

```bash
curl http://localhost:5000/api/jobs/{jobId}
```

```json
{ "jobId": "…", "status": "Running", "progress": 0.42, "manifestUrl": null, "error": null }
```

## What is not wired yet

The in-memory store means jobs do not persist across restarts, and there is no queue or
worker running in this minimal setup — the job stays `Queued`. See
[the roadmap](roadmap.md) for the ordered steps to make submission actually dispatch to a
worker (implement `IObjectStorage` and `IJobStore`, then add MassTransit + RabbitMQ).
