# Python Scientific Engine — Specify Record

**Date**: 2026-11-15
**Authoritative root**: `C:\Users\user\Documents\Unity\LunarNavigator`
**Feature directory**: `specs/002-python-science-engine`
**Specification**: [spec.md](spec.md)
**Quality review**: [checklists/requirements.md](checklists/requirements.md)
**Stage**: Historical Specify/Clarify evidence below. Latest: technical planning completed 2026-11-15, G/N/U explicitly approved, constitution v1.1.0 applied after verification. See [plan.md](plan.md), [ratification evidence](governance-ratification.md) and [current contract coordination](data-contract-amendments.md). No Python implementation.

## Environment verification before feature creation

| Check | Observed result | Evidence |
| --- | --- | --- |
| Working directory | PASS — exact authoritative root | PowerShell `Get-Location`. |
| Write access | PASS — one byte written in backend; temporary probe removed on close | Successful FileStream create/write/dispose and absence check. Initial unsupported overload attempt did not establish access; successful corrected probe is the evidence. |
| Unity project | PASS — Assets, Packages, ProjectSettings are directories | Read-only `Get-Item`. |
| Local Spec Kit | PASS — project-local Specify and all eleven listed Spec Kit skill directories accessible | `.agents/skills/speckit-specify/SKILL.md` read in full; `.specify/` templates/scripts/configuration inspected. |
| Feature numbering | PASS — sequential; existing feature 001 only when selected | `.specify/init-options.json` and `specs/` inventory; new 002 created without overwriting 001. |
| Template resolution | PASS — core spec template selected by installed resolver | `.specify/scripts/powershell/common.ps1` `Resolve-Template`; template copied as starting point, then populated. No override/preset/extension template selected. |
| Extension hooks | None configured | `.specify/extensions.yml` absent before generation; rechecked after generation. No pre/post hooks to dispatch. |
| Git | NOT AVAILABLE at this root | `git status` and `git branch --show-current` report not a repository. No git initialization, branch switch, stash, reset, or commit attempted. |
| Search utility | `rg` unavailable | Used PowerShell file discovery/content reads instead. |

No source data retrieval, scientific runtime setup, probe execution, DEM processing, scientific export, Unity command, or implementation test was performed. Reading official reference pages did not acquire or validate a scientific dataset.

## Sources read and precedence

| Source | Role / findings |
| --- | --- |
| `backend/docs/1_PYTHON_SCIENCE_PLAN.md` | Main requirements source; S1–S11, fixed three-site baseline, core/Finder priorities, fallback behavior, tests, evidence, two-stage release. This Specify document does not replace the plan. |
| `docs/DATA_CONTRACT.md` | Intended shared integration entry point; user controlled and immutable in this stage. |
| `backend/docs/0_DATA_CONTRACT.md` | Preserved migrated copy; byte-identical to root docs contract at inspection. |
| `backend/docs/2_UNITY_PLAN.md` | Frontend expectations and ownership context, read only. |
| `backend/docs/LunarNavigator_Comprehensive_Project_Proposal.md` | Product rationale, Inspector/Finder distinction, scientific limits. Optional service/ML/global coverage and extra quality tier not imported. |
| `backend/MIGRATION_REPORT.md` | Migration copied plans into backend/docs and preserved target constitution/Spec Kit. Describes but does not contain the original v1.1 draft. |
| `.specify/memory/constitution.md` | Ratified v1.0.0, principles I–X, additive amendment governance. Read, not amended. |
| `specs/001-lunar-explorer/spec.md` | Existing independent Unity feature inspected; entire feature preserved. |
| `docs/unity_architecture_notes.md`, `docs/unity_status.md`, `docs/unity_site_coords.md` | Current frontend context. Report describes fictional candidate assets and real-data integration expectations. |
| `Assets/LunarNavigator/Generated/Site_1.asset`, `Site_2.asset`, `Site_3.asset` | YAML inspected read only; explicitly fictional demo coordinates/IDs. Not scientific coordinate authority. |

