# Job API reference

Two endpoints. The API validates input, persists job state, and (once wired) publishes a
message to the queue. Status reads come straight from the job store.

## POST /api/jobs

Submit a new encoding job.

Request body:

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `sourceUrl` | string | yes | URL FFmpeg can read for the source video |
| `outputs` | array | yes | One or more renditions (the HLS ladder) |
| `outputs[].name` | string | yes | Variant name, e.g. `1080p` |
| `outputs[].width` | int | yes | Output width in pixels |
| `outputs[].height` | int | yes | Output height in pixels |
| `outputs[].videoBitrateKbps` | int | yes | Target video bitrate |
| `outputs[].audioBitrateKbps` | int | no | Defaults to 128 |
| `callbackUrl` | string | no | Webhook called on completion |

Responses:

- `202 Accepted` with `{ "jobId": "...", "status": "Queued" }` and a `Location` header
  pointing at the status endpoint.
- `400 Bad Request` if `sourceUrl` is missing or `outputs` is empty.

## GET /api/jobs/{id}

Poll job status.

Responses:

- `200 OK`:

  ```json
  {
    "jobId": "...",
    "status": "Queued | Running | Completed | Failed",
    "progress": 0.0,
    "manifestUrl": "https://.../master.m3u8 or null",
    "error": "message or null"
  }
  ```

- `404 Not Found` for an unknown id.

## Status values

| Status | Meaning |
|--------|---------|
| `Queued` | Accepted, waiting for a worker |
| `Running` | Worker is encoding; `progress` is 0..1 |
| `Completed` | Done; `manifestUrl` points at the HLS master playlist |
| `Failed` | Encoding failed; `error` has the reason |
