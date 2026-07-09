# PharmaSynth — Asset Production Spec

**Purpose:** the exact art / audio / video assets still to create, so production is turnkey the moment it's greenlit. Every item below is currently a placeholder/stand-in or absent; the game logic that consumes it already exists and is tested.

**Cost note:** items marked **[credits]** would be produced with `Unity_AssetGeneration_*`, which **spends Unity AI credits** — do not start these without explicit approval + budget. Items marked **[DCC]** can be authored in Blender/Substance from the sources in `Docs/raw-art-sources/`. **[store]** = Asset Store purchase may be faster.

---

## A. Art & models

| # | Asset | Current state | Spec | Path when done |
|---|-------|--------------|------|----------------|
| A1 | **Pharmee animation set** | static glb, `FloatBob` only | idle-float, talk, gesture-point, celebrate, warn — looping, root-motion-free | `Art/Characters/` anims + Animator |
| A2 | **Pharmee face states** | `PharmeeFace` points at wrong mesh | happy / neutral / warning / thinking screen-face materials; re-point `PharmeeFace.faceRenderer` at the robot's screen mesh | face material set |
| A3 | **Fume hood** | `FumeHood_StandIn` (glass shell + working zone) | real hood: sash, extractor grille, interior light; keep the `FumeHoodZone` trigger volume | `Art/Environment/` |
| A4 | **Dr. Jimenez** | primitive stand-in + label | rigged scientist (coat, glasses, blue tie) + idle/observe/clipboard anims; `ExaminerNPC` component to add | `Art/Characters/` **[store]** |
| A5 | **PPE items** | primitive coat/gloves/goggles stand-ins | wearable coat prop + glove hand-material swap + goggles; mirror/avatar reflection | `Art/Characters/PPE/` |
| A6 | **Clean reagent-label textures** | garbled AI text on bottles | re-author 16 reagent labels (name + hazard pictogram), legible at 0.3 m | `Art/UI/Labels/` **[credits or DCC]** |
| A7 | **VFX set** | none | URP particles: pour splash, bubbling, steam, smoke (overheat), glass shatter, confetti (pass) | `Art/VFX/` |
| A8 | **ILO cards ×11** | none | one intro card per experiment (title + objective + ILO), 16:9, legible in VR | `Art/UI/ILO/` **[credits]** |
| A9 | **Grade-screen / hub polish art** | functional primitive panels | frames, iconography, period-door art | `Art/UI/` |

## B. Audio (0 audio assets currently exist — needs an `AudioService` + mixer)

| # | Asset | Spec |
|---|-------|------|
| B1 | **Pharmee "voice"** | robotic beep/chirp set (it IS the character's voice) — greeting, instruct, warn, celebrate, idle; short, distinct pitches |
| B2 | **SFX** | pour, bubble, glass clink, glass shatter, burner ignite/roar, alarm, UI click/confirm/error, task-complete toast, grade pass/fail sting |
| B3 | **Ambient** | lab room tone loop (fume-hood hum, faint bubbling) |
| B4 | **Music** | menu theme + subtle hub bed; no music during lab work |
| B5 | **Wiring** | ✅ **DONE (2026-07-09, credit-free):** `Audio/AudioService.cs` (4 categories SFX/Ambient/Voice/Music, `Play("key")`/`PlayAt`, per-category volumes persisted to PlayerPrefs, optional `AudioMixer` dB mapping via `VolumeUtil`) + `Audio/SoundBank.cs` (name→clip lookup SO with an `ExpectedKeys` production checklist). Runs as silent no-ops with zero clips — regression-covered (`AudioSuite`). **Remaining:** author the clips (B1–B4), drop them into a `SoundBank.asset`, place an `AudioService` in the scenes, wire the existing AudioSource hook points + settings sliders. |

## C. Video — procedure demo clips (0 VideoClips exist)

| # | Asset | Spec |
|---|-------|------|
| C1 | **Per-experiment demo clip ×11** | short (~20–40 s) screen-capture or authored clip showing the procedure, played on a lab TV via a `VideoPlayer` screen; storyboard is the reference to exceed. Capture from the built experiments once art-complete. |
| C2 | **TV screen object** | a `VideoPlayer` + RenderTexture screen placed in the lab, play/pause via a poke button |

## D. Order of production (when greenlit)
1. Audio placeholder pass (B1 beeps + B2 core SFX) — cheapest, biggest feel uplift, wire `AudioService`.
2. Pharmee anims + face (A1/A2) — unblocks cutscene staging quality.
3. Reagent labels (A6) + VFX (A7) — visible polish across all 11 experiments.
4. Fume hood (A3), PPE (A5), Dr. Jimenez (A4).
5. ILO cards (A8) + demo videos (C1) — capture last, after art-complete.
