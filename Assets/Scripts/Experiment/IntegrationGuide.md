# Experiment System Integration Guide

This guide shows how to wire the new modular systems in Unity Inspector.

## 1) Module and Score UI

1. Create a `ExperimentModuleDefinition` asset:
   - Right click in Project -> Create -> VR ChemLab -> Experiment Module.
   - Set module title, intended learning outcomes, and task checklist.
2. Add `ExperimentFlowManager` to a scene object (for example `ExperimentSystems`).
3. Assign TextMeshPro references:
   - `moduleTitleText` -> top title text
   - `moduleStatusText` -> task/mistake notification text
   - `summaryTitleText` and `summaryBodyText` -> end summary panel
   - `outcomesText` -> learning outcomes panel body
4. Set `outcomesPanel` and `summaryPanel` GameObjects.
5. Call `BeginModule()` from your Start button.

## 2) Task Completion and Mistake Reporting

1. Add `ExperimentTaskTrigger` to trigger colliders and set `taskId`.
2. Use `ExperimentErrorReporter` on procedure steps that can fail.
3. `LiquidPhysics` now reports wrong reagent mixing automatically.
4. Add `FireProcedureZone` around burner areas.

## 3) NPC Tutorial and Skip

1. Add `NPCNarrationController` to an NPC object.
2. Assign `AudioSource`, subtitle TMP text, and skip button GameObject.
3. Add narration lines with subtitle + voice clip.
4. Hook skip button onClick to `SkipNarration()`.

## 4) Cutscenes and Skip

1. Add `ModuleCutsceneController` to a scene object.
2. Assign `PlayableDirector` references in sequence order.
3. Hook skip button onClick to `SkipCutscenes()`.

## 5) Weighing Scale

1. Add `WeighingScaleController` to the weighing scale object.
2. Assign measurement TMP text.
3. Call `SetTargetMass`, `AddMass`, `RemoveMass` through events.

## 6) Glass Break and Safety

1. Add `BreakableGlassware` to glass apparatus prefabs.
2. Assign broken glass prefab, particle effect, and shatter audio.
3. Set floor tag and impact threshold.

## 7) Improved Pour Animation

1. `LiquidPourer` now supports curved stream animation.
2. Tune:
   - `streamSegments`
   - `streamDropStrength`
   - `flowSmoothingSpeed`
   - `minStreamWidth` and `maxStreamWidth`

## 8) Labels, Tablet Gesture, TV Position

1. Add `EquipmentLabelVisibilityController` and assign label objects.
2. Add `TabletGestureController`, assign Input Action, then bind `onTabletGesture`.
3. Add `TVRepositionController`, assign TV transform and target transform values.
