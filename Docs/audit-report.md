# PharmaSynth — Inherited Code Audit Report

**Date:** 2026-07-07 · **Auditors:** Claude (5-agent subsystem review) · **Scope:** the 36 C# files recovered from the previous team's `Transition.unitypackage`, now at `Assets/Scripts/`
**Baseline facts:** all files compile with ZERO errors on Unity 6000.5.2f1 + XRI 3.5.1; no missing script references in the scene or any of 91 prefabs; no legacy Input System, no legacy XR API, no obsolete-API usage anywhere. The previous team was on the modern stack.

## Go/no-go verdict (plan §4.2)

**GO — reuse the framework.** The event-driven spine (`ExperimentFlowManager` events → decoupled UI, ScriptableObject experiment modules, collider-based task triggers) is the right architecture and matches the plan's TaskGraph direction. No XR-interaction rewrite is needed: where XRI is used it is used correctly (XRI 3.5 `Interactables.XRGrabInteractable`, `selectEntered/selectExited`, proper subscribe/unsubscribe). The costs are design-depth ones (scoring, reaction model, error matrix), exactly where the plan already budgeted new systems.

## ⚠ Critical findings

1. **The liquid shader is MISSING.** `LiquidPhysics`/`Wobble` drive `_Fill`, `_WobbleX/_WobbleZ`, `_LiquidColour`, `_SceneColourAmount`, `_UpVector`, `_LocalYMin/Max` — no shader or material under `Assets/` declares any of these (only `ChemLab/Glass` exists). Unity silently no-ops unknown material properties, so **all liquid fill/wobble/color visuals render nothing** until we author a URP Shader Graph liquid-fill shader. → New work item, W2.
2. **Reaction data model can't express our experiments.** `ReactionRule` = binary A+B → one liquid + bool precipitate. No temperature thresholds, gas evolution, observable outcomes (color/fizz/precipitate-color as gradeable results), multi-step chains, or ratios; `currentChemical` is overwritten on react, destroying mixture identity. → Rewrite the rule model (keep the SO-registry concept).
3. **"Wrong reagent auto-detect" is naive.** No-rule-found == "wrong" — context-free, false positives/negatives. Needs TaskGraph-context-aware validation.
4. **Custom `Tunneling.cs` is dead code** (filename/class mismatch = non-attachable) — replace with XRI 3.5's built-in `TunnelingVignetteController` (not referenced anywhere yet).
5. **`ModuleCutsceneController` double-invoke bug**: skipping during the last director fires `onCutscenesFinished` twice; state-polling can hang on Hold wrap mode → switch to `director.stopped` event before building the cutscene system on it.

## Verdict table

