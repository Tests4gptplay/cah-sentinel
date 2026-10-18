# GF-02 development player (not a release)

The five candidate GIFs were archived and independently verified from GitHub
before this engineering phase. See [binary verification](GIF_BINARY_VERIFICATION_20261018.md).
The shipped v0.0.2 release is unchanged. GF-02 and M1 are **in progress**, not
visually approved or certified on the owner's Windows PC.

## Try the development build on Windows

Build from the repository root with .NET 10:

```powershell
dotnet run --project tests/CAHSentinel.Core.Tests -c Release
dotnet run --project tests/CAHSentinel.Animation.Tests -c Release
dotnet run --project src/CAHSentinel
```

In Settings select an optional local GIF pack directory containing `states/`.
For a candidate-only local review, select `assets/animation-candidates` in the
checkout or the extracted owner ZIP directory. Do not copy the assets into
published EXEs. The program does not automatically discover, download or bundle
these unreviewed files. Leave the path blank to retain static PNG/Unicode fallback.
Original static PNG sheets still load from `assets/sprites` beside the executable.
Use “减少动态” for a frozen first frame. No network authentication or private
repository access is required by the player.

## Behavior

- `GifClip` verifies GIF signatures, uses a local WPF decoder, composes offset
  patches/transparency and disposal 0/1/2/3, and preserves positive centisecond
  delays. Missing/zero delay defaults to 100ms. File size is capped at 16MiB,
  canvas at 1024×1024, frame count at 256 and composed pixels at 32 million.
- `GifPlayer` has PlayLoop / PlayOnce / Stop / SwitchState and static fallback.
  A monotonic clock and separate DispatcherTimer advance frames independently
  of the 30-second status poll. Repeated same-state polls do not restart playback.
  COMPLETE is forced once even if a GIF contains an infinite loop extension;
  the final delay is respected, then static fallback is restored.
- Only an observed transition from an existing non-complete **Task Cell** to
  explicit `COMPLETE_ACCEPTED` can start celebration. Generic DONE/SUCCESS,
  Worker/Planner outcomes, first-sync history and already-seen events cannot.
  Unsupported acceptance representations fail closed; GF-05 still needs canonical
  parent-acceptance fixture/contract review before broader completion support.
- WAIT_USER, ERROR, BLOCKED and Helper handling preempt celebration; a suppressed
  completion is consumed, not queued. WAIT_USER and ERROR loops persist until
  canonical local state changes. M2 moods use static fallback.
- Pause stops the status timer, cancels in-flight local reads, invalidates their
  generation and stops playback/notifications, including the notification test
  command. Resume and reconnect rebaseline historical completions. Exit disposes
  the player/tray and cancels outstanding reads. Stale state immediately switches
  to DISCONNECTED and cannot produce fresh success/help alerts.
- Pack filenames are fixed relative `states/<group>/*.gif` names. URLs, UNC
  shares, traversal, alternate streams and symlinks/reparse points are rejected.
  Nothing executes from a pack. Unknown, missing or corrupt media use the
  original static pose; corrupt/missing PNG falls back to Unicode.

## Validation and remaining gates

Current Linux instance: 46 core assertions plus existing synthetic local Git
reader tests passed. Windows application cross-publish and Windows test-project
cross-build passed. A Linux build cannot prove Windows runtime playback.

GitHub-hosted `windows-latest` CI runs the dependency-free Windows test executable:
synthetic four-frame GIF with independently decoded pixel oracle (disposal,
transparency, offsets and exact timing), dispatcher playback, loop/forced-once,
same-state polling, urgent preemption, pause, stale, reduced motion and fallback.
It also decodes all five checked-in candidate GIFs and checks canvas, duration and
frame count, without copying them into build artifacts. Check the actual run
result; test-project compilation is not an executed Windows test pass.

CI produces a code-only `CAH-Sentinel-development-win-x64` artifact. The automatic
Release creation step was removed and workflow permissions reduced to read-only;
no tag or existing Release is changed and no v0.0.3 is created.

Still required: owner visual acceptance (scale/character continuity/edges),
real Windows tray and pause-during-poll QA, 60s+ resource/interaction tests, and
GF-03–GF-07 per-group integration acceptance. No source PNGs were changed.
