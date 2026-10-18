# GIF technical audit — M1 v0.4 (five local candidates)

This is a **technical candidate review**, not formal aesthetic approval, a WPF runtime test, or proof the GIF binaries have entered the public repository.

Archive: `CAH-Sentinel-GIF-Candidates-v0.4.zip` stored in the owner's ChatGPT Library. ZIP SHA-256: `2e50b81ddd48e03408a90ca9159b5d5f001dd60b86f13af99c33001093ba6bff`, 3201546 bytes. Integrity check completed; each member re-opened in Pillow, transparency, duration, bounds and loop semantics verified.

| State | Version | Frames / duration | On-screen visible bounds in 420×420 GIF | Review |
| --- | --- | --- | --- | --- |
| IDLE | v0.1 | 7 frames / 6050ms | width 215–225px; height 341–348px | Tiny variation from original artist's deliberately different eye/idle poses; owner should check flicker |
| **WORKING** | **v0.1** | 10 frames / 5690ms | **width 255px; height 328px exactly** | Original W4 laptop pose, one fixed scaling and foot position, slight 0–1px breathing |
| **WAIT_USER** | **v0.3** | 11 frames / 5800ms | **width 237px; height 331px exactly** | One held-sign PNG reused for the entire loop; avoids the face/garment/size shifts of v0.2 |
| ERROR | v0.3 | 7 frames / 5500ms | width 315px; height 180px exactly | Stable prone figure breathing; older false self-recovery rejected |
| COMPLETE | v0.2 | 48 frames / 5760ms | width 223–226px; height 263–268px | Source figure stays the same size while following a flight path; **one-shot, no GIF loop directive** |

All GIF canvases are 420×420. Different states have different natural silhouettes (prone ERROR is lower; flying COMPLETE is off the ground). They must **not** be enlarged independently to fill 420px, or characters will seem to grow and shrink between states. Body scale within each loop is kept consistent. On-screen review still needed at 128/192/256px, white/dark Windows desktop, to catch transparency fringing and confirm the deliberate small movements are visible.

**User's requirement addressed:** stable canvas + stable character raster scale. The new WAIT_USER v0.3 replaces three interchangeable AI-rendered full-body frames with **one consistent character image** for the hold loop; lifting the sign can later be a separate entry animation only after it passes a continuity review.

**What is still incomplete:** the original material 20+3+9 inventory is not fully accepted (AS-00/GF-00 in progress); GIFs are not yet played by the published WPF app, there is no verified real Windows GUI video; candidate binaries remain in the separate ZIP/Library and are **not in public Git**; no owner aesthetic acceptance. Planned next engineering step after getting the actual binary into a Git checkout: GF-01/02 complete tested GIF assembler/playback switcher, then GF-07 M1 app integration.

**Original artist/licensing:** ZipZipPipe (https://space.bilibili.com/4168597/dynamic), artwork and AI-generated derivatives CC BY-NC-SA 4.0; original source code AGPL-3.0-only. Preserve separate attribution and derivative labeling. No automatic bundling into the public executable.

## Subsequent Git archive / player engineering update

The historical inventory-only observations above are superseded for binary availability by [remote blob verification](GIF_BINARY_VERIFICATION_20261018.md): all five actual GIFs are now archived in public Git. [GF-02](GF02_PLAYER.md) adds a development player and tests; the original audit does not claim runtime or aesthetic acceptance, and the published v0.0.2 is unchanged.
