# StreamForge — C4 model

This document describes the architecture of StreamForge using the
[C4 model](https://c4model.com/) (Context, Containers, Components, Code). For an MVP
the Context and Container levels carry the most value; a Component sketch for the
worker is included because that is where the real complexity lives.

The diagrams below use Mermaid, which GitHub renders natively. Static SVG copies live
alongside this file in `docs/diagrams/`.

---

## Level 1 — System context

StreamForge is a single system that turns submitted source video into adaptive HLS
output. A client application submits and polls jobs; the system reads from and writes
to cloud object storage; viewers stream the output via a CDN.

```mermaid
flowchart TD
    client["Client application<br/><i>Submits + polls jobs</i>"]
    sf["StreamForge<br/><i>Encoding orchestration</i>"]
    storage["Object storage<br/><i>S3 / Azure Blob</i>"]
    cdn["CDN + viewers<br/><i>Stream HLS output</i>"]

    client -->|REST / HTTPS| sf
    sf -->|read / write| storage
    sf -->|publishes| cdn
```

---

## Level 2 — Containers

Opening the system box reveals four deployable units plus the shared library. The API
accepts jobs and publishes messages; the queue decouples submission from processing; the
worker runs FFmpeg; PostgreSQL holds job state. All reference `StreamForge.Core`.

```mermaid
flowchart TD
    subgraph sf["StreamForge system"]
        api["API<br/><i>ASP.NET Core</i>"]
        queue["Queue<br/><i>RabbitMQ</i>"]
        worker["Worker<br/><i>Worker service + FFmpeg</i>"]
        db["Job state<br/><i>PostgreSQL</i>"]
        core["StreamForge.Core<br/><i>Shared NuGet library</i>"]
    end
    storage["Object storage<br/><i>External</i>"]

    api -->|publish| queue
    queue -->|consume| worker
    worker -->|status| db
    api -->|read| db
    worker -->|read / write| storage
    api -.references.-> core
    worker -.references.-> core
```

---

## Level 3 — Worker components

The worker is the only container with non-trivial internal structure. A queue consumer
hands each message to a job processor, which orchestrates the storage client, the FFmpeg
command builder, the FFmpeg runner, and the progress parser.

```mermaid
flowchart TD
    consumer["Queue consumer<br/><i>Receives EncodeJobMessage</i>"]
    processor["Job processor<br/><i>EncodeWorker.ProcessAsync</i>"]
    runner["FFmpeg runner<br/><i>Spawns + monitors process</i>"]
    builder["Command builder<br/><i>JobRequest to args</i>"]
    parser["Progress parser<br/><i>Reads -progress pipe</i>"]
    storageClient["Storage client<br/><i>IObjectStorage</i>"]

    consumer --> processor
    processor --> storageClient
    processor --> runner
    runner --> builder
    runner --> parser
```

---

## Job lifecycle sequence

The time-ordered interaction from submit to completion, including the client's status
polling loop running concurrently with encoding.

```mermaid
sequenceDiagram
    participant C as Client
    participant A as API
    participant Q as Queue
    participant W as Worker

    C->>A: POST /api/jobs
    A->>Q: publish message
    A-->>C: 202 + job id
    Q->>W: consume
    activate W
    Note over W: download source,<br/>run FFmpeg, update progress
    C->>A: GET /api/jobs/{id}
    A-->>C: status + progress
    W-->>A: mark complete + manifest url
    deactivate W
```

---

## Notes on the model

- The Context and Container levels are the durable documentation; keep them in sync with
  reality. The Component level is illustrative and will drift fastest — treat it as a
  sketch, not a contract.
- `StreamForge.Core` deliberately has no dependency on the API, worker, queue, or any
  cloud SDK. That keeps the encoding logic unit-testable and reusable.
- Object storage and the CDN are external systems in the C4 sense: StreamForge depends on
  them but does not own them.
