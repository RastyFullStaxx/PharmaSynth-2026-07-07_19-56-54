# Tutorial Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an ungraded, heavily-guided practice mode where all 9 experiments are unlocked, the next apparatus/reagent is shown (glow + waypoint + hint text + always-on labels), and a stuck player can ask why, skip forward, or be coached.

**Architecture:** One static flag (`TutorialSession.Active`, mirroring `DemoSession.Active`) gates everything. A scene-sweep resolver maps `taskId → Transform[]` by reading the *same* components that complete the task, so guidance cannot drift from the binding. A 5 Hz driver diffs the target set from `runner.Graph.AvailableTasks()` and applies the visual channels. The driver never detects completion itself.

**Tech Stack:** Unity 6000.5.2f1, URP 17.5, OpenXR + XRI 3.5.1, C# global namespace, thin MonoBehaviours over pure suite-tested cores.

**Spec:** [2026-08-07-tutorial-mode-design.md](../specs/2026-08-07-tutorial-mode-design.md)

**Suite count walks:** 1288 → 1327 across the plan (T2 +2, T3 +3, T4 +9, T5 +3, T6 +4, T7 +3, T8 +2, T9 +2, T10 +3, T11 +5, T12 +3). Each task states its expected running total.

## Global Constraints

- **Suite:** `Tools ▸ PharmaSynth ▸ Run Self-Tests`, **EDIT MODE ONLY**. Baseline **1288/1288 ALL GREEN** + exactly 3 expected warnings. Read the result from `Logs/selftest-result.txt` — do not wrap the run in a capture script.
- **Test at phase boundaries, not per micro-edit.** One suite run per task, after the task's batch compiles. Never re-run without intervening changes.
- **`Unity_ReadConsole` lies about compile errors.** The only source of truth is `grep "error CS" Logs/Editor.log | tail`. A stale `Library/ScriptAssemblies/*.dll` after a refresh means the compile FAILED. A suite run whose assertion count did not move ran the OLD assembly.
- **Unity MCP is currently disconnected.** Fallbacks: write `Temp/selftest-autorun-request.txt` (suite runs on next domain reload) or `Logs/menu-autorun-request.txt` (menu list). Headless: `Unity.exe -batchmode -quit -projectPath <proj> -executeMethod MenuAutoRun.RunNow` with the editor CLOSED.
- **Never run editor commands while the user is in Play mode.** Check editor state first.
- **New `.cs` files can import but never reach the assembly.** After creating one, verify the type landed: `grep -a <TypeName> Library/ScriptAssemblies/Assembly-CSharp.dll`. If absent, rename the file to a NEW asset path (fresh guid) and refresh.
- **Edit mode fires no `Awake`/`OnEnable` on `AddComponent`** → every component needs a `Bind()` seam. In PLAY mode `AddComponent` fires `OnEnable` IMMEDIATELY, before `Bind()` — so event subscription must live in `Bind()`, not `OnEnable` alone.
- **Never use `renderer.material` in edit mode.** MaterialPropertyBlock or `sharedMaterial` only.
- **Never edit a `hint` string for readability.** 76 of 81 hint ACTION lines are voiced; copy changes force a voice regen. Collect complaints, batch them into a deliberate regen pass.
- **Commits are the user's.** Each task ends at a checkpoint; ask before committing.
- **Docs are living.** Update `Docs/gameplay-flow.md`, `Docs/systems-reference.md`, and the CLAUDE.md current-state line **in the same change as the code**, editing canonical lines in place — never appending "actually B".
- **Campaign behaviour must be byte-for-byte unchanged.** Every consumer gates on `TutorialSession.Active` with an early return.

---

## File Structure

**Create:**
- `Assets/PharmaSynth/Scripts/Progression/TutorialSession.cs` — the mode flag. Sits beside `DemoMode.cs`, which owns the analogous `DemoSession`.
- `Assets/PharmaSynth/Scripts/Interaction/TaskTargetRegistry.cs` — the widened registry **and** the `TutorialTargets` sweep that fills it. These change together (a new binding type means a new sweep line and a new entry shape), so they live together.
- `Assets/PharmaSynth/Scripts/Interaction/TutorialHighlighter.cs` — the 5 Hz driver plus its x-ray silhouette helper.
- `Assets/PharmaSynth/Scripts/UI/TutorialCoach.cs` — the stuck-escalation ladder and the end-of-run summary. Both are "what the mode says to a struggling player", one responsibility, and they share the `MistakeLog` read.

**Modify:**
- `Interaction/ExperimentTaskStation.cs` — delete the inline `ExperimentStationRegistry` class, keep the MonoBehaviour.
- `Interaction/HoverHighlight.cs` — split `_lit` into `_hover` / `_guide`.
- `Interaction/WeighStation.cs` — add `public string TaskId => _taskId;`.
- `Interaction/WaypointGuide.cs` — resolve through `TaskTargetRegistry`.
- `NPC/PharmeeGatekeeper.cs` (3 sites), `Interaction/ExperimentStarter.cs`, `UI/LabMenuController.cs` — registry rename, mechanical.
- `UI/WristWatchController.cs` — hint line, skip button.
- `UI/ProximityLabel.cs` — always-on radius in tutorial mode.
- `Editor/PharmaSelfTests.cs` — the `tutorial:` pins.
- MainMenu button, HUD clock, and flow skip — exact files identified in each task's "Verify first" step.

---

## Task 1: `WeighStation.TaskId` + registry rename

Pure mechanical groundwork. No behaviour change — the suite must stay at exactly 1288 and green.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/Interaction/WeighStation.cs` (near line 13/41)
- Create: `Assets/PharmaSynth/Scripts/Interaction/TaskTargetRegistry.cs`
- Modify: `Assets/PharmaSynth/Scripts/Interaction/ExperimentTaskStation.cs:8` (remove inline registry)
- Modify: `Assets/PharmaSynth/Scripts/NPC/PharmeeGatekeeper.cs:555,588,747`
- Modify: `Assets/PharmaSynth/Scripts/Interaction/ExperimentStarter.cs:35`
- Modify: `Assets/PharmaSynth/Scripts/UI/LabMenuController.cs:95`

**Interfaces:**
- Produces: `WeighStation.TaskId → string`; `TaskTargetRegistry.Register(string, Transform, TargetRole, bool)`, `.Targets(string) → IReadOnlyList<TaskTarget>`, `.Clear()`; `enum TargetRole { Source, Destination, Tool, Station }`; `struct TaskTarget { Transform transform; TargetRole role; bool stayLitWhenHeld; }`

- [ ] **Step 1: Verify first — read the existing registry**

Read `ExperimentTaskStation.cs` lines 1–80 and note the exact signatures of `Register` / `Unregister` /
`Get` / `Clear` before replacing them. `ExperimentTaskStation` itself is still used by the synthetic
builder fixture — delete only the static registry class it declares, not the MonoBehaviour.

- [ ] **Step 2: Add the `WeighStation` getter**

```csharp
    /// The graph task this scale satisfies. Held privately for the condition
    /// registration; exposed so the tutorial target sweep can locate the scale
    /// for its step (2026-08-07). No behaviour change.
    public string TaskId => _taskId;
