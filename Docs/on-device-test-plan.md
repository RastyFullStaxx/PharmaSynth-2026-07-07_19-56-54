# PharmaSynth — On-Device (Quest 3) Test Plan

**Purpose:** turnkey checklist for the moment the Quest 3 arrives, so the device pass is execution, not planning. Build config is already done (Android, OpenXR, Meta Quest feature group, Oculus Touch + Hand profiles, Foveated Rendering, Vulkan-first, IL2CPP/ARM64, ASTC — see `Docs/build-and-run.md`).

---

## 0. First boot / build
- [ ] `File ▸ Build Settings` → Android target confirmed; scenes = [MainMenu(0), SampleScene(1)].
- [ ] Set up an Android **keystore** (document alias/password off-repo).
- [ ] Build APK; `adb install`; launch on device; controllers + hand-tracking both enumerate.
- [ ] Zero errors in `adb logcat` on boot.

## 1. Menu XR activation (the scaffold is in place, inactive)
The `MainMenu` scene has an **inactive `XR Origin (XR Rig)` scaffold** + `TrackedDeviceGraphicRaycaster` on `MenuCanvas`. To finish on device:
- [ ] Activate the `XR Origin (XR Rig)`.
- [ ] Make the XR rig's camera the menu's `worldCamera` (on `MenuCanvas`); **disable the plain `Main Camera`** to stop double-render.
- [ ] Add an `XRUIInputModule` to the `EventSystem` (for ray→UI).
- [ ] Convert `ExperimentSelect` from ScreenSpaceOverlay to **World Space** and give it a `TrackedDeviceGraphicRaycaster`; position beside `MenuCanvas`.
- [ ] Verify controller-ray clicks fire Tutorial / Enter Laboratory / experiment-select rows.

## 2. Comfort pass
- [ ] Teleport + snap-turn + continuous-move all work; tunneling vignette visible on smooth locomotion.
- [ ] No forced head motion; cutscenes never move the HMD (subtitles + fades only).
- [ ] Seated/standing recenter works; UI reachable in both.
- [ ] Wrist-flip watch checklist gesture ergonomic (both hands); button fallback works.

## 3. Performance — 90 Hz target (72 fallback)
Capture with the Unity Profiler / OVR Metrics on the worst-case scene (all burners lit + particles + both NPCs + a full experiment built):
- [ ] Holds 90 Hz (or document where it drops).
- [ ] ≤150 draw calls, ≤1.2M visible tris (per plan §4.5).
- [ ] Transparent glassware overdraw acceptable (the known risk) — validate MSAA level vs fill rate.
- [ ] ≤40 active rigidbodies (kinematic-on-grab); ≤3k live particles.
- [ ] Foveated Rendering on; single-pass instanced confirmed (both eyes render).
- [ ] Textures ASTC; atlas/batch where draw calls spike.

## 4. Interaction UAT — per experiment (all 11)
For each experiment (drive via the hub select → build spawns it): 
- [ ] Grab reagent bottles; tilt-pour into the vessel completes the mapped step in order.
- [ ] Wrong reagent → WrongReagent mistake + Pharmee warns; out-of-order → WrongStep.
- [ ] Carry apparatus to its zone; **sim stations** (heat/crystallise/filter/collect) require holding the verb to target before auto-completing.
- [ ] Fume-hood reagents used outside the hood → FumeHoodViolation.
- [ ] Post-lab tablet: answer 3 MCQs + set yield → Submit → grade screen; gate math correct.
- [ ] Intro / reagent-prep / success **or** failure cutscene plays (end cutscene always).
- [ ] Grade persists; passing unlocks the next experiment + period doors in order.

## 5. Error-branch UAT (per the error matrix)
- [ ] Dropped/broken glassware → cleanup task + sanitation hit.
- [ ] Overheat (aspirin/others) → smoke → dispose → redo (progress dip).
- [ ] Missing PPE gate; hazard-zone contact debounced mistake.

## 6. Acceptance
- [ ] ISO/IEC 25010 mapping filled (functional suitability, performance efficiency, usability, reliability, maintainability).
- [ ] Revision-checklist UAT signed.
- [ ] Final APK + user guide + technical handover.

## Known device-day risks (carry forward)
- Standalone XR `InitManagerOnStart` is FALSE (headless-PC-safe); **Android keeps auto-init ON** — verify a real headset actually initializes OpenXR.
- XR Device Simulator "walk through walls" is an HMD-drag artifact; on device, body collision uses the CharacterController (r=0.25) + gravity — re-verify real collision.
