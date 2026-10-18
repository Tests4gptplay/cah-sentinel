# CAH Sentinel — GIF-first animation plan v0.2 (revised)

> **PLAN ONLY.** This revision corrects the first-batch ordering. The shipped Sentinel v0.0.2 remains a local-only, read-only Windows tray/desktop-pet application that displays static poses. This document does not assert any GIF has been generated, integrated, tested on the owner's PC, or approved. Machine progress: [GIF_ANIMATION_BACKLOG.json](GIF_ANIMATION_BACKLOG.json). Art authority: [AS-00–AS-08](../art/ACTION_GENERATION_PLAN.md) and [art backlog](../art/ACTION_BACKLOG.json).
>
> Decision: pre-render a small GIF per CAH presentation state, then let a minimal WPF player switch between local GIFs. **No skeletal animation engine, no CAH code changes, no network monitoring, no private repository link or authentication.**

> **M1 media inventory update (2026-10-18):** Five technical GIF candidates now exist in a separate owner-held ZIP: IDLE v0.1, WORKING v0.1, WAIT_USER v0.3 (same source image every frame), ERROR v0.3 and COMPLETE v0.2 (one-shot). [SHA-256 manifest](../../assets/animation-candidates/manifest.v0.4.json) / [quality audit](GIF_QA_v0.4.md). **The Git repository has not yet received the binary GIFs; the Windows v0.0.2 app still does not play them.** User aesthetic approval and local UI integration are separate gates.

## What was corrected

1. **First milestone is five working animation groups:** IDLE, WORKING, WAIT_USER, ERROR, COMPLETE — not eleven groups before the first useful prototype. BLOCKED, RECOVERING, THINKING, AUTO_WAIT, PAUSED and DISCONNECTED form a second milestone. For the first milestone, the latter six still have the existing static pose fallback; monitoring, errors and pause controls must remain functional.
2. **No all-or-nothing art gate:** GF-00 inventories all 20+3+9 = **32 pose candidates** but prioritizes precision review for the five MVP groups. Asset approval proceeds **per group**. An unapproved image remains a candidate; an individual group's animation can be worked on once its own art references pass review. The animation writer/player can be tested sooner using wholly synthetic placeholder GIFs.
3. **Correct stage dependencies:** GF-03, 04, 05 and 06 can be developed independently once the common GIF player exists, rather than each waiting for the previous group's artistic acceptance. GF-07 joins the five approved groups into the first runnable milestone.
4. **Technical fidelity:** Source images remain original RGBA PNG files; GIF is a derived delivery format with palette and 1-bit transparency limitations. Smoothness is achieved through consistent anchors and modest in-between motion, not by assuming AI-generated full images are consecutive animation frames.
5. **No false success:** COMPLETE plays only on a newly observed, verifiably accepted **parent task** terminal event. Existing old completions and Worker/Planner/Helper done signals must not trigger repeated celebration.

## 1. Immutable baseline and materials

- Current program: **v0.0.2**, local-only Git FETCH_HEAD reader, 30-second state poll and tray notification; WPF currently has only SpriteCatalog single-PNG selection.
- Three user-supplied sheets: pet-idle.png **6**, pet-work.png **6**, pet-rest.png **8** poses, **20 total**. The current equal-width slicing is only a temporary fallback; AS-00 needs individual bounding-box and anchor checks.
- Three initial AI-generated derivative candidates: wait_user_need_help, error_prone, complete_flying_check. [Manifest](../../assets/sprites/candidates/manifest.v0.1.json).
- Nine additional AI-generated derivative candidates: wait_user_sign_raise, wait_user_sign_hold, wait_user_sign_wave, blocked_prone_watch, error_prone_low, recover_prone_lift, complete_takeoff, complete_bank, complete_hover_settle. [Manifest and SHA-256](../../assets/sprites/candidates/manifest.generated-9.v0.2.json).
- **None of the 12 derivative candidates has yet passed formal image-by-image signoff**, and the public Git repository holds their inventory, not the original PNG binaries. Full ZIPs and contact sheets remain in the owner's separately retained asset storage.
- Some additional AI candidates' alpha content touches the canvas edge: detect and reject clipped wings/horns/tail before assembling GIFs.

**License boundary:** Sentinel's original code is AGPL-3.0-only. The ZipZipPipe character and derivative art are under the project-owner-confirmed **CC BY-NC-SA 4.0**, with creator, source URL, AI-edit disclosure and original/derived-file hashes. Do not silently redistribute unapproved art in source code, GitHub Release, or executable. The default source build must still work with placeholder or generic static assets.

## 2. Milestones and GIF groups

### Milestone M1: first five practical groups (target after GF-07)

