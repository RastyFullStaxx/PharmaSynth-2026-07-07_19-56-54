# Tutorial Mode — design spec

**Date:** 2026-08-07 · **Status:** approved, not yet implemented · **Target:** PharmaSynth (Quest 3)

## Goal

A third top-level mode alongside Campaign and Lab Tour: **every one of the 9 experiments is
playable, unlocked, and heavily guided.** The player is shown *which* apparatus and reagent to
use next (glow + see-through silhouette + waypoint beacon) and *what to do with it* (the task's
hint text, printed on the wrist watch). Ungraded — it teaches, it does not measure.

Campaign already gives procedural guidance through Pharmee's dialogue. Tutorial Mode adds the
**spatial** layer campaign deliberately withholds: where the thing is.

**Scope extended 2026-08-07** — six affordances folded in after the core design was approved,
each gated on the same flag and each mostly a wrapper over something that already exists:

| Addition | Rationale | Reuses |
|---|---|---|
| **No timer** | A guided learning session that runs a stopwatch reads as an exam. | `ExperimentRunner` clock gate |
| **Always-on labels** | The cheaper answer to findability than x-ray, and it teaches label-reading, which x-ray does not. May make the silhouette unnecessary. | `ProximityLabel` |
| **Skip this step** | A stuck student must be able to reach the interesting part. | `DemoActions.CompleteCurrentStep` — already exists |
| **"What is this / why this one?"** | Teaches *why*, not only *where*. The copy is already written and underused. | `LabInfoDatabase`, `HoverInspector` |
| **Stuck-escalation ladder** | Catches the player who has the glow and still doesn't know what to do. Reuses existing Pharmee pools — no new voice lines. | poke system, `NPCNarrationController` |
| **No-grade run summary** | Closure. Counts, never a percentage — a score turns practice back into an exam. | `MistakeLog` |

Implementation order and per-task detail: [2026-08-07-tutorial-mode.md](../plans/2026-08-07-tutorial-mode.md).

## Non-goals

- No grading, quiz, BKT update, unlock, or save write. A Tutorial run leaves no trace.
- No duplicated scenes, modules, layouts, or task graphs. Same 9 modules, one flag.
- No new authored per-task highlight data (see "Rejected: authored targets").
- No ghost-hands / gesture demonstration. Deferred until playtest proves the *gesture* is the
  confusion rather than the *target*.

## Decisions taken

| Question | Decision |
|---|---|
| Graded? | **No.** Ungraded practice. Skips quiz → grade → BKT → unlock entirely. |
| Save? | None. A Tutorial run writes nothing. (Contrast: demo mode has its own save file.) |
| Entry point | **Third MainMenu button** in the cube room, beside Laboratory / Settings / Quit. |
| Module access | All 9 unlocked, no period gating, replayable in any order. |
| Hint text | **Yes** — the task's `hint` prints on the wrist watch, permanently, in Tutorial Mode only. |

## Architecture

Four units. Each is independently testable and has one job.

### 1. `TutorialSession` — the flag

```
public static class TutorialSession { public static bool Active; }
```

Set by the MainMenu button before the scene load, cleared on return to menu. Direct mirror of
`DemoSession.Active` ([DemoMode.cs:138](../../../Assets/PharmaSynth/Scripts/Progression/DemoMode.cs)).

Every consumer reads this one flag. The campaign path costs exactly one early-return per consumer,
so campaign behaviour is unchanged by construction rather than by testing.

**Consumers:**
- `TutorialHighlighter` — early-returns to a no-op when false.
- `WristWatchController` — appends the hint line when true.
- `ProgressionFlow` / `GameFlow` — skips the review-corner → quiz → grade → outro → unlock chain
  when true; a finished run returns straight to the picker.
- The module picker — ignores lock state when true.

### 2. `TutorialTargets` — the resolver

Answers one question: **`taskId` → which Transforms does this step involve?**

Built by sweeping the live scene once at run start. Every component that already owns a `taskId`
contributes its own transform:

| Source | Contributes |
|---|---|
| `LiquidTaskBinding.expectedReagents[]` | its own transform (pour **destination**) + the bottle whose `LiquidPhysics.currentChemical == step.reagent` (pour **source**) |
| `GrindController`, `StirController`, `WaterBathController`, `IceBathController`, `LitmusStrip`, `FlameTest`, `DryDistill`, `ZoneSimStation`, `FermentationController`, `RackTaskGroup`, `ExperimentTaskStation` | own transform (each already exposes a public `TaskId` getter) |
| `WeighStation` | own transform — **needs `public string TaskId => _taskId;` added**; it holds the id privately and registers a graph condition, but exposes nothing. One line, the only code gap in the sweep. |
| `ZoneItemSensor.requiredItemId` | the `LabItem` in the scene with the matching `itemId` — i.e. the **tool** the step needs |

Storage reuses **`ExperimentStationRegistry`**, widened from `Transform` to `List<Transform>` per
taskId. Its `Clear()` is already called at all four teardown points
([PharmeeGatekeeper.cs:555](../../../Assets/PharmaSynth/Scripts/NPC/PharmeeGatekeeper.cs) /588/747,
[ExperimentStarter.cs:35](../../../Assets/PharmaSynth/Scripts/Interaction/ExperimentStarter.cs),
[LabMenuController.cs:95](../../../Assets/PharmaSynth/Scripts/UI/LabMenuController.cs)) — that
lifecycle plumbing comes for free and is why we widen rather than add a second registry.

Each entry carries a role tag (drives tint) and a `stayLitWhenHeld` flag (drives grab behaviour):

```
enum TargetRole { Source, Destination, Tool, Station }
struct Target { Transform t; TargetRole role; bool stayLitWhenHeld; }
```

`stayLitWhenHeld` is **false for `Source`** (you grabbed the bottle — stop shouting about it) and
**true for `Destination` and `Tool`** (the tube you must pour into, the rod you must stir with:
holding it is not the end of the step, so it stays the answer while in hand).

**Naming:** `ExperimentStationRegistry` no longer holds stations — none exist since the zone-free
conversion. Rename it `TaskTargetRegistry` in the same change that widens it, so a cold session
doesn't read the old name and conclude stations are still a thing. 9 call sites, mechanical.

**Why sweep rather than register or author** — three options were considered:

- **Sweep (chosen).** One pass, ~12 `GetComponentsInChildren` calls, once per run. Zero new
  authoring. Derives the glow from *the same components that actually complete the task*, so the
  guidance is structurally incapable of disagreeing with the binding.
- **Self-registration** — each component registers in `Bind()`. Cleaner in principle, but touches
  12 files and walks into the documented edit-mode/play-mode `OnEnable` ordering traps.
- **Authored per-task target list** — a `highlightIds` field in the module definition. Rejected:
  9 modules × ~10 tasks of authoring, and the authored list can **drift** from the binding it
  describes. That is precisely the bug class the W5.34 clueless-player audit was spent on (a hint
  whose ACTION line contradicted its binding). Do not reintroduce it.

### 3. `TutorialHighlighter` — the driver

One MonoBehaviour on `ExperimentSystems`. Polls at **5 Hz** (not per-frame):

1. If `!TutorialSession.Active` or the runner isn't running → clear everything, return.
2. Read `runner.Graph.AvailableTasks()`.
3. Resolve each available task through `TutorialTargets` → the current target set.
4. **Diff** against last poll's set. Only objects *entering* or *leaving* get touched.
5. Apply/remove the three channels on the delta.

**It never decides that a step is done.** Completion happens through the existing binding exactly
as in campaign; the highlight follows one poll later because `AvailableTasks()` changed. No
parallel detector exists, therefore no drift between what glows and what the game wants is
possible. This is the single most important property of the design.

### 4. The three channels

**Glow** — reuse `HoverHighlight.SetHighlight()`, already public and documented as callable by
"other affordance drivers".

Requires one fix: `HoverHighlight._lit` is a single bool, so a hover-exit would clear a tutorial
glow. Split into `_hover` / `_guide`, with one `Apply()` that ORs them and picks the tint by
precedence. ~8 lines. Tints: hover = existing cyan, guide-source = amber, guide-destination =
green. MaterialPropertyBlock only — never `renderer.material`.

