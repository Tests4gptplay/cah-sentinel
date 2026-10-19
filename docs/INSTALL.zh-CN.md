# CAH Sentinel 安装与本地配置

## 0.0.6 main 开发构建：Helper 专用动画

0.0.6 为 Helper 处理中加入工具箱与诊断面板的专用循环 GIF。统一使用 420x420 透明画布，可见高度及脚底基线按现有工作动画对齐。自定义 SpritePackPath 中存在 `states/wait_helper/loop.gif` 时优先使用；没有时自动使用 EXE 同目录 `sprite-pack` 内附的 Helper 素材。随包保留 `helper-ATTRIBUTION.md`、`helper-LICENSE-ART.md`。其他已有 GIF、配置和桌宠尺寸不变。

升级前从托盘退出 Sentinel，替换应用文件并合并随包 `sprite-pack` 子目录，保留 `settings.json`、`seen-alerts.json` 和其他五种 GIF。设置窗口应显示 v0.0.6。旧版 Release 下载说明见下文，本次没有改动旧 Release；0.0.6 可从 main 的成功 Windows Actions 构建获取。

## 选择版本

- **推荐：[v0.0.5 正式版](https://github.com/Tests4gptplay/cah-sentinel/releases/tag/v0.0.5)**：下载 `CAH-Sentinel-0.0.5-win-x64.zip` 和 `SHA256SUMS.txt`，核对哈希后解压一次，运行 `CAHSentinel.exe`。这一版将旧 Planner `WAIT` 映射为与 `NEED_USER` 相同的桌宠状态及通知；不会把 `WAIT_HELPER` 误报为用户待回复。下方 Actions 双层 ZIP 说明只适用于开发构建。

- 已发布的 **v0.0.2**：下载该 Release 的 Windows x64 ZIP，解压后运行
  `CAHSentinel.exe`。该版本使用静态姿势，不含 GF-02 GIF 播放器。
- **main 开发构建（后续更新）**：从 [GitHub Actions](https://github.com/Tests4gptplay/cah-sentinel/actions/workflows/windows.yml) 的成功 Windows 构建下载
  `CAH-Sentinel-development-win-x64` 构建产物，先解压外层 artifact ZIP，再解压内层应用 ZIP，运行 `CAHSentinel.exe`。
  也可以按 README 的命令从源码构建。0.0.5 包含 GIF 播放器、半尺寸桌宠及人物下方提示、CAH EXE/托盘图标；构建输出
  同时提供 `settings.example.json` 和本文。未改动已发布的 v0.0.2 下载包。

运行需要 Windows 10/11 x64，以及 PATH 中可用的 Git for Windows。
自包含发布包不要求另装 .NET；从源码构建需要 .NET 10 SDK。
运行 Sentinel 无需管理员权限、GitHub 登录、私有仓库地址或 Token；但下载 GitHub Actions 构建产物可能需要登录 GitHub。

## 升级到 0.0.5（包括从 0.0.4 升级）

1. 从托盘菜单选择“退出”，不要只隐藏桌宠。
2. 将新版应用文件放入原安装目录；保留已有 `settings.json`、`seen-alerts.json` 和素材目录，不用示例覆盖实际配置。
3. 旧 AppData 配置不会自动迁移。需要保留时，在程序退出后手动复制到 EXE 同目录；旧目录由用户自行处理。
4. 启动后可在设置窗口查看 `v0.0.5`。安装目录必须可写；不需要重装或重启 CAH。
5. 旧 Planner `WAIT` 与 `NEED_USER` 都显示“需要人工确认”，可通过设置中的 Foreground 链接查看；普通 Helper、Runner 结果等待不变。

## 桌宠如何操作

- 桌宠是可拖动的透明置顶窗口。按住人物、占位符或状态标签的左键拖动即可移动。
- 首次启动位于主屏幕工作区右下角。main 开发版会在松手时保存位置，**下次启动恢复到上次放置的位置**。
  拖动时跟随鼠标，松手就停留，不自动吸附或弹回角落；屏幕边缘及跨屏放置也会保留。
  若拔掉显示器后保存的位置已不可见，则回到最近的可见区域。已发布的 v0.0.2 不记忆位置。
- 双击打开你配置的 Foreground 对话；右键打开操作菜单。
- 托盘菜单可显示桌宠、打开设置、暂停/恢复监控、测试通知及退出。
- 隐藏桌宠不会停止监控。选择“暂停监控”才会停止监控与通知；选择“退出”结束进程。
  main 开发版暂停时也会停止 GIF 帧计时。

## 首次配置：推荐使用设置窗口

1. 保持你已经安装的 CAH 在本机正常运行和同步。
2. 首次运行 Sentinel 时，CAH 路径为空，会自动打开设置窗口。
3. 在“CAH 本地仓库根目录”中点击“浏览”，选择你实际使用的 **CAH 根目录**，
   不是 `cah-sentinel` 目录，也不是 CAH 的 `state` 子目录。
   根目录通常包含 `.git`；Git worktree 的 `.git` 也可能是文件。
4. Foreground 对话链接可留空；需要点击通知跳转时再填入自己的
   `https://chatgpt.com/…` 对话链接。Sentinel 不会自动回复。
5. main 开发版可选填“本地 GIF 素材包”，并可开启“减少动态”；详见下节。
6. 保存。main 开发版配置只写入 EXE 同目录的 `settings.json`，通知去重记录为同目录的 `seen-alerts.json`。

没有选定 CAH 目录时不读取 CAH 状态；不会凭空选择某个私有目录。
Sentinel 只读 CAH 已同步的本地 Git 对象，不执行 fetch/pull/clone，也不修改 CAH。

## 空白配置示例与手工配置

仓库根目录和后续开发发布输出提供 `settings.example.json`。
其中 `LocalRepoPath`、`SpritePackPath`、`ForegroundUrl` 都为空，未预填任何个人信息。
程序**不会自动读取 EXE 旁的示例文件**；main 开发版真正的配置文件位置是：

```text
<CAHSentinel.exe 所在目录>\settings.json
```

main 开发版不读取、迁移或写入 AppData；已有用户可退出旧版后自行将旧配置移到 EXE 目录。安装目录必须可写，不可写时不会回退到 AppData。已发布的 v0.0.2 包保持旧行为。

通常使用设置窗口即可，不需要手工复制。如果希望预先创建空白配置，先从托盘退出
Sentinel，再在包含 `settings.example.json` 的目录运行 PowerShell：

```powershell
$sentinelConfigDir = (Get-Location).Path # 在 EXE 所在目录执行
$sentinelConfigFile = Join-Path $sentinelConfigDir 'settings.json'
New-Item -ItemType Directory -Path $sentinelConfigDir -Force | Out-Null
if (Test-Path $sentinelConfigFile) {
    Write-Host '已有配置，保留原文件；请通过设置窗口修改。'
} else {
    Copy-Item '.\settings.example.json' $sentinelConfigFile
}
```

手工编辑时使用合法 JSON。Windows 路径可以用 `/` 分隔；如果使用反斜杠，JSON 中
每个反斜杠必须写成 `\\`。不要把本机真实配置提交到公开仓库。

| 字段 | 含义与默认值 |
| --- | --- |
| `LocalRepoPath` | 空白；通过设置窗口选择本机 CAH Git 根目录 |
| `SpritePackPath` | 空白；main 开发版可选的本地 GIF 包根目录 |
| `ForegroundUrl` | 空白；可选的个人 ChatGPT 对话链接 |
| `ReducedMotion` | `false`；main 开发版开启后只显示 GIF 首帧 |
| `PollSeconds` | `30`；当前程序固定每 30 秒检查状态，手改该值不会改变间隔 |
| `Monitoring` | `true`；允许监控，但仍须先选择 CAH 目录 |
| `Notifications` | `true`；允许 Windows 通知，暂停时不发送 |
| `ShowPet` | `true`；显示桌宠 |
| `PetLeft` / `PetTop` | `null`；main 开发版拖动后自动记录的桌面坐标，不需要手填。支持副屏负坐标 |

v0.0.2 不提供 GIF 包和减少动态功能；这些字段用于 main 开发版。

## 本地 GIF 包（main 开发版，可选）

在设置中选择 **包含 `states` 子目录的上一级文件夹**，结构如下：

```text
选中的包根目录/
  states/idle/loop.gif
  states/working/loop.gif
  states/wait_user/loop.gif
  states/error/loop.gif
  states/complete/once.gif
```

本地查看候选素材时可选择 checkout 中的 `assets/animation-candidates`，或解压后的
v0.4 候选素材 ZIP 根目录。素材不会自动下载，也不会自动加入 EXE。
GIF 缺失或损坏时回退到原静态 PNG；PNG 也不存在时显示 Unicode 占位符。
静态姿势图仍可放在 EXE 旁的 `assets/sprites/pet-idle.png`、`pet-work.png`、`pet-rest.png`。

候选 GIF 仍待美术验收。原角色设计署名 **ZipZipPipe**，美术及 AI 衍生动画按
**CC BY-NC-SA 4.0** 管理，与软件 **AGPL-3.0-only** 分开。
请保留素材目录内的 `ATTRIBUTION.md` 和 `LICENSE-ART.md`。

## 常见情况

- **本地状态过期／读取失败**：确认所选目录正确，Git for Windows 可用，且 CAH 自己的
  同步正常。现有本地 `FETCH_HEAD` 缺失或超过 5 分钟未更新时，Sentinel 会显示失联，
  不把旧状态冒充实时状态。它不会替 CAH 修复或联网同步。
- **看不到人物**：没有装图包时占位符是正常回退；检查本地包层级和文件名。
- **没有弹通知**：检查监控与通知开关、Windows 勿扰模式及通知设置。
- **找不到桌宠**：使用托盘“显示桌宠”；main 开发版会恢复记忆位置，若该位置已不在当前屏幕内会移回可见区域。
- **想停止后台监控**：使用托盘“暂停监控”或“退出”，隐藏窗口不会结束进程。

GF-02 的技术验证、完成动画触发条件和待验收项目见
[GF02_PLAYER.md](https://github.com/Tests4gptplay/cah-sentinel/blob/main/docs/animation/GF02_PLAYER.md)。

## 本地读取的静默重试（main 开发版）

偶发本地读取失败会间隔 10 秒静默重试，每轮最多读取 5 次；中途成功不会显示失联或发送读取失败通知。连续 5 次失败才显示失联，持续失败不重复通知；后续由 30 秒轮询计时器继续尝试，同一时刻只运行一轮读取。4 次等待共 40 秒，实际读取耗时另计。重试期间保留上次显示，首次读取时保留等待状态，不把旧数据宣称为新结果。暂停或退出会取消重试。真实任务的错误、受阻或需人工确认通知不受影响。
