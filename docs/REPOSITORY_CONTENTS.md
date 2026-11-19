# Public repository and release policy

The repository holds the inputs needed to inspect, test and rebuild LunarNavigator.
It is not a dump of the developer workstation or the download distribution.

| Content | Destination | Reason |
| --- | --- | --- |
| Unity scripts, scenes, materials, meshes, textures and `.meta` files | Git: `Assets/` | Scene references depend on GUIDs and serialized assets |
| Unity manifest, lock and shared settings | Git: `Packages/`, `ProjectSettings/` | Reproducible project setup |
| Python sources, pinned dependencies and site catalog | Git: `backend/` | Scientific implementation and runtime inputs |
| Tests and small portable genuine/synthetic fixtures | Git: `backend/tests/` | Clean-clone verification without local cache paths |
| Launcher, installer definition and build/test scripts | Git | Rebuild the shipped package |
| Proposal, science reference, contracts, attribution and current release notes | Git: curated docs | Maintained public documentation |
| Fresh UI capture and small branding image | Git: `media/` | Public presentation |
| Setup EXE and its matching SHA-256 text file | GitHub Release assets | Download distribution and integrity check |
| Completed narrated MP4 | Release asset or video host | Avoid large video blobs in Git history |
| Empty MP4 placeholder | Local only | An empty file is not a demo |
| `.agents/`, `.specify/`, `.codex/`, `specs/` | Local only | Agent instructions and planning workspace |
| `Screenshots/`, `Documentation/`, working QA reports and duplicate document exports | Local only | Superseded captures, scratch and historical evidence |
| Builds, ZIPs, environments, caches, logs and runtime analyses | Local only | Generated or machine-specific output |

## Important distinctions

`Assets/LunarNavigator/Generated/` contains referenced application assets, including
NASA maps and saved procedural meshes. Despite the name, it must remain in Git;
it is not equivalent to Unity's disposable `Library/` cache. The largest retained
source map is about 59 MiB. Moving these existing assets to Git LFS would be a
separate migration, not part of this release cleanup.

The updated `.gitignore` deliberately avoids blanket `!Assets/**`, `!docs/**` and
`!*.meta` overrides, which can defeat exclusions. Maintained documentation is
allowlisted. The hand-authored `launcher/LunarLauncher.csproj` is included even
though Unity-generated project files are ignored.

Ignoring a previously committed file does not remove it from Git. The v1.1.0
release removed excluded paths from the published source index while preserving their
local copies. Old versions remain in repository history; no history rewrite or
force-push is required for this cleanup.

## Old downloads

The old standalone ZIP is superseded by setup and must not appear as the recommended
download. No ZIP is tracked in the local baseline. Its remote release-asset state
must be checked when authenticated GitHub access is available. Keep older releases
as historical records, marked superseded; do not automatically delete their assets
or rewrite old tags. The v1.1.1 assets are the setup, its checksum, and the cropped narrated video.

## Before publication

Check the exact staged tree, required Unity GUID assets, tests, relative links,
secret patterns and release checksum. Build outputs are never staged. The published
tag must refer to the reviewed release commit, and release notes must disclose
the actual verification limits. README download instructions link directly to the current verified installer
and narrated demo, with a link to the release page and checksum.

## Current audited decisions

See [REPOSITORY_AUDIT.md](REPOSITORY_AUDIT.md) for the full folder matrix and audit limits.
Keep the shared `.vsconfig` (it contains only a Unity workload component, not a user profile).
Maintain manual clock checks under `deployment/qa/`; their outputs remain ignored.
Keep the current clock validation reports linked by the release summary.
Only `backend/data/sites.json` is included from the working data directory;
portable regression evidence remains under `backend/tests/fixtures/`.
Do not infer a license grant: this checkout currently has no project LICENSE file.
The owner must decide the code/artwork terms before describing it as open source.