| Group | Image / proposed output | Behavior | Existing candidates |
| --- | --- | --- | --- |
| IDLE | states/idle/loop.gif | Stand, blink and barely breathe; long, quiet loop | I1–I6 on the original idle sheet |
| WORKING | states/working/loop.gif | Sit at PC, subtle typing and occasional small head movement | W2/W4 on original work sheet |
| WAIT_USER | states/wait_user/entry.gif + loop.gif | **Lift a NEED HELP sign**, then maintain sign with a small occasional wave | wait_user_sign_raise, hold, wave, earlier need_help |
| ERROR | states/error/loop.gif | **Dragon-girl lying prone**, wings and tail drooped, slow breath; not automatic recovery | error_prone_low, original error_prone |
| COMPLETE | states/complete/once.gif | **Take off, fly a small loop, carry a √ sign, land**; one-shot 5–8s | complete_takeoff, bank, hover_settle, earlier flying_check |

**M1 acceptance is not just “five GIFs exist.”** They must actually load and switch in the local-only WPF application, show the right states in synthetic CAH fixtures, preserve alert deduplication and not write to CAH. If proprietary/CC-licensed images are still unapproved for redistribution, demonstrate locally with a separately installed pack; don't bundle them into a public release.

### Milestone M2: remaining six groups and standalone Sprite Pack

| Group | Desired GIF | Distinguishing rule |
| --- | --- | --- |
| THINKING | states/thinking/loop.gif | Planner reading/thinking, not Worker typing |
| AUTO_WAIT | states/auto_wait/loop.gif | Waiting for results/dependencies without asking user |
| RECOVERING | states/recovering/loop.gif | Helper actively investigating/repairing; no NEED HELP sign |
| BLOCKED | states/blocked/loop.gif | Prone but looking up/awake, different from fatal ERROR |
| PAUSED | states/paused/loop.gif | Sleeping when Sentinel monitoring is paused; **no further Git polling or notifications** |
| DISCONNECTED | states/disconnected/loop.gif | Eye-open low posture and “stale/local connection lost”; old success is not reported as fresh |

At M1, the six M2 groups **must retain accurate labels and a static image fallback** rather than map erroneously to another semantic state. The existing coarse PetMood mapping of PARKED_WAIT_EVENT to Running must be refined in Sentinel for honest auto-wait/recovery interpretation, using local fixtures; CAH itself is not modified.

## 3. What one GIF should contain

**WAIT_USER**: A 0.35–0.6s entry in which the sign is raised; stable hold for 3–5s; a tiny 0.4–0.6s wave; return to hold. Sign text must remain legible (or be drawn clearly onto a blank sign in the offline export). Same decision_ref → no repeated toast/entry, but holding GIF continues until CAH confirms the wait is cleared. Clicking the notification is *not* a response.

**ERROR**: Low prone with wings drooped, still frame with gentle breath and long pauses, roughly 5–10s per loop. “Recovers” only when the monitored CAH state changes. No graphic wounds, and no PAUSED/sleep lookalike.

**COMPLETE**: Takeoff → small arc or elliptical flight → √ sign hover → land. The entire clip is roughly 5–8s, not infinite motion. Avoid flipping the check mark, moving outside the pet window or visually disconnected props. On first launch, old finished tasks become a baseline only; same parent terminal decision must not be celebrated repeatedly.

**IDLE / WORKING**: Usually one carefully aligned original PNG with subtle pre-rendered breathing/eye or typing motion looks better than rapidly alternating independent poses. Estimate 8–12 FPS for test GIFs, evaluate 128, 192, 256px, and choose smaller frame counts/longer holds for resource savings. Status polling cannot reset animation timing.

**Transitions:** Entry/exit and source pose transitions are optional for the first M1 preview, except WAIT_USER entry and COMPLETE once. User help, ERROR, PAUSED, stale data must be able to immediately preempt other playback. Large pose differences need a stable intermediate/brief fade, not naïve crossfades with doubled horns.

## 4. Minimal software architecture (not yet implemented)

- **Offline GIF builder:** Python + Pillow reads separate PNG source files, keeps raw originals unchanged, normalizes crop and anchors, produces controlled in-between movement, exports GIFs with tested palette, transparency, frame durations and disposal. Writes contact sheets, SHA-256 and measurable alpha/clipping notes.
- **Small WPF GIF player:** GIF decoder with correct per-frame timing/composition/disposal, PlayLoop / PlayOnce / Stop / SwitchState / FallbackPng; expose a reduced-motion option. WPF Image loading a GIF by itself does not guarantee animated playback. Player must not depend on 30-second state polling to advance frames or fetch external URLs.
- **State selector:** A pure mapping from verified **local** CAH state to display intent, with event IDs for entry/notification/one-shot celebration. WAIT_USER has higher urgency than COMPLETE. When data is stale, stop fresh success/help alerts and show stale status.
- **External pack:** Asset loader reads safe relative paths under user-selected pack root, validates approved manifest and dimensions; no arbitrary absolute/URL paths, no script execution. Missing GIF or unsupported format falls back to static PNG so Sentinel still runs without any art pack.
- **Hard boundaries:** No update to private CAH, no access to hosted credentials, no Windows service, no automatic permissions elevation. CI uses GitHub-hosted Windows runners, not CAH's managed or rescue runners.