```

- [ ] **Step 3: Create `TaskTargetRegistry.cs`**

```csharp
using System.Collections.Generic;
using UnityEngine;

/// What role an object plays in a step — drives the guidance tint and whether
/// grabbing it should silence the glow.
public enum TargetRole { Source, Destination, Tool, Station }

/// One highlightable object for one task.
public struct TaskTarget
{
    public Transform transform;
    public TargetRole role;

    /// Source bottles go quiet once in hand (you got the message). Destinations
    /// and tools stay lit while held — "this is the right tube" is still the
    /// answer while you're carrying it.
    public bool stayLitWhenHeld;
}

/// taskId -> the scene objects that step involves. Formerly ExperimentStationRegistry,
/// which mapped one Transform per task and was fed only by ExperimentTaskStation; since
/// the zone-free conversion (2026-07-17) no module stages a station, so nothing ever
/// registered and every consumer silently got null. Widened to a list and fed by the
/// TutorialTargets sweep instead.
public static class TaskTargetRegistry
{
    private static readonly Dictionary<string, List<TaskTarget>> _map =
        new Dictionary<string, List<TaskTarget>>();

    public static void Register(string taskId, Transform t, TargetRole role, bool stayLitWhenHeld)
    {
        if (string.IsNullOrEmpty(taskId) || t == null) return;
        if (!_map.TryGetValue(taskId, out var list))
        {
            list = new List<TaskTarget>();
            _map[taskId] = list;
        }
        for (int i = 0; i < list.Count; i++)
            if (list[i].transform == t) return;                 // idempotent: sweeps may overlap
        list.Add(new TaskTarget { transform = t, role = role, stayLitWhenHeld = stayLitWhenHeld });
    }

    /// Live targets for a step. Null transforms are filtered on every read, not just
    /// on build — a broken vessel can be destroyed mid-run.
    public static IReadOnlyList<TaskTarget> Targets(string taskId)
    {
        if (string.IsNullOrEmpty(taskId) || !_map.TryGetValue(taskId, out var list))
            return System.Array.Empty<TaskTarget>();
        list.RemoveAll(e => e.transform == null);
        return list;
    }

    public static int TaskCount => _map.Count;

    public static void Clear() => _map.Clear();
}
```

- [ ] **Step 4: Delete the old registry and repoint its call sites**

Remove `ExperimentStationRegistry` from `ExperimentTaskStation.cs`. In that file replace its
`Register`/`Unregister` calls (lines ~62, ~73) with
`TaskTargetRegistry.Register(taskId, transform, TargetRole.Station, true)` and nothing on unregister
(the sweep rebuilds per run; per-object unregister is dead weight). Replace
`ExperimentStationRegistry.Clear()` with `TaskTargetRegistry.Clear()` at all five sites under **Files**.

- [ ] **Step 5: Point `WaypointGuide` at the new registry so it compiles**

In `WaypointGuide.cs:28`:

```csharp
        Transform station = null;
        var targets = TaskTargetRegistry.Targets(id);
        if (targets.Count > 0) station = targets[0].transform;
```

Behaviour unchanged (still nothing registers yet, still hides) — this is a compile fix; the revival is Task 6.

- [ ] **Step 6: Verify the compile actually succeeded**

```bash
grep "error CS" Logs/Editor.log | tail
```

Expected: no output. If `TaskTargetRegistry` is missing from the assembly, confirm with
`grep -a TaskTargetRegistry Library/ScriptAssemblies/Assembly-CSharp.dll`; if absent, rename the file
to a new path (fresh guid) and refresh.

- [ ] **Step 7: Run the suite** → read `Logs/selftest-result.txt`. Expected **1288/1288 green, 3 warnings**. The count must not move.

- [ ] **Step 8: Checkpoint** — ask the user before committing.

---

## Task 2: `TutorialSession` flag + ungraded flow

Delivers: a module launched with the flag on runs normally and ends **without** quiz, grade, BKT, save, or unlock.

**Files:**
- Create: `Assets/PharmaSynth/Scripts/Progression/TutorialSession.cs`
- Modify: the review/grade chain — exact file found in Step 1
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Produces: `TutorialSession.Active → bool` (settable static); `ShouldEnterReview() → bool`.

- [ ] **Step 1: Verify first — find the grade chain**

```bash
grep -rn "ReviewFlowActive\|IsReviewState\|PostLabController\|GradeScreen" Assets/PharmaSynth/Scripts/NPC/PharmeeGatekeeper.cs Assets/PharmaSynth/Scripts/Progression/ | head -30
```

Identify the single call site where a finished run enters the review sequence. That one place gets the
early return. Note its method name — later steps call it `<EnterReview>`.

- [ ] **Step 2: Create the flag**

```csharp
/// Tutorial Mode (2026-08-07): all 9 experiments unlocked, heavily guided
/// (glow + waypoint + hint on the watch + always-on labels), and UNGRADED —
/// no quiz, no grade screen, no BKT update, no save write, no unlock.
///
/// Deliberately the same shape as DemoSession.Active: one static flag every
/// consumer early-returns on, so the campaign path is unchanged by
/// construction rather than by testing.
public static class TutorialSession
{
    public static bool Active;
}
```

- [ ] **Step 3: Write the failing suite assertions**

```csharp
        // tutorial: a guided run is practice — it must never reach the graded chain.
        TutorialSession.Active = true;
        Check("tutorial: flag gates the review chain", !ShouldEnterReview());
        TutorialSession.Active = false;
        Check("tutorial: campaign still enters the review chain", ShouldEnterReview());
```

Extract the gate as a pure static beside `<EnterReview>` so it is testable without a scene:

```csharp
    /// Pure gate: only a graded (campaign) run goes to the review corner.
    public static bool ShouldEnterReview() => !TutorialSession.Active;
