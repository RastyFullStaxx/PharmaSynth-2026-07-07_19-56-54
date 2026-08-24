# The Lab Scene

Up: [[Home]] · [[Architecture MOC]] · Generated companion: [[Scene Objects]]

There are **two scenes**, and that is the whole game.

| Build index | Scene | Role |
|---|---|---|
| 0 | `Assets/Scenes/MainMenu.unity` | the cube spawn room |
| 1 | `Assets/Scenes/SampleScene.unity` | the laboratory — everything else |

Experiments are **data**, not scenes — one lab, nine modules built into it at run
time. → [[Architecture MOC]]

> [!tip] Switching between them in the editor
> There is no view toggle: double-click the scene asset in the Project window, or
> File ▸ Open Scene. Opening one replaces the other. **Whichever scene is open is
> where Play starts** — and the self-test suite's scene pins only pass with
> SampleScene open. → [[Build and Test Loop]]

---

## SampleScene — key objects

### Systems
- **`ExperimentSystems`** — the runner, launcher, scene builder, supply monitor and
  result recorder all live here
- **`Services`** — audio, settings, progression
- **`ScreenFader`** — composing callbacks; every teleport and scene change goes
  through it
- **`HudRig`** — the wrist/HUD canvas rig

### The player
- **`XR Origin (XR Rig)`** — prefab instance (same prefab as MainMenu), overridden
  here with CharacterController **r = 0.25**, step 0.25, plus `HeadCollisionPushback`
  (which exists **only in this scene**)
- **`FrontDoorSpawn`** — the entrance teleport marker at the lab's front door. Both
  the initial menu→lab entry *and* HUD Restart route through it, so "walk in from the
  cube room" and "press Restart" land identically.

### The gate
- **`RobotNPC`** — Pharmee plus `PharmeeGatekeeper`, parked at the corridor corner
- **`LabDoorController`** + door triggers — the door Pharmee guards
- **`PPELocker`** — coat, goggles, gloves

### The review corner
- **`PostLabTablet`** — the 3-question quiz
- **`GradeScreen`** — floored %, PASSED / TRY AGAIN

### The lab floor
- **`MethaneStage`** — the 4 methane-only staged props
- **`DynamicStage`** — where `ExperimentSceneBuilder` builds the running module
- **`ReagentCabinets` (east)** — **THE reagent home**: all 57 `Raw_*` bottles,
  2 units × 5 shelves × 7 slots = 70 places
- **`ReagentShelf` (west)** — now **empty**; its 17 oversized `Reagent_*` bottles were
  consolidated east on 2026-07-16
- **`WorkspaceShelf`** — gantry platforms
- **`WorldLabels`**

Full enumeration, generated from the scene files: [[Scene Objects]].

---

## Two rules about what is in the room

> [!danger] Everything is out, always
> All general tools, apparatus and raw reagents are present across **every**
> experiment. Never hide, remove, declutter or per-experiment-gate them. A real lab
> keeps every instrument out; the player is meant to **choose** the right one. Reduce
> confusion with labels, hints and highlights — never by removing items.

**The three narrow exceptions:**

1. The finished **end products** are demo-only (`EndProductVisibility`, gated
   per-experiment) → [[Content MOC]]
2. The **4 methane-only staged props** — `Prop_reagent-jar`, `Prop_glass-tube`,
   `Prop_collection-tube`, `Prop_burner` — appear only in the editor, the Lab Tour and
   the methane tutorial (`MethaneStageVisibility`)
3. **VR-inappropriate support items deleted from the scene** (`Remove VR-Inappropriate
   Apparatus`): iron stand, clamps, aspirator, condenser, thermometer, iron ring ×2,
   clay triangle, crucible, crucible tongs, alcohol burner, 8 empty vials, forceps.
   Pure scaffolding and unused items that no experiment touches. **Prefabs are kept in
   folders; do not re-add the removed set.**

**Deliberately kept:** tripod + wire gauze (the heat platform), the scoopula (2 g per
dip — distinct from the 0.1 g porcelain spatula), and all used glassware including
some deliberate spares.

> [!danger] The bench already exists — a layout must never stage it
> Vessels *bind* to bench objects via `Vessel.benchItem`. This one cost a 46-object
> duplication. → [[Gotchas]]

---

## Glassware inventory

- `Kit_TestTube_0-18` — **19 regular tubes**. Exp 2's worst case drives the count;
  every other module needs ≤ 4.
- `Kit_Hard-GlassTestTube_0-3` — **4 hard-glass tubes**. The *only* naked-flame tube
  in any experiment is Exp 6's dry distillation; everything else, including Exp 2's
  boil, is a ≤ 100 °C water bath.

Two rack roles share `itemId kit-testtuberack` — **storage** racks (tubes live and
home there, **no slots**) and **workspace holders** (start empty, get `Slot_0-5`
anchors). Runtime snap: `TubeRackSlots` seats any tube released within 0.15 m of a
free green slot. → [[Gotchas]]

---

## MainMenu — the cube room

The cube spawn room: **Laboratory / Tutorial / Settings / Quit**, plus an amber Demo
button when config-enabled. `MenuCanvas` plus `MenuRoomFx` (breathing neon, drifting
lights, arc stutter, haze, motes).

It has its **own** `XR Origin (XR Rig)` instance of the same prefab — with no
`HeadCollisionPushback`, and a different position override.

Choosing Laboratory fades into SampleScene at `FrontDoorSpawn`, where Pharmee greets
the player. → [[gameplay-flow]]

---

## Rig height

One eye height everywhere: **1.45 m**, fixed for every player (`SpawnHeightWiring`).
Not relative to real height — see [[Gotchas]] for why every relative scheme failed.

Wire it per scene with `Tools ▸ PharmaSynth ▸ Wire Spawn Height`.