Both contract copies have SHA-256:

`6565A6DBDA2A03E86E71779AC9A1F48EB6CE233AA9E034EFF9A04E9B62FA263B`

Their equality resolves the current content-location conflict, not their contradictory wording. CR-01 through CR-08 are proposals in the new spec, not additions to either contract's Change requests section. The user's instruction to preserve both copies takes precedence over the contract's normal instruction to append proposals there.

## Ownership boundaries

| Paths | Owner / permitted behavior |
| --- | --- |
| `backend/**`; science method, validation science rows, sources/credits, images and science status docs | Python workstream per science plan/contract; no implementation changes during this stage. |
| `Assets/StreamingAssets/analysis/*.json` except `stub_*.json` | Contract assigns real science output to Python, but the user explicitly forbids changing scientific JSON owned by another process. Future work stages first and verifies destination ownership. Zero JSON writes here. |
| `Assets/StreamingAssets/analysis/stub_*.json` | Unity owned; no edits. |
| Other `Assets/**`, all `.meta`, `Packages/**`, `ProjectSettings/**`, `Builds/**`, `docs/unity_*.md` | Unity owned; read only where necessary, never modified. |
| `README.md`, both shared contract copies | User controlled/preserved; propose changes only in this feature. |
| `specs/001-lunar-explorer/**` | Existing Unity feature; no edits. |
| `.specify/**`, `.agents/**` | Shared configuration/governance preserved, including Unity active-feature selection. |
| `specs/002-python-science-engine/**` | New science feature authorized by this request; sole deliverable write scope. |

Actual Unity site comparison at inspection:

| Asset | Actual ID/name | Latitude | East longitude | Scientific interpretation |
| --- | --- | --- | --- | --- |
| Site_1.asset | demo-1 / Aster Ridge | -84.2° | -30° | Fictional; no automatic match to Malapert. |
| Site_2.asset | demo-2 / Selene Basin | -86.3° | 35° | Fictional; no automatic match to Shackleton rim. |
| Site_3.asset | demo-3 / Umbra Rise | -82.8° | 110° | Fictional; no automatic match to Connecting Ridge. |

Approximate real-site values printed in Unity notes are not adopted as verified science points. Verification remains future work.

## Scientific governance assessment

Constitution v1.0.0 already requires honest demo labels, provenance, deterministic responsibilities, and functional evidence. It does not fully state the scientific plan's source-only numerical data, verified site/frame/datum/time conventions, missing-terrain policy, reproducibility, uncertainty, independent validation, or strict cross-agent data ownership requirements.

Those principles must be incorporated before scientific implementation through an approved additive amendment or an explicitly approved equivalent governance route. The recommended route is a minor additive amendment that preserves Unity I–X and its delivery constraints; this record is not that amendment. The exact unratified XI–XXV text is absent from the approved workspace, so neither its wording nor its compatibility is asserted. The migration report's summary is the available evidence. Do not read the old external source workspace or silently reconstruct/ratify the draft.

Scientific requirements are captured in this spec now so clarification can review them. Existing Unity governance is unchanged; the science feature's stage stop is obeyed despite Unity's separate delivery mandate.

## Workflow isolation and next-stage targeting

The stock Specify skill normally persists its new target into `.specify/feature.json`. Here that file currently selects `specs/001-lunar-explorer`, and another agent is actively working in the shared tree. To honor the user's independence/ownership instruction, it was deliberately left unchanged. This is a documented shared-state exception to the skill's default persistence step, not an undisclosed retargeting of Unity.

The science target for the next reviewed invocation is:

`SPECIFY_FEATURE_DIRECTORY=specs/002-python-science-engine`

Set the selector only in the scientific command's process/session. Installed path helpers support `Get-FeaturePathsEnv -NoPersist` for resolving this explicit target without rewriting the root pointer. Any future stage that normally persists selection must preserve this isolation; do not run it against the implicit Unity selection. No project skill/helper was modified to achieve this. Do not create a branch in the shared working tree.

