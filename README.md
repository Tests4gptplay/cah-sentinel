# CAH Sentinel

**CAH Sentinel 是 [chat-agent-harness（CAH）](https://github.com/Tests4gptplay/chat-agent-harness) 的桌宠式状态监控工具，用于只读查看该项目在本机已同步的任务状态；不是通用系统进程监控器，也不会代替 CAH 执行任务。**

**CAH Sentinel is a desktop-pet task-status monitor for [Chat Agent Harness (CAH)](https://github.com/Tests4gptplay/chat-agent-harness). It provides a read-only view of CAH's locally synchronized task state; it is not a general-purpose process monitor and does not execute tasks on CAH's behalf.**

**Version 0.0.4 — Local-only Windows companion (experimental).**

Sentinel is a voluntary desktop pet + Windows notification utility for CAH. **It does not need a GitHub account, repository URL, Personal Access Token, or private-repository credentials.** It never fetches from the network and never modifies CAH state. Code is [AGPL-3.0-only](LICENSE); independent character artwork has separate [CC BY-NC-SA 4.0](ASSET_LICENSE.md) terms.

**Support / 赞助：** [Ko-fi](https://ko-fi.com/kou392350) · [Patreon](https://www.patreon.com/cw/Kou848) · [爱发电 / Afdian](https://afdian.com/a/kkt_ifd1) · [支付宝 / Alipay](SPONSOR.md)

## Local-only monitoring (0.0.4)

- Read-only local Git object inspection using `git rev-parse`, `git ls-tree`, and `git show`.
- Reads the **existing CAH Git repository on your PC**, selected via a folder picker. The path is user-configured, **not shipped with any maintainer-specific location**.
- CAH's own local Bridge already refreshes `.git/FETCH_HEAD`. Sentinel reads exactly that local revision rather than the potentially stale working-tree JSON. **Sentinel performs no git fetch/pull/clone, remote HTTP requests or credential lookups.**
- Each poll reads up to **5 times, 10 seconds apart** before reporting disconnection: 40 seconds of retry waits, plus read execution time. A successful retry stays silent; persistent failures notify only once until recovery. Missing or more-than-5-minute-old `FETCH_HEAD` is not treated as current state. Pause/exit cancels retries. Freshness measures CAH's local synchronization boundary, not independent remote truth.
- Reads Task Cell JSON, exact Planner Outcome and task title from local Git objects. Alerts for `WAIT_USER`, `BLOCKED`, `ERROR` and parent `COMPLETE` are deduplicated.
- WPF desktop pet (optional pose sheets), Windows tray balloon alerts, pause/resume, hide/show and **Exit = no monitoring**. Windows Focus Assist may suppress balloon visibility.
- Foreground link is **optional and local-only**. Clicking notifications opens the user-configured ChatGPT conversation; Sentinel never sends answers or authorizes CAH actions.

## Install / first run

中文完整指引：[安装、桌宠拖动、本地 CAH 路径和 GIF 包配置](docs/INSTALL.zh-CN.md)。
空白配置示例：[settings.example.json](settings.example.json)（个人路径和链接均留空）。
main 开发版实际配置保存在 EXE 同目录的 `settings.json`，通知去重记录保存在同目录的 `seen-alerts.json`；设置窗口保存时创建，
main 开发版不再读取或写入 AppData，也不会自动迁移旧配置。EXE 目录必须可写，不可写时不回退到 AppData。已发布的 v0.0.2 包仍使用旧路径。程序不会自动读取 EXE 旁的示例。0.0.4 开发构建随包提供示例和中文安装文档。

The pet supports **left-button dragging**. It starts at the bottom-right of the
primary work area on first launch. Release the mouse to leave it where you drop it;
the position is saved locally and **restored across restarts**, without edge snapping.
A saved position outside the available screens is moved back into view.
Double-click opens the configured Foreground; right-click opens the menu.

1. Download the [0.0.4 Windows x64 release](https://github.com/Tests4gptplay/cah-sentinel/releases/tag/v0.0.4), verify the ZIP with SHA256SUMS.txt, extract it once into a writable folder and run CAHSentinel.exe. For newer main development builds, use a successful [Windows Actions build](https://github.com/Tests4gptplay/cah-sentinel/actions/workflows/windows.yml); Actions artifacts require two ZIP extractions and may require GitHub login. Running Sentinel does not require login.
2. Keep **CAH installed and running locally**. Ensure Git for Windows is available (CAH normally uses it).
3. In Settings select your **local CAH Git root directory** with Browse (the folder containing `.git`). No GitHub URLs or credentials are entered.
4. Optionally configure your own Foreground ChatGPT conversation URL for click-through notifications.
5. Optionally select a local GIF pack containing `states/idle/loop.gif`, `states/working/loop.gif`, `states/wait_user/loop.gif`, `states/error/loop.gif`, and `states/complete/once.gif`. Set `SpritePackPath` to its root in Settings. Candidate art is separate and not bundled in the application; keep its attribution and license. Without artwork a placeholder is expected.
6. Use the tray menu to test a Windows notification, pause/resume, or exit the process.

**Important:** If CAH's own Git sync stops, Sentinel cannot determine the latest task state. It reports **local snapshot stale**; it does not attempt to repair or connect to the private repository.

## Upgrading to 0.0.4

Exit Sentinel from its tray menu before replacing application files. Preserve existing `settings.json`, `seen-alerts.json` and local artwork; never overwrite personal settings with the blank example. Users of older AppData-based versions may manually copy their old configuration beside the EXE while Sentinel is stopped. 0.0.4 does not read, migrate, create or delete the old AppData directory.

See [CHANGELOG.md](CHANGELOG.md) for version history: portable settings, half-size pet with labels underneath, CAH executable/tray icons, active Planner state mapping, and the updated retry policy.

### Historical v0.0.1 → v0.0.2 behavior (not 0.0.4)

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
- 0.0.4 uses `NotifyIcon.ShowBalloonTip`, not guaranteed persistent Windows Action Center notifications.
- Optional GIF playback is supported. Candidate character artwork remains unreviewed and separate from the executable; missing state artwork falls back to static sheets or a placeholder. See [GIF player guide](docs/animation/GF02_PLAYER.md).
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
