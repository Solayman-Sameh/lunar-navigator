# Repository content audit - 17 November 2026

## Decision and scope

The recommended tree contains 324 files (about 121 MiB). The existing index contains
440 paths. 176 indexed paths must be removed from tracking before a clean future
source commit; their local files are not deleted. No files were staged, committed,
pushed, uploaded or deleted during this audit. This checkout exposes .git as read-only.

The inventory examined 2,100 non-cache files, all root entries, retained source/configuration,
documentation, data layout and test references. Regenerated/internal trees such as Library/,
Builds/, environments, .git and caches were classified as whole trees rather than reviewing
every compiled/cache byte. This is a repository-content audit, not a new full algorithm,
license-rights, binary malware or Git-history secret audit.

## Every root area

| Area | Destination | Decision |
| --- | --- | --- |
| Assets/ | Git | All app/editor code, scenes, UI, HDRP settings, branding, maps and their .meta files. Generated/ is required source content. |
| Packages/ | Git | manifest.json and packages-lock.json pin the Unity environment; no package cache is vendored. |
| ProjectSettings/ | Git | Shared serialized project settings, including the Editor version and quality/rendering settings. |
| backend/ | Git selectively | Science implementation, tools, tests, pins, runtime entry point and sites.json. Ignore environments and working data. |
| launcher/ | Git | All C# sources and the hand-written csproj; bin/ and obj/ stay local. |
| installer/ | Git | Inno Setup source, not compiled installers. |
| deployment/ | Git selectively | Build scripts, tests, QA harnesses, notices and maintained README; machine build manifests and handoff/validation scratch stay local. |
| docs/ | Git allowlist | Proposal, contracts, scientific reference, attribution, current release/clock verification and repository policy/audit. Historical QA logs and PDF/Word duplicates stay local. |
| media/ | Git selectively | Verified UI screenshot, small branding image, media README. Empty MP4 is local; finished narrated video goes to a Release asset or video host. |
| .gitattributes, .gitignore, .vsconfig | Git | Line-ending/provenance policy, ignore rules and shared Unity IDE workload selection. |
| README.md, CHANGELOG.md, requirements.txt | Git | Entry documentation and development dependency forwarding. |
| .agents/, .codex/, .specify/, specs/ | Local only | Agent skills, planning records, workspace configuration and generated handoffs are not product source. |
| Screenshots/, Documentation/ | Local only | Development captures, diagnostic evidence and superseded source/QA notes; current attribution is consolidated in docs/. |
| Builds/ | Local / selected Release assets | Keep staged players, backups, runtimes, wheelhouse and raw reports local. Publish only the selected Setup EXE and its exact matching SHA256; optional sanitized provenance can accompany a release. |
| Library/, Temp/, Logs/, UserSettings/ | Local only | Unity-generated caches, scratch, logs and workstation preferences. |
| tmp/ | Local only | Temporary extracted players, native checks and scratch copies. |
| Unity-generated .csproj / .sln files | Local only | Unity regenerates these; the launcher project is explicitly retained. |
| .git/ | Git internal metadata | Not an application file or payload; never copy it into source/distribution archives. |

## Important file decisions

- Keep both NASA TIFF maps: approximately 59.0 and 31.7 MiB. Keep Moon.asset, Terrain.asset,
  materials, prefabs, saved configuration and folder/asset .meta files. Their names do not
  make them disposable. No Git LFS or history rewrite was performed; assess that separately
  if binary history growth becomes a problem.
- Keep the small retained NASA regression fixture (about 130 KiB), including original JSON,
  text, CSV, receipts, validation and plot: tests verify their bytes/hashes and provenance.
  Preserve .gitattributes -text rules; do not apply blanket raw/JSON/PNG/CSV exclusions.
- Keep labeled StreamingAssets analysis stubs: the repository/Editor demo loader uses them.
  They remain illustrative data and are not evidence of NASA results.
- Keep all current Assets, including template/sample-scene/readme assets for now. Removing
  unused template content is a separate Unity dependency cleanup, not a gitignore rule.
- The two horizons_probe.py files are legacy diagnostic utilities; keep them for now. They
  are not the production parser. Consolidation can be a later code cleanup.
