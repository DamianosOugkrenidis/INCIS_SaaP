# Out-of-process load/stability smoke

Date: 2026-11-11T19:00:04
App: http://127.0.0.1:63904 (Kestrel, isolated DB INCIS_SaaP_Load_61f03010)

| Metric | Value |
|---|---|
| Sequential sample | 20 requests to /health |
| Latency p50 | 19.3 ms |
| Latency p95 | 29.2 ms |
| Sample failures | 0 |
| Parallel requests | 17600 @ concurrency 20 (duration 60s) |
| Successful | 17600 / 17600 |
| Throughput | 292 req/s |
| Process CPU (during) | 69.95 s |
| WorkingSet BEFORE | 227.1 MB |
| WorkingSet AFTER | 223.2 MB |

Note: single-node local smoke (includes PowerShell/Invoke-WebRequest overhead); not a substitute for sustained staging soak.