```

- [ ] **Step 4: Run the suite and confirm the new assertions FAIL**

Expected: 1290 total, 2 failures. If the count reads 1288, the OLD assembly ran — fix the compile first.

- [ ] **Step 5: Wire the gate into the real call site**

```csharp
        if (!ShouldEnterReview())
        {
            // Tutorial Mode: skip quiz -> grade -> BKT -> unlock entirely. A practice
            // run leaves no trace. TutorialCoach.ShowSummary() lands here in Task 12.
            ResetToEntrance();
            return;
        }
```

Confirm `ResetToEntrance()` is the correct existing return-to-picker call in `PharmeeGatekeeper`;
substitute the real one if it differs.

- [ ] **Step 6: Run the suite** → expected **1290/1290 green**, 3 warnings.

- [ ] **Step 7: Checkpoint.**

---

## Task 3: MainMenu button + unlocked picker + no timer

Delivers: press **Tutorial** in the cube room, launch any of the 9 modules, and no stopwatch runs.

A guided *learning* session that still runs a clock feels like an exam — folding the clock suppression
in here keeps all the "what does the flag change about the run's framing" work in one reviewable task.

**Files:**
- Modify: the MainMenu builder, the picker's lock check, and the HUD clock — all found in Step 1
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `TutorialSession.Active` (Task 2).
- Produces: `ExperimentRunner.ClockRuns → bool` (pure gate, used by the HUD).

- [ ] **Step 1: Verify first — find the three touch points**

```bash
grep -rn "MenuCanvas\|Laboratory\|demoEnabled" Assets/PharmaSynth/Scripts/Editor/MenuCubeRoomBuilder.cs | head -20
grep -rn "IsUnlocked\|locked\|ModulePick" Assets/PharmaSynth/Scripts/Progression/ Assets/PharmaSynth/Scripts/NPC/PharmeeGatekeeper.cs | head -20
grep -rn "FreezeClock\|elapsed\|Clock\|timer" Assets/PharmaSynth/Scripts/Experiment/ExperimentRunner.cs Assets/PharmaSynth/Scripts/UI/HudRig.cs | head -25
```

Note whether `MenuCubeRoomBuilder` is idempotent (re-runnable without duplicating buttons) before
adding a fourth. The amber Demo button is the pattern to copy — it is already config-gated.

- [ ] **Step 2: Write the failing assertions**

```csharp
        // tutorial: practice mode ignores the linear chain — every module is playable.
        TutorialSession.Active = true;
        Check("tutorial: all 9 modules selectable regardless of lock state",
              ExperimentCatalog.AllModuleIds().TrueForAll(id => IsSelectable(id)));
        Check("tutorial: no clock runs in practice mode", !ExperimentRunner.ClockRuns);
        TutorialSession.Active = false;
        Check("tutorial: campaign still runs the clock", ExperimentRunner.ClockRuns);
```

Confirm the real catalog accessor name from Step 1 and substitute it for `AllModuleIds()`.

- [ ] **Step 3: Run the suite, confirm 3 failures** (expected total 1293).

- [ ] **Step 4: Add the lock bypass**

At the picker's lock test, prepend:

```csharp
        // Tutorial Mode is practice: the linear chain and period doors do not apply.
        if (TutorialSession.Active) return true;
```

- [ ] **Step 5: Stop the clock**

In `ExperimentRunner`:

```csharp
    /// Practice runs are untimed. A guided learning session that still runs a
    /// stopwatch reads as an exam, which is the opposite of the mode's point.
    public static bool ClockRuns => !TutorialSession.Active;
```

Guard the clock tick with it, and hide the HUD clock element when it is false (do not merely freeze a
visible `00:00` — a frozen clock invites "is this broken?").

- [ ] **Step 6: Add the menu button**

Copy the Demo button block. Label **"Tutorial"**, and on click:

```csharp
        TutorialSession.Active = true;
        DemoSession.Active = false;      // the two modes are mutually exclusive
        // then the same scene-load call the Laboratory button makes
```

The Laboratory button must set `TutorialSession.Active = false` on click, or a returning player carries
the flag into a campaign run.

- [ ] **Step 7: Run the builder menu, then the suite** → expected **1293/1293 green**. One DevCapture of the cube room (the layout *is* the question) — one shot, yaw 0–360 only.

- [ ] **Step 8: Checkpoint.**

---

## Task 4: The target resolver

**The highest-risk task.** This is where authoring gaps surface across all 9 modules.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/Interaction/TaskTargetRegistry.cs` (add `TutorialTargets`)
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `TaskTargetRegistry.Register/Targets/Clear`, `TargetRole`, `TaskTarget` (Task 1).
- Produces: `TutorialTargets.Build()` → void; `TutorialTargets.AuditAgainst(TaskGraph, IEnumerable<ExperimentTask>)`; `TutorialTargets.LastUnresolved` → `List<string>`.

- [ ] **Step 1: Verify first — confirm the accessors**

```bash
grep -n "requiredItemId\|TaskId" Assets/PharmaSynth/Scripts/Interaction/ZoneItemSensor.cs
grep -n "expectedReagents\|class ReagentStep" Assets/PharmaSynth/Scripts/Chemistry/LiquidTaskBinding.cs
grep -n "public.*TaskId\|VaporTaskId\|FermentTaskId" Assets/PharmaSynth/Scripts/Interaction/*.cs
```

Write down the exact field/property names. **Do not guess** — a wrong accessor compiles to a silent
empty sweep, which looks identical to "this module has no targets".

- [ ] **Step 2: Write the failing assertion — the one that matters**

```csharp
        // tutorial: every step in every module must resolve to at least one object,
        // or the player is told to do something with nothing to point at. Wrap-up
        // steps (autoCompleteWhenOthersDone) legitimately have no physical target.
        foreach (var moduleId in ExperimentCatalog.AllModuleIds())
        {
            BuildStageForModule(moduleId);          // same edit-mode path Reveal Stage uses
            TutorialTargets.Build();
            TutorialTargets.AuditAgainst(graph, tasksFor(moduleId));
            Check("tutorial: " + moduleId + " resolves every task to a target",
                  TutorialTargets.LastUnresolved.Count == 0,
                  "unresolved: " + string.Join(", ", TutorialTargets.LastUnresolved));
        }
```

Substitute the real Reveal-Stage entry point for `BuildStageForModule`.

- [ ] **Step 3: Run the suite, confirm 9 failures** (expected total 1302).

- [ ] **Step 4: Implement the sweep**

