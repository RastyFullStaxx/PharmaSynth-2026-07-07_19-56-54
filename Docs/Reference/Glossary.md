# Glossary

Up: [[Home]]

Project vocabulary, in the sense this project uses it. Terms that mean something
*different* here than in general Unity practice are marked ⚠.

---

## Flow and progression

**Gate / gatekeeper** — Pharmee guarding the lab door. The flow's state machine is the
pure `GatekeeperModel`; `PharmeeGatekeeper` only drives it. → [[gameplay-flow]]

**Period** — Prelim / Midterm / Finals. The first level of the two-step experiment
picker.

**Module** — one experiment, identified by `moduleId` (e.g. `midterm-acetone`). The
second level of the picker. Nine of them. → [[Content MOC]]

**Two-part gate** — a module passes only when **rubric ≥ 90%** *and* **BKT mastery
≥ 0.90**. Both, independently.

**BKT** — Bayesian Knowledge Tracing. `MasteryModel` estimates P(learned) per
`LabSkill`. ⚠ A module's `trackedSkills` must be its *signature* skills, or the gate
becomes unwinnable on flawless runs.

**Armed** — the stage is built and the door is open, but the clock is **held**.
Crossing the threshold starts the timer. `PrepareExperiment` + `StartRun` is the armed
seam.

**Review corner** — where the player is teleported after chemical tests complete.
Jimenez briefs, the quiz tablet opens, the clock is frozen.

**Abort** — HUD Restart. Kills an un-graded run and fully resets.

**Demo session** — separate save, everything unlocked, skip buttons, infinite supply,
end products visible.

**Tutorial Mode** — a third top-level mode. All 9 modules unlocked, heavily guided,
**ungraded**: no quiz, grade, BKT, unlock, save or clock. Distinct from the
*tutorial-methane* experiment. ⚠ Easy to confuse — they are unrelated features.

**Lab Tour** — the ungraded guided walkthrough of the room, offered at the door.

---

## Content

**Manuscript** — the client's document. **Appendix C is the only chemistry
authority.** → [[Content MOC]]

**Storyboard** — a client reference to **exceed**, never a chemistry source.

**ILO** — Intended Learning Outcome. Verbatim from Appendix C; surfaces on the wrist
board, in the intro cutscene and in Jimenez's pre-quiz recap.

**End product** — the thing an experiment synthesises. ⚠ Hidden **per-experiment**,
not globally: only the *running* module's own product is hidden, because several
products are also reagents for other modules.

**Errata** — a known, deliberate divergence from the manuscript. All tabulated in the
[[experiments-reference]] header.

---

## The lab

**Bench item** — an object that **already exists in the scene**. A layout sets
`Vessel.benchItem` to bind to it rather than spawning a duplicate. ⚠ This is the rule
that a 46-object duplication was caused by breaking. → [[Gotchas]]

**Stage** — what `ExperimentSceneBuilder` builds into `DynamicStage` for the running
module. May contain **only task-bound vessels**.

**Zone-free** — the binding rule that the entire lab is the interaction zone. No fixed
stations or pads; tools work wherever they are brought together. → [[Systems MOC]]

**Verb** — a physical action that completes a task: stir, grind, weigh, scoop, clean,
pour, strike, heat, chill, litmus, flame.

**The verb contract** — *the number in the manuscript instruction IS the action
count*, whatever its unit. "5 drops" → 5 squeezes; "0.5 g" → 5 spatula dips.

**Storage rack vs workspace holder** — two roles sharing `itemId kit-testtuberack`.
Storage racks are where tubes live (no slots). Workspace holders start empty and get
`Slot_0-5` anchors.

**Ledger** — `VesselLedger`, the running story of what was poured into a vessel
("Ethanol 120 ml + NaOH 50 ml"). Drives hover cards and popups.

**Wake-from-empty** — `LiquidPhysics`' contract: vessels default to 0 ml and *adopt*
the first chemical poured in.

**Settle-freeze** — a released rigidbody at rest for 2.5 s goes kinematic in place.

**Re-home** — adopt current hand-placed transforms as the respawn homes.
⚠ Adopt the *current* placement; never re-apply a transform from git.

---

## Code conventions

**Pure core** — a plain C# class holding the maths and decisions, with no Unity
lifecycle, callable from an edit-mode test. Every mechanic has one.
→ [[Architecture MOC]]

**Thin MonoBehaviour** — the driver that reads the scene, calls the pure core and
applies the result. Mandatory pattern.

**`Bind()` seam** — the explicit wiring method every component exposes, because
edit-mode `AddComponent` fires no `Awake`/`OnEnable`. ⚠ In *Play* mode it fires
`OnEnable` immediately, before `Bind()` — so event subscription belongs in `Bind`.

**Scene pin** — a suite assertion against a real scene object (`bench:`, `match:`,
`wired:`, `verbwire:`, `simrun:`). ⚠ Pure math cannot see a missing component; only
scene pins can. → [[Gotchas]]

**Builder** — an idempotent, re-runnable editor menu item. All scene edits go through
one. ~108 exist. → [[Editor Menus]]

**Suite** — `PharmaSelfTests`, ~1,350 edit-mode assertions.
→ [[Build and Test Loop]]

---

## Tooling

**MCP** — the Unity MCP bridge (official Assistant server). Requires a **Unity AI
seat**. ⚠ "Connection revoked" usually means the seat lapsed, not that approval was
withdrawn. A speed layer, not a capability layer.

**DevCapture** — the working screenshot tool (`Unity_Camera_Capture` is broken).
⚠ Yaw **0–360 only**; negative values misparse.

**Autorun request file** — the MCP-down fallback:
`Temp/selftest-autorun-request.txt` (suite) or `Logs/menu-autorun-request.txt` (any
menu). ⚠ `Logs/`, not `Temp/`, for menus — Unity wipes `Temp`.

**Headset Play Mode** — the toggle that drives a Quest-Link headset from editor Play.
⚠ Never trust a doc for its current state; read
`Assets/XR/XRGeneralSettingsPerBuildTarget.asset`.

---

## Week labels

**W1 … W5.35** — development batches. History is in [[changelog]], one line per batch.
Referenced constantly in code comments (e.g. "W5.12 pour assist") as a date shorthand.
