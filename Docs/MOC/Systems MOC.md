# Systems MOC

Up: [[Home]] · Siblings: [[Architecture MOC]] · [[Content MOC]] · [[Process MOC]]

Every runtime mechanic, and where its authoritative description lives. The deep detail
is in [[systems-reference]] — this note is the index into it, plus the cross-cutting
rules that span several systems.

> [!info] Where the truth lives
> **[[systems-reference]]** is authoritative for mechanics. **[[Class Index]]** is
> authoritative for API surface. This note routes you; it does not restate them.

---

## The zone-free tool rule

> [!important] Binding, user directive 2026-07-17
> *"I don't want any zone — the entire lab IS the zone; tools function when brought
> together ANYWHERE."*

No fixed stations, pads, labels or teleport anchors for any step a tool can own.
All station-based modules have been converted; the legacy spawn path survives only in
the synthetic builder test fixture.

What replaced the stations:

| Verb | How it works now | Data hook |
|---|---|---|
| Heat | bench `WaterBath` — pour distilled water in, lit burner beside it, warms nearby vessels, capped 100 °C | `Vessel.heatToC` |
| Chill | `Raw_IceBucket` + `IceBathController` | `Vessel.chillToC` / `chillTaskId` |
| Litmus | touch a strip to the mixture; pH from `LiquidPhysics.CurrentPH` | `Vessel.litmusTaskId` |
| Flame confirm | hold a lit match/burner to the served sample | `Vessel.flameTaskId` |
| Stir | circle any bench glass rod in the vessel (tip-tracked, anchor-free) | `Vessel.stirTaskId` |
| Filter | the funnel pour itself | `LiquidPassthrough` |

---

## Systems by area

### Liquids and chemistry
`Chemistry/` — [[systems-reference]] §1

`LiquidPhysics` (wake-from-empty contract, the `VesselLedger` "story" of what went
in), `LiquidPourer` (tilt >45°, downward raycast, sphere-cast pour assist),
`ReactionRule`/`Registry`, `TemperatureSim`, gas/crystallise/filter controllers,
`HazardousMix`, `ReagentSupplyMonitor` (finite reagents → starvation → restart offer).

> [!warning] Measure vessels with `ExperimentSceneBuilder.SolidWorldBounds`
> Never `GetComponentsInChildren<Renderer>()`. `LiquidPourer`'s **world-space**
> stream objects outlive the pour and keep pointing at the floor — encapsulating
> them dragged bounds down a metre and launched racked tubes into the air.

### Tool verbs
`Interaction/` — [[systems-reference]] §2

Stir, grind, weigh, scoop, clean, match/burner, apparatus assembly. Each is a pure
maths core plus a controller. All verb controllers re-`Register()` their TaskGraph
conditions on `ExperimentStarted` **and** on re-enable, so retries work.

> [!important] The verb contract
> *The number in the manuscript instruction IS the action count, whatever its unit.*
> "5 drops" → 5 squeezes. "2 ml" → 2 squeezes. "0.5 g" → 5 spatula dips. Bulk ml →
> tilt-pour band. The wrist panel prints the real quantity as a fact and the action
> underneath. Do not collapse drops and millilitres into one rule.

### Physics, breakage, respawn
`Interaction/` — [[systems-reference]] §3

`Mishandling` (thin glass only), `BreakableGlassware` (7.0 m/s threshold — bench-height
drops always survive), `DropRespawn` (settle-freeze, floor-only reclaim, refills
supply on respawn), `PhysicsProfiles`/`RealSizes`/`GrabTuning`.

> [!warning] A broken tube respawns at its **baked** home
> Positions must be correct *before* re-homing. Order: `Name Tubes + Build Rack Slots`
> → drag the green `Slot_*` gizmos → `Re-Home Scene Items (Adopt Current)`.

### Feedback
`Interaction/` + `UI/` — [[systems-reference]] §4

`VesselStatus` (live contents tag, self-binds in `Awake`), `MixFeedback` (floating
popups), `StationStatusLabel`, `HoverInspector` → `LabInfoDatabase`.

> [!warning] `GlyphSafe.Sanitize` every new TMP string
> LiberationSans lacks ☑ ▶ → Δ ↑ °. And transparent geometry does not z-write, so
> text visibility is **sortingOrder**: HUD 30000, bubble 29000, world panels
> 4000–5000, TMP labels 20000.