```csharp
/// Builds the taskId -> objects map by sweeping the live scene once per run.
///
/// Deliberately derived from the components that ACTUALLY COMPLETE each task rather
/// than from an authored per-task list: an authored list can drift from its binding,
/// which is exactly the bug class the W5.34 clueless-player audit was spent on (a hint
/// whose ACTION line contradicted the binding it had to satisfy). Derivation makes that
/// disagreement structurally impossible.
public static class TutorialTargets
{
    public static readonly List<string> LastUnresolved = new List<string>();

    public static void Build()
    {
        TaskTargetRegistry.Clear();

        // --- Pours and scoops: the binding sits on the DESTINATION and names the SOURCE
        //     chemical. ScoopController has no taskId of its own — it completes the step
        //     via AddLiquid on the target vessel, so it is covered here too.
        var bindings = Object.FindObjectsByType<LiquidTaskBinding>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        var vessels = Object.FindObjectsByType<LiquidPhysics>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var b in bindings)
        {
            if (b == null) continue;
            foreach (var step in b.ExpectedSteps())          // exact accessor from Step 1
            {
                if (step == null || string.IsNullOrEmpty(step.taskId)) continue;
                TaskTargetRegistry.Register(step.taskId, b.transform,
                                            TargetRole.Destination, true);
                if (step.reagent == null) continue;
                foreach (var v in vessels)
                    if (v != null && v.currentChemical == step.reagent && v.transform != b.transform)
                        TaskTargetRegistry.Register(step.taskId, v.transform,
                                                    TargetRole.Source, false);
            }
        }

        // --- Verb components: each already owns its taskId.
        RegisterVerb<GrindController>(c => c.TaskId);
        RegisterVerb<StirController>(c => c.TaskId);
        RegisterVerb<WaterBathController>(c => c.TaskId);
        RegisterVerb<IceBathController>(c => c.TaskId);
        RegisterVerb<LitmusStrip>(c => c.TaskId);
        RegisterVerb<FlameTest>(c => c.TaskId);
        RegisterVerb<ZoneSimStation>(c => c.TaskId);
        RegisterVerb<RackTaskGroup>(c => c.TaskId);
        RegisterVerb<WeighStation>(c => c.TaskId);
        RegisterVerb<DryDistill>(c => c.TaskId);
        RegisterVerb<DryDistill>(c => c.VaporTaskId);
        RegisterVerb<FermentationController>(c => c.FermentTaskId);

        // --- Tools a step demands by itemId.
        var items = Object.FindObjectsByType<LabItem>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var sensor in Object.FindObjectsByType<ZoneItemSensor>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sensor == null) continue;
            string need = sensor.RequiredItemId;             // exact accessor from Step 1
            string task = sensor.TaskId;
            if (string.IsNullOrEmpty(need) || string.IsNullOrEmpty(task)) continue;
            foreach (var it in items)
                if (it != null && it.itemId == need)
                    TaskTargetRegistry.Register(task, it.transform, TargetRole.Tool, true);
        }
    }

    private static void RegisterVerb<T>(System.Func<T, string> taskIdOf) where T : Component
    {
        foreach (var c in Object.FindObjectsByType<T>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (c == null) continue;
            string id = taskIdOf(c);
            if (!string.IsNullOrEmpty(id))
                TaskTargetRegistry.Register(id, c.transform, TargetRole.Station, true);
        }
    }

    /// Records which of a module's tasks nothing claimed. A wrap-up step
    /// (autoCompleteWhenOthersDone) legitimately has no physical target and is
    /// excluded; anything else is an authoring gap worth surfacing loudly.
    public static void AuditAgainst(TaskGraph graph, IEnumerable<ExperimentTask> tasks)
    {
        LastUnresolved.Clear();
        foreach (var t in tasks)
        {
            if (t == null || t.autoCompleteWhenOthersDone) continue;
            if (TaskTargetRegistry.Targets(t.taskId).Count == 0)
                LastUnresolved.Add(t.taskId);
        }
    }
}
```

If `expectedReagents` is private, add a read-only `ExpectedSteps()` accessor to `LiquidTaskBinding`
rather than making the field public.

- [ ] **Step 5: Run the suite** → expected **1302/1302 green**.

**If a module reports unresolved taskIds, do not weaken the assertion.** Read each named taskId and
find which component should own it. An unresolved non-wrap-up step is a genuine gap — that is the
whole point of this pin.

- [ ] **Step 6: Add the editor-only warning**

```csharp
#if UNITY_EDITOR
        foreach (var id in TutorialTargets.LastUnresolved)
            Debug.LogWarning("[Tutorial] no target resolves for task '" + id
                             + "' — the player will be told to act with nothing to point at.");
#endif
```

- [ ] **Step 7: Checkpoint.**

---

## Task 5: Hint on the wrist watch

Smallest visible win. No art, no new components.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/UI/WristWatchController.cs:157`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Produces: `WristWatchController.StepText(string label, string hint, bool tutorial) → string` (pure, static).

- [ ] **Step 1: Write the failing assertions**

```csharp
        Check("tutorial: watch shows label only in campaign",
              WristWatchController.StepText("Weigh 2 g", "Use the balance.", false) == "Weigh 2 g");
        Check("tutorial: watch appends the hint in tutorial mode",
              WristWatchController.StepText("Weigh 2 g", "Use the balance.", true)
                  == "Weigh 2 g\n<size=70%>Use the balance.</size>");
        Check("tutorial: watch omits an empty hint cleanly",
              WristWatchController.StepText("Weigh 2 g", "", true) == "Weigh 2 g");
```

- [ ] **Step 2: Run the suite, confirm 3 failures** (expected total 1305).

- [ ] **Step 3: Implement the pure formatter**

```csharp
    /// Pure: what the holo checklist prints for the current step. Tutorial Mode adds
    /// the task's hint beneath the label; campaign keeps hints on the stuck/poke path.
    public static string StepText(string label, string hint, bool tutorial)
    {
        if (!tutorial || string.IsNullOrEmpty(hint)) return label;
        return label + "\n<size=70%>" + hint + "</size>";
    }
```

- [ ] **Step 4: Call it at line 157**

```csharp
        string hint = null;
        foreach (var t in runner.Graph.AvailableTasks())
        {
            current = GlyphSafe.Sanitize(t.label);
            hint = GlyphSafe.Sanitize(t.hint);
            break;
        }
        current = StepText(current, hint, TutorialSession.Active);