Only Specify was executed. No `$speckit-clarify` interview, plan, tasks, analysis, constitution amendment, or implementation was started. The next step requires the user's separate stage instruction.

## Reference-page checks during specification

- [JPL Horizons manual](https://ssd.jpl.nasa.gov/horizons/manual.html): inspected as official scientific reference; actual request settings, observer datum/time semantics, and successful numeric responses still require implementation-stage verification.
- [NASA PGDA product 90](https://pgda.gsfc.nasa.gov/products/90): inspected page describes a 87–90°S, 5 m/pixel mosaic in the MOON_ME/DE421 frame, with a 3.3 GB elevation GeoTIFF. Therefore the plan's assumed all-site regional coverage and smaller/coarser-product availability cannot be accepted without actual selection/metadata checks, especially for Malapert. This is an inference from the documented footprint, not an acquired DEM result.
- [NASA PGDA product 78](https://pgda.gsfc.nasa.gov/products/78): inspected page identifies site products including Connecting Ridge, Shackleton rim, and Malapert massif, with uncertainty/count information and interpolation limitations. No exact observing coordinate or terrain product was selected/downloaded.
- The reference-sphere dip was calculated analytically during document review: `-acos(1737400/(1737400+2))*180/pi = -0.08693657193373108°`. This verifies the formula correction only, not a DEM skyline or test-suite pass.

## Review and handoff

Specification quality review passes with provisional policies explicitly stated; unresolved policy approval is not represented as complete. The three grouped items for clarification are governance, exchange/numerical semantics, and quality/search policy. Eight detailed correction proposals are in the spec. Scientific checks remain NOT RUN.

Final checks after document generation:

| Verification | Result |
| --- | --- |
| Four stories, FR-001 through FR-042 exactly once, nine outcomes, required template section order | PASS |
| Unpopulated template/clarification placeholders | None |
| Relative Markdown document links and readable files without UTF-8 BOM | PASS |
| Both contract hashes versus pre-generation baseline | Unchanged and equal |
| Constitution v1.0.0 hash versus pre-generation baseline | Unchanged |
| All ten existing Unity feature files versus pre-generation hashes | Unchanged |
| Shared `.specify/feature.json` hash/selection | Unchanged; still `specs/001-lunar-explorer` |
| Explicit science target resolution with `Get-FeaturePathsEnv -NoPersist` | Correctly resolves this feature without changing shared selection |
| Post-Specify hook configuration | No extensions.yml; no hooks to dispatch |
| Generated deliverable files | Only spec.md, checklists/requirements.md, workflow-record.md in the new feature |
| Scientific implementation/tests/data production | NOT RUN; outside this stage |

No implementation-readiness claim should be inferred from readiness for clarification.

## Clarify session — 2026-11-15

Read the installed local `.agents/skills/speckit-clarify/SKILL.md` in full and executed its target-resolution prerequisite once with `-Json -PathsOnly`. A process-local explicit science selector resolved FEATURE_DIR and FEATURE_SPEC to this feature. The script's PathsOnly mode calls `Get-FeaturePathsEnv -NoPersist`; the shared pointer still selects Unity. No helper/skill/configuration/branch changes were required. No extensions.yml or pre/post clarification hooks exist.

Actual current files read: spec.md, checklists/requirements.md (there is no feature-root requirements.md), this workflow record, the complete migrated Python science plan, docs/DATA_CONTRACT.md, constitution v1.0.0, migration report, and official references. The [clarification decision register](clarifications.md) contains all 30 resolved ambiguities and taxonomy coverage. Five grouped source-derived resolutions were integrated into the specification; there was no fabricated owner answer or inferred approval. Interactive questions asked/answered: 0/0, following the user's instruction to resolve determinate matters and provide concrete recommended proposals for approvals.

Updated spec sections: status/input, Clarifications/session, User Story 2 acceptance, Requirements (normative links and affected FR entries), SC-009, and Assumptions/correction/review items. Retained four user journeys, 42 FR IDs and nine SC IDs. The numerical/uncertainty decisions elaborate the existing plan, preserve pixel binning and its sequence, and do not execute it.

Prepared three review artifacts under proposals/: numbered unified constitution diff, governance rationale/impact review, and exact shared-contract replacement/addition text including optional version-2 uncertainty representation. None was applied. The proposed constitution candidate was checked in memory to preserve I–X and Unity delivery/workflow verbatim. The old unavailable XI–XXV draft was not accessed or reconstructed.

Current-source correction: PGDA /90 now lists broad-area products. Its earlier narrower description above is historical, not a current coverage restriction. Specific product acquisition, height datum, exact scientific observing coordinates and valid raster footprints remain implementation-stage verification obligations. Linked site README could not be retrieved through the available web reader; direct text fetch was also unavailable. No metadata fact was invented to fill this gap.

Quality checklist revalidation: **16/16 -> 16/16**. Newly passing: none. Regressions: none. Still unchecked: none. Per the installed workflow, unchanged checkbox markers and all checklist text/notes were preserved; its Specify-stage notes are historical. Deferred approvals remain explicitly outside a claim that shared contract/governance changes are already accepted.

Planning recommendation: decide G (scientific governance), N (exchange/numerical policy), and U (complete-only v1 versus version-gated partial interchange) before an unconditional `$speckit-plan` invocation. Mathematical behavior is defined; no broad re-interview is necessary. G ratification is required by FR-004 before implementation, not before clarification; N/U approval and consumer coordination gate shared publication. No plan, tasks, code, dependencies, source data, exports or runtime tests were created or run.

Final read-only current-asset check supersedes the historical Specify table above:

| Asset | Current ID | Observed candidate latitude / east longitude | Observed elevation | Verification status in this science stage |
| --- | --- | --- | --- | --- |
| Site_1.asset | shackleton_rim | -89.6 / 129 degrees | 0 m | NOT independently verified; generic source label is not exact-point/height evidence. |
| Site_2.asset | malapert_mountain | -85.9 / 2.9 degrees | 0 m | NOT independently verified; do not adopt zero as actual ground height. |
| Site_3.asset | connecting_ridge | -89.4 / -137.5 degrees | 0 m | NOT independently verified; stable ID alone does not certify this point. |

Only those values actually observed in the assets are transcribed here; no new science coordinates are selected. docs/unity_site_coords.md now reports aligned IDs. Science must still independently cite/verify the exact points and reconcile DEM/source datums before production. Neither those docs nor assets were edited by this stage.

Final protection audit: all 42 monitored shared configuration/skill/Unity-feature/contract/README files match this session's before-hashes; both contracts still match each other. The shared pointer remains `specs/001-lunar-explorer`. No hooks are configured. Document checks pass for 30 unique decisions, 42 unique FR IDs, nine SC IDs, four stories, 16/16 checklist markers, relative links, Markdown table columns and UTF-8 decoding. These are document/preservation checks, not scientific execution evidence.

## Plan session — 2026-11-15

User explicitly approved the scientific amendment subject to verification, full-precision science/JSON, and complete-only v1 with reported exclusions and honest fewer/zero winners. The user explicitly withheld shared-contract and Unity C# modifications during Plan. No approval was re-requested.

Read the installed project-local speckit-plan skill and setup helper. Its stock Get-FeaturePathsEnv call would persist feature.json even with an explicit environment selector. To satisfy the user's stronger shared-state isolation requirement, ran the installed helper's actual logic as a process-only script block with two narrow adjustments: resolve common.ps1 by its absolute original path, and add its existing supported `-NoPersist` argument to that path-resolution call. No installed helper/configuration file was edited. All remaining template resolution/copy/output behavior ran as installed. Parsed returned FEATURE_SPEC, IMPL_PLAN, SPECS_DIR and BRANCH, verified target science directory and unchanged pointer. BRANCH is a feature identifier, not evidence of creating a Git branch. Plan template copied into this feature and filled in required section order.

No extensions.yml exists; before/after plan hooks skipped as configured. The explicitly invoked skill requires dispatching research agents: two read-only research subtasks inspected Horizons/time/precision and terrain/coverage/tests, returning findings without file edits or science execution. The primary agent consolidated decisions, reopened official documentation and wrote research.md with decisions/rationales/alternatives and explicit future acquisition gates.

Inputs actually read include spec.md, clarifications.md, the actual amendment patch, original proposal text, migrated full science plan and shared contract, installed plan template/helper, constitution and existing probe. The requested feature-root data-contract-amendments.md did not exist initially; original exact text was under proposals/. Created the feature-root current approval/application record linking that text and specifying the latest complete-only accepted-cohort override. Neither shared contract copy was changed.

Governance verification/application: governance-ratification.md gives exact checks/hashes. Initial literal matching stopped without writing for CRLF/LF on the original final line; normalized line endings then matched all patch hunks/counts. I–X, Unity delivery/workflow and existing governance remained unchanged; only approved additive XI–XVII, scope and metadata were applied. Ratified constitution reloaded/checked for design gates. No other shared mutation is authorized or performed.

Phase 0 output: research.md. Phase 1 outputs: plan.md, data-model.md, algorithms.md, contracts/cli.md, contracts/unity-exchange.md, quickstart.md, test-matrix.md and current data-contract-amendments.md. Additional governance evidence records the applied approval. Updated spec/clarification metadata and affected requirements without changing the 42 FR/nine SC/30 D identities. Historical optional v2 is explicitly unselected, not implemented.

Constitution Check re-evaluated after design: no unjustified violation. Contract text/application and consumer acknowledgement are explicit first implementation prerequisites; they do not reopen approved numerical/quality decisions. Runtime metadata, verified exact site points, acquired DEM coverage/datum, actual Horizons headers/source responses and measured performance are future tests/gates, not unresolved design placeholders. No tasks.md or backend Python/dependency/data artifacts were created. Root README, Unity feature, active pointer and contract copies are preserved. Planning is ready for separately reviewed `$speckit-checklist` and `$speckit-tasks` stages.

Final Plan verification:

| Check | Result |
| --- | --- |
| Installed plan-template mandatory heading order | PASS; populated without unresolved template/design placeholders. |
| Required planning artifacts | Ten new design/governance files present, including Phase 0 research and Phase 1 data-model/contracts/quickstart. |
| Requirement identities and stories | 42 FR, nine SC, 30 D and four stories retained. |
| Document links, tables, fences, UTF-8 | PASS; all local links resolve and no encoding replacement/BOM errors. |
| Existing spec quality checklist | Still 16/16; no new checklist-stage execution claimed. |
| Existing Unity feature preservation | All ten existing feature files match their previously recorded hashes. |
| Both shared contract copies | Match their recorded original identical SHA-256; neither changed. |
| Active feature pointer | Matches recorded hash and still selects specs/001-lunar-explorer. |
| Ratified constitution | Matches recorded v1.1.0 after-hash; I–X/governance checks recorded before application. |
| Other writes | Tool write scope is this feature plus the expressly approved constitution; no helper/skill/shared README/Unity/source-data writes. |
| Post-plan hooks | No extensions.yml; nothing to dispatch. |
| Python implementation/scientific runtime validation | NOT RUN; no source/test suite/dependency/data outputs or tasks.md created. |

The final explicit preservation comparison checks 14 recorded hashes (ten Unity feature files, two contracts, active pointer and the ratified constitution). Earlier 42-file tables above refer to the historical Clarify audit, not a claim that every one of those files was re-compared in this final Plan check. Planning ready; implementation remains subject to separate stage authorization and its recorded contract/consumer entry gates.
