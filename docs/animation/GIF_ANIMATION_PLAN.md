# CAH Sentinel — GIF 优先桌宠动画计划 v0.1

> **仅设计规划，所有阶段均未执行。** 适用现有 v0.0.2 **本地只读** Sentinel；当前软件只切换单张 PNG，尚未具备动图播放。目标是让程序只根据状态选择预渲染的 GIF，避免提前开发完整骨骼/关键帧运动引擎。进度以 [GIF_ANIMATION_BACKLOG.json](GIF_ANIMATION_BACKLOG.json) 为准。**不触碰 CAH 私有库、Runner、Task Cell 和已发布的 v0.0.2。**

## 一、为什么先选 GIF

- 为每个表现状态准备 `entry.gif`（仅一次）、`loop.gif`（反复）、`once.gif`（一次性完成庆祝）及可选 `fallback.png`（缺素材或减少动画）。
- Sentinel 只读取本机 CAH 已同步状态，切换动画资源，不经 GitHub 登录，也不修改 CAH。每 30 秒状态轮询**不得**重新触发 GIF 播放，动画有独立时间轴。
- **GIF 不会自动让几张 AI 静图流畅。** 需要精确裁切与锚点注册，检查服装/脸/翅膀/尾巴的一致性；差异过大时宁可使用一张主姿势经离线脚本微幅平移、旋转和眨眼形成一小段连续循环，也不要粗暴硬切。
- GIF 采用最高 256 色/单级透明，无法完整保留原 RGBA 的半透明边缘。先试 192px、256px 的 8–12FPS 小动图，与 PNG 在黑白背景对比边缘、浅紫渐变和蕾丝纹理。**原始 PNG 不可覆盖或删除**。GIF 是第一阶段实用格式，不排除将来换 APNG/动画 WebP/独立 PNG 帧。
- **WPF 实施提醒：** 设置 `Image.Source` 为 GIF 并不足以保证自动连续播放；需要最小 `GifBitmapDecoder` + 定时换帧（或经验证的等价组件）。无需写通用骨骼系统，也不得引入不必要的重量级依赖。

## 二、现有资产与权利范围

| 来源 | 个数 | 是否已验收 |
| --- | ---: | --- |
| ZipZipPipe 原始 `pet-rest.png` / `pet-idle.png` / `pet-work.png` 三张横向图集 | 8 + 6 + 6 = **20** | 来自用户，准确裁切与锚点仍需检查 |
| [早期衍生候选](../../assets/sprites/candidates/manifest.v0.1.json)：求助、趴卧、举√牌 | **3** | 尚未获得逐张正式验收 |
| [追加衍生候选](../../assets/sprites/candidates/manifest.generated-9.v0.2.json)：各三张求助/错误/庆祝 | **9** | 已生成但尚未正式验收；Git 目前仅记录哈希，不含 PNG 原件 |

共 **32 个候选姿势**，不等于 32 帧已经连续且可播的动画。各个帧应先同 [AS-00～AS-08 美术计划](../art/ACTION_GENERATION_PLAN.md) 联合检查人物比例、Alpha、外轮廓裁断、精确 bounding box、脚底/趴卧腹部接地点、飞行旋转中心。必须明确图像是原画还是 AI 衍生，不要混称为 ZipZipPipe 原始绘制。

美术依旧按 **CC BY-NC-SA 4.0** 保留作者 ZipZipPipe、来源 https://space.bilibili.com/4168597/dynamic、AI 改编说明和每个文件 SHA-256。Sentinel 代码保持 **AGPL-3.0-only**，代码许可证不可覆盖角色美术。公开发布图片包需要独立的用户认可与可分发授权核验。

## 三、不同状态播放哪个 GIF

| 表现状态 | 文件 | 动作定义 | 播放策略 |
| --- | --- | --- | --- |
| **WAIT_USER** | `wait_user/entry.gif` + `loop.gif` | 举 **NEED HELP** 牌、保持、偶尔轻晃 | 首次进入举牌一次，之后持续循环，CAH 正式解除等待才退出 |
| **BLOCKED** | `blocked/loop.gif` | **趴卧抬头、睁眼**，还在等待解阻 | 4–7 秒缓慢循环 |
| **ERROR** | `error/loop.gif` | **趴卧低头、翅膀下垂**，出错停摆 | 5–10 秒微弱呼吸，不自发恢复 |
| **RECOVERING** | `recovering/loop.gif` | 尝试撑起、翻资料/排障 | 仅真实 Helper 已介入才播放 |
| **COMPLETE** | `complete/once.gif` | 举 **√** 牌起飞、盘旋、落地 | **父任务最终验收后一次性播放**，5–8 秒，不循环 |
| **IDLE** | `idle/loop.gif` | 站立、眨眼、轻微呼吸 | 慢循环，复用 I1–I6 |
| **THINKING** | `thinking/loop.gif` | 看纸张、思考 | 复用 W1/W3 |
| **WORKING** | `working/loop.gif` | 坐在电脑前工作、轻微动作 | 复用 W2/W4 |
| **AUTO_WAIT** | `auto_wait/loop.gif` | 坐下等待执行结果、偶尔抬头 | 复用 R4/W1，不算系统故障 |
| **PAUSED** | `paused/loop.gif` | 睡觉、可选 Zzz | 用户主动暂停后停止状态读取和通知 |
| **DISCONNECTED** | `disconnected/loop.gif` | 睁眼趴卧、断线标志 | 本地状态缺失/过期，不能把旧状态冒充实时 |

**优先顺序：** 用户关闭监控以及本地状态过期先行；新鲜快照下 WAIT_USER > ERROR > BLOCKED > RECOVERING > WORKING > THINKING/AUTO_WAIT > IDLE。COMPLETE 是**一次性终态事件**，新 WAIT_USER 可以立即打断飞行，不因 Worker `done` 或 Helper `done` 触发庆祝。

