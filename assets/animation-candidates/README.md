# GIF 候选资产索引（非最终发布包）

本目录下的 [manifest.v0.3.json](manifest.v0.3.json) 是 **Git 中已提交的文字库存**。**当前尚未把四个 GIF 二进制提交到 Git**；这些文件现在保存在所有者 ChatGPT Library 的私有 ZIP `/CAH-Sentinel/GIFDrafts/CAH-Sentinel-GIF-Candidates-v0.3.zip`，SHA-256 可对照清单。没有 GIF 二进制，克隆仓库后不会自动出现桌宠动画。

计划经独立美术验收后放置：

- `states/idle/loop.gif`
- `states/wait_user/loop.gif`
- `states/error/loop.gif`
- `states/complete/once.gif`

后续需要以**真正上传二进制**的方式 Git add/commit/push，确认 `git ls-tree` 中存在这些路径并与清单哈希一致；不应把 ZIP 文件名或 SHA 记录当作已上传的证明。

原人物 ZipZipPipe，来源：https://space.bilibili.com/4168597/dynamic；原图以及 AI 衍生素材按 CC BY-NC-SA 4.0，程序代码 AGPL-3.0-only。镜像/公开发行要保留署名、衍生改编事实和作品授权；本素材未通过正式用户验收，暂不打包公开发行。

## 离线导入（需要真正上传二进制后 GitHub 才有 GIF）

从本次对话保存 `CAH-Sentinel-GIF-Candidates-v0.3.zip`，然后在 **Sentinel** 仓库工作目录中使用：

```powershell
python tools/gif/import_from_archive.py --zip "C:\\Downloads\\CAH-Sentinel-GIF-Candidates-v0.3.zip" --dry-run
python tools/gif/import_from_archive.py --zip "C:\\Downloads\\CAH-Sentinel-GIF-Candidates-v0.3.zip"
git add assets/animation-candidates/states
git commit -m "assets(gif): add four dragon-girl candidate animations (CC BY-NC-SA 4.0)"
git push origin main
```

Importer verifies archive SHA and every GIF byte length/hash, refuses traversal and unexpected content, and writes **only** into the Sentinel repository; no `git fetch`, authentication or CAH modification. **It does not do the Git commit/push for you.** Keep the original PNGs and the artist's attribution; candidates remain unreviewed until user sign-off.
