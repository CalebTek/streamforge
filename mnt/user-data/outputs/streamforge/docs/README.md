# StreamForge documentation

Documentation for the StreamForge encoding orchestration service.

## Contents

- [Architecture (C4 model)](architecture.md) — system context, containers, worker
  components, and the job lifecycle sequence, with Mermaid diagrams.
- [Getting started](getting-started.md) — run the MVP locally and exercise the API.
- [Job API reference](api-reference.md) — the two endpoints and their payloads.
- [Encoding pipeline](encoding-pipeline.md) — how a job flows through FFmpeg, including
  the command-building and progress-parsing internals.
- [Productionization roadmap](roadmap.md) — the ordered steps from MVP to a deployable
  service, and what is intentionally out of scope.
- `diagrams/` — static SVG copies of the architecture diagrams.

## Project layout

```
StreamForge.sln
├── src/
│   ├── StreamForge.Core/      NuGet library: models, FFmpeg builder, progress parser, abstractions
│   ├── StreamForge.Api/       ASP.NET Core minimal API (job submit + status)
│   └── StreamForge.Worker/    Worker service: consumes jobs, runs FFmpeg
├── docs/                      this folder
└── .github/workflows/         CI (build + pack)
```

## The one-line summary

StreamForge is a .NET orchestration layer around FFmpeg. The reusable encoding logic
lives in the `StreamForge.Core` NuGet package; the API and worker are apps that consume
it. The MVP scope is encoding to HLS — not the player or analytics products.