`WAIT_HELPER` 是系统自行修复，不展示 NEED HELP 牌。点击 Windows 通知不代表用户回复已经通过 CAH 的正式 `foreground_intent`。同一 `decision_ref` 只发一次提醒，但 GIF 可继续举牌。第一次启动若存在早已完成的任务，记入基线而不重复庆祝。

## 四、重点三组的简易分镜

### G01 · WAIT_USER（优先）

- `entry.gif`：`wait_user_sign_raise` 牌从低位升起，约 0.35–0.60s；若 AI 帧的角色五官/裙摆差异明显，改用单帧做位置补间。
- `loop.gif`：`wait_user_sign_hold` 保持约 3–5s → `wait_user_sign_wave` 小幅摆牌约 0.4–0.6s → 再保持；建议一个完整循环 6–12s。
- `NEED HELP` 在 192/256px 可读。若生成的牌面文字糊掉，可使用原始无字牌面并在离线制 GIF 时画上清晰字形；不能发布乱码。
- 只有 CAH 的 WAIT_USER 真正消失才结束；不能“GIF 播完”就误宣告已得到用户回复。

### G02/G03 · BLOCKED / ERROR（龙娘趴卧）

- `blocked_prone_watch`：趴卧抬头睁眼、略微呼吸，循环；与 ERROR 的趴平低落明显区分。
- `error_prone_low`：翅膀和尾巴低垂，轻微呼吸/长停顿；不是血腥受伤，也不同于 PAUSED 的正常睡觉。
- `recover_prone_lift`：只在状态实际转到 RECOVERING 时短暂撑起；不能让动画“自我治愈”替代真实 Helper 状态。
- 统一地面接触位置，避免站/趴切换时人物瞬移。

### G05 · COMPLETE（空中举√牌报喜）

- 候选顺序 `complete_takeoff → complete_bank → complete_hover_settle → land`。在离线制作时以同一飞姿为主加入温和椭圆/8 字运动，必要时用经过审核的另一个姿势；避免服饰骤变。
- 单次约 5–8 秒，√ 方向正确、翅膀/尾巴完整、牌子握持真实；不做水平镜像以免对号反转。
- 结束后返回 IDLE 或安静的已完成静态姿势，**绝不每 30 秒重新播放**；同一父任务的终态结果去重。

## 五、素材包与运行时接口草案

```text
sprite-packs/
  zipzippipe-gpt-dragon-girl/
    pack.json
    ATTRIBUTION.md
    LICENSE-ART.md
    states/
      idle/loop.gif
      working/loop.gif
      thinking/loop.gif
      auto_wait/loop.gif
      recovering/loop.gif
      wait_user/entry.gif
      wait_user/loop.gif
      blocked/loop.gif
      error/loop.gif
      complete/once.gif
      paused/loop.gif
      disconnected/loop.gif
    fallback/
      idle.png
      wait_user.png
      error.png
      complete.png
```

`pack.json` 记录 `schema` / `pack_id` / `art_license` / `creator_credit` / 状态→`entry,loop,once,fallback` 的相对路径、大小/哈希校验和循环策略。绝不支持配置文件任意执行脚本、网络链接或越界路径。这个目录只是**未来目标结构**，v0.0.2 尚不认识此格式。

GIF 导出用离线 Python/Pillow 工具：不覆盖原件、裁切和锚点对齐、自动生成中间运动帧、量化透明色/调色板、处置方式校验、输出文件哈希与接触图。WPF 播放器只做 `PlayOnce` / `PlayLoop` / `Stop` / `Switch` / `Fallback`，并可随时被高优先事件抢占。

## 六、逐项工作流 GF-00～GF-08

| 编号 | 工作 | 通过条件 |
| --- | --- | --- |
| **GF-00** | 与 AS-00 合并：审核 20+3+9 姿势、精确裁切/锚点、授权 | 可复现索引及 128/192/256px 审核图；确定主帧和需重做帧 |
| **GF-01** | 写 Pillow GIF 离线导出器 | 可重复生成、帧时长/透明色/颜色测试正确，不改原 PNG |
| **GF-02** | 最小 WPF GIF 播放/切换器，先用无版权占位图 | 支持循环/一次性/暂停/错误回退及高优先级抢占 |
| **GF-03** | WAIT_USER 举牌入场和循环 GIF | 字可读、姿势稳定、状态解除才退场，用户视觉认可 |
| **GF-04** | BLOCKED / ERROR / RECOVERING 三组 GIF | 趴卧层级明确，不自动恢复，用户视觉认可 |
| **GF-05** | COMPLETE 飞行一次性 GIF | 5–8 秒、√ 正确、父任务终态去重，可中断，用户认可 |
| **GF-06** | IDLE / WORKING / THINKING / AUTO_WAIT / PAUSED / DISCONNECTED | 原始 20 帧复用、动幅低、静态 fallback 可用 |
| **GF-07** | 所有状态与本地监控综合 QA | 至少 60s Windows 动图播放、128/192/256px 预览、暂停/失联/多任务/首次完成不误报 |
| **GF-08** | 完整 Sprite Pack、署名、许可证、发布审批 | 用户批准的 GIF 才纳入独立素材包；可回滚，旧 v0.0.2 不被覆盖 |

**下一步：GF-00。** 优先核查原始三张横向图集和 12 张生成候选，确认哪些可用；同时可用虚构占位图独立开发 GF-01/02，避免美术瓶颈阻塞程序验证。所有步骤需要实测与用户确认才可标记 accepted。本轮**不生成 GIF、不修改程序，也不触发发行**。