### NPCs and dialogue
`NPC/` — [[systems-reference]] §5

`GatekeeperModel` (the flow FSM), `PharmeeGatekeeper`, `PharmeeBrain`, `PharmeeLines`
(all pools), `NPCNarrationController`, `ExaminerNPC` (Dr. Jimenez), `ProctorRoamer`,
`LabTourGuide`, `CutsceneDirector`.

> [!danger] Two NPCs, one room
> Pharmee and Jimenez own **separate** narration controllers with no shared timeline.
> Three rules keep them from talking over each other and **all three are
> load-bearing**: `PharmeeBrain.Speak()` returns early during `ReviewFlowActive`; the
> static speaking floor `NPCNarrationController.FloorBusy`; and beats scheduled off
> `SecondsFor(line, dwell)`, never a fixed dwell.

> [!bug] The line-truncation traps
> TMP will not rebuild `textInfo` on an **inactive** GameObject — activate, *then*
> write text, *then* `ForceMeshUpdate`. Five separate traps here caused the
> long-standing "text doesn't get completely written" report. → [[systems-reference]] §5

### UI
`UI/` — [[systems-reference]] §4, §5

`HudRig`/`HudDialogueBar`, `ChoicePanel`, `ScreenFader`, `GradeScreenController`,
`PostLabController` (quiz; opening freezes the clock), **`WristWatchController`** —
the holo checklist, which *is* the procedures panel — `HoloScroller`, `SettingsService`.

### Progression and scoring
`Progression/`, `Scoring/` — [[gameplay-flow]], [[Architecture MOC]]

`ProgressionService` (JSON save), `ExperimentCatalog`, `ProgressionFlow`, `GameFlow`,
`ResultRecorder`, `UnlockDiff`, `DemoMode`/`DemoSession`, `MasteryModel`,
`ScoreCalculator`, `ExperimentGrader`.

### Safety
`Safety/` — `PPESetModel`/`PPEController` (coat + goggles + gloves at the locker),
`FumeHoodZone`, `HazardZone`.

### Audio and voice
`Audio/` — `AudioService`, `SoundBank`.

> [!warning] Voice regeneration is a cost
> **76 of 81 task hints are voiced, and 299 NPC lines.** Editing a hint's ACTION line
> or any dialogue copy invalidates the `VoiceBank` text-hash and costs a regeneration
> pass. `LabTourGuide`/gate lines also keep a `[SerializeField]` copy **in the scene** —
> run `Voice ▸ Sync Gate Dialogue to Code` after any code-side copy edit or the stale
> scene line keeps playing.
>
> Pharmee's robot character is a **runtime filter** (`RobotVoiceFx` +
> `RobotVoiceProfile.asset`), not baked into the clips — so retuning his voice is free.

### Tutorial Mode
`Tutorial/` — [[gameplay-flow]] §14, [[systems-reference]]

A third top-level mode: all 9 experiments unlocked, heavily guided, **ungraded** — no
quiz, grade, BKT, unlock, save or clock. `TutorialSession` flag, `TaskTargetRegistry`
+ `TutorialTargets`, `TutorialHighlighter` (5 Hz diff), `TutorialCoach`, `WaypointGuide`.

Design record: [[2026-08-07-tutorial-mode-design]] (spec) and
[[2026-08-07-tutorial-mode]] (plan).

> [!important] Targets are DERIVED, never authored
> The taskId→objects map is swept from the components that actually complete each
> step. An authored list is exactly what drifted before. Verify with
> `Tools ▸ PharmaSynth ▸ Audit Tutorial Targets` (9/9 modules, 81/81 steps).

> [!bug] URP's stock Unlit declares no `_ZTest`
> `SetInt("_ZTest", …)` on it is a **silent no-op** — the material inspects as
> configured and shows through nothing. Hence `Art/Shaders/PharmaGuide.shader`.
> Verify by grepping the `.mat` for `_ZTest`.

---

## Cross-cutting

- **Full API surface** → [[Class Index]]
- **Every builder menu** → [[Editor Menus]]
- **Scene anatomy** → [[The Lab Scene]] · [[Scene Objects]]
- **Traps that have cost real time** → [[Gotchas]]
