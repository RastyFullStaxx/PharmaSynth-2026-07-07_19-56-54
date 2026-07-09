# PharmaSynth — Remaining-Work Checklist

The single actionable tracker for everything still to do before the **2026-08-31** turnover. Check items off as they land. Grounded in a repo/scene inventory run on **2026-07-08** (self-tests 157/157 green). Companion docs: [gaps.md](gaps.md) (analysis, severities, client decisions), [project-overview.md](project-overview.md) (what exists), CLAUDE.md (session handoff).

Legend: `[x]` done & verified · `[ ]` open · **bold** = blocking for Tier-1 contract scope (Tutorial + Prelims + Benzoic Acid + Aspirin).

---

## 1. Per-experiment wiring matrix

Engine + data are done for all 11; everything below is scene/content wiring. "Sim rigs" = TemperatureSim / GasCollection / Crystallization / Filtration hookups via `TaskGraph.RegisterCondition`.

| # | Experiment | v2 data | Props+stations | Reaction rules | Vessel bindings | Sim rigs | Cutscenes ×4 | Quiz ×3 |
|---|-----------|:--:|:--:|:--:|:--:|:--:|:--:|:--:|
| T0 | **Methane (Tutorial)** | [x] | [x] carry-to-zone | [ ] | n/a (dry) | [x] burner heats + gas fills | [x] | [x] |
| P1 | **Chemical Compounding** | [x] | [x] auto-built | [ ] | [x] pour | [ ] | [x] | [x] |
| P2 | **Ethyl Alcohol** | [x] | [x] auto-built | [~] | [x] pour | [ ] ferment+distil | [x] | [x] |
| M1 | **Benzoic Acid** | [x] | [x] auto-built | [~] | [x] pour | [ ] cryst+filt | [x] | [x] |
| M2 | Acetanilide | [x] | [x] auto-built | [~] | [x] pour | [ ] | [x] | [x] |
| M3 | Acetone | [x] | [x] auto-built | [ ] | [x] pour | [ ] distil 56° | [x] | [x] |
| M4 | Chloroform | [x] | [x] auto-built | [~] | [x] pour | [ ] reflux+distil | [x] | [x] |
| F1 | Benzamide | [x] | [x] auto-built | [~] | [x] pour | [ ] ice bath | [x] | [x] |
| F2 | **Aspirin** | [x] | [x] auto-built | [~] | [x] pour | [ ] **overheat branch** | [x] | [x] |
| F3 | Caffeine (Tier 3) | [x] | [x] auto-built | [ ] | [x] pour | [ ] extraction | [x] | [x] |
| F4 | Wine Making | [x] | [x] auto-built | [ ] | [x] pour | [ ] time-skip | [x] +montage | [x] |

Legend: `[~]` = partially covered by `MasterReactionRegistry` (some tests produce products/observations; newer tests still need rules). Counts: cutscene SOs **44/44** ✅ · quiz questions **33/33** ✅ + **quiz/data-sheet UI ✅** · **reaction-rule assets 10** (`MasterReactionRegistry`) · **pour path built + verified** · hands-on scene wiring **11/11** ✅ — Methane (hand-built) + all 10 others **auto-built** via `ExperimentSceneBuilder`.

**Sim-rig auto-check hookups ✅ (2026-07-09):** `Interaction/ZoneSimStation` generalises the Methane rig — layout stations tagged `StationSim` (Heat/Crystallise/Filter/Collect) run a real sustained verb (prop-in-zone drives `TemperatureSim`/`CrystallizationController`/`FiltrationController`/`GasCollection`; task auto-completes via `TaskGraph.RegisterCondition`). **17/42 stations sim-driven** across the 10 layouts (heat/distil/reflux, ice/cryst, filter/buchner/decant, collect); setup/prep/weigh/tests stay instant zone-touch. Retry-safe; verified by `SimRigSuite`. So the "Sim rigs" column above is now largely wired (the remaining `[ ]` are tests/setup steps that are correctly touch-completion, plus reaction/observation polish pending client chemistry).

