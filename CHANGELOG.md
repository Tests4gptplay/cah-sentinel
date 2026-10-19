# Changelog

## 0.0.6 - 2026-10-19

- Add the user-approved Helper troubleshooting design as a dedicated transparent looping GIF; preserve the original PNG and separate artwork attribution/license.
- Map WaitHelper to `states/wait_helper/loop.gif`. Prefer a custom-pack Helper GIF; otherwise use the bundled one instead of the placeholder.
- Match the existing 420x420 canvas, working-pose visible height and foot baseline. Keep the other five GIFs, pet window size, state semantics, retry policy and portable settings unchanged.
- Include only the newly approved Helper GIF plus its art notices in application builds; older candidate artwork remains separate.
- Update executable/settings version, README, installation guide and regression tests.

## 0.0.5 - 2026-10-19

- Match CAH's Planner semantics: standalone `WAIT` in the Task Cell and legacy Planner `outcome=WAIT` / `turn_signal=wait` are displayed as `NEED_USER`, using the existing WaitUser pet state and Windows notification. Use `semantic.wait_condition` as the question only when an old WAIT does not have `semantic.question`.
- Keep actual internal `WAIT_HELPER`, `WAIT_WORKER_HELPER`, and other executor/result wait kinds distinct. A new Planner turn already in progress is not reclassified solely because an older decision said WAIT.
- Sentinel remains local-only and read-only: no CAH state mutations, Git fetch, authentication or runtime/Helper changes.
- Upgrade from 0.0.4: exit Sentinel via tray before replacing the EXE, preserve `settings.json`, `seen-alerts.json` and any separately installed GIF/sprite pack; run the new self-contained Windows x64 package. Installed .NET is not required. Artifact SHA-256 checksum is provided.


## 0.0.4 - 2026-10-19

- Updated README and installation instructions for current Actions downloads, portable configuration, preserving settings during upgrades, optional GIF packs, and the distinction between artifact-download login and credential-free runtime. The old v0.0.2 release is not replaced.

- Keep the local FETCH_HEAD snapshot source; increase silent read attempts from 3 to 5, ten seconds apart (40 seconds of retry waits before reporting persistent failure). Each read may take additional time. Cancellation and notification deduplication are unchanged.


## 0.0.3 - 2026-10-18

- Adopt CAH brand assets for the Windows executable and notification-area (tray) icon. The multi-resolution ICO is assembled losslessly from the source 16/24/32/48/128 PNGs; the tray icon is embedded for single-file publishing.
- Store settings.json and seen-alerts.json beside the executable, without AppData reads, migration or fallback.
- Halve the pet image dimensions and place readable status labels below it.
- Silently retry local state reads up to three times, two seconds apart, before reporting disconnection. Pause/exit cancels pending retries.
- Recognize ACTIVE, WAKING and REPLANNING Planner activity while the parent Task Cell is parked.
- Update executable version, settings heading and README to 0.0.3. Existing v0.0.2 release assets are unchanged.

## 0.0.2

Existing published local-only monitoring release; see the v0.0.2 Git tag for its original contents.