```

- [ ] **Step 5: Run the suite** → expected **1305/1305 green**.

- [ ] **Step 6: Checkpoint.** ⚠ Do **not** edit any `hint` string for readability here — see Global Constraints.

---

## Task 6: Glow + revived waypoint

Delivers the visible feature: the next apparatus glows and an arrow points at it.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/Interaction/HoverHighlight.cs`
- Create: `Assets/PharmaSynth/Scripts/Interaction/TutorialHighlighter.cs`
- Modify: `Assets/PharmaSynth/Scripts/Interaction/WaypointGuide.cs`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `TaskTargetRegistry.Targets`, `TargetRole`, `TaskTarget` (T1); `TutorialTargets.Build()` (T4); `TutorialSession.Active` (T2).
- Produces: `HoverHighlight.SetGuide(bool, TargetRole)`; `TutorialHighlighter.ShouldLight(TaskTarget, bool held, bool taskAvailable) → bool` (pure, static).

- [ ] **Step 1: Write the failing assertions for the pure grab rule**

```csharp
        var src  = new TaskTarget { role = TargetRole.Source,      stayLitWhenHeld = false };
        var dest = new TaskTarget { role = TargetRole.Destination, stayLitWhenHeld = true  };

        Check("tutorial: source glows when not held",
              TutorialHighlighter.ShouldLight(src, false, true));
        Check("tutorial: grabbing the source silences it",
              !TutorialHighlighter.ShouldLight(src, true, true));
        Check("tutorial: destination stays lit while held",
              TutorialHighlighter.ShouldLight(dest, true, true));
        Check("tutorial: nothing glows once the step is done",
              !TutorialHighlighter.ShouldLight(dest, false, false));
```

- [ ] **Step 2: Run the suite, confirm 4 failures** (expected total 1309).

- [ ] **Step 3: Split `HoverHighlight`'s single `_lit` flag**

`_lit` is one bool, so a hover-exit would clear a tutorial glow. Two sources, one apply:

```csharp
    private bool _hover, _guide;
    private TargetRole _guideRole;

    private static readonly Color GuideSource      = new Color(1f, 0.72f, 0.20f, 1f); // amber
    private static readonly Color GuideDestination = new Color(0.35f, 1f, 0.45f, 1f); // green

    public bool IsHighlighted => _hover || _guide;

    /// Tutorial guidance channel — independent of hover, so neither clears the other.
    public void SetGuide(bool on, TargetRole role)
    {
        if (_guide == on && _guideRole == role) return;
        _guide = on; _guideRole = role;
        Apply();
    }

    public void SetHighlight(bool on)
    {
        if (_hover == on) return;
        _hover = on;
        Apply();
    }

    private void Apply()
    {
        Cache();
        bool lit = _hover || _guide;
        transform.localScale = HighlightScale(_baseScale, lit, scaleFactor);
        if (_rends == null) return;
        // Guide wins the tint when both are on: the tutorial's "this one" outranks
        // the generic "you're pointing at something" cue.
        Color tint = _guide
            ? (_guideRole == TargetRole.Source ? GuideSource : GuideDestination)
            : glow;
        for (int i = 0; i < _rends.Length; i++)
        {
            if (_rends[i] == null) continue;
            _rends[i].GetPropertyBlock(_mpb);
            Color c = lit ? Color.Lerp(_orig[i], tint, glowMix) : _orig[i];
            if (_hasBase[i]) _mpb.SetColor(BaseColorID, c);
            if (_hasColor[i]) _mpb.SetColor(ColorID, c);
            _rends[i].SetPropertyBlock(_mpb);
        }
    }
```

`OnSelect`'s `SetHighlight(false)` must **not** clear `_guide` — the highlighter owns that channel.

- [ ] **Step 4: Create `TutorialHighlighter`**

```csharp
using System.Collections.Generic;
using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Drives the Tutorial Mode guidance channels off the task graph.
///
/// It NEVER decides that a step is done. It is a pure read of AvailableTasks(); the
/// task completes through its existing binding exactly as in campaign, and the glow
/// follows one poll later. Because no parallel completion detector exists, the
/// guidance cannot disagree with the game.
public class TutorialHighlighter : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private float pollSeconds = 0.2f;      // 5 Hz — per-frame is waste
    [SerializeField] private float regrabDelay = 0.5f;      // dropped-unused flicker guard

    private readonly Dictionary<Transform, TaskTarget> _lit = new Dictionary<Transform, TaskTarget>();
    private readonly Dictionary<Transform, float> _droppedAt = new Dictionary<Transform, float>();
    private float _nextPoll;

    /// Edit-mode seam — Awake does not fire on AddComponent in edit mode.
    public void Bind(ExperimentRunner r) => runner = r;

    /// Pure rule: should this target be glowing right now?
    /// Sources go quiet in hand (message received); destinations and tools stay lit
    /// (holding the right tube does not end the step).
    public static bool ShouldLight(TaskTarget target, bool held, bool taskAvailable)
    {
        if (!taskAvailable) return false;
        return !held || target.stayLitWhenHeld;
    }

    private void Update()
    {
        if (!TutorialSession.Active) { ClearAll(); return; }
        if (Time.time < _nextPoll) return;
        _nextPoll = Time.time + pollSeconds;

        if (runner == null || runner.Graph == null || !runner.IsRunning) { ClearAll(); return; }

        var wanted = new Dictionary<Transform, TaskTarget>();
        foreach (var task in runner.Graph.AvailableTasks())
        {
            // ALL available tasks, not just the first: suppressing a valid parallel
            // branch would teach the wrong procedure. (The beacon still shows one arrow.)
            foreach (var t in TaskTargetRegistry.Targets(task.taskId))
            {
                if (t.transform == null) continue;
                bool held = IsHeld(t.transform);
                if (held) _droppedAt.Remove(t.transform);
                else if (!_lit.ContainsKey(t.transform)
                         && _droppedAt.TryGetValue(t.transform, out float when)
                         && Time.time - when < regrabDelay) continue;
                if (ShouldLight(t, held, true)) wanted[t.transform] = t;
                else if (held) _droppedAt[t.transform] = Time.time;
            }
        }

        foreach (var kv in _lit)                       // leaving the set
            if (kv.Key != null && !wanted.ContainsKey(kv.Key)) SetGuide(kv.Key, false, kv.Value.role);
        foreach (var kv in wanted)                     // entering the set
            if (!_lit.ContainsKey(kv.Key)) SetGuide(kv.Key, true, kv.Value.role);

        _lit.Clear();
        foreach (var kv in wanted) _lit[kv.Key] = kv.Value;
    }

    private static bool IsHeld(Transform t)
    {
        var grab = t.GetComponent<XRGrab>();
        return grab != null && grab.isSelected;
    }

    private static void SetGuide(Transform t, bool on, TargetRole role)
    {
        if (t == null) return;
        var hh = t.GetComponent<HoverHighlight>();
        if (hh != null) hh.SetGuide(on, role);
    }

    private void ClearAll()
    {
        foreach (var kv in _lit) SetGuide(kv.Key, false, kv.Value.role);
        _lit.Clear();
        _droppedAt.Clear();
    }
}
```

