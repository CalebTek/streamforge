# Productionization roadmap

The MVP runs with zero infrastructure but does not yet dispatch jobs to a worker. These
are the ordered steps to make it a real service.

## 1. Implement object storage

Provide an `IObjectStorage` for your cloud (S3 or Azure Blob). `DownloadAsync` pulls the
source to a local temp path; `UploadDirectoryAsync` pushes the finished HLS tree and
returns the master manifest URL. Keep the cloud SDK dependency in the app project, not in
`StreamForge.Core`.

## 2. Implement job persistence

Replace the in-memory `IJobStore` with a PostgreSQL-backed implementation (EF Core is the
natural choice). Swap the single DI registration in the API. Now jobs survive restarts and
multiple API/worker instances share state.

## 3. Wire the queue

Add `MassTransit.RabbitMQ` to both the API and the worker:

- API: publish an `EncodeJobMessage` after saving the job.
- Worker: register a consumer that calls `EncodeWorker.ProcessAsync`.

At this point submission actually dispatches and the job progresses through `Running` to
`Completed`.

## 4. Put a CDN in front of storage

Serve the HLS output through a CDN (CloudFront, Azure CDN, etc.) so viewers stream
segments from the edge rather than directly from the bucket.

## 5. Hardening

- Authentication on the API (API keys or OAuth).
- Structured logging of the full FFmpeg command, exit code, and stderr per job.
- Health checks and metrics on the worker pool.
- Horizontal scaling of workers (more consumers on the same queue).

## Deliberately out of scope for the MVP

- A player SDK and client-side telemetry.
- An analytics pipeline (time-series storage, dashboards).

These are separate products. Adding them now would balloon scope; add them only once the
encoding service stands on its own.
