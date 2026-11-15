# Approved-design Unity exchange profile

**Status**: technical design approved by the user's Planning request; shared files deliberately unchanged. The implementation-entry gate is application/recording of the coordinated wording in [data-contract-amendments.md](../data-contract-amendments.md). This document is not a replacement authoritative contract.

## Wire shape and complete-only policy

Keep schema_version=1, existing filenames/site IDs and typed root/object/array structures. Numeric time series remain parallel primitives; named object arrays are allowed as in current examples. No arbitrary maps, root arrays, arrays of arrays, partial known masks, schema_version=2, new status enum or hidden extension fields. All scientific numbers preserve binary64 round trips; display formatting only is rounded. Reject NaN, Infinity, duplicate keys, non-object roots and unsupported types/version.

| File / object | Invariants |
| --- | --- |
| index.json | Lists only existing independently validated complete accepted sites; opportunities_file empty until an existing validated compatible cohort file is ready. Early one-site index permitted, final baseline all three. No reference escapes the selected set directory. |
| site.id/coordinates/height/provenance | Exact verified point and observer model; latitude north-positive, longitude east-positive; zero asset ground is not proof. Source/frame/height assumptions link to science evidence. |
| ephemeris | Source identity, UTC t0, positive step_s, n instants, four finite degree arrays length n; baseline n=4465. Actual source stop=t0+(n−1)*step_s. |
| terrain | Exact source or explicitly identified spherical fallback, actual nominal resolution/range, az_step_deg=1, finite 360 horizon values and half-open/split incomplete sectors. No relevant unknown skyline support used by exported Sun/Earth samples. |
| visibility | n integer 0/1 flags each, all scientifically known, solar margin explicit. Equality hidden under strict >; last row describes an instant only. |
| reference_metrics | Nine fully known baseline windows/site, starts Dec 1/8/15 at 00:00 UTC and 24/72/168 h. Require a+m<=n−1; no partial/bounded scalar metrics. |
| opportunities | Existing request/constraints, explicit accepted complete candidate_sites, score description, best/earliest up to 3, known closest misses up to 5, evaluated/rejected counts. Empty valid lists allowed. |

An incomplete scientific result is retained locally as INCOMPLETE evidence and is not certified/published as a terrain-aware site. Relevant means required by any exported Sun/Earth sample's interpolation support. A warning about an unused sector is not a statement of full terrain coverage, and an absent essential ephemeris sample invalidates the dataset. Whole-scenario GEOMETRIC_ONLY is separately selected and never called terrain validation.

## Explicit excluded-cohort accounting without partial interchange

The latest approval permits fewer/zero recommendations and reporting exclusions. For v1, keep original requested IDs, excluded scenarios/reasons and audit counts in the science-owned run/status report. Only an explicitly accepted complete cohort is put in wire candidate_sites, and its counts describe that cohort. Print/write/report exclusions; never claim the reduced cohort is exhaustive over omitted sites. `--allow-exclusions` makes this transition explicit; strict `--all` otherwise fails.

If every requested site is unevaluable, retain a report with zero recommendations; do not produce an index/opportunities file that appears to certify an empty scientific campaign. If complete accepted sites exist but no scenario meets thresholds, publish their valid site files and an opportunities file with empty qualifier lists. Unknown candidates are not placed in closest_misses as if an observed constraint failed.

The baseline's 168-count test applies when all three are verified/eligible and the full request is evaluable. An incremental one-site request has 56 arrivals. Both are legitimate explicitly described workloads; neither pads results or hides exclusions.

## Numerical parity required of the Unity owner

| Quantity | Scientific / cross-engine rule |
| --- | --- |
| Parsing/math | Scientific arrays, coordinates, heights, margins, metrics and scores use C# double where needed; float conversion only after science for rendering. Unknown fields cannot substitute for agreed schema semantics. |
| Instants/cells | 4465 rows versus 4464 cells; t_stop adds no extra ten minutes; `[landing,landing+D)`, a+m<=n−1; all interface times UTC. |
| Skyline | A modulo 360, k=floor(A), w=A−k, `(1−w)*H[k]+w*H[(k+1)%360]`; degrees/radians explicit and clockwise-from-north basis. |
| Flags | Strict clearance>0; equality hidden. Exact agreement outside abs(clearance)<=1e-10 degrees. Within that arithmetic band enumerate/audit differences, use authoritative stored flags for common metrics and retain explicit resolution evidence. No ignored mismatch PASS. |
| Metrics/score | Integer sums/runs exact; fractions and scores abs tolerance 1e-12, hours 1e-9; interpolation 1e-10 degrees. These are arithmetic budgets, not physical accuracy. |
| Ranking | Exported authoritative order; recompute qualification/score from unrounded values. Best is score then UTC/ID; earliest is UTC first then score/ID at that timestamp. |
| Display | Three-decimal presentation, ties-to-even, negative zero normalized, rounded azimuth 360 displayed 0. Presentation values never drive recomputation/filtering. |

Handoff parity vectors: exact horizon equality, 359/0 interpolation, an arithmetic boundary clearance, six cells/hour, ten hand-worked cells, last fitting mission/one-cell overflow, 1/3 fraction, high precision JSON values, tied scores, earliest-versus-best, zero/fewer-than-three, NaN/Infinity/duplicate-key rejection and relevant unknown-sector exclusion. Supply expected values/evidence outside production scientific files; synthetic vectors remain marked synthetic.

## Publication and acknowledgement gates

Before implementation, owner-controlled contract text/profile must be updated or an explicit owner-applied version/profile receipt must be available, and Unity owner acknowledges the double/cell/complete-subset rules. Notification by the user is already authorized and underway; this agent does not message/edit the frontend.

Before publish: retain exact applied contract/profile hash, acknowledged consumer version, source/evidence set, staged file hashes and independent validator report. Resolve ownership of fixed scientific filenames, including an existing index another process created; never overwrite stubs or .meta. Establish a no-reload/no-startup consumer barrier during the multi-file commit. Fixed filenames permit atomic replacement per file only; index-last plus backups/rollback under this barrier prevents advertising an unvalidated set. If no barrier is available, leave artifacts staged and report ready-for-handoff. After commit verify hashes, release barrier, request one deliberate consumer reload through the user and record actual parity outcomes.

No Unity code, scene, package, settings or shared contract file is modified during planning. Future consumer implementation/parity evidence is NOT RUN at this stage.