- [ ] **Step 5: Revive the waypoint**

In `WaypointGuide.Update()`, gate on the flag and take the **source-first** target:

```csharp
        if (!TutorialSession.Active) { Hide(); return; }
        ...
        var targets = TaskTargetRegistry.Targets(id);
        Transform station = null;
        // Point at the SOURCE first (go fetch it); once it is in hand, hop to the
        // destination. One arrow only — two would be ambiguous.
        foreach (var t in targets)
            if (t.role == TargetRole.Source && t.transform != null
                && !IsHeld(t.transform)) { station = t.transform; break; }
        if (station == null)
            foreach (var t in targets)
                if (t.transform != null) { station = t.transform; break; }
```

Set the beacon's arrow and disc materials to `ZTest Always` so they read through a closed cabinet door.

- [ ] **Step 6: Call `TutorialTargets.Build()` at run start**

Hook it where the stage finishes building for a run (the `ExperimentRunner.StartRun` seam), guarded by
`if (TutorialSession.Active)`. Campaign pays nothing.

- [ ] **Step 7: Run the suite** → expected **1309/1309 green**. Then one DevCapture of a lit target set.

- [ ] **Step 8: Checkpoint.**

---

## Task 7: Skip-step button

Deliberately early: **you will want this during the headset playtest of Tasks 5–6.** A stuck tester who
cannot advance can't evaluate the rest of the mode.

Almost entirely a wrapper — `DemoActions.CompleteCurrentStep(runner)` already exists and does the work.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/UI/WristWatchController.cs`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `DemoActions.CompleteCurrentStep(ExperimentRunner) → string`; `TutorialSession.Active`.
- Produces: `WristWatchController.SkipAllowed(bool tutorial, bool running) → bool` (pure, static).

- [ ] **Step 1: Write the failing assertions**

```csharp
        Check("tutorial: skip offered in practice mode",  WristWatchController.SkipAllowed(true,  true));
        Check("tutorial: skip never offered in campaign", !WristWatchController.SkipAllowed(false, true));
        Check("tutorial: skip hidden when no run is active", !WristWatchController.SkipAllowed(true, false));
```

- [ ] **Step 2: Run the suite, confirm 3 failures** (expected total 1312).

- [ ] **Step 3: Implement**

```csharp
    /// Practice mode lets a stuck player move on. Campaign never does — skipping a
    /// step there would hand out a grade for work not done.
    public static bool SkipAllowed(bool tutorial, bool running) => tutorial && running;
```

Add a "Skip step" button to the holo panel, `SetActive(SkipAllowed(...))` each refresh, wired to:

```csharp
        string skipped = DemoActions.CompleteCurrentStep(runner);
        if (!string.IsNullOrEmpty(skipped)) AudioService.TryPlay("hover");
```

- [ ] **Step 4: Run the suite** → expected **1312/1312 green**.

- [ ] **Step 5: Checkpoint.**

---

## Task 8: Always-on labels

**Do this before Task 13.** Readable labels on every bottle may solve findability outright and make the
x-ray silhouette unnecessary — it is the cheaper answer to the same question, and it teaches the player
to read labels, which x-ray does not.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/UI/ProximityLabel.cs`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Produces: `ProximityLabel.VisibleRadius(float baseRadius, bool tutorial) → float` (pure, static).

- [ ] **Step 1: Verify first — read `ProximityLabel`**

```bash
grep -n "radius\|distance\|SetActive\|Update" Assets/PharmaSynth/Scripts/UI/ProximityLabel.cs
```

Note the field that gates visibility and its default value; substitute the real name below.

- [ ] **Step 2: Write the failing assertions**

```csharp
        Check("tutorial: label radius widens in practice mode",
              ProximityLabel.VisibleRadius(1.5f, true) > ProximityLabel.VisibleRadius(1.5f, false));
        Check("tutorial: campaign radius unchanged",
              Mathf.Approximately(ProximityLabel.VisibleRadius(1.5f, false), 1.5f));
```

- [ ] **Step 3: Run the suite, confirm 2 failures** (expected total 1314).

- [ ] **Step 4: Implement**

```csharp
    /// Practice mode reads labels from across the bench: a student who cannot tell
    /// two bottles apart learns nothing from finding the right one by glow alone.
    /// Campaign keeps the close-range radius so reading a label stays a deliberate act.
    public const float TutorialRadiusMultiplier = 4f;

    public static float VisibleRadius(float baseRadius, bool tutorial)
        => tutorial ? baseRadius * TutorialRadiusMultiplier : baseRadius;
```

Call it wherever the distance test happens, passing `TutorialSession.Active`.

- [ ] **Step 5: Run the suite** → expected **1314/1314 green**. One DevCapture of the reagent cabinets with labels on — check for text soup before accepting the 4× multiplier; tune the constant if it is unreadable.

- [ ] **Step 6: Checkpoint.**

---

## Task 9: Suppress guidance during time-skips + wrap-up steps

Two edge cases that look like bugs if left.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/Interaction/TutorialHighlighter.cs`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Produces: `TutorialHighlighter.GuidanceAllowed(bool running, bool skipping) → bool` (pure, static).

- [ ] **Step 1: Verify first — does `TimeSkipController` expose a fading signal?**

```bash
grep -n "public\|IsFading\|fade" Assets/PharmaSynth/Scripts/Interaction/TimeSkipController.cs
```

If none exists, add `public static bool IsSkipping { get; private set; }` set around the fade. Do not
add an event — a static bool is what the highlighter needs and nothing else reads it.

- [ ] **Step 2: Write the failing assertions**

```csharp
        Check("tutorial: nothing glows during a time-skip fade",
              !TutorialHighlighter.GuidanceAllowed(running: true, skipping: true));
        Check("tutorial: guidance resumes after the fade",
              TutorialHighlighter.GuidanceAllowed(running: true, skipping: false));
```

- [ ] **Step 3: Run the suite, confirm 2 failures** (expected total 1316).

- [ ] **Step 4: Implement**

```csharp
    /// A longProcess step fades the screen to black; a glowing bottle floating on
    /// black reads as a bug. Suppress every channel for the duration.
    public static bool GuidanceAllowed(bool running, bool skipping) => running && !skipping;