**Module-driven scene builder ✅ (2026-07-08) + ALL 11 LAYOUTS ✅ (2026-07-09):** `ExperimentSceneBuilder` + `ExperimentLayout` SO + `SceneAssetLibrary` (**42 prefabs + 37 chemicals** after +6 new reagents: Bromine Water, Silver Nitrate, Sodium Carbonate, Dichloromethane, Sodium Sulfate, Grape Juice). All **10 `ExperimentLayout` assets** authored (`Layouts/Layout_*.asset`) + wired into `ExperimentSceneBuilder.layouts`; each maps add-reagent tasks to pour→task bindings on a vessel and physical-action tasks to zone stations. Regression-covered — `SceneBuilderSuite` now builds all 10 + validates name resolution; **Suite 200/200**. **Remaining per experiment:** reaction-rule products for newer tests, sim-rig hookups (heat/distil/cryst/filter to auto-check), cutscene wiring.

- [x] **Methane heat/collect real verbs** — burner in zone heats (`TemperatureSim`), hot apparatus + tube in zone fills (`GasCollection`); tasks complete via auto-check (`MethaneApparatusRig` + `ZoneItemSensor`, regression-covered).
- [ ] Remaining Methane verb polish: mortar grind interaction (prepare-mixture), splint-flame test visual, flame/bubble VFX.
- [ ] **Wine bespoke rubric** (workmanship/appearance/presentation/documentation/flavour) — currently standard 6-category.
- [x] **Cutscene data — all 44** (`<Experiment>_Intro/ReagentPrep/Success/Failure` for all 11) authored with per-experiment chemistry + safety dialogue.
- [x] **Cutscene wiring — all 11 ✅ (2026-07-09):** `NPC/CutsceneLibrary` (`CutsceneLibrary.asset`, moduleId→4 SOs) + `CutsceneDirector.LoadForModule` swap on `ExperimentStarted`, wired onto `RobotNPC`. Regression-covered (`CutsceneLibrarySuite`). **Still TODO: staging/fades + wine time-skip montage (art-pass visual staging).**
- [ ] Per-experiment ILO cards ×11 (visual intro-cutscene card art).

## 2. Assets that do not exist yet (create / source / buy)

