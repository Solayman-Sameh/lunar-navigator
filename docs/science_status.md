# Scientific engine status

| Time | Step | Status | Note |
| --- | --- | --- | --- |
| 2026-11-15, run T001-20261115T170917Z (UTC run identity) | T001 environment | FAIL | Isolated Python 3.11.15 x64 and pip 24.0 available; core wheels unavailable through network/offline cache. Five imports and NumPy smoke failed; pytest could not launch. Both probes compile. No NASA request. See `backend/data/runs/T001-20261115T170917Z/report.md`. |
| 2026-11-15, same run | T002 inspection only | FAIL (BLOCKED) | Both original contracts identical/unamended. Unity endpoint rule still permits a+m==n; strict alignment/half-open sector rules also need coordination. Applied profile and consumer receipt absent. See `backend/data/runs/T001-20261115T170917Z/T002-readiness.md`. T003–T006 not ready. |

| 2026-11-15, horizons-proof-20261115 | T001 recovered | PASS | Eight imports and all 14 environment checks pass; exact lock captured; no reinstall. |
| 2026-11-15, same run | T004/T005 diagnostic implementation | PASS | 60 synthetic tests; CLI actually executed. Production site/contract gates remain. |
| 2026-11-15, same run | T003/T006 | FAIL / incomplete | No verified landing point; both local HTTP transports refused. Browser Sun observation is separate from retained proof. No real paired CSV/plot or JSON publication. See `backend/data/runs/horizons-proof-20261115/report.md`. |

| 2026-11-15, acquisition-recovery-20261115T180732Z | T006 recovery | BLOCKED / MANUAL HANDOFF | Cause established: configured proxy 127.0.0.1:9 refuses connection. Stopped after one request/body. Runnable downloader, exact browser URLs and no-network importer prepared. Five new checks pass; baseline 60 not repeated. See `backend/MANUAL_ACQUISITION.md`. |

| 2026-11-15, local-horizons-20261115T182625Z-4a08a2a5 | T006 diagnostic proof | PASS | Genuine retained Sun/Earth data acquired locally by user; 73 samples each, three real-data checks passed, plot inspected. Production point, ten-minute/month baseline and contract gates remain. |

| 2026-11-15, visibility-20261115 | Geometric visibility/mission engine | PASS (diagnostic milestone) | 98 tests pass; real missions: f_sun=1, f_earth=f_both=0, dark_max=0, Earth gap=24/72 h. GEOMETRIC_ONLY, unverified point, hourly piecewise-constant cells. T008 complete; T009 month-baseline acceptance open. No API/server/publication. |

| 2026-11-15, visibility-review-20261115 | Existing visibility milestone reverified | PASS | No engine rewrite or NASA acquisition. 98 tests pass. Original GEOMETRIC_ONLY outputs reused; explicit binary timeline added. T007/month baseline, full T009 acceptance and T010-T014 remain pending. No HTTP server, Unity or contract edits. |

| 2026-11-15, api-20261115 | Dynamic ONLINE API | IMPLEMENTED BUT AWAITING WINDOWS LIVE TEST | GET /health, POST /analyze, 128 tests PASS; real Uvicorn health PASS. No actual February NASA acquisition claimed. Run backend/live_api_proof.py outside Codex for acceptance. |

| 2026-11-15, finder-20261115 | Verified catalogue + dynamic Finder | IMPLEMENTED/TESTED; Finder live proof pending | T029 real analyze proof PASS; 3 cited reference centers; 156 tests PASS. New Windows proof script provided; no sandbox NASA retries, Unity edits or terrain claims. |

| 2026-11-15, release-20261115 | FINAL GEOMETRIC_ONLY backend | READY FOR FINAL UNITY INTEGRATION | T029/T032 actual NASA API proofs PASS, 156 tests PASS. HTTP contract frozen with real fixtures. Unity MUST synchronize coordinates from GET /sites. No backend P0 verification blocker. |
