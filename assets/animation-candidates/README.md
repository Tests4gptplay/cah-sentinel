# CAH Sentinel GIF candidate inventory · v0.4

This public Git repository now contains **all five real GIF binaries** under `states/`, copied byte-for-byte from the verified v0.4 archive. They remain **candidate_unreviewed**, with no executable or release packaging approval. The full five-state GIF ZIP is preserved in the owner's ChatGPT Library: `/CAH-Sentinel/GIFDrafts/CAH-Sentinel-GIF-Candidates-v0.4.zip`. SHA-256: `2e50b81ddd48e03408a90ca9159b5d5f001dd60b86f13af99c33001093ba6bff`.

The current candidate set (each 420×420 pixels):

| State | Binary path | Technical review |
| --- | --- | --- |
| IDLE | `states/idle/loop.gif` | Original I1/I2/I5 blinking loop; pending owner visual review |
| WORKING | `states/working/loop.gif` | **New:** one W4 laptop pose, fixed scale, 10 frames, 5690ms; pending owner review |
| WAIT_USER | `states/wait_user/loop.gif` | **Revised:** v0.3 hold-sign artwork, same character bitmap in every frame; pending owner review |
| ERROR | `states/error/loop.gif` | v0.3 stable prone breathing; no false recovery |
| COMPLETE | `states/complete/once.gif` | v0.2 one-shot flight, NOT a repeating animation |

See [v0.4 manifest](manifest.v0.4.json) for exact per-GIF SHA, byte size, frame count, canvas and bounding-box measurements. The previous [v0.3 inventory](manifest.v0.3.json) remains as historical evidence. The [QA review](../../docs/animation/GIF_QA_v0.4.md) distinguishes technical checks from aesthetic approval.

## Offline binary import into your **Sentinel** Git checkout

Download the ZIP from the conversation/ChatGPT Library and run the following commands **inside your local `cah-sentinel` checkout (not the private CAH repository)**:

```powershell
python tools/gif/import_from_archive.py --zip "C:\Downloads\CAH-Sentinel-GIF-Candidates-v0.4.zip" --dry-run
python tools/gif/import_from_archive.py --zip "C:\Downloads\CAH-Sentinel-GIF-Candidates-v0.4.zip"
git add assets/animation-candidates/states
git commit -m "assets(gif): import five checked candidate loops (CC BY-NC-SA 4.0)"
git push origin main
```

The importer verifies the exact ZIP SHA-256/length and all five GIF hashes/lengths, refuses unsafe paths and never reads, writes or executes CAH. **The importer does not itself perform a Git commit/push.** After pushing, verify `git ls-tree -r --name-only main assets/animation-candidates/states` and verify actual binary digests against the inventory. Do **not** mistake a manifest or importer commit for a GIF binary upload.

License: character design by **ZipZipPipe**, source https://space.bilibili.com/4168597/dynamic, original and AI derivative art CC BY-NC-SA 4.0 (https://creativecommons.org/licenses/by-nc-sa/4.0/). Art is not under Sentinel's AGPL software license; label all AI-generated derivative poses and preserve attribution. Public redistribution in a software release requires the owner's explicit final approval and applicable license compliance.

Art notices: [ATTRIBUTION.md](ATTRIBUTION.md) and [LICENSE-ART.md](LICENSE-ART.md).
