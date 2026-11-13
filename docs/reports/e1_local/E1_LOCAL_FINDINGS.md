# E1 LOCAL ENVIRONMENT FINDINGS
Date: 2026-11-11T22:25:12
Host: LAPTOP-2K6OPG6H (LOCAL - not production)

| # | Category | Status | Method | Finding | Evidence |
|---|---|---|---|---|---|
| 1 | 0. Environment | CHECKED (LOCAL) | hostname/os | Local workstation inspected; not production host. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\00_environment.txt |
| 2 | 1. API Gateway / Reverse Proxy / WAF | CHECKED (LOCAL - no INCIS API evidence) | filesystem | IIS log dir present; no INCIS API host configured locally. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\01_iis_logs.txt |
| 3 | 1b. nginx | NOT AVAILABLE | filesystem/Get-Command | nginx not installed / no logs locally. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\02_nginx_logs.txt |
| 4 | 2. Firewall / egress logs | CHECKED (LOCAL - logging config only) | Get-NetFirewallProfile | Firewall logging config recorded; no INCIS egress log analysis possible locally. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\03_firewall.txt |
| 5 | 4. Scheduled tasks | CHECKED (LOCAL) | Get-ScheduledTask | Matched=1 tasks referencing API/curl/python/node. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\04_scheduled_tasks.txt |
| 6 | 3. Cloud activity / audit logs | REQUIRES ENVIRONMENT ACCESS | Get-Command/config dirs | Cloud CLIs/config not available or unauthenticated; production cloud audit logs require authorized access. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\05_cloud_cli.txt |
| 7 | 6. Service/API credentials inventory | CHECKED (LOCAL) | config/env names | Local config/env credential names recorded (values masked). | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\06_credentials_inventory.txt |
| 8 | 7. Webhook subscribers (OUTBOUND, separate) | CHECKED (LOCAL dev DB) | sqlcmd counts | Local dev DB only: 0 webhook subscriptions. NOT production. Webhook subscriber != inbound consumer. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\07_webhooks_local.txt |
| 9 | 8. DNS / certificates / hostnames | CHECKED (LOCAL) | hosts/cert store | Local hosts/certs inspected; production DNS/TLS requires environment access. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\08_hosts_certs.txt |
| 10 | 2b. Network sockets | CHECKED (LOCAL) | Get-NetTCPConnection | Snapshot only; not an API consumer audit. | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e1_local\09_network.txt |
| 11 | 10. App Phase 0 logs (cross-check) | CHECKED (LOCAL - not production) | analyze-phase0-logs.ps1 | Local logs: 0 API-write/CSRF lines, completeness=false (see E2b dry-run). | C:\Users\daoug\Desktop\ΠΡΟΣ_ΠΑΡΑΔΟΣΗ_ΙΩΑΝΝΟΥ\INCIS_FINAL\INCIS_SaaP_v5\docs\reports\e2b_local_dryrun\phase0_report.md |

## Enabled/Disabled checks
- Enabled: filesystem logs (IIS/nginx), firewall config, scheduled tasks, cloud CLI presence, local config/env names, local dev DB counts, hosts/certs, sockets, local Phase 0 logs.
- Requires authorized third-party/production access: production gateway/WAF logs, production firewall/egress, cloud audit logs, integration registry/CMDB, production credential inventory, production webhook subscribers, production DNS/TLS, team interviews.

## Conclusion
- Local checks CANNOT establish "no external consumers" for production. Option A remains undocumented until production-wide checks are executed by authorized personnel.
- Webhook subscribers are OUTBOUND and are recorded separately from inbound consumers.
- This evidence package is READ-ONLY and masked.
