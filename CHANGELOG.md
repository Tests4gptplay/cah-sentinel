# Changelog

## 0.0.3 - 2026-10-18

- Adopt CAH brand assets for the Windows executable and notification-area (tray) icon. The multi-resolution ICO is assembled losslessly from the source 16/24/32/48/128 PNGs; the tray icon is embedded for single-file publishing.
- Store settings.json and seen-alerts.json beside the executable, without AppData reads, migration or fallback.
- Halve the pet image dimensions and place readable status labels below it.
- Silently retry local state reads up to three times, two seconds apart, before reporting disconnection. Pause/exit cancels pending retries.
- Recognize ACTIVE, WAKING and REPLANNING Planner activity while the parent Task Cell is parked.
- Update executable version, settings heading and README to 0.0.3. Existing v0.0.2 release assets are unchanged.

## 0.0.2

Existing published local-only monitoring release; see the v0.0.2 Git tag for its original contents.