**X-ray silhouette** — a child mesh copy with an unlit `ZTest Greater` material, spawned on
set-enter and destroyed on set-exit.

Explicitly *not* a URP Renderer Feature: that approach needs targets moved onto a dedicated layer,
and layers are load-bearing for XRI interaction masks. A per-object child costs more draw calls
but touches nothing global.

**Waypoint beacon** — `WaypointGuide` + `WaypointBeacon` already exist and already animate (bobbing
spinning arrow + pulsing floor disc). They are currently **dead code**: `WaypointGuide` resolves
position through `ExperimentStationRegistry.Get(taskId)`, and since the zone-free conversion
(2026-07-17) no module stages an `ExperimentTaskStation`, so nothing registers and it calls
`Hide()` every frame in all 9 modules.

Reviving it is: swap the lookup for `TutorialTargets`, and set the beacon material to
`ZTest Always` so it reads through a closed cabinet door.

### 5. Hint on the wrist watch

`WristWatchController` currently shows only the current task's `label`
([WristWatchController.cs:157](../../../Assets/PharmaSynth/Scripts/UI/WristWatchController.cs)).
In Tutorial Mode it also prints `ExperimentTask.hint` beneath it, permanently — campaign keeps
hint text on the existing stuck/poke path.

**Consequence to accept:** every `hint` string becomes player-facing in a second place. Per the
W5.34 findings, hint ACTION lines are voiced (76 of 81), so **any hint copy edit made for
readability on the watch costs a voice regen**. Treat hint strings as frozen copy unless a change
is worth the regen.

`GlyphSafe.Sanitize` applies to the hint line as it already does to the label.

## Data flow

```
MainMenu "Tutorial" button
  └→ TutorialSession.Active = true → load SampleScene

Player picks any module (picker ignores locks)
  └→ ExperimentRunner.StartRun
       └→ TutorialTargets.Build()   [one scene sweep → ExperimentStationRegistry]

Every 0.2 s while running:
  runner.Graph.AvailableTasks()
    └→ TutorialTargets.Resolve(taskId) → [(Transform, TargetRole)]
         └→ diff vs previous set
              ├→ glow      (HoverHighlight.SetGuide)
              ├→ x-ray     (spawn/destroy silhouette child)
              └→ beacon    (WaypointGuide position)

Player acts → existing binding completes the task → AvailableTasks() changes
  └→ next poll moves all three channels. No completion logic in the highlighter.

Last task done
  └→ TutorialSession.Active → skip review/quiz/grade/outro/unlock → back to picker
       └→ ExperimentStationRegistry.Clear()  [already wired]
```

## Edge cases

| Case | Behaviour |
|---|---|
| Player **grabs** a glowing object | That object's guide glow clears immediately (`selectEntered`). Other targets for the same step **stay lit** — grabbing the acetone means the beaker now glows as destination. Shout until acknowledged, then point at the next thing. |
| Player **drops** it unused | Glow returns after ~0.5 s if the task is still available. The delay suppresses flicker on fumbles. |
| Hover glow vs guide glow | Two flags (`_hover`, `_guide`), one `Apply()`. Neither clears the other. |
| Step needs one specific tube from a rack of 19 | Only the tube the binding names glows, and it **stays lit while held** — "the right tube" remains the answer while it's in hand. Driven by `stayLitWhenHeld`, true for `Destination`/`Tool`. |
| Pour A into B | Both glow, colour-coded (source amber, destination green). The beacon sits on the **source**, and hops to the **destination** once the source is held. |
| Two tasks available at once | `AvailableTasks()` can return more than one. **All** their targets glow — suppressing a valid path teaches the wrong procedure. The beacon takes only the first, so there is never more than one arrow. |
| Target inside a closed cabinet | X-ray silhouette + `ZTest Always` beacon. The cabinet is **not** auto-opened; finding it is part of the lesson. |
| Target is broken | `BreakableGlassware` → `DropRespawn` returns the *same* GameObject to its baked home, so cached Transforms survive. Still null-guard every sweep, and rebuild the resolver on respawn. |
| `longProcess` time-skip | All three channels suppress during the fade-to-black, or a glowing bottle floats on a black screen. Hook the existing `TimeSkipController`. |
| `autoCompleteWhenOthersDone` wrap-up step | No physical target exists → resolver returns empty → beacon hides, nothing glows, and the watch carries the step alone via its hint. Do not strand the beacon on the previous object. |
| Target off-screen / player wanders | No off-screen indicator in v1. The wrist watch is the fallback. Revisit only if playtest shows people get lost. |
| Reagent starvation restart | Existing `ReagentSupplyMonitor` restart path fires as normal; the resolver rebuilds on the new run. |
| Campaign mode | `TutorialSession.Active == false` → highlighter early-returns, watch prints label only, flow is byte-for-byte unchanged. |