| Verdict | Files |
|---|---|
| **REUSE (7)** — as-is or trivial guards | `ExperimentTaskTrigger`, `XRBottleUI` (XRI reference pattern), `TabletGestureController` (generic input→event relay; not a real gesture detector), `TVRepositionController` (preset applier only), `EquipmentLabelVisibilityController`, `UIfuncs`, `ChemLabelUpdater` (drop legacy-Text path, add `Refresh()`) |
| **REFACTOR (18)** — keep design, fix issues | `ExperimentModuleDefinition` (add phase layer, prereqs, condition refs, per-phase weights), `ExperimentFlowManager` (keep event spine; extract scoring → BKT `MasteryModel`/`ScoreCalculator`; add mistake COUNT; data-driven error matrix; add phase/prereq enforcement), `ProcedureChecklistUI` (build items from module data at runtime; feeds tablet + watch), `ExperimentErrorReporter` (fold into error matrix), `ChemicalData` (add temp/state/gas/pH/precipitate/hazard-type fields), `LiquidPhysics` (rip out hardcoded fermentation logic; context-aware reagent validation; needs the missing shader), `LiquidPourer` (fix down-raycast vs tilt; pool spills; cache GetComponentInParent), `PowderPhysics` (fix scale-y stomp; material instantiation; reaction hook), `PowderPourer` (best pourer; cache targets), `ReactionRegistry` (null guard; dict lookup), `WeighingScaleController` (add real placement sensing via socket/trigger + mass source; dirty-flag the per-frame string alloc), `BreakableGlassware` (pool shards; detach FX before SetActive(false) — current FX are cut off same-frame; add cleanup task hook; ID by component not name), `NPCNarrationController` (~80% ready for subtitle-only; add PlayLine/queue/interrupt API + separate beep AudioSource), `ModuleCutsceneController` (fix bugs above), `MoveInstructionsOnTilt` (best-engineered file — the wrist-watch trigger seed; replace reflection + Resources.FindObjectsOfTypeAll with typed refs), `AcidCorrosion`/`SaltBurn`/`SpoonSaltController` (salvage techniques into one event-driven SO-backed hazard component; currently auto-play demos with no grading hooks) |
| **REWRITE (4)** — keep only the idea | `ReactionRule` (see critical #2), `FireProcedureZone` (latch bug: any first entrant disarms it; flags all untagged colliders incl. hands/head), `TimerController` (per-frame string.Format alloc; no API/events), `Tunneling` (see critical #4 — use XRI built-in) |
| **DISCARD (7)** | `TestTaskCompletion`, `EthylProcedureActionUI` (redundant with `ExperimentTaskTrigger.CompleteTaskFromEvent`), `EthylSynthesisDebugTester` (OnGUI, invisible in HMD), `Editor/EthylSynthesisModuleCreator` (hardcoded C# authoring = anti-pattern; use CreateAssetMenu/Inspector authoring; also writes to Resources/), `LiquidData` (redundant), `Testsol` (dead file, class/filename mismatch), `Wobble` (duplicates LiquidPhysics wobble; uncached string property IDs per frame) |

## Subsystem summaries

- **Framework:** event spine + SO data model are keepers; scoring/mistakes/phases are the rebuild area (as planned for TaskGraph + BKT). `ExperimentFlowManager` is a god-class — split data/scoring/UI. Authoring path for 11 experiments = Inspector-authored `ExperimentModuleDefinition` assets (the CreateAssetMenu already exists), not per-experiment C#.
- **Chemistry sim:** visually-competent prototype, design-shallow. Pour streams are Quest-appropriate (LineRenderer + ParticleSystem, alloc-free). Blockers: missing liquid shader (critical #1), thin reaction model (critical #2), hardcoded experiment logic inside physics components.
- **Equipment:** XRI usage is modern and correct where present. Weighing scale is a display tween, not a sensor — needs socket-based placement sensing. Glass break needs pooling + FX-detach fix + cleanup-task hook. Fire zone needs a rewrite (latch + false positives).
- **NPC/cutscene/UI:** `NPCNarrationController` verified to work with null audio clips (subtitle-only shipping mode ✓). Cutscene controller is the right primitive after two bug fixes. `MoveInstructionsOnTilt` is a gift — it's already 80% of the wrist-watch gesture feature.
- **Safety/misc:** demo-grade one-offs; salvage techniques into a unified hazard component wired to the error matrix. Use XRI's TunnelingVignetteController for comfort.

## Actions taken / next

- [x] Audit complete; this report written. `Transition.unitypackage` can now be deleted (backup at `C:\Users\MSI\PharmaSynth-handoff-backup\`).
- [ ] W2: move keepers into `Assets/PharmaSynth/Scripts/` (GUID-preserving `AssetDatabase.MoveAsset`), delete the 7 discards, apply the small reuse-tier guards.
- [ ] W2: author URP liquid-fill Shader Graph (critical #1) and validate `LiquidPhysics` visuals end-to-end.
- [ ] W2: TaskGraph + BKT scoring per plan §4.2, extending `ExperimentModuleDefinition`/`FlowManager` per verdicts above.
- [ ] W2: fix `ModuleCutsceneController` bugs before cutscene template work; replace `Tunneling` with XRI vignette.
