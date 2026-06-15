# StreamForge

A .NET video encoding orchestration service — a focused, FFmpeg-backed MVP in the spirit
of Bitmovin's encoding layer (not the player or analytics products).

## What it is

StreamForge wraps FFmpeg behind a REST job API. Submit a source video and a set of output
renditions; a worker pool transcodes it to adaptive HLS and reports progress. The reusable
encoding logic ships as the `StreamForge.Core` NuGet package; the API and worker are apps
that consume it.

## Repository layout

```
StreamForge.sln
├── src/
│   ├── StreamForge.Core/      NuGet library: models, FFmpeg builder, progress parser, abstractions
│   ├── StreamForge.Api/       ASP.NET Core minimal API (job submit + status)
│   └── StreamForge.Worker/    Worker service: consumes jobs, runs FFmpeg
├── docs/                      architecture (C4), getting started, API reference, roadmap
├── .github/workflows/ci.yml   build + pack
└── bootstrap-github.sh        one-shot local script to create the repo and push
```

## Quick start

```bash
dotnet restore && dotnet build
dotnet run --project src/StreamForge.Api
```

See [docs/getting-started.md](docs/getting-started.md) for submitting a job, and
[docs/architecture.md](docs/architecture.md) for the C4 diagrams.

## Publishing this to GitHub

This repo is set up so you push it with your own credentials — no tokens shared anywhere:

```bash
gh auth login          # one-time, opens a browser, stores credential locally
./bootstrap-github.sh  # creates the repo and pushes; defaults to a private repo
```

Pass a name and visibility if you like: `./bootstrap-github.sh streamforge public`.

## A note on the name

There is an existing company called Streamforge (influencer marketing, unrelated domain).
Fine for an internal project or OSS repo/namespace; pick a distinct name before any
commercial/SaaS branding.

## Status

MVP scaffold. The API runs with an in-memory store; wiring real storage, PostgreSQL, and
RabbitMQ are the next steps — see [docs/roadmap.md](docs/roadmap.md). The scaffold has not
been compiled in CI yet, so expect to nudge package versions on first `dotnet restore`.
