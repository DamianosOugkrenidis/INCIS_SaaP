# Out-of-process load/stability smoke

Date: 2026-11-11T15:24:19
App: http://127.0.0.1:63196 (Kestrel, isolated DB INCIS_SaaP_Load_54a0820f)

| Metric | Value |
|---|---|
| Sequential sample | 50 requests to /health |
| Latency p50 | 27 ms |
| Latency p95 | 39.1 ms |
| Sample failures | 0 |
| Parallel requests | 200 @ concurrency 20 |
| Successful | 200 / 200 |
| Throughput | 271.5 req/s |
| Process CPU (during) | 2.56 s |
| WorkingSet BEFORE | 225.3 MB |
| WorkingSet AFTER | 244.8 MB |

Note: single-node local smoke (includes PowerShell/Invoke-WebRequest overhead); not a substitute for sustained staging soak.
