# Scientific validation evidence

| ID | Check | Status | Evidence file |
| --- | --- | --- | --- |
| T001-20261115T170917Z/environment | 64-bit isolated CPython 3.11.15, paths, backend write access, Python binary64 and both probe syntax checks | PASS | `backend/data/runs/T001-20261115T170917Z/environment.json` |
| T001-20261115T170917Z/core | Wheel-only dependency install, five imports and NumPy float64 | FAIL | `backend/data/runs/T001-20261115T170917Z/install.log`; `environment.json` |
| T001-20261115T170917Z/pytest | Invoked pytest; module missing, no tests ran | FAIL | `backend/data/runs/T001-20261115T170917Z/pytest.log` |
| T001-20261115T170917Z/science | NASA/terrain/engine/consumer execution | NOT RUN | `backend/data/runs/T001-20261115T170917Z/report.md` |
| T002-20261115T170917Z/readiness | Contract copies equal but approved wording unapplied; consumer endpoint/alignment/sector differences and missing acknowledgement | FAIL (BLOCKED) | `backend/data/runs/T001-20261115T170917Z/contract-receipt.json`; `T002-readiness.md` |

| horizons-proof-20261115/T001 | Existing 3.11.15 x64 venv: eight imports, binary64, writes and both probe syntax checks; 14 checks | PASS | `backend/data/runs/horizons-proof-20261115/environment.json` |
| horizons-proof-20261115/T003 | Production point/frame/height verification | FAIL / incomplete | `docs/site_coordinate_check.md` |
| horizons-proof-20261115/T004-T005 | Synthetic site/parser/cache/time acceptance; 60 tests, no skips | PASS (implementation only) | `backend/data/runs/horizons-proof-20261115/tests.xml`; `test-summary.json` |
| horizons-proof-20261115/T006 | Actual Requests and urllib Sun/Earth acquisition, local proxy connection refused before HTTP status | FAIL | `backend/data/runs/horizons-proof-20261115/acquisition.log`; `cli/summary.json` |
| horizons-proof-20261115/source-observation | Browser displayed genuine 73-row Sun response; raw bytes not retained; Earth not observed | OBSERVED, source proof NOT PASSED | `backend/data/runs/horizons-proof-20261115/browser-observation.json` |
| horizons-proof-20261115/real-validation | Retained real pair/cadence/finite angles/plot inspection | NOT RUN | No paired source bytes/CSV/plot available |

| acquisition-recovery-20261115T180732Z/HTTP | One GET per Sun/Earth; configured loopback proxy connection refused before HTTP/TLS | FAIL | `backend/data/runs/acquisition-recovery-20261115T180732Z/direct-attempt.json`; `diagnosis.json` |
| acquisition-recovery-20261115T180732Z/handoff | Downloader/importer: encoding, rerun preservation, rejected-source retention, no-network import | PASS (5 new synthetic integration tests) | `backend/data/runs/acquisition-recovery-20261115T180732Z/handoff-tests.xml` |
| acquisition-recovery-20261115T180732Z/real-source | Retained NASA pair / scientific CSV / real plot | NOT RUN; no original bodies received | `backend/data/runs/acquisition-recovery-20261115T180732Z/T006-evidence.json` |

| local-horizons-20261115T182625Z-4a08a2a5/real-proof | Actual HTTP 200 both targets; original hashes; 73 paired hourly rows; exact CSV replay; plot visually inspected; 3 real-data tests pass, zero skips | PASS (diagnostic proof) | `backend/data/runs/local-horizons-20261115T182625Z-4a08a2a5/T006-review-evidence.json` |

| visibility-20261115/T008 | Hand-worked oracle, strict horizon/dip/wrap, missing samples, exact half-open cells, outages/events/precision; full existing/new suite | PASS: 98 tests, zero skips | `backend/data/runs/visibility-20261115/tests.xml` |
| visibility-20261115/T009-diagnostic | Retained real 24/72 h missions, second-date comparison, 73-hour rejection; CSV/JSON hashes and 73 instants/72 cells audited; plot inspected | PASS (diagnostic scope) | `backend/data/runs/visibility-20261115/T009-evidence.json` |

| visibility-review-20261115 | Existing engine reused; original NASA provenance and saved artifacts checked; recomputation exactly matches 24/72 h JSON; 73-hour coverage rejects; CSV replay; elevation and binary timeline plots inspected | PASS; 98 tests, zero failures/skips | `backend/data/runs/visibility-review-20261115/verification.json`; `tests.xml` |

| api-20261115/T028 | Dynamic API validation, request identities, cache integrity, HTTP errors, JSON and December/February mocked source routing; full regression | PASS: 128 tests, zero failures/skips | `backend/data/runs/api-20261115/tests-all.xml`; `API-evidence.json` |
| api-20261115/Uvicorn | Actual local production Uvicorn process and GET /health, no NASA call or mocks | PASS | `backend/data/runs/api-live-20261115T185345Z-a60094bd/comparison.json` |
| api-20261115/T029 | Actual December/February API -> NASA dynamic proof | NOT RUN: awaiting Windows handoff | `backend/live_api_proof.py`; `backend/docs/API_CONTRACT.md` |

| finder-20261115 | Real T029 proof replay; source-cited catalogue and Finder model/HTTP tests including bounds, policies, constraints, caches and analyze regressions | PASS: 156 tests, zero failures/skips; Finder LIVE pending | `backend/data/runs/finder-20261115/Finder-evidence.json` |

| release-20261115/T032 | Real Windows Finder proof independently replayed: six raw NASA envelopes, all 24 full missions/filters, correct top three; real cached empty search; runtime free of mock imports | PASS; full suite 156 passed, no failures/skips | `backend/data/runs/release-20261115/T032-evidence.json` |