```

Call it in `Update()` before building the wanted set; `ClearAll()` when it returns false.

Wrap-up steps need no code — `autoCompleteWhenOthersDone` tasks resolve to zero targets, so `wanted` is
empty and everything clears. **Confirm the beacon hides** rather than stranding on the last object.

- [ ] **Step 5: Run the suite** → expected **1316/1316 green**.

- [ ] **Step 6: Checkpoint.**

---

## Task 10: "What is this / why this one?" on demand

The mode's education payload: teach *why*, not only *where*. `LabInfoDatabase` already holds the copy
and is currently underused.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/UI/HoverInspector.cs`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `LabInfoDatabase` lookup (exact accessor from Step 1); `TutorialSession.Active`.
- Produces: `HoverInspector.InfoFor(string itemId, bool tutorial) → string` (pure, static).

- [ ] **Step 1: Verify first — read the info database's lookup shape**

```bash
grep -n "public\|Lookup\|Get\|entries" Assets/PharmaSynth/Scripts/UI/LabInfoDatabase.cs | head -20
grep -n "public\|Show\|hover" Assets/PharmaSynth/Scripts/UI/HoverInspector.cs | head -20
```

Note the accessor that turns an itemId into descriptive text; substitute the real name below.

- [ ] **Step 2: Write the failing assertions**

```csharp
        Check("tutorial: info shown for a known item in practice mode",
              !string.IsNullOrEmpty(HoverInspector.InfoFor("kit-bunsenburner", true)));
        Check("tutorial: campaign shows no extra info",
              HoverInspector.InfoFor("kit-bunsenburner", false) == "");
        Check("tutorial: unknown item degrades to empty, not an exception",
              HoverInspector.InfoFor("no-such-item", true) == "");
```

- [ ] **Step 3: Run the suite, confirm 3 failures** (expected total 1319).

- [ ] **Step 4: Implement**

```csharp
    /// Practice mode answers "what is this and why this one?" for anything the player
    /// points at. Campaign stays silent — identifying apparatus is part of the
    /// assessment there. Unknown ids degrade to empty rather than throwing: a missing
    /// database entry is a content gap, not a crash.
    public static string InfoFor(string itemId, bool tutorial)
    {
        if (!tutorial || string.IsNullOrEmpty(itemId)) return "";
        string text = LabInfoDatabase.Describe(itemId);      // exact accessor from Step 1
        return string.IsNullOrEmpty(text) ? "" : GlyphSafe.Sanitize(text);
    }
```

Show it in the existing hover panel, passing `TutorialSession.Active`.

- [ ] **Step 5: Run the suite** → expected **1319/1319 green**. Note in the checklist any itemId that returns empty — those are content gaps for a later copy pass, not blockers.

- [ ] **Step 6: Checkpoint.**

---

## Task 11: Stuck-escalation ladder

Catches the player who *has* the glow and still does not know what to do with it.

**Files:**
- Create: `Assets/PharmaSynth/Scripts/UI/TutorialCoach.cs`
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `TutorialSession.Active`; Pharmee's existing speak path (accessor from Step 1).
- Produces: `TutorialCoach.LevelFor(float secondsOnStep) → int` (pure, static; 0 = nothing, 1 = watch nudge, 2 = beacon pulse, 3 = Pharmee speaks).

- [ ] **Step 1: Verify first — find the speak path and the poke suppression**

```bash
grep -rn "SuppressNpcPokes\|Say\|Speak\|NarrationController" Assets/PharmaSynth/Scripts/NPC/NPCNarrationController.cs Assets/PharmaSynth/Scripts/UI/WristWatchController.cs | head -20
```

Note the existing method that makes Pharmee say a line, and respect `SuppressNpcPokes` — the coach must
not talk over a poke that is already playing.

- [ ] **Step 2: Write the failing assertions**

```csharp
        Check("tutorial: no coaching in the first 15 s on a step", TutorialCoach.LevelFor(10f)  == 0);
        Check("tutorial: watch nudge at 15 s",                     TutorialCoach.LevelFor(20f)  == 1);
        Check("tutorial: beacon pulse at 30 s",                    TutorialCoach.LevelFor(40f)  == 2);
        Check("tutorial: Pharmee speaks at 60 s",                  TutorialCoach.LevelFor(90f)  == 3);
        Check("tutorial: coaching does not escalate past 3",       TutorialCoach.LevelFor(600f) == 3);
```

- [ ] **Step 3: Run the suite, confirm 5 failures** (expected total 1324).

- [ ] **Step 4: Implement the pure ladder**

```csharp
/// The stuck ladder. Escalates only while the SAME step stays unsolved, and resets
/// the moment the available task changes — a player working steadily is never nagged.
public class TutorialCoach : MonoBehaviour
{
    public const float NudgeAfter = 15f, PulseAfter = 30f, SpeakAfter = 60f;

    /// Pure: 0 = silent, 1 = watch nudge, 2 = beacon pulses harder, 3 = Pharmee speaks.
    public static int LevelFor(float secondsOnStep)
    {
        if (secondsOnStep >= SpeakAfter) return 3;
        if (secondsOnStep >= PulseAfter) return 2;
        if (secondsOnStep >= NudgeAfter) return 1;
        return 0;
    }
}
```

Drive it from the same `AvailableTasks()` read the highlighter uses: reset the timer whenever the first
available taskId changes. **Reuse existing Pharmee lines for level 3** — new voice lines cost credits
and a regen, so route level 3 to the module's existing stuck/poke pool rather than writing new copy.

- [ ] **Step 5: Run the suite** → expected **1324/1324 green**.

- [ ] **Step 6: Checkpoint.**

---

## Task 12: No-grade run summary

