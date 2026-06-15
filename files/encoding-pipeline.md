# Encoding pipeline

How a single job flows from message to finished HLS output. This is the heart of the
worker and the part most worth understanding.

## Steps

1. The worker receives an `EncodeJobMessage` (job id + the original `JobRequest`).
2. It downloads the source to a temp directory via `IObjectStorage.DownloadAsync`.
3. It probes the source duration with `ffprobe` — needed to turn elapsed time into a
   percentage.
4. `FfmpegCommandBuilder.BuildHlsArgs` turns the `JobRequest` into an FFmpeg argument
   list: one video map/encode per rendition, one shared AAC audio track, and HLS muxing
   that emits a variant playlist per rendition plus a `master.m3u8`.
5. `FfmpegRunner` spawns FFmpeg with `-progress pipe:1`, reads the progress pipe
   line-by-line, and feeds each line to `FfmpegProgressParser`.
6. On exit code 0, the output directory is uploaded via
   `IObjectStorage.UploadDirectoryAsync`, and the job is marked `Completed` with the
   manifest URL. Non-zero exit marks it `Failed`.

## Why `-progress pipe:1` instead of scraping stderr

FFmpeg's human-readable stderr (`frame= … time= … speed=`) is meant for people, not
parsing — its format varies between builds and is easy to break. The `-progress` option
emits a stable stream of `key=value` lines (`out_time_us`, `frame`, `progress=continue`,
`progress=end`). `FfmpegProgressParser` tracks `out_time_us` against the probed duration
to produce a clamped 0..1 fraction, and flips to complete on `progress=end`. This is the
robust way to report progress.

## Why the command builder is pure

`FfmpegCommandBuilder` takes data in and returns an argument list — no process execution,
no I/O. That makes the most error-prone part of the system (getting the FFmpeg invocation
right) trivially unit-testable: assert on the produced args without ever running FFmpeg.
`FfmpegRunner` is the only place that touches the process.

## Idempotency and retries

Make the worker's `ProcessAsync` idempotent (deterministic output keys per job id) and let
the queue redeliver on transient failure rather than building bespoke retry logic. A job
that runs twice should overwrite the same output location and reach the same end state.

## HLS first, DASH later

The MVP emits HLS only: a master playlist referencing one variant playlist per rendition,
each with its own segment files. DASH can be added later as an additional output format
without changing the job model.