### Art & models
- [ ] **Fume hood model** — a glass **stand-in + working `FumeHoodZone` volume is now placed** on the back counter (`FumeHood_StandIn`); a real hood model (sash, extractor) is still an art-pass item. Required for Acetanilide/Benzamide/Caffeine/Chloroform safety rules.
- [ ] **Procedure demo videos** — **0 VideoClip assets exist in the project** (user expectation: TV-screen demos of what to perform, per storyboard). To create: short per-experiment demo clips + a `VideoPlayer` TV screen in the lab. NOTED for asset production.
- [~] **Dr. Jimenez** — **primitive stand-in placed** (`DrJimenez`, front-right, facing the islands, proximity-labelled "Examiner"). Still to source/rig the real scientist model (Asset Store; budget = client decision). `ExaminerNPC` behaviour component still to build.
- [ ] Pharmee **animation set** (enter / idle-float / talk / gesture / celebrate / warn) + **face-state materials**.
- [ ] **Clean reagent-label textures** for all 16 chemicals + apparatus labels (never copy storyboard labels — garbled AI text).
- [ ] Wrist-watch **3D model** (currently a primitive canvas).
- [ ] Fermentation set: vessels + airlocks ×3 + balloon (Ethyl Alcohol, Wine).
- [ ] Wine bottle + wine glass (tasting finale).
- [ ] Tea/caffeine props (tea leaves, kettle) — check pack first.
- [ ] Separatory funnel (Caffeine/Chloroform) — check pack first; author if absent.
- [ ] WASTE bin + cleanup props (sanitation mechanic).
- [~] **PPE stand-ins placed** — lab-coat / gloves / goggles primitive placeholders at the locker room (`PPE_Standins`, proximity-labelled). Real coat/glove art + don-on-body + mirror/avatar reflection still to build.
- [x] **Lab toolset placed** — 21 real ChemLab prefabs (mortar, pestle, spatula, scoopula, dropper, forceps, funnel, glass rod, watch glass, evaporating dish, crucible+tongs, test-tube rack, wash bottle, tripod, wire gauze, clay triangle, retort stand, iron ring, alcohol burner, beaker) on the perimeter counters under `LabTools`, each with a **proximity name label** (`ProximityLabel` — shows within ~1.5 m). Reagents + Methane props + balance also proximity-labelled.
- [x] **Two open shelf units stocked:** the `3x4` west-wall grid = **reagent shelf** (16 reagent bottles, uniform ~0.26 m, solid colour, visible & labelled); the `5x1` back-left corner = **equipment shelf** (13 apparatus across 5 tiers under `EquipmentShelf`, labelled). Both were empty; now full.
- [x] **Walk-bob** (`WalkBob` on the rig's Camera Offset) — subtle, speed-scaled head bob when locomoting (comfort-capped; amplitude exposed for a comfort toggle).
- [ ] VFX set: smoke, steam, fire, glass shatter, confetti (URP particles, ≤3k live).
- [ ] Grade-screen art + polished world-space canvas skins (HUD/tablet/watch/grade are primitive panels).
- [ ] Period-hub room dressing (3 doors).
- [ ] Aisle/station **pad art** (current pads are colored primitive cubes — readable but placeholder).
- Note: **`Assets/PharmaSynth/Prefabs/` and `Timeline/` folders are empty** — game-ready prefab variants (e.g., pre-wired station kits) and Timeline assets are all still to be made.

### Audio (folder is empty — 0 files)
- [ ] SFX set: pour, bubble, boil, glass shatter, alarm, success/fail stingers, UI clicks.
- [ ] **Pharmee robotic beep "voice" set** (it IS the character's voice).
- [ ] Ambient lab loop + menu music.
- [ ] `AudioService` + mixer groups (master/SFX/voice/ambient) and wiring to the existing AudioSource hooks (`NPCNarrationController`, `BreakableGlassware`) — only 2 AudioSources exist in the scene today (defaults).

### Data
- [x] **Quiz bank: 33 MCQs** (3 per experiment) authored as 11 `QuizBank` assets (`ScriptableObjects/Quizzes/`) + `QuizBank`/`QuizQuestion` type with `Score()`. Regression-covered.
- [x] **Post-lab quiz / data-sheet tablet UI ✅ (2026-07-09)** — `UI/PostLabController` + `Experiment/QuizBankLibrary` (`QuizBankLibrary.asset`, 11 banks) + `PostLabTablet` world-space canvas. Opens on ChemicalTests-phase-complete; MCQ option buttons + explanation + Submit; Submit completes the terminal `record-*` task then `runner.Finish(quizFraction)` (quiz → Documentation rubric criterion). Verified by `PostLabSuite`. **Follow-up: numeric yield-entry pad (yield not yet graded).**
- [x] **Reaction rules** — 10 `ReactionRule` assets + `MasterReactionRegistry` (`ScriptableObjects/Reactions/`), plus ~16 product/reagent `ChemicalData` (Benzoic Acid, Aspirin, Acetanilide, Benzamide, Chloroform, Iodoform, MnO₂, ester, CaCO₃, acetyl chloride, NaOCl, sugar, yeast, limewater, CO₂). Bidirectional lookup + pour→react→task chain regression-covered (PourReactionSuite). **Still TODO: assign the registry to each experiment's vessels in-scene + author remaining test reactions (ethanol functional-group tests, acetone Tollen's/Schiff, caffeine/wine).**
- [ ] Data-sheet definitions (expected yield ranges per experiment).

## 3. Built & tested code that is NOT yet placed in the scene

These components pass self-tests but appear **zero times** in SampleScene — the mechanic can't fire in play until placed:

- [x] **`FumeHoodZone`** — placed (stand-in hood, back counter). Still to wire into `LiquidTaskBinding` checks per experiment.
- [ ] **`HazardZone`** — place on hot surfaces / spill areas / acid zones **per experiment** (deliberately deferred: a generic zone at a station would punish correct actions).
- [ ] **`LiquidTaskBinding`** — attach per experiment vessel with expected reagent→task map (the pour path).
- [x] **Balance placed** on the right island (kinematic, grabbable). `WeighingScaleController` wiring per experiment still open.
- [ ] **`BreakableGlassware`** (inherited) — enable on glass props: drop → shatter → cleanup task + DroppedGlassware mistake.
- [ ] `CrystallizationController` / `FiltrationController` / `TemperatureSim` / `GasCollection` rigs as scene stations (ice bath, Büchner setup, burner, trough).
- [ ] `PPEController` full flow (see §6 — current scene has only the poke-sign).

## 4. Attribute passes on existing assets

### Done (verified this week)
- [x] **118/118 Environment meshes solid** (walls, doors, tables, cabinets, windows, counters — static MeshColliders).
- [x] **42/42 equipment prefabs grab-ready** (convex/box collider + Rigidbody + XRGrabInteractable; liquid meshes skipped; skinned tools = box-from-bounds).
- [x] URP port of the 5 broken ChemLab glass/liquid materials; `_WithLiquid` glass/liquid roles fixed; `LiquidPhysics.mainRenderer` → liquid mesh.
- [x] 16 reagent vessels filled, tinted, seated in the `3x4` cabinet grid.
- [x] Real Methane props + billboarded labels (`FaceCamera` on all world text/panels).
- [x] Rig CharacterController radius 0.1 → 0.25 (thumbstick locomotion collides; simulator WASD moves the HMD and legitimately doesn't).
- [x] Station zone pads grounded on the island; PPE sign clear of meshes; HUD bar Filled-type; Pharmee bubble raised.
- [x] **Waypoint beacon**: floor circular glow + bobbing/spinning down-arrow (`WaypointBeacon`) replaces the yellow blob.
- [x] **Pharmee alive + interactable**: `FloatBob` hover animation; poke the robot (`PharmeePoke` + collider + XRSimpleInteractable) to repeat the current step hint.

### Still to do
- [ ] **XRI sockets** (`XRSocketInteractor`) at stations/racks so props snap into place — 0 in scene.
- [ ] **Drop respawn** (kill-Z + idle return-to-home) — released props currently hover (XRI restores their kinematic state on release).
- [ ] **Teleport anchors** at each workstation (0 `TeleportationAnchor` in scene; only the floor `TeleportationArea`).
- [x] **Body collision verified** — walls/doors/tables have colliders; rig `CharacterController` (r=0.25) + `DynamicMoveProvider` (useGravity, no fly) + `GravityProvider`. **Thumbstick locomotion collides. The "walk through walls" is the XR Device Simulator moving the HMD device in tracking space (WASD/mouse), which bypasses the body by design — use the left thumbstick to test collision, or a real headset.** (Optional future: a head-collision "blackout" fade to discourage clipping.)
- [ ] Refine crude convex hulls on tall apparatus (tripod, retort stand, burner).
- [ ] **Proper VR glass shader** (current = flat URP Lit transparent; needs stereo-instancing validation + Quest overdraw budget; cheaper fallback ready).
- [ ] Liquid fill-line child-offset compensation (fill height is approximate on `_WithLiquid` vessels).
- [ ] XRI **interaction-layer audit** (everything currently on default layers; sockets/hands will need masks).
- [ ] **Re-point `PharmeeFace.faceRenderer`** at the robot's screen mesh (still `Ears_Black_Matt_0`) + face materials.
- [ ] Lighting: full lightmap re-bake + light probes once layout locks (current = placeholder realtime).
- [ ] Prop readability: real-scale apparatus is small — evaluate slight scale-up or outline/highlight shader after the grab-test.

## 5. Scenes, menus & UI flow
- [ ] **Menu scene XR-ization**: XR Origin + `TrackedDeviceGraphicRaycaster` + ray interactors (menu is desktop-click only today).
- [ ] Settings panel content: audio sliders, text size, subtitle speed, vignette intensity, snap-turn angle, seated/standing, handedness (`SettingsService` to build).
- [ ] **Period hub** (3 mastery-gated doors; `ProgressionFlow.IsPeriodUnlocked` ready) + experiment-select UI (`ExperimentCatalog`/`ExperimentLibrary` ready).
- [x] **Period hub + experiment-select UI** ✅ (2026-07-09) — `UI/HubSelectController` (pure `BuildModel`/`CanSelect` over `ProgressionFlow`, roster grouped by period with Locked/Available/Passed + period-door gating) + desktop-clickable `ExperimentSelect` panel in `MainMenu.unity` (11 rows + live progress, launches gated experiments). Regression-covered (`HubSelectSuite`). **VR follow-up: menu XR Origin + `TrackedDeviceGraphicRaycaster` for controller-ray clicks (desktop mouse works now).**
- [x] **Post-lab quiz UI** (3 MCQs on tablet) ✅ — see §1. [x] **data-sheet yield-entry** ✅ (VR-friendly −5/+5 stepper, clamped 0–100, on the same tablet; `PostLabController.AdjustYield`/`SetYield`, regression-covered). Yield is recorded on the data sheet; **not yet folded into the grade** (design decision for client — currently only the quiz MCQs drive the Documentation criterion).
- [ ] Results/History screen + exportable scores file (the agreed analytics descope).
- [ ] PPE ante-room flow: locker interaction → coat/goggles/gloves visibly donned → door gate (current = one poke sign).

## 6. NPC & narrative
- [ ] Pharmee skeletal animations wired to `PharmeeBrain` states.
- [ ] `ExaminerNPC` component (assessment mode: observes, no hints) + Dr. Jimenez staging.
- [ ] Dialogue copy pass for all 11 experiments → **client sign-off** (front-load; W3-item now late).
- [ ] Cutscene staging polish: fades, prop staging, per-experiment `CutsceneData` (40 SOs, §1), wine "7 days & 7 nights" time-skip montage + tasting finale.

## 7. On-device & release (needs the Quest 3)
- [ ] **Headset escalation**: if no Quest 3 delivered by W5 (Aug 4–10), flag the client — contractual risk.
- [ ] Day-1 on-device: 90 Hz hold worst-case (72 fallback), comfort pass, wrist-gesture ergonomics, hand-tracking input pass, MSAA 4× vs fill-rate decision, FFR validation.
- [ ] Perf hardening (proactive from W4–6): ASTC/atlasing, draw-call audit vs ≤150, tris ≤1.2M, rigidbody budget ≤40 kinematic-on-grab.
- [ ] Android keystore (not yet created) + signing docs.
- [ ] Full revision-checklist UAT + **ISO/IEC 25010 acceptance instrument**.
- [ ] Final APK + user guide + technical handover + implementation summary.

## 8. Client decisions & process hygiene
- [ ] **Chemistry sign-offs**: Benzoic Acid = benzaldehyde route (manual defect), Acetanilide acylating agent (acetyl chloride vs safer acetic anhydride), Benzamide nitrite typo.
- [ ] **Scoring-weight sign-off** (hard W2 exit criterion — still open).
- [ ] Dr. Jimenez budget confirmation; analytics-descope confirmation.
- [ ] Commit the untracked `FaceCamera.cs` (+meta) and current doc set; retire the dead `feature/asset-intake` branch.
- [ ] Keep committing checkpoints (two editor crashes have occurred; user commits, Claude commits only when asked).

---

**Reading the board:** the engine (100%) and experiment data (100%) are done; the remaining work is ~an art/audio/content-wiring production pass (§§1–6) plus the on-device tail (§7). Tier-1 bolded items are the contract-critical path. Suggested order stays as gaps.md §6: Methane real-verbs + Ethyl Alcohol wiring → menu XR + quiz/data-sheet UI → per-experiment wiring in tier order → audio + art passes → device QA.
