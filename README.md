# CAH Sentinel

**Version 0.0.2 — Local-only Windows companion (experimental).**

Sentinel is a voluntary desktop pet + Windows notification utility for CAH. **It does not need a GitHub account, repository URL, Personal Access Token, or private-repository credentials.** It never fetches from the network and never modifies CAH state. Code is [AGPL-3.0-only](LICENSE); independent character artwork has separate [CC BY-NC-SA 4.0](ASSET_LICENSE.md) terms.

## Local-only monitoring (v0.0.2)

- Read-only local Git object inspection using `git rev-parse`, `git ls-tree`, and `git show`.
- Reads the **existing CAH Git repository on your PC**, selected via a folder picker. The path is user-configured, **not shipped with any maintainer-specific location**.
- CAH's own local Bridge already refreshes `.git/FETCH_HEAD`. Sentinel reads exactly that local revision rather than the potentially stale working-tree JSON. **Sentinel performs no git fetch/pull/clone, remote HTTP requests or credential lookups.**
- If `FETCH_HEAD` is missing or older than 5 minutes, Sentinel shows disconnected / stale instead of pretending old status is current. Freshness measures the local synchronization boundary, not independent remote truth.
- Reads Task Cell JSON, exact Planner Outcome and task title from local Git objects. Alerts for `WAIT_USER`, `BLOCKED`, `ERROR` and parent `COMPLETE` are deduplicated.
- WPF desktop pet (optional pose sheets), Windows tray balloon alerts, pause/resume, hide/show and **Exit = no monitoring**. Windows Focus Assist may suppress balloon visibility.
- Foreground link is **optional and local-only**. Clicking notifications opens the user-configured ChatGPT conversation; Sentinel never sends answers or authorizes CAH actions.

## Install / first run

中文完整指引：[安装、桌宠拖动、本地 CAH 路径和 GIF 包配置](docs/INSTALL.zh-CN.md)。
空白配置示例：[settings.example.json](settings.example.json)（个人路径和链接均留空）。
实际配置保存在 `%LOCALAPPDATA%\CAHSentinel\settings.json`；设置窗口保存时创建，
程序不会自动读取 EXE 旁的示例。后续开发构建会随包提供示例和中文安装文档。

The pet supports **left-button dragging**. It starts at the bottom-right of the
primary work area; its position is currently **not saved across restarts**.
Double-click opens the configured Foreground; right-click opens the menu.

1. Download [v0.0.2 Windows zip](../../releases/tag/v0.0.2) and extract it. Run `CAHSentinel.exe` (Win x64).
2. Keep **CAH installed and running locally**. Ensure Git for Windows is available (CAH normally uses it).
3. In Settings select your **local CAH Git root directory** with Browse (the folder containing `.git`). No GitHub URLs or credentials are entered.
4. Optionally configure your own Foreground ChatGPT conversation URL for click-through notifications.
5. Optionally unpack the separate user's ZipZipPipe sprite sheets into `assets/sprites/pet-rest.png`, `pet-idle.png`, `pet-work.png` beside the EXE. The app also works without images.
6. Use the tray menu to test a Windows notification, pause/resume, or exit the process.

**Important:** If CAH's own Git sync stops, Sentinel cannot determine the latest task state. It reports **local snapshot stale**; it does not attempt to repair or connect to the private repository.

## Upgrading from v0.0.1

The old prototype could fetch a user-configured private Git URL. That path is removed from v0.0.2. On startup the old `GitRemote` and `Branch` preference keys are dropped from `%LOCALAPPDATA%\CAHSentinel\settings.json`; the new local path remains unset until the user selects one.

The older `%LOCALAPPDATA%\CAHSentinel\readonly-mirror.git` may contain a copy of private task data and a remote URL from v0.0.1. **It is no longer read or used**, and is not deleted without your decision. You may manually delete that specific obsolete folder once confident you do not need it. Do not delete your CAH project repository itself.

## Privacy and security

- No embedded personal GitHub owner, private repository address, credentials, maintainer drive path or ChatGPT conversation URL.
- Local Git reads are limited to the user-selected repository, operate on existing objects, and do not modify Git refs, run network commands or access a private GitHub account.
- The app still reads local CAH task metadata (including possible private user questions) for on-screen notifications. Those are not sent to an external service.
- If the local snapshot is not fresh, state-dependent success and help alerts are suppressed until CAH resumes local synchronization.
- No Windows service, scheduled task, autostart setting, Admin requirement or always-on runner is installed.

## Known limitations

- Requires CAH's existing local `FETCH_HEAD` to stay fresh; not an independent source of canonical Git freshness.
- v0.0.2 still uses `NotifyIcon.ShowBalloonTip`, not guaranteed persistent Windows Action Center notifications.
- The art pack currently uses temporary equal-width sheet slicing. New AI-derived candidate animations are not accepted or integrated yet; see [art generation plan](docs/art/ACTION_GENERATION_PLAN.md) and [backlog](docs/art/ACTION_BACKLOG.json).
- The binary is Windows CI-built; user-machine rendering, antivirus behavior and first-run UX require real device verification.

## GIF-first animation roadmap

**Two milestones:** first deliver five genuinely playable GIF states (IDLE, WORKING, WAIT_USER, ERROR, COMPLETE), then expand the other six. [Revised GIF-first plan v0.2](docs/animation/GIF_ANIMATION_PLAN.md) and [GF-00–GF-10 execution backlog](docs/animation/GIF_ANIMATION_BACKLOG.json). Animation is a separate resource, not a complex rigging engine. The shipped v0.0.2 still uses static sprites, and unreviewed derivative art is not publicly bundled.

**Five candidate GIFs now exist** (IDLE, WORKING, WAIT_USER, ERROR, COMPLETE), rechecked at a 420×420 canvas with stable figure scale. WAIT_USER v0.3 uses one consistent source image to eliminate inter-pose jumps. See the [v0.4 SHA-256 inventory](assets/animation-candidates/manifest.v0.4.json), [QA audit](docs/animation/GIF_QA_v0.4.md), and [offline binary importer](tools/gif/import_from_archive.py). The full verified ZIP is stored in the owner's ChatGPT Library; **All five real GIF binaries are now archived in [assets/animation-candidates/states](assets/animation-candidates/states), with ZipZipPipe attribution and CC BY-NC-SA 4.0 notices**. The shipped v0.0.2 application still uses static poses.

## Development

- `src/CAHSentinel.Core/`: deterministic state projector and local-only Git reader.
- `src/CAHSentinel/`: native Windows pet, tray notification, settings.
- `tests/CAHSentinel.Core.Tests/`: state projection and local Git fixture tests, no external network.
- `.github/workflows/windows.yml`: build on GitHub-hosted `windows-latest`, **never CAH's own self-hosted Runner**.

`dotnet run --project tests/CAHSentinel.Core.Tests`

`dotnet publish src/CAHSentinel/CAHSentinel.csproj -c Release -r win-x64 --self-contained true`

## Licenses

Original CAH Sentinel code and documentation are [AGPL-3.0-only](LICENSE). ZipZipPipe character art and related derivatives retain separate [CC BY-NC-SA 4.0](ASSET_LICENSE.md) terms. Five unreviewed candidate GIFs are archived in the source repository with attribution; they are not bundled into application releases.

Development GIF player: [GF-02 usage and validation](docs/animation/GF02_PLAYER.md). It loads an optional user-selected local pack; current source changes do not modify the published v0.0.2 Release or bundle candidate artwork into executables.
