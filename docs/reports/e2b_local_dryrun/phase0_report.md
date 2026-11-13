# Phase 0 Log Analysis Report

Requested window: last 14 days (cutoff 2026-10-28T22:07:55)
Log window (parsed): (no parseable timestamps)
Days covered by data: 0 / 14 (complete=False)

## Data quality
| Metric | Value |
|---|---|
| Files | 9 |
| API-write/CSRF lines | 0 |
| Parsed rows (in window) | 0 |
| Malformed (unparsable) | 0 |
| Duplicates removed | 0 |
| Out of window | 0 |

## Summary
| Category | Count |
|---|---|
| ALLOW (authorized) | 0 |
| PHASE-0 OBSERVATION (would be denied) | 0 |
| DENY (enforced) | 0 |

## By endpoint / method / decision
| Category | Method | Path | Decision | Count |
|---|---|---|---|---|

## Decision criteria
- Cleared for H-2/CSRF Phase 1: ZERO PHASE-0 OBSERVATION from roles outside Admin/SuperUser for the full window AND written PO confirmation.
- Any OBSERVATION => identify client/role before enabling.
- If completeness=false (data do not span 14 days) => EXTEND observation; do not conclude approval.
- ABSENCE of calls does NOT prove absence of external consumers.

## Privacy / limitations
- Identities are FULLY redacted by default (user-<hash>) - no emails exported. Use -ShowMaskedEmails only if explicitly approved.
- Token-like values masked.
- Duplicates/malformed lines are reported, not silently dropped.
- Analysis is limited to the provided logs (no server-side correlation).
