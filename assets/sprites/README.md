# Temporary sprite pack interface (v0.0.1)

The three pose sheets supplied by the user are kept as exact RGBA PNGs in the separately distributed sprite pack:

- `pet-rest.png` — 2048×682, 8 poses (rest / sleep / seated)
- `pet-idle.png` — 2048×682, 6 poses (idle / expressions)
- `pet-work.png` — 2048×682, 6 poses (reading / working / happy)

Copy the PNGs next to the EXE in `assets/sprites/`. The program maps each mood to a fixed equal-width cell and crops at runtime; no pixel-level modification or re-encoding occurs.

This is a **temporary** mapping, not a final sprite rig or animation contract. Later versions may use a per-pose manifest with explicit bounding boxes, anchor points and animation sequences.

Artist / character: ZipZipPipe — https://space.bilibili.com/4168597/dynamic

Applicable asset license: CC BY-NC-SA 4.0 — https://creativecommons.org/licenses/by-nc-sa/4.0/

Original code in the repository: AGPL-3.0-only, **not** a substitute for the artist's asset license.