Right now a practice run ends by dumping the player back at the picker with no closure. Counts, not a
percentage — the moment it shows a score it stops being practice.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/UI/TutorialCoach.cs`
- Modify: the review-skip branch from Task 2 Step 5
- Modify: `Assets/PharmaSynth/Scripts/Editor/PharmaSelfTests.cs`

**Interfaces:**
- Consumes: `MistakeLog` (accessor from Step 1); the Task 2 skip branch.
- Produces: `TutorialCoach.SummaryText(int stepsDone, int corrections) → string` (pure, static).

- [ ] **Step 1: Verify first — read `MistakeLog`'s accessors**

```bash
grep -n "public" Assets/PharmaSynth/Scripts/Experiment/MistakeLog.cs | head -20
```

Note how to read the count and the per-mistake descriptions.

- [ ] **Step 2: Write the failing assertions**

```csharp
        Check("tutorial: clean run summary names no corrections",
              TutorialCoach.SummaryText(12, 0) == "Practice complete — 12 steps, no corrections needed.");
        Check("tutorial: summary singularises one correction",
              TutorialCoach.SummaryText(12, 1) == "Practice complete — 12 steps, 1 correction along the way.");
        Check("tutorial: summary pluralises many corrections",
              TutorialCoach.SummaryText(12, 3) == "Practice complete — 12 steps, 3 corrections along the way.");
```

- [ ] **Step 3: Run the suite, confirm 3 failures** (expected total 1327).

- [ ] **Step 4: Implement**

```csharp
    /// Closure without a score. A percentage would turn practice back into an exam,
    /// which is precisely what this mode exists to avoid.
    public static string SummaryText(int stepsDone, int corrections)
    {
        string tail = corrections == 0 ? "no corrections needed."
                    : corrections == 1 ? "1 correction along the way."
                    : corrections + " corrections along the way.";
        return "Practice complete — " + stepsDone + " steps, " + tail;
    }
```

Show it on the HUD dialogue bar in the Task 2 skip branch, before `ResetToEntrance()`, listing each
logged correction beneath the headline.

- [ ] **Step 5: Run the suite** → expected **1327/1327 green**.

- [ ] **Step 6: Checkpoint.**

---

## Task 13: X-ray silhouette (conditional — decide after playtest)

**Do not start until Tasks 1–12 have been played in the headset.** Hint text + glow + through-wall
beacon + always-on labels may already solve "I can't find it". Building this unprompted is the exact
waste the ordering exists to prevent.

**Files:**
- Modify: `Assets/PharmaSynth/Scripts/Interaction/TutorialHighlighter.cs`
- Create: `Assets/PharmaSynth/Art/Materials/TutorialXray.mat`

- [ ] **Step 1: Create the material**

Unlit, `ZTest Greater`, `ZWrite Off`, transparent, render queue 3000+, colour matching the guide tint.
Explicitly **not** a URP Renderer Feature: that needs targets moved onto a dedicated layer, and layers
are load-bearing for XRI interaction masks.

- [ ] **Step 2: Spawn/destroy the silhouette on set-enter/exit**

In `SetGuide`, when `on` is true add a child GameObject carrying a copy of the target's `MeshFilter`
mesh plus the x-ray material; when false, destroy it. If any sizing is needed, measure with
`ExperimentSceneBuilder.SolidWorldBounds` — **never** all child renderers (`LiquidPourer`'s world-space
`StreamLine`/`PourStream` outlive a pour pointing at the floor and drag bounds down a metre).

- [ ] **Step 3: Run the suite** → expected still **1327/1327** (visual only, no new pins).

- [ ] **Step 4: One DevCapture** of a target behind a closed cabinet door.

- [ ] **Step 5: Checkpoint.**

---

## Task 14: Docs

**Files:** `CLAUDE.md`, `Docs/gameplay-flow.md`, `Docs/systems-reference.md`, `Docs/changelog.md`, `Docs/remaining-work-checklist.md`

- [ ] **Step 1: Edit canonical lines in place**

`gameplay-flow.md` — Tutorial Mode as a third entry alongside Campaign and Lab Tour: ungraded,
all-unlocked, untimed, writes no save, skip-step allowed. `systems-reference.md` — document
`TaskTargetRegistry`, `TutorialTargets`, `TutorialHighlighter`, `TutorialCoach`, and **correct the
`WaypointGuide` entry**, which currently implies a live station-based system.

Edit the stale claims; do not append "actually B".

- [ ] **Step 2: One changelog line** — end state only: date · name · one sentence · suite count.

- [ ] **Step 3: Update the CLAUDE.md current-state block and suite count.** Replace, never append. Hard cap ~100 lines.

- [ ] **Step 4: No suite run** — docs-only change, per the efficiency policy.

- [ ] **Step 5: Checkpoint.**

---

## Deliberately excluded

- **Undo the last step.** `TaskGraph` has `Reset()` but no per-task uncomplete, and the graph is the
  easy half — reverting the *world* (vessel contents, temperatures, precipitates) by hand is where
  subtle state bugs breed. **Task 7's skip plus a restart already covers the need**: rebuild the stage,
  `Reset()`, then `CompleteCurrentStep` × N gives a *correct* world state out of calls that already
  exist, instead of a hand-patched one.
- **Spoken per-mistake corrections.** Cheap in code, but every line costs voice credits and a regen,
  and each mistake class needs new copy. Task 11 level 3 reuses the existing pools instead. Revisit as
  a deliberate, budgeted voice pass.
- **Multi-step lookahead preview.** Needs speculative graph evaluation; unclear payoff over one beacon.

## Self-Review

**Spec coverage:** `TutorialSession` → T2. Resolver + rejection of authored targets → T4. Highlighter,
5 Hz diff, never-detects-completion → T6. Glow two-flag split → T6. Beacon revival → T6. X-ray → T13.
Hint on watch → T5. Entry point → T3. Unlocked picker → T3. Ungraded flow → T2. `WeighStation` getter +
registry rename → T1. Edge cases: grab/drop/roles → T6; time-skip → T9; wrap-up → T9; broken target →
T1 (null filtering in `Targets()`); campaign unaffected → T2/T3/T5/T7/T8/T10 pins. Spec suite pins 1–5
→ T2/T3/T4/T5/T6.

**Folded-in additions:** no timer → T3. Always-on labels → T8. Skip step → T7. Info on demand → T10.
Stuck ladder → T11. Run summary → T12. Each carries its own pins; suite walks 1288 → 1327.

**Open spec items resolved:** `TimeSkipController` fade signal → T9 Step 1. Beacon material → T6 Step 5.
`MenuCubeRoomBuilder` idempotency → T3 Step 1.

**Known deliberate gaps:** exact accessor names for `ZoneItemSensor.RequiredItemId`,
`LiquidTaskBinding.ExpectedSteps()`, the `<EnterReview>` call site, the catalog's module-id accessor,
the Reveal-Stage entry point, `ProximityLabel`'s radius field, `LabInfoDatabase.Describe`, the Pharmee
speak path, and `MistakeLog`'s counters are each opened by a **"Verify first"** step in their task
rather than guessed. Guessing them would compile to silent no-ops indistinguishable from real gaps.