## Error handling

- **Unresolvable taskId** (no component claims it): resolver returns empty, beacon hides, watch
  still shows label + hint. Log one editor-only warning naming the module and taskId — a step with
  no locatable target is an authoring gap worth surfacing, not a crash.
- **Null transforms** from destroyed objects: filtered on every poll, not just on build.
- **Missing `ChemicalData` match** for a pour source (reagent bottle absent from the scene): the
  destination still glows; log the unmatched chemical name in editor.

## Testing

Suite pins, prefix `tutorial:` — all runnable in **edit mode** over a built stage, the same trick
`Reveal Stage` already uses:

1. `tutorial: resolver finds targets for every task in all 9 modules` — build each module's stage,
   assert every non-`autoCompleteWhenOthersDone` task resolves to ≥1 transform. This is the
   assertion that catches an authoring gap the moment a module changes.
2. `tutorial: pour steps resolve both source and destination`.
3. `tutorial: grab/release state machine` — pure function over (role, held, taskAvailable) → lit.
   No scene needed.
4. `tutorial: campaign unaffected` — with `TutorialSession.Active == false`, the highlighter
   produces an empty target set for every module.
5. `tutorial: every task with a hint has non-empty sanitized text` — guards the new watch surface.

Plus a `Simulate Campaign` re-run to confirm the campaign path is untouched, and one DevCapture of
a glowing target set (the visual *is* the question for the tint/x-ray pass — one shot).

## Build order

Task-by-task detail lives in [the implementation plan](../plans/2026-08-07-tutorial-mode.md);
14 tasks, suite 1288 → 1327. Three ordering principles that must survive any replanning:

1. **Mechanical groundwork first** (`WeighStation.TaskId`, registry rename) so the suite count
   stays flat and a regression is unambiguous.
2. **The resolver's all-9-modules pin lands before any visual work.** That assertion is where
   authoring gaps surface, and finding them after the glow is built means debugging two things.
3. **X-ray silhouette last and conditional.** Hint text + glow + through-wall beacon + always-on
   labels may already solve findability. It is the most expensive channel and the least essential;
   build it only if a headset playtest of everything else still leaves players hunting.

**Also deliberately excluded — undo the last step.** `TaskGraph` has `Reset()` but no per-task
uncomplete, and the graph is the easy half: reverting the *world* (vessel contents, temperatures,
precipitates) by hand is where subtle state bugs breed. Skip-step plus a restart covers the need
using calls that already exist, and yields a *correct* world state rather than a hand-patched one.

## Open items to confirm during implementation

- ~~`WeighStation` / `ScoopController` / `CleanableVessel` coverage~~ — **resolved 2026-08-07.**
  `WeighStation` needs a one-line public `TaskId` getter (folded into the resolver table above).
  `ScoopController` has no taskId by design: it completes the step via `lp.AddLiquid()` on the
  target vessel, so it already resolves through the `LiquidTaskBinding` path — and correctly, since
  a scoop step's real targets are the source container and the destination vessel.
  `CleanableVessel` has no taskId because **cleaning is never a graded task** ("Educational only —
  never graded"); nothing to resolve, no gap.
- Whether `TimeSkipController` exposes a "fading" signal the highlighter can read, or needs one.
- Whether the waypoint beacon is a scene object or spawned by a builder (affects where the
  `ZTest Always` material is assigned).
- MainMenu button wiring — `MenuCubeRoomBuilder` is the likely home; confirm it is idempotent
  before adding a fourth button.
