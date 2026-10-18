# CAH Sentinel

**Version 0.0.1 — Windows desktop companion (experimental).**

CAH Sentinel is an **independent, opt-in, read-only status monitor** for a Git-canonical CAH task system. It has no scheduler authority, no AI API calls, and never writes to CAH's checkout or task states.

### v0.0.1 features

- Windows WPF transparent desktop pet with a system-tray icon and native Windows tray-balloon notifications.
- Git read-only **separate shallow bare mirror** inside `%LOCALAPPDATA%\CAHSentinel\readonly-mirror.git`.
- State projection from `state/task_cells/*.json`, relevant Task contracts, and the exact Planner `outcome.json`: `RUNNING`, `WAIT_USER`, `WAIT_HELPER`, `BLOCKED`, `ERROR`, `COMPLETE`.
- First-time and changed `WAIT_USER` decisions trigger one deduplicated notification; reminders are **not** repeatedly spammed by polling.
- Configurable HTTPS Git repository and Foreground conversation; click a balloon to open the configured ChatGPT Foreground.
- Pause, resume, show/hide pet, configure, and exit from tray. **Exit terminates monitoring and notifications.** No service, autostart, scheduled task, admin rights, or background daemon is installed.
- Three optional original-size PNG pose sheets loaded and cropped **without editing their pixels**. When absent, a simple Unicode placeholder keeps all monitoring features available.

### Installation (v0.0.1)

1. On Windows 10/11, download `CAH-Sentinel-v0.0.1-win-x64.zip` from [Releases](../../releases) when the Windows CI build has completed. Extract it to a writable folder; run `CAHSentinel.exe`.
2. Ensure **Git for Windows** is installed and that its Git Credential Manager already has read access to your **private** CAH Git repository (e.g. authenticate once using ordinary `git clone` in a terminal). Sentinel never stores tokens.
3. First-run Settings: enter your own full HTTPS Git clone URL (no embedded credential), branch (usually `main`), and the **Foreground** ChatGPT conversation URL for click-through.
4. Optionally extract the separately supplied `CAH-Sentinel-SpritePack-v0.0.1.zip` into the directory beside `CAHSentinel.exe`. The resulting files should be `assets/sprites/pet-rest.png`, `assets/sprites/pet-idle.png`, `assets/sprites/pet-work.png`.
5. From the tray icon, select **Test Windows Notification**, or pause/resume monitoring. Windows Focus Assist/Do Not Disturb may suppress visible balloon notifications.

**Important:** v0.0.1 uses the Windows `NotifyIcon.ShowBalloonTip` mechanism, not a guaranteed Action Center history notification. Some versions of Windows and notification settings can suppress these toasts. Source is provided, and the release is an experimental baseline, not a claim of production-tested GUI behavior.

### Boundaries and privacy

- The official CAH state machine remains in its own repository. Sentinel only projects it for display and alerts.
- Git access is via the user's local `git.exe`, using their already-configured Git credentials. No PAT is embedded or written to Settings.
- The local mirror can cache private repository **contents**, including sensitive task metadata. Use normal OS user-account protection and delete `%LOCALAPPDATA%\CAHSentinel` to erase the private mirror, settings and alert history.
- User questions are read from the exact corresponding Planner Outcome and displayed transiently; they are not written into alert-history JSON.
- On network/auth errors Sentinel shows **DISCONNECTED**, never a misleading green RUNNING.
- v0.0.1 does not send user replies. Click-through opens the configured Foreground; users respond via CAH's normal process.
- The supplied temporary art pack is distributed **separately** from the source-code release; see [sprite instructions](assets/sprites/README.md) and [art license](ASSET_LICENSE.md).

### Art-action generation roadmap

The staged, review-gated plan for expanding ZipZipPipe-inspired dragon-girl gestures is in [docs/art/ACTION_GENERATION_PLAN.md](docs/art/ACTION_GENERATION_PLAN.md), with [machine-readable progress](docs/art/ACTION_BACKLOG.json). It reuses the 20 original poses and the three unapproved derivative candidates before requesting new images; v0.0.1 remains unchanged.

### Development

- `src/CAHSentinel.Core/`: read-only Git mirror + pure status projection.
- `src/CAHSentinel/`: Windows WPF pet/tray/settings + notifications.
- `tests/CAHSentinel.Core.Tests/`: dependency-free cross-platform state projection tests.
- `.github/workflows/windows.yml`: builds on **GitHub-hosted `windows-latest`**, not the CAH self-hosted runners.

Build: `dotnet run --project tests/CAHSentinel.Core.Tests`, then
`dotnet publish src/CAHSentinel/CAHSentinel.csproj -c Release -r win-x64 --self-contained true`.

### Licensing

Original CAH Sentinel source code and documentation are **AGPL-3.0-only**: see [LICENSE](LICENSE) and [NOTICE](NOTICE).

ZipZipPipe's GPT dragon-girl artwork and applicable derivative sprite packs retain **CC BY-NC-SA 4.0** (separate from the code license); see [ASSET_LICENSE.md](ASSET_LICENSE.md). No artwork is included in this source repository as of v0.0.1.