Proposed files (planning only):

```text
sprite-packs/<user-selected-pack>/
  pack.json
  ATTRIBUTION.md
  LICENSE-ART.md
  states/idle/loop.gif
  states/working/loop.gif
  states/wait_user/entry.gif
  states/wait_user/loop.gif
  states/error/loop.gif
  states/complete/once.gif
  # + M2 group folders, when approved
  fallback/*.png
```

## 5. Revised work orders (each is separately verifiable)

| Work item | Task / deliverable | Independent gate / needed evidence |
| --- | --- | --- |
| **GF-00** | **M1-focused baseline.** Inventory 32 source poses and review crop/alpha/source attribution. Deep-check only the five first-batch groups first; record useful/needs-rework poses | Inventory + M1 pose contact sheet and 128/192/256 previews; don't block builder/player engineering on *all* M2 art approvals |
| **GF-01** | Offline GIF export tool, initially one stable **WAIT_USER prototype** (plus synthetic fixtures) | Reproducible output; source PNG hashes unchanged; per-frame durations, loop, disposal, edges validated |
| **GF-02** | Minimal WPF GIF playback + local state switching, test with synthetic placeholders | Repeated loop and one-shot work; unknown/missing files fallback; pause/preemption does not leave active monitor; no CAH changes |
| **GF-03** | WAIT_USER entry and loop as accepted art candidates permit | NEED HELP readable, continuous hold until verified response, alert fired once; user visual check |
| **GF-04** | ERROR prone GIF (not all blocked/recovery states yet) | Lying low, slow stable motion, not sleep; never self-heals; user visual check |
| **GF-05** | COMPLETE one-shot flying GIF | Parent terminal-only proof, historical completion baseline, 5–8s playback and interruption; user visual check |
| **GF-06** | IDLE + WORKING loops from original sheets | Gentle loops with correct crop and anchors; poll-independent, no jitter; user visual check |
| **GF-07** | **M1 delivery check:** five animations playing in the same app | Windows test recording, synthetic state transitions and notification dedupe; no public art bundle without approval |
| **GF-08** | Six M2 groups: THINKING, AUTO_WAIT, RECOVERING, BLOCKED, PAUSED, DISCONNECTED | Correct semantics; each clip/fallback visual reviewed individually; no new images unless a need is proven |
| **GF-09** | Full 11-state/multi-task/regression and resource QA | 60s+ actual Windows GUI playback, GIF alpha/disposal, low-motion behavior, stale/paused/no-fetch tests |
| **GF-10** | Versioned independent Sprite Pack + license/attribution + distribution gate | File hashes, CC attribution, user approval, safe packaging & rollback; only then consider new public Release |

**Dependencies:** GF-00 → GF-01 → GF-02; GF-03/04/05/06 share GF-02 but can be completed **in any order** after their own corresponding AS acceptance (AS-01, AS-02, AS-03, AS-05). GF-07 requires all four. GF-08 follows GF-07 and appropriate M2 art approvals; GF-09 follows GF-08; GF-10 follows GF-09 + AS-08 and user approval. An unreviewed AI sprite never becomes accepted merely because it was generated.

**M1 can be locally testable before full M2.** If the user wants a preview build before all assets are distributable, publish **code-only / generic placeholder** artifacts and give licensed art to the owner as a separate optional pack, subject to the user's distribution decision.

## 6. Acceptance tests and failure handling

1. GIF source and output hashes, dimensions, frame count, duration, loop/disposal and transparent edges verified. No alpha surprises, visible clipping, double-horns/extra limbs, corrupted HELP or √.
2. Visual comparisons at 128/192/256px on light and dark backgrounds, stationary anchor drift (goal at 192px: ≤3px unintentional displacement) and animation continuity; acceptance comes from **user viewing** the GIF, not automated quality estimates.
3. Synthetic local Task Cells exercise stable WAIT_USER, released WAIT_USER, long RUNNING, unexpected ERROR, valid parent COMPLETE, multiple tasks and stale FETCH_HEAD. Make sure no traffic to GitHub and no changes to CAH files.
4. Status and media must be independent: new GIF content does not modify planner/worker status, state polling does not restart idle clips, alert-click doesn't mark the question answered, and same result doesn't replay celebration.
5. Pause and Exit must stop monitoring/notification work even if decorative GIF animation is still displayed; bad media or unknown pack gracefully falls back without crashing UI.
6. Regression checks must be run using **GitHub-hosted Windows CI and local/synthetic fixtures**. Do not touch the ongoing CAH business task, its Runner, MV workflow or private operational state.

## 7. Immediate next action

**Start GF-00, but scope its first review to the M1 images and their crop/anchor continuity.** A light 32-pose inventory establishes provenance and hashes; only the five first-batch groups need full GIF-ready visual checks now. In parallel where useful, make a synthetic proof-of-concept GIF to validate GIF-encoding constraints before demanding new generated art.

All GF stages are currently **not_started**. This revision is a plan correction, not work claimed complete.