- Keep the Markdown project proposal. It already describes the delivered geometric-only
  scope, installer and limitations. Exclude stale PDF/Word exports rather than publishing
  contradictory versions. No speculative scientific claims were added.
- Keep current clock validation reports because the release summary links to them. Reusable
  clock scripts were copied from ignored Builds/ to deployment/qa/ and documented. Outputs
  remain ignored. The copied scripts create their output directory in a clean checkout.
- Keep .vsconfig: it contains only the shared ManagedGame workload component. Ignore .vs/,
  .idea/, .vscode/ and generated Unity project/solution output.
- The zero-byte media/demo-record.mp4 is not a demonstration. Publish a real reviewed
  narrated recording separately, then link it. Do not commit an empty placeholder.
- README now uses the verified seconds/clock-fix screenshot and links to Releases without
  claiming that the locally prepared installer has already been published.
- Replace absolute workstation paths in public API examples with repository-root commands.

## Ignore policy changes

Added protection for local AWS/SSH folders, Unity temporary/test/profiler output, crash
dumps, root log files, Python analysis/coverage caches and utility output directories.
Only backend/data/sites.json is retained from working backend/data/. Test fixtures live
elsewhere and remain included. MP4 exclusion is scoped to release/presentation locations
so a future required Unity video asset is not silently excluded. Environment examples
are allowed, but must still be checked for real credentials. No blanket DLL or .meta
ignore was introduced: native Unity plugins and asset GUIDs can be legitimate source.

## Validation

- Proposed source-tree auditor: zero errors and warnings; 109 distinct Unity GUIDs.
- 44 project-owned serialized asset references resolve to retained assets and metadata.
  External package/builtin GUID resolution was not independently reimplemented.
- Required project/build/runtime/test fixture files remain included.
- Relative Markdown links resolve within the proposed tree, including current clock reports.
- The selected files had no matches for the scanned private-key, GitHub token, AWS access-ID
  and OpenAI-style token patterns. This is not a guarantee that no secret exists, and old
  commits, ignored environments and build caches were not scanned for secrets.
- All 39 root/backend ignore cases passed, including future Unity video/plugin assets,
  raw scientific fixtures, shared config, release checksums and generated output.
- The proposed-tree auditor rejected a parent-directory escape in its path-list input.
- No runtime Assets, Packages or ProjectSettings were changed. The packaged EXE and its
  checksum remain the previously verified pair.

## Index cleanup still required

Already-tracked files are unaffected by .gitignore. The 176-path removal plan is:
10 .agents, 19 .specify, 28 specs, 51 Screenshots, 36 Documentation, 21 old docs,
9 backend legacy/working-data files, and the two obsolete root Markdown files.

Exact inventories, per-file decisions, validation JSON and a NUL-delimited path list
are saved locally in Builds/RepositoryAudit/. The proposed tree can be rechecked without
modifying the index:

```powershell
.\backend\.venv\Scripts\python.exe deployment/audit_repository.py --files-from Builds/RepositoryAudit/proposed-files.json
```

When you decide to prepare a commit, the exact untracking-only command is shown below.
It was NOT executed; --cached retains working files. Review the path list first.

```powershell
git --literal-pathspecs rm --cached --ignore-unmatch --pathspec-from-file=Builds/RepositoryAudit/remove-from-index-paths.nul --pathspec-file-nul
```

This does not purge prior history. Do not force-push or delete old releases as part of
routine cleanup. If a real credential is found later, handle that separately.

## Before publication

1. Resolve the project LICENSE decision with the owners. No project license exists;
   attribution and third-party notices are not a license for your own code/artwork.
2. Stage only the reviewed source set after untracking the unwanted paths; audit the
   actual index again with deployment/audit_repository.py.
3. Keep original raw fixture bytes and asset .meta identities intact.
4. The Setup EXE and its matching .sha256 belong together in Releases, not Git. The
   checksum verifies bytes and is not a digital signature. No old ZIP is tracked in
   this baseline; remote release contents were not accessed or changed in this task.
5. Do not publish machine paths, user analysis caches or stale audit claims. Keep release
   validation limits explicit. A local package is not proof of a published release.
