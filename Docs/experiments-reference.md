# Experiments Reference — the 11 PharmaSynth modules

**Generated 2026-07-12 (W5.11) from the live ScriptableObject data + manuscript Appendix C.** This is the canonical per-experiment reference: open it whenever a task touches an experiment's chemistry, steps, reagents, tests, quiz or stage layout. Regenerate the data body after content changes with the extraction script pattern (see `Docs/systems-reference.md` §Authoring) or update by hand and keep the suite's ContentSuite counts in sync.

**Authority:** the client manuscript (`Docs/Documentations/manuscript.pdf`, extract via `pdftotext -layout` → `manuscript.txt`; the Read tool cannot open PDFs on this machine). Appendix C (Experiments 1–9) is the lab manual. The storyboard is a reference to EXCEED — never a source for chemistry or labels. Evidence trail for every deviation: `Docs/manuscript-reconciliation.md`.

## Deviations & client flags (authoritative summary)

| Topic | Manuscript says | Game does | Status |
|---|---|---|---|
| Benzoic Acid route | Exp 4 "procedure" is a verbatim copy-paste of the ethanol fermentation (confirmed print defect); materials list names Benzaldehyde | Benzaldehyde + KMnO₄ oxidation | ✅ correct-by-intent; client rubber-stamp pending (§7 signoff) |
| Acetanilide acylating agent | Appendix C procedure: acetyl chloride (intro prose wrongly says anhydride) | Acetyl chloride | ✅ matches; optional safer-anhydride swap = client call |
| Benzamide nitrous test | Reagent header "sodium nitrate" (typo); procedure body: 10% sodium NITRITE | Sodium Nitrite | ✅ matches |
| Chemical Compounding battery | Multi-substrate ID lab: FeCl₃ enol, KMnO₄ rate-of-oxidation (3 butyl alcohols + control), Tollen's, ester formation ×2, aspirin hydrolysis | ~~Single-substrate ethanol (combustion/sodium/bromine/KMnO₄)~~ → **REBUILT to the manuscript 2026-07-15** (13-task graph, ILOs restored verbatim, quiz realigned) | ✅ **client chose "rebuild to manuscript"** — the old battery failed its own ILO ("differentiate tests for *different* compounds") and used sodium/bromine, which the manuscript never lists. Layout/bindings still to author. |
| Wine fruit | Grapes EXCLUDED (L3830-31) | Ferments **Mixed Fruit Juice** (renamed from Grape Juice, W5.9) | ✅ fixed |
| Chloroform oxidation test | Dichromate + conc H₂SO₄ (procedure + results sheet) | Added W5.9: `test-oxidation` + rule | ✅ fixed |
| **Methane** | Not in Appendix C — appears only as a molecular-weight worked example in Exp 1 (Stoichiometry) | Game-authored tutorial | ✅ **CLIENT-CONFIRMED 2026-07-16: methane REMAINS, as the tutorial-only experiment.** Being game-authored is NOT grounds to remove it — do not lump it with the dropped aspirin/caffeine modules. |
| **Aspirin / Caffeine** | Not in Appendix C (Aspirin named in intro prose only; Caffeine absent entirely) | ~~Game-authored modules~~ → **DROPPED 2026-07-16** | ✅ **Absent from the client's grouping → both removed; chain 11 → 9.** Aspirin survives as a **raw reagent** (Exp 2 §D hydrolyses it). |
| **Apparatus lists vs procedures** | Exp 3–8 repeat ONE identical boilerplate list; **Exp 2's list omits the water bath** it uses 3×; **no list mentions the bent/delivery tube** Exp 3 requires | Stage from the PROCEDURE, not the list | ⚠ list defects — see §Apparatus; harmless in-game (all tools are always present) but do NOT infer steps from the lists |
| Wine rubric | Bespoke tasting/presentation rubric (group activity) | Standard 6-category rubric | ✅ client-resolved 2026-07-09 |
| Yield | Data-sheet records yield | Record-only, never graded (±5 stepper on the quiz tablet) | ✅ client-resolved |

## How to read each section
- **Task graph** = the module's `graphTasks` in authored (play) order; `phase` drives the loop (ReagentPrep → Synthesis → ChemicalTests → DataSheet); prerequisites gate order (violation = WrongStep mistake); hints surface in the wrist checklist.
- **Stage layout** = what `ExperimentSceneBuilder` spawns on the center table from `Layout_*.asset`. Station sims: Heat/Crystallise/Filter/Collect run while the required prop occupies the zone; Stir/Grind/Weigh are tool verbs (W5.8); zone-touch completes on prop contact. Every stage ALSO auto-spawns: a test-tube RackKit (6 tubes), 2 spare beakers + 1 spare flask, and (Heat modules) 2 matches + a striker.
- **Vessel bindings** = pour ≥N ml of the named reagent into that vessel to complete the task (works held or on the table). `completesTask:false` = the pour is expected but the WEIGH station completes the task.
- **Reactions** = MasterReactionRegistry rules that can fire from this stage's chemical set; `expectedObservation` pops as floating text on reaction (W5.8 feedback layer).
- **Manuscript lists** are OCR-extracted verbatim; minor column-merge artifacts (e.g. "bath Rubber tubing" = "water bath" + "rubber tubing") are the PDF's two-column layout, kept as-is for fidelity. **These lists are the source for apparatus grouping/kits** (queued work, checklist §13a).

**Wine Making (Exp 9) materials note:** the manuscript frames it as a real-world group activity — 250–500 mL of a **non-grape** fruit juice, sugar, yeast, sealed fermentation vessel + airlock, ~1 week ferment, video documentation. Adapted for solo VR per client resolution (standard rubric); see reconciliation §4.

## Apparatus — what each tool is actually FOR

**Why this exists:** the manuscript lists apparatus in one block per experiment and then **almost never names a tool in the procedure**. It says *"place 1 ml of ethanol"* — never *with what*. So the lists alone don't tell you what a step needs. This table maps every listed tool to the step that actually uses it (inferred from the procedure verbs), so a session knows what to stage and bind.

### ⚠ The lists are UNRELIABLE — the PROCEDURE is the source of truth
1. **Exp 3–8 all share ONE identical boilerplate list** (the same 19 items, verbatim). It is generic, not tailored — Exp 5 does no distillation yet still lists a condenser + distilling flask. **Never infer a step from the list.**
2. **Exp 2's list omits the WATER BATH** — yet its procedure calls for one **three times** (`:2185` heat Tollen's 5 min, `:2192` warm the ester, `:2203` boil the aspirin). A genuine list defect, same class as the Exp 4 copy-paste.
3. **No list anywhere mentions the bent/delivery tube** — yet Exp 3's procedure requires it (`:2430` *"attach a bent tube, fitted to a stopper"*). Equipment used but never listed.
4. **Exp 9 (Wine) has no apparatus list at all** — it is a take-home group activity, not bench work.

### Tool → real use
| Apparatus | What the procedure uses it for (the manual rarely says so) | Experiments |
|---|---|---|
| **Pipette** | EVERY *"place N ml"* / *"add N drops"* of a **liquid**. Never named in a single procedure step. | 2–8 |
| **Porcelain spatula** | EVERY *"place N grams"* of a **solid** (0.5 g aspirin, 0.1 g salicylic acid, 4 g yeast) | 2–8 |
| **Test tube** | the vessel for every discrete test | 2–8 |
| **Test tube holder** | gripping a tube that has been in the water bath | 2–8 |
| **Test tube brush** | washing tubes between tests → feeds the **Sanitation** rubric criterion | 2–8 |
| **Beaker** | bulk liquid, the dilution water, receiving a pour (Exp 2 `C.I.d`: *"pouring the beaker containing 1 ml of distilled water"*) | 2–8 |
| **Erlenmeyer flask** | the larger reaction/fermentation vessel (Exp 3: *"prepare 100 ml of 12% brown sugar solution in a flask"*) | 2–8 |
| **Stirring rod** | mixing / shaking | 2–8 |
| **Water bath** | EVERY *"heat / warm / boil … using a water bath"* | **2** (×3, unlisted!), 3–8 |
| **Aspirator** | suction for the *"filter off"* steps | 2 (`D.I.c`), 3–8 |
| **Watch glass** | the **combustion test** (*"pour 1 ml of the distillate into a watch glass and apply a lighted matchstick"*); evaporation | 3–8 |
| **Thermometer** | the distillation cut-off temperature (ethanol ~78 °C, acetone 56 °C) | 3–8 |
| **Distilling flask** | the distillation pot | 3–8 |
| **Condenser** | condensing the distillate | 3–8 |
| **Cork / stopper** | stoppers the flask and **carries the bent/delivery tube** (Exp 3 `:2430`) | 3–8 |
| **Rubber tubing** | condenser water lines | 3–8 |
| **Iron stand · Iron clamp · S-clamp** | holds the distillation train / condenser | 3–8 |
| **Bent (delivery) tube** | **UNLISTED but required** — bubbles evolved CO₂ into limewater (Exp 3 `:2430-2434`), with a cotton swab capping the receiving tube | **3 only** |
| **Water** | distilled water — effectively a reagent, not a tool | 2–8 |

### Manuscript item → game reality (STATUS FLAGS — review as each module is built)

**Nothing here is removed.** Every tool stays on the bench (⛔ all-tools rule). This flags which canonical items currently do a **real job** vs which are **decorative** — so as we build each procedure we consciously either give them a job or accept them as set dressing.

| Legend | Meaning |
|---|---|
| ✅ **Functional** | a verb/mechanic actually uses it |
| 🔁 **Replaced / consolidated** | the prop was swapped or deleted; another prop or mechanic serves it |
| 📋 **Served by a feature** | prop stays, but its FUNCTION is now a game feature |
| ⬜ **Decorative (FLAGGED)** | present + grabbable, but no mechanic uses it **yet** — candidate for a job |

| Manuscript item | Game reality | Status |
|---|---|---|
| **Pipette** | The glass pipette prop was **deleted** (`RemovePipette`, 2026-07-15) as redundant with the pack's **MechanicalPipette** (modern micropipette) which remains. Its FUNCTION — *"place 1 ml"* — is served by the **pour mechanic** (`LiquidPourer` + `requiredMl` threshold bindings): you tip the vessel and pour. | 🔁 Replaced |
| **Thermometer** | Prop **kept** on the bench. Its FUNCTION — temperature monitoring — is served by **`ProcessReadout.BindHeat`**: "62 C -> 120 C" floats over the heated vessel, tinting cool-blue → hot-orange, plus the red-hot glow. The user can monitor without holding it. | 📋 Served by a feature (prop currently decorative) |
| **Wooden splint** | **Deleted** — appears NOWHERE in the manuscript, which uses a *"lighted matchstick"*. `Matchstick` runs the gas/combustion test. | 🔁 Replaced |
| **S-clamp · Iron clamp** | Consolidated into the game's **UtilityClamp** (3-prong, W5.12) alongside **RetortStand** + **IronRing**. | 🔁 Consolidated |
| **Iron stand** | **RetortStand** | 🔁 Renamed |
| **Water bath** | **WaterBath** prop + `StationSim.Heat` + `TemperatureSim` — every "heat/warm/boil using a water bath". | ✅ Functional |
| **Porcelain spatula** | **`ScoopController`** — dip-and-deposit, 2 g per scoop. | ✅ Functional |
| **Stirring rod** | **`StirController`** (OrbitMath circular motion). ⚠ needs a tip anchor — see below. | ✅ Functional |
| **Test tube brush** | **`BrushController` + `CleanableVessel`** — real swipe-by-swipe scrubbing; feeds the **Sanitation** rubric. | ✅ Functional |
| **Beaker · Erlenmeyer · Test tube · Distilling flask (as a vessel)** | `LiquidPhysics` vessels — pour targets with fill visuals. | ✅ Functional |
| **Aspirator** | Present + grabbable. Suction filtration is served by **`FiltrationController` / `StationSim.Filter`**; the aspirator prop itself has no verb. | ⬜ Decorative (FLAGGED) |
| **Condenser · Rubber tubing** | Present. **Distillation is modelled as Heat + Collect** (there is no distil sim), so nothing binds to them. | ⬜ Decorative (FLAGGED) |
| **Cork / rubber stopper** | Present. Setup steps complete by **proximity/contents**, so a stopper is never mechanically required. | ⬜ Decorative (FLAGGED) |
| **Test tube holder** | Present. Grabbing works directly and nothing is hot-to-touch, so it has no job. | ⬜ Decorative (FLAGGED) |
| **Watch glass** | Present. The manual's combustion test pours onto it — **could** bind as a receiving vessel when we build Exp 3. | ⬜ Decorative (FLAGGED — give it a job in Exp 3) |
| **Bent / delivery tube** | Present (×3). **Required by Exp 3** (CO₂ → limewater) but not yet bound to anything. | ⬜ Decorative (FLAGGED — job comes in Exp 3) |

### Placement anchors — how the user hand-tunes positions

**Why:** bounds-guessing cannot know an imported mesh's axis convention — we could not tell which end of a match is the head or which end of a spatula is the blade, and guessing wasted hours. So the code reads a **`PlacementAnchor`** child that the user **drags in the editor** to the exact spot. Menu **`Add Placement Anchors`** creates them at a best guess (orange gizmo); drag, then **`Lock My Layout`** to bake. Re-running never moves one you have already placed.

| Anchor | Lives on | Supplies | Purpose |
|---|---|---|---|
| `FlameAnchor` | Matchstick · BurnerController | position | exactly where the flame appears (match **head**, burner **mouth**) |
| `ScoopAnchor` | ScoopController (scoopula/spatula) | position | the **blade/bowl** — the pickup probe AND the carried heap ride it |
| `BowlAnchor` | GrindController (mortar) | position | where the ground powder + grinding dust appear |
| `PowderAnchor` | solid receivers (hard-glass tube) | position **+ SIZE** | drag it AND **scale it** — the scale is the powder's size at a full charge |

**⚠ The rule that bit twice:** only an anchor with **`previewsScale = true`** supplies SIZE. A position-only anchor has scale **1**, so sizing from it produces a **1-metre blob** (this caused both the giant gizmos and the giant mortar mound). The flag is the discriminator — suite-pinned.

**✅ LIQUIDS NEED NO ANCHOR** (confirmed with the user): liquid/gas fills auto-fit the vessel's **own interior** via `LocalMeshBounds` / `LongestAxis` / `BoreOf` — they follow the bore in the vessel's LOCAL frame, so they stay contained and aligned even when the vessel is tilted or held. Only **solids** (a mound's resting spot) and **point effects** (a flame's tip) need anchors, because those cannot be derived from bounds.

**Anchor candidates — attach as each procedure needs the tool**
| Tool | Anchor to add | Why |
|---|---|---|
| **Stirring rod** | **`StirAnchor`** (rod tip) | ⚠ **`StirController` tracks `_rod.position` — the transform ORIGIN.** This is the exact latent bug that made the grind silently never register (the origin can be the handle, never entering the vessel). **Exp 2 and Exp 3 both use the rod — fix this when we get there.** |
| **Test tube brush** | `BrushAnchor` (bristle end) | `BrushController` accumulates origin travel; less brittle (any motion counts) but the contact point is the bristles |
| **MechanicalPipette / dropper** | `TipAnchor` | only if we ever model drop-dispensing rather than pouring |
| **Crucible tongs** | `GripAnchor` | only if we model gripping |
| **Watch glass · funnel · any liquid vessel** | *(none)* | liquid auto-fits — see above |

### Per-STEP apparatus — where to find it
The manual gives one apparatus block per experiment and never names a tool in a step, so **each module's Task-graph table carries an `apparatus the step needs` column** — inferred from the procedure verbs. It is authored as each module is polished (the one-by-one method):
- ✅ **tutorial-methane** — mortar · pestle · scoopula · open beaker (acetate+soda lime) · hard-glass tube · gas collection tube · burner · matches
- ✅ **prelim-chemical-compounding** — full per-step column authored (2026-07-15); roll-up under its task graph
- ⬜ **prelim-ethyl-alcohol → final-winemaking** — author the column as each module is reached. **Read the PROCEDURE, never the apparatus list** (see the defects above).

**Verbatim per-experiment lists** are reproduced under each module's *Manuscript Equipment & Apparatus* line below (OCR artifacts kept for fidelity — "bath Rubber tubing" = *water bath* + *rubber tubing*, a two-column merge).

**Game-status note:** the **pipette was deliberately dropped** (2026-07-15) as redundant with the modern micropipette already in the pack — but note the manuscript lists a pipette for **every** experiment, so pouring stands in for it. The **wooden splint was deleted** (never in the manuscript — it uses a *"lighted matchstick"*; see `manuscript-reconciliation.md` §0).

---

---

## ⭐ CLIENT PERIOD GROUPING (2026-07-16)

The client's grouping maps **exactly** onto the manuscript's 8 bench labs:

| Period | Modules | Manuscript |
|---|---|---|
| **Tutorial** | methane | *game-authored — sits OUTSIDE the graded periods.* ✅ **Client-confirmed 2026-07-16: stays as the tutorial-only experiment.** |
| **Prelim** | chemical reactions · ethyl | Exp 2 · Exp 3 |
| **Midterm** | benzoic · acetanilide · acetone · chloroform | Exp 4 · 5 · 6 · 7 |
| **Finals** | benzamide · wine making | Exp 8 · Exp 9 |

**Manuscript Exp 1 = STOICHIOMETRY** — a pen-and-paper exercise (balance equations, molecular weight, dimensional analysis, % yield), NOT a bench lab (`:1899`, `:2055`). That is why "methane" appears in the manuscript only as a molecular-weight worked example (`:1995`) — the game's methane tutorial is entirely game-authored.

### ✅ RESOLVED — Aspirin + Caffeine DROPPED (user 2026-07-16)
Both were **game-authored** (Aspirin is named only in the manuscript's intro prose; Caffeine appears nowhere) and are absent from the client's grouping. **Both dropped — the chain is now 9** (tutorial + the 8 bench labs).

**Deleted:** the two module defs, layouts, quizzes, 8 cutscene assets, `AspirinSynthesis`, `Test_CaffeineMurexide` — plus their entries in `ExperimentLibrary` / `QuizBankLibrary` / `CutsceneLibrary` / `MasterReactionRegistry` and the SampleScene builder's layout list. Suite pins moved in the same change (catalog 9, period split 1/2/4/2, 36 cutscenes, 9 banks / 29 questions, 8 layouts, results 9 rows).

**⚠ Aspirin is now a RAW REAGENT, not an end product.** Exp 2 §D hydrolyses aspirin (`0.5 g` in a test tube) and tests unhydrolysed aspirin as the control — but Aspirin existed ONLY as a demo-only end product, so dropping the module naively would have **hidden the reagent Exp 2 needs and made it unplayable outside demo**. It is now in `RawReagentCatalog` (Powder, `GroupOrganics`, "Exp 2") and out of `DemoMode.IsEndProduct`; **end products 9 → 7**.

**Orphaned but deliberately KEPT** (the hard client rule: never remove bench reagents/apparatus): `Chem_Caffeine`, the `Murexide Reagent` catalog row (marked `"(unused)"`), `Label_Aspirin` / `Label_Caffeine`, `Art/UI/Icons/aspirin.png`. `Salicylic Acid` stays — Exp 2's ester test uses it (its `usedIn` dropped "Aspirin" → "Exp 2").

**The Appendix-C TOC does NOT contradict this grouping.** Its Term column is a **vertically-centred merged cell** (labels land beside Exp 2 / 5 / 8), which reads as 3/3/3 but fits 3/4/2 just as well — a 4-row span centres between rows, which is exactly the extraction artifact seen. The narrative chapter independently confirms the client's grouping. Don't "re-derive" a chloroform conflict from it.

---

## ⭐ POLISH STATUS — we are perfecting the experiments ONE BY ONE

**Method (user directive 2026-07-15):** take ONE module from "the data exists" to "actually playable end-to-end in VR", finish it, then move to the next. **Cross-check the module against the manuscript BEFORE building** — the tutorial burned hours on props the manuscript never mentions (`manuscript-reconciliation.md` §0: "splint" appears **nowhere**; the **bent/delivery tube is real but belongs to Exp 3 only**; Exp 9 is a group activity with no bench chemistry).

| Module | Period | Polish status |
|---|---|---|
| **tutorial-methane** | Tutorial | ✅ **DONE (2026-07-15)** — playable end-to-end: scoop → grind → load tube → heat → collect → match test → quiz → grade |
| **prelim-chemical-compounding** | Prelim | 🔨 **PLAYABLE, awaiting headset playtest (2026-07-16)** — rebuilt to manuscript Exp 2. ✅ 13-task graph + ILOs + quiz. ✅ VR design SETTLED + BUILT: **dropper = counted squeezes**, **spatula 0.1 g/dip**, KMnO₄→liquid 0.1%, **RackTaskGroup** (a step waits for EVERY tube), layout rebuilt (20 vessels / 4 racks), 8 reactions, 13 two-line hints. ⬜ Left: two-step picker, `StirController` tip-tracking fix, headset pass. |
| **prelim-ethyl-alcohol** | Prelim | ✅ BUILT + simulated clean 2026-07-17 (8/8). Zone-free ferment→CO₂→limewater (`FermentationController`) + week time-skip + distillation + 3 warm tests. ⬜ Left: headset playtest; combustion could gain real match-ignition; the "wash" is bench-Ethanol shorthand. |
| midterm-* / final-* | — | ⬜ not started |

### The METHANE FLOW — the template every module follows
1. **Gate** — Pharmee → Campaign → pick module → don **all 3 PPE** (hard-gated: `GatekeeperModel.RequiresPPEToOpen`) → "I'm ready" → cross the threshold (timer starts).
2. **ReagentPrep** — scoop the solid from its open beaker into the mortar → grind with the pestle.
3. **Synthesis** — **load** the ground mix into the reaction vessel → **heat** it → **collect** the product.
4. **ChemicalTests** — confirm with the manuscript's own test (methane: lit **match** → pop; the manuscript always uses a "lighted matchstick", never a splint).
5. **DataSheet** — clock freezes → review corner → Jimenez briefs → quiz → grade → Retry / Complete Experiment.

### Reusable systems — REUSE THESE, don't rebuild
| Need | Reuse | Notes |
|---|---|---|
| Step text on the wrist panel | task `label` + `hint`; `ChecklistPager` renders the ACTIVE step's hint | **Write hints to match the MECHANIC and name the exact item** ("Grab the item labelled *Hard-glass tube*"). A label naming a prop the code never checks (the phantom "delivery tube & trough") cost hours of confusion. |
| Temperature monitoring | `ProcessReadout.BindHeat(name, TemperatureSim, targetC)` | Floats "62 C -> 120 C" over the vessel, tints cool-blue → hot-orange. **Every heating module reuses this.** It does NOT replace the thermometer — that apparatus stays on the bench. |
| Collection progress | `ProcessReadout.BindCollect(name, GasCollection)` | "Collecting 45%". |
| Hot-vessel glow | `MethaneApparatusRig.GlowFor(currentC, targetC)` | Emissive red ramp; saturates at the reaction temperature. |
| Solid handling | `ScoopController` (2 g/dip) + `GrindController` | Grinding requires the mortar to actually HOLD the reagent (`CanGrind`) — else you finish the step with powder still on the scoop. |
| Solid SFX | keys `scoop` / `powder-pour` via `AudioService.TryPlayFirstAt` | **Granular, never liquid** — deliberately NO liquid fallback (silence beats the wrong material). Clips in `Audio/Generated/`, wired by `Wire Scoop Sounds`. |
| Powder / gas fills | `ExperimentSceneBuilder.EnsurePowderVisual` + `LocalMeshBounds` / `LongestAxis` / `AxisAlign` / `BoreOf` | Fit contents in the vessel's **LOCAL** frame, sized as a **FRACTION** of local bounds. |
| Hand-tuned placement | `PlacementAnchor` children + menu `Add Placement Anchors` | `FlameAnchor` (match head / burner mouth), `ScoopAnchor` (blade), `BowlAnchor` (mortar), `PowderAnchor` (position **and size** — scale it). Only size-setting anchors set `previewsScale`. |
| Quiz → grade | `PostLabController` | **NEVER score-gated** (client rule). Back/Next review nav, picked answer highlighted; score shown plainly on the grade screen (`ExperimentResult.quizScore01`); Retry re-runs the experiment, **Complete Experiment** exits. |
| Full reset | `DropRespawn.ResetAllHome()` | Re-homes every item + restores contents, disassembles rigs, **extinguishes all burners/matches**, destroys used consumables so dispensers restock. |
| Methane-only props | `MethaneStageVisibility` | Gates ONLY the 4 methane staged props. **All general apparatus/reagents stay permanent** — see the ⛔ rule in CLAUDE.md. |
| **Play the module WITHOUT a headset** | `Tools ▸ PharmaSynth ▸ Simulate Run ▸ <module>` (`SimulatedRun.Run`) | **PLAYER-PATH honest**: every reagent is drawn from the real bench bottle and landed through `LiquidPhysics.AddLiquid` (verb-contract increments, 20% overshoot on bulk pours, snapshot/restore of all vessel contents); completion may only arrive through the real event chain (binding/rack/station). ⛔ NEVER "simulate" by calling `binding.HandleReagent` directly — the direct call reported CLEAN while a real player was hard-stuck (the binding had never subscribed to its vessel's events). Audits supplies + end-of-run fill visibility; transcript → `Logs/simrun-<module>.txt`. **Run BEFORE every headset pass**; the suite plays Exp 2 clean as a pin (`simrun:`). Cannot feel grabs/gestures/visual placement — headset checklist. Bug classes it has caught: no-completion-mechanism deadlock, phantom fume-hood mistakes, the unsubscribed-binding stuck bug, expected-mix scolding, ghost bindings, funnel-wastes-the-pour. |
| Closing beat with no physical verb | `ExperimentTask.autoCompleteWhenOthersDone` (wrap-up flag) | "Record your observations"-type steps auto-complete via `Graph.Tick()` once every other task is done. Exp 2's `record-observations` uses it. |
| **Heat-gated reactions** (2026-07-17) | `ReactionRule.minTemperatureC` + `LiquidPhysics.SetTemperature`/`ReactionPending` | A cold mix of the right recipe HOLDS as pending — no early observation, no wrong-mix scold; MixFeedback shows "Needs heat — warm to N C (water bath)". 12 rules game-wide carry a threshold — every warm-bath test (Tollens, esters, iodoform, chloroform AgNO3, benzamide acid/alkali…) now happens WHEN the procedure says. `SetContents` clears pending. |
| **⛔ ZONE-FREE tool rule (user 2026-07-17, binding for every module pass)** | `WaterBathController` + `VesselHeatTask` (`ExperimentLayout.Vessel.heatToC`) | "The entire lab IS the zone — tools function when brought together ANYWHERE." NO fixed stations/pads/DynLabels/TeleAnchors for steps the tools can own: heating = the bench `WaterBath` (player pours distilled water in ≥5 ml, stands a LIT burner within 0.45 m; heats to a 100 °C cap, warms vessels within 0.32 m, label narrates its next need) + `heatToC` on the synthesis vessel (task completes when served AND hot, wherever); filtering = the funnel pour itself (destination binding completes on receiving the product). Exp 2's two stations are deleted; when polishing each next module, REPLACE its Heat/Filter stations the same way (Weigh/Stir/Collect stations remain until each gets its pass). |

### ⚠ Hard-won gotchas — CHECK THESE FIRST on the next module
- **`LiquidPhysics` on an opaque vessel makes the vessel VANISH in play.** `Start()` adopts the host's own renderer when `mainRenderer` is null, then disables it while empty (this hid the mortar for days). `ShouldAdoptHostRenderer` now only adopts a real `_Fill` surface — never point `mainRenderer` at a vessel's own mesh.
- **Verb refs are NOT serialized.** `_runner` / `_pestle` are null after a domain reload, so the task silently never completes. Bind at runtime (`GrindController.BindRunner`, `AutoFindPestle`).
- **Never mix absolute metres with local-unit bounds** — on an import-scaled prefab a "3 cm" cap becomes microscopic and the content is invisible.
- **Never guess a tool's working end from bounds** (which end is the blade/head is unknowable) — use an anchor.
- **World-space fills break on tilted/held vessels** — always fit in local space.
- **A world-space canvas needs `TrackedDeviceGraphicRaycaster`**, or the XR ray cannot click it (the quiz answers were dead for this reason).
- **Read `SampleScene.unity` directly to diagnose** — it is ground truth and far faster than theorising.

---

## tutorial-methane

**moduleId** `tutorial-methane` | **period** Tutorial | **prerequisite** `(none)` | **manuscript** (game-authored) | **end product** Methane gas (splint pop; no shelf product)

**Official ILOs:** (1) (game-authored - pending client confirmation)


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `prepare-mixture` | ReagentPrep | Grind & mix sodium acetate with soda lime | - | Combine dry sodium acetate with soda lime in the mortar. |
| 2 | `setup-apparatus` | Synthesis | Assemble the hard-glass tube, delivery tube & trough | prepare-mixture | Clamp the tube, fit the cork + delivery tube into the water trough. |
| 3 | `heat-mixture` | Synthesis | Heat the mixture with the burner | setup-apparatus | Apply gentle then strong heat; watch for gas bubbling through the trough. |
| 4 | `collect-gas` | Synthesis | Collect methane over water | heat-mixture | Invert the collection tube over the delivery outlet; let it fill. |
| 5 | `test-gas` | ChemicalTests | Confirm methane by combustion (clean blue flame) | collect-gas | Bring a lit splint to the mouth of the tube; methane burns with a pale blue flame. |

### Quiz (Documentation score)

1. **Which gas is produced when sodium acetate is heated with soda lime?**
   - Ethane
   - Methane [CORRECT]
   - Carbon dioxide
   - Hydrogen
   - *why:* Decarboxylation of the acetate gives methane (CH4).
2. **Why is methane collected over water?**
   - It reacts with air
   - It is denser than water
   - It is nearly insoluble in water [CORRECT]
   - It dissolves the soda lime
   - *why:* Methane is only slightly soluble in water, so it displaces it cleanly.
3. **A clean, pale-blue flame on ignition indicates:**
   - Complete combustion [CORRECT]
   - Presence of water
   - Carbon dioxide
   - Incomplete reaction
   - *why:* Complete combustion of the collected methane.

---

## prelim-chemical-compounding

**moduleId** `prelim-chemical-compounding` | **period** Prelim | **prerequisite** `tutorial-methane` | **manuscript** Exp 2 (test battery DIVERGES - client flag, see header) | **end product** identification lab (no product)

**Official ILOs:** (1) Write chemical reactions involved in some organic compounds such as alcohols, aldehydes, ketones, carboxylic acids and esters. (2) Differentiate tests for different organic compounds.

**Manuscript Equipment & Apparatus:** Stirring rod; Aspirator; Test tube; Beaker; Test tube brush; Erlenmeyer flask; Test tube holder; Pipette; Porcelain spatula

**Manuscript Reagents:** 0.1% Potassium permanganate; Concentrated sulfuric; acid 10% Ferric chloride; Diluted acetic acid; 10% Sodium bicarbonate; Ethyl alcohol; 6N Sodium hydroxide; Glycerol; 6N Sulfuric acid; Methanol; Acetaldehyde; n-butyl alcohol; Acetone; Phenol; Aspirin; Salicylic acid; Benedict's reagent; Sec-butyl alcohol; Benzyl alcohol; Ter-butyl; alcohol Concentrated hydrochloric acid Tollen's; reagent


### Task graph (play order) — REBUILT to manuscript Exp 2 (2026-07-15)

Structure follows the manual's own sections **A → B → C → D**. Preps are free-order; each section gates the next.
**Apparatus column = what the step ACTUALLY needs, inferred from the procedure verbs** (the manual names almost no tool — see §Apparatus). **Nothing is pre-set** — the student prepares every tube; volumes are played as **dropper squeezes** (drops/small ml), **tilt-pour into a tolerance band** (bulk ml), or **spatula dips** (grams). See §VR adaptation.

| # | task | phase | label | prereq | manual ref | apparatus the step needs |
|---|------|-------|-------|--------|-----------|--------------------------|
| 1 | `prep-enol-tubes` | ReagentPrep | Set up the enol-test tubes (5 alcohols) | - | A.I.a | **5 × test tube + rack**; *pour* (stands in for **pipette**) for the 5 samples; **dropper** for the 1 ml samples; **tilt-pour** the 10 ml water |
| 2 | `test-enol-fecl3` | ChemicalTests | Enol test - add ferric chloride to each tube | 1 | A.I.b-c | **pipette/pour** (FeCl₃ ×5 tubes); the 5 tubes |
| 3 | `prep-oxidation-tubes` | ReagentPrep | Set up the rate-of-oxidation tubes (4 tubes) | - | A.II.a | **4 × test tube + rack**; student builds the **KMnO₄ + NaOH medium** (2 squeezes each) |
| 4 | `test-oxidation-alkaline` | ChemicalTests | Rate of oxidation in ALKALINE conditions (+ negative control) | 3 | A.II.b-c | **pipette/pour** (3 butyl alcohols); **stirring rod** (shake); **per-tube decolorisation TIMER** (`ProcessReadout`) |
| 5 | `test-oxidation-acidic` | ChemicalTests | Rate of oxidation in ACIDIC conditions | 4 | A.II.d-e | as #4, second rack; student builds the **KMnO₄ + H₂SO₄ medium**; both racks side-by-side |
| 6 | `test-tollens` | ChemicalTests | Aldehyde vs ketone - Tollen's test (water bath 5 min) | 5 | B.I | **2 × test tube**; **pipette/pour** (acetone, acetaldehyde, Tollen's); ⚠ **WATER BATH** (*absent from Exp 2's own apparatus list*); **test tube holder** (hot tube) |
| 7 | `test-ester-acetate` | ChemicalTests | Ester formation - ethyl acetate | 6 | C.I.a-b | **test tube**; **pipette/pour** (ethanol, acetic acid, conc H₂SO₄); ⚠ **WATER BATH**; **odour cue** (on-screen) |
| 8 | `test-ester-salicylate` | ChemicalTests | Ester formation - methyl salicylate | 7 | C.I.c-d | **test tube**; **PORCELAIN SPATULA** (0.1 g salicylic acid = **solid → scoop**); **pipette/pour** (methanol, conc H₂SO₄); ⚠ **WATER BATH**; **beaker** (1 ml water); **odour cue** |
| 9 | `prep-hydrolysis` | ChemicalTests | Hydrolyse the aspirin (boil in the water bath) | 8 | D.I.a-b | **test tube**; **PORCELAIN SPATULA** (0.5 g aspirin = **solid → scoop**); **pipette/pour** (10 ml water, conc HCl); ⚠ **WATER BATH** (boil) |
| 10 | `filter-hydrolysate` | ChemicalTests | Filter off the undissolved crystals | 9 | D.I.c | **ASPIRATOR** (the list's filtration tool) or **funnel + filter paper**; receiving vessel — ⬜ *decides the aspirator's job* |
| 11 | `test-hydrolysis-fecl3` | ChemicalTests | Test the filtrate with ferric chloride | 10 | D.I.d | **pipette/pour** (FeCl₃ ×2 drops) onto the filtrate |
| 12 | `test-hydrolysis-control` | ChemicalTests | Compare against UNhydrolysed aspirin | 11 | D.I.e | **test tube**; **PORCELAIN SPATULA** (plain aspirin); **pipette/pour** (FeCl₃) |
| 13 | `record-observations` | DataSheet | Record every colour and odour on the data sheet | 2,5,6,8,12 | Data Sheet | quiz tablet (+ results board) |

**Apparatus roll-up for the whole module:** 15 × test tube + racks · beaker · **water bath** (⚠ unlisted by the manual, needed 3×) · **porcelain spatula** (3 solid weigh-outs) · stirring rod · aspirator *or* funnel+filter paper · test tube holder · test tube brush (between tests → Sanitation). **No distillation train, no condenser, no thermometer** — this module never distils.

**par time** 900 s · **tracked skills** Measuring, Heating, Filtration, TestInterpretation · **all 20 reagents already exist as `ChemicalData`**.

**Still to author:** `Layout_ChemicalCompounding` (stations/vessels/bindings), the reaction rules + `expectedObservation` per test, and the odour cue (see VR-adaptation notes).

### VR adaptation — design decisions (SETTLED + BUILT 2026-07-16)

**The problem in numbers.** Done literally, Exp 2 is **~64 discrete pours across ~19 tubes** (A.I 15 · A.II ~28 for the two runs · B 5 · C 8 · D 8) — 30-40 min of pipetting, which is the least educational part of the lab.

**The governing principle (agreed):**
> **Preserve every SAMPLE, every OBSERVATION and every COMPARISON. Compress only the MANIPULATION.**

**The accuracy rule — NO pre-setting: the student prepares EVERY tube.**
An earlier draft pre-set the "constant" (the bulk water/medium) to save pours. **That was dropped 2026-07-16.** The real problem was never the pour COUNT — it is that **VR cannot hit an accurate volume**. Discretise the action and the accuracy problem vanishes, so there is no reason to pre-stage anything.

> **The watch panel prints the manuscript's REAL quantity (for facts), then the achievable action beneath it.**
> manual: *"In both test tubes, add 2 ml of Tollen's reagent."*
> panel: **"Add 2 ml of Tollen's reagent"** · *"2 squeezes of the sample, then 2 of Tollen's, in BOTH tubes."*

**THE CONTRACT: the number in the instruction IS the action count, whatever its unit.** The manuscript
measures Exp 2 in **three** units and all three become countable actions:

| Unit | Exp 2 examples | Verb |
|---|---|---|
| **drops** (dominant) | 5 drops FeCl₃ · 2 drops NaOH · 2 drops acetone · 10 drops ethanol · 1 drop conc H₂SO₄ | **Dropper** — 1 squeeze = 1 drop (physically honest) |
| **ml** | 1 ml sample · 2 ml KMnO₄ · 2 ml Tollen's | **Dropper** — 1 squeeze = 1 ml (the deliberate abstraction) |
| **ml, bulk** | the 10 ml of water | **Tilt-pour** — existing `LiquidPourer`, generous `requiredMl` band + live readout |
| **grams** | 0.1 g salicylic · 0.5 g aspirin | **Porcelain spatula** — 0.1 g/dip = 1 and 5 dips |

⚠ Do NOT collapse drops and ml into one rule: "1 squeeze = 1 ml" would turn "5 drops" into 5 ml. The
manuscript distinguishes them and so must the copy; the SQUEEZE COUNT is what unifies them.

**This is the same trick the project already uses for solids.** `ScoopMath.GramsPerScoop = 2f` plays
"weigh 4 g" as two countable dips — `DropperMath.MlPerSqueeze` is its liquid twin (pure static math +
thin controller + `Bind()` seam). `RawReagentCatalog.LabwareKind.DropperBottle` already existed,
already documented as "test reagents added by drops".

**⚠ The scoop could not express Exp 2's solids at all**: `GramsPerScoop = 2f`, but Exp 2 weighs 0.1 g and
0.5 g — BOTH smaller than one dip. Hence `ScoopMath.GramsPerSpatula = 0.1f` + `ScoopController.gramsPerDip`.

Errors stay real: wrong count, wrong tube and wrong reagent all still grade, animate, and can starve the
supply into a restart. Only the analog precision is gone.

**Other agreed adaptations**
- **Odour → on-screen cue** ✅ **client-approved**: the two ester tests show e.g. *"Sweet, fruity odour - ethyl acetate formed"* / *"Wintergreen - methyl salicylate"*. The one genuine sensory loss.
- **Time compression:** the 5-min water bath / 3-5 min boil → **~15 s** with `ProcessReadout` counting. No learning in real-time waiting.
- **BUT keep the decolorisation TIMER.** The manual explicitly says *"note the time of decolorization"* — that is a **measurement**, so each tube gets a live per-tube timer. Compress the wait; never hide the number.
- **Both oxidation racks side by side** (alkaline + acidic simultaneously). The manual runs them sequentially, so the student compares from memory — VR can show both at once. This is **better than the manual**, not a compromise.
- **Results board:** a rack-side panel auto-filling the manual's own data-sheet table as each result lands, so `record-observations` is a real review rather than recall.

**⛔ UNTOUCHABLE — these ARE the experiment (never trim to save actions)**
- all **5 enol samples** (phenol-vs-the-rest is the lesson)
- all **3 butyl alcohols + the negative control** (the 1°/2°/3° rate difference, and the control teaches experimental design)
- **acetone vs acetaldehyde** (the contrast IS the test)
- **hydrolysed vs unhydrolysed aspirin** (same)

**✅ DECIDED 2026-07-16**
1. **No pre-set** — the student prepares everything; accuracy is solved by the dropper / tilt-pour / spatula split above.
2. **Episode picker: period → MODULE (two-step).** Choosing **Prelim** lists its **two modules** (Compounding, Ethyl Alcohol), the second dimmed until the first passes — same for Midterm (4) and Final (2). Today `GatekeeperModel.EpisodeOptions` lists only the **four periods** and `ChooseEpisode` silently auto-starts `FirstPlayableInPeriod`, so the player never sees module names. Needs: a `ModuleOptions(flow, period)` beside `EpisodeOptions`, one new gate state between `EpisodePick` and the confirm, and a row-index handler in `PharmeeGatekeeper` (~line 229). The 9-module chain **already locks in sequence** — only the picker's second level is missing. **Exp 2 stays ONE module** (13 tasks); a picker change, not a content split.
3. **A PASSED module stays REPLAYABLE**, marked ✓ PASSED with its previous grade; best score kept. Note `FirstPlayableInPeriod` already falls back to "first unlocked" once a period is fully passed — replay exists today only as an accident of that fallback; the module picker makes it intentional.

**✅ BUILT 2026-07-16** — Exp 2 is playable end-to-end; suite 1055/1055.
1. `DropperMath` + `DropperController` (the contract above; a full dropper = 10 squeezes, the largest single Exp 2 instruction). Each squeeze deposits through the normal `AddLiquid` path, so `LiquidTaskBinding.requiredMl` counts it for free — no new task plumbing.
2. `ScoopMath.GramsPerSpatula = 0.1f` + `ScoopController.gramsPerDip`.
3. **KMnO₄ → Liquid, renamed `Potassium Permanganate 0.1%`** — it was **Solid**, which `LiquidPourer` cannot pour, so A.II's "2 ml of 0.1% KMnO₄" was unbuildable. The manuscript only ever uses the 0.1% SOLUTION at the bench.
4. **`RackMath` + `RackTaskGroup`** — see §Stage layout.
5. **`Layout_ChemicalCompounding` REBUILT** from the retired battery.
6. **8 reaction rules + `expectedObservation`**; **13 hints** rewritten.
7. **`LiquidTaskBinding` multi-reagent fix** — `_accumulated` was keyed by taskId alone, so a task naming SEVERAL reagents pooled them and **whichever landed first completed the step with half the chemistry missing**. It is now per-STEP and a task waits for all its reagents. This also silently fixed three SHIPPED modules (Acetone + EthylAlcohol `test-iodoform` needed KI *and* hypochlorite; BenzoicAcid `test-ester` needed the alcohol *and* its acid catalyst).

**⬜ STILL TO DO**
1. **Two-step episode picker** (per §2) — not started.
2. ⚠ **Fix `StirController` before wiring any "shake/stir" step** — it tracks the rod's transform ORIGIN (`_rod.position`), the same latent bug that made the grind silently never complete. Give it tip-tracking + a `StirAnchor`, exactly as `GrindController` got (`PestleTip` closest-point + `BowlAnchor`). See §Apparatus → anchor candidates.
3. **Headset playtest** — the 19-tube reach, dropper feel and rack pacing are unvalidated in VR.

### Stage layout (rebuilt 2026-07-16 — the old file was the RETIRED battery)

⚠ The previous `Layout_ChemicalCompounding` still staged `test-combustion` / `test-sodium` /
`gather-ethanol` / `test-bromine` / `test-kmno4` — **zero overlap** with the 13 manuscript tasks. The
stage built, nothing errored, and **not one task could ever complete**. `LayoutGraphCoverageSuite` now
pins layout↔graph coverage both ways so this cannot recur.

**Rack groups** (`rackGroup` + `completesTask:0` → the step waits for EVERY tube):
- `enol` (5 tubes) — Ethanol · n-Butyl · Benzyl · **Phenol** · Glycerol. 1 ml sample + 10 ml water each, then 5 drops FeCl₃.
- `oxalk` (4) — 2 ml KMnO₄ 0.1% + 2 drops NaOH 6N in all four; the 3 butyl alcohols into tubes 1–3.
- `oxacid` (4) — the same with H₂SO₄ 6N. **Both racks side by side** (the manual runs them sequentially so the student compares from memory — VR shows both at once: better than the manual, not a compromise).
- `tollens` (2) — Acetone vs Acetaldehyde, 2 drops each + 2 ml Tollen's.

**Why RackTaskGroup exists:** `LiquidTaskBinding` is per-VESSEL, so five tubes bound to one task meant
**the first tube to hit its threshold completed the step** and the other four became optional — silently
throwing away the comparison that IS the lesson (the doc's four UNTOUCHABLES). Members defer completion;
the group calls the step in once every member is ready, and floats `"tube 3 of 5"` while it fills.

**The negative control is the 4th oxidation tube.** It takes the medium but **declares no binding for the
alcohol step**, so it is not a member of that group — leaving it alone is correct play, and pouring into
it is a genuine wrong-reagent mistake. That is how the control teaches experimental design.

**Standalone vessels:** `EsterAcetateTube` (10 drops ethanol + 4 acetic + 1 conc H₂SO₄) ·
`EsterSalicylateTube` (methanol + 1 spatula dip salicylic + acid) · `HydrolysisTube` (5 dips aspirin +
10 ml water + 1 drop conc HCl) · `HydrolysisControlTube` (plain aspirin + FeCl₃) · `FiltrateBeaker`.

**Stations:** `prep-hydrolysis` (Heat 95 °C — the bath OWNS this step) · `filter-hydrolysate` (Filter,
Funnel prop). **No station for `record-observations`** — DataSheet steps close via the quiz/tablet (the
shipped Acetanilide layout stages none either).

⚠ **ENGINE CONSTRAINT (carry forward):** a task completes by **EITHER its pours OR a station, never
"pour THEN heat"** — each heated step must pick one owner. So `prep-hydrolysis` is owned by the BATH (its
label *is* "boil in the water bath"; its reagents are `completesTask:0`, same shape as Acetanilide's
`heat-bath`), while `test-tollens` is owned by its RACK and gets **no** station — a station there would
race the rack and complete the step the moment the tubes were warm, reagent or not. The ester warmings
likewise complete on their reagents; their graded evidence is the **odour cue**, not the heat.
*Known consequence of the same constraint:* boiling an empty tube completes `prep-hydrolysis`, exactly as
in every shipped Heat module. Worth revisiting engine-wide, not per-module.

**⛔ Tools staged: NONE — and no reagent bottles either.** The bench already holds every
general tool (`Eq_Dropper`, `Eq_PorcelainSpatula`, `Eq_Funnel`/`Funnel_2`, `Kit_BunsenBurner_0_9`,
`Kit_TestTube_0-5`, `Kit_Hard-GlassTestTube_0-3`, `Eq_TestTubeBrush`, `Eq_GlassRod`, `Eq_WashBottle`,
cylinders, beakers, watch glass…) and **all 21 of this module's reagents** as shelf bottles
(`Reagent_*` west cubby + `Raw_*` east cabinets — Ethanol, Acetone, Ferric Chloride 10%, KMnO₄ 0.1%,
Salicylic Acid and Aspirin included). The first draft of this layout staged **46 duplicates** of that
bench. Stations point at bench itemIds instead (`kit-bunsenburner`, `kit-funnel`) — a `ZoneItemSensor`
matches a `LabItem.itemId` wherever it already lives. Suite-pinned (`bench:` assertions).
**Only the task-bound VESSELS are staged.**

⚠ **Tube budget:** Exp 2 needs **19 regular tubes simultaneously** — the worst case in the whole game
(every other module needs ≤4; Exp 6 has 4 tests, Exp 7/8 three each). The bench has 6, so **13 more
regular tubes** are needed. **Hard-glass: 4 on the bench is plenty** — the only naked-flame tube in any
experiment is Exp 6's dry distillation; everything else (including Exp 2's boil) heats in a water bath
at ≤100 °C. Methane's hard-glass is its own staged prop. (19 assumes the alkaline + acidic racks sit
side by side, which the design deliberately prefers over the manual's sequential reuse; reusing would
make it 15.)

### Reactions & expected observations (authored 2026-07-16 — 8 rules)

**Colours are OURS.** The manuscript's `"10% - violet - 0"`-style annotations are a previous developer's
notes of unknown meaning and are **explicitly disregarded (user 2026-07-16)**. These come from the real
chemistry.

| Rule | Pair | Outcome | Observation |
|---|---|---|---|
| `Test_EnolPhenolFeCl3` | Phenol + FeCl₃ 10% | ColorChange | Deep violet at once — the four other alcohols stay unchanged: **that contrast IS the test** |
| `Test_OxidationPrimaryKMnO4` | n-Butyl + KMnO₄ | ColorChange | Purple fades **fastest**; brown MnO₂ left. Note the decolorisation time |
| `Test_OxidationSecondaryKMnO4` | sec-Butyl + KMnO₄ | ColorChange | Fades **noticeably slower** — a 2° alcohol resists more |
| `Test_TollensAldehyde` | Acetaldehyde + Tollen's | Precipitate (≥50 °C) | Bright **silver mirror**; acetone leaves it clear |
| `Test_EsterEthylAcetate` | Ethanol + Diluted Acetic | **Odor** (≥50 °C) | *Sweet, fruity — ethyl acetate formed* |
| `Test_EsterMethylSalicylate` | Methanol + Salicylic Acid | **Odor** (≥50 °C) | *Sharp wintergreen — methyl salicylate formed* |
| `Test_AspirinHydrolysis` | Aspirin + conc HCl | ColorChange (**≥90 °C**) | Boiling in acid frees salicylic acid into the filtrate |
| `Test_HydrolysateFeCl3` | Salicylic Acid + FeCl₃ | ColorChange | Violet — the ester **was** hydrolysed; plain aspirin gives none |

**⛔ The NEGATIVES are deliberately UNAUTHORED — do not "fix" this.** tert-Butyl resisting KMnO₄ (no
α-hydrogen), acetone leaving Tollen's clear (a ketone cannot reduce it), and unboiled aspirin giving no
violet are **not missing rules**: "nothing happens" IS each test's answer, and the contrast is the whole
lesson. A rule there would invent chemistry the manuscript denies. Suite-pinned as absent.

**Odour → on-screen cue** ✅ client-approved: the one genuine sensory loss, so both esters carry it in
`expectedObservation` and fire `ReactionOutcome.Odor`.

### Watch-panel instructions — the 13 hints (rewritten 2026-07-16)

**MATERIALS header (2026-07-17, ALL 9 modules):** the holo board opens with a gather-first guide —
reagents **with totals derived from the layout bindings** (`Tools ▸ PharmaSynth ▸ Generate Materials
Guides`; liquids "N ml" / solids "N g" in the game's own 1-squeeze-=-1-ml system, so it can never
drift from what the tasks consume) and **apparatus authored from the PROCEDURE** (never the
manuscript's defective lists). Stored as `materialReagents`/`materialApparatus` on the module,
rendered by `ChecklistPager.BuildMaterialsHeader` above the checklist. NOT a checklist item — it
completes nothing, stays fully visible, and the board **keeps its scroll position between opens**
(the W5.12 snap-to-top is gone; re-run the generator after any layout change).


**The contract:** every step prints the manuscript's **REAL quantity** (so the student learns the true
recipe), then the **achievable action** underneath.

> *"Add 2 ml of Tollen's reagent"*
> `→ 2 squeezes of the sample, then 2 of Tollen's, in BOTH tubes. Only the aldehyde plates a silver mirror.`

**The machinery was already built and is generic — the instruction quality is pure DATA** (the `hint`
field). `ChecklistPager.BuildFocusedText` renders the CURRENT step's hint under it (`•` done · `»` current
· `□` pending) and `WristWatchController` rebuilds that text **every frame** while the board is up, so it
can never go stale. Suite-pinned: every hint is two lines, and every action line names a **countable
verb** (squeeze / dip / tilt-pour / funnel / burner / board) — that test caught two real copy failures,
including an action line that said only "compare" and never named a thing to touch.

**Step-completion alerts are generic too — nothing to build per module:**
- **HUD toast** `"Step complete: <label> ✓"` (`ExperimentHudController:82`)
- **success chime** `grade-pass` (`:84`)
- **Pharmee praises + speaks the NEXT step** (`PharmeeBrain:126`)
- **Rack progress** floats `"tube 3 of 5"` so a finished tube never reads as a finished step.

### Quiz (Documentation score)

1. **Ethanol contains which functional group?**
   - Carbonyl
   - Hydroxyl [CORRECT]
   - Carboxyl
   - Amino
   - *why:* Ethanol is an alcohol (hydroxyl, -OH).
2. **Adding sodium metal to ethanol produces:**
   - Oxygen
   - Methane
   - Hydrogen gas [CORRECT]
   - Chlorine
   - *why:* Sodium displaces the -OH hydrogen, releasing hydrogen gas.
3. **What shows that potassium permanganate has OXIDISED an alcohol?**
   - The purple colour fades / turns brown [CORRECT]
   - The mixture freezes
   - White smoke appears
   - Nothing changes
   - *why:* KMnO4's violet colour is discharged (brown MnO2 forms) as it oxidises

---

## prelim-ethyl-alcohol

**moduleId** `prelim-ethyl-alcohol` | **period** Prelim | **prerequisite** `prelim-chemical-compounding` | **manuscript** Exp 3 | **end product** Ethanol

**Official ILOs:** (1) Synthesize ethyl alcohol. (2) Determine its identity through chemical tests.

**Manuscript Equipment & Apparatus:** S-clamp; Aspirator; Stirring rod; Beaker; Test tubes; Condenser; Test tube brush; Cork; Test tube holder; Distilling flask; Thermometer; Erlenmeyer flask; Iron clamp; Iron stand; Watch glass; Pipette; Water; Porcelain spatula; bath Rubber tubing

**Manuscript Reagents:** Concentrated sulfuric acid; 6% Sodium hypochlorite; Diluted acetic acid; 10% Potassium iodide; Dry yeast; 10% Sodium hydroxide; Ethanol; Ammonium phosphate; Saturated calcium hydroxide; Brown sugar

### ⭐ VR apparatus review (2026-07-17) — every manuscript item IS on the bench
All 19 apparatus map to existing bench objects: distilling flask `kit-distillingflask` · Erlenmeyer `kit-erlenmeyerflask` · **delivery/"bath Rubber tubing"** `kit-deliverytube` (the CO₂→limewater bubbler — Exp 3's signature) · cotton swab `Raw_CottonSwabs` · cork `kit-rubberstopper` · watch glass `Eq_WatchGlass` · graduated cylinder `Eq_GraduatedCylinder_50mL` · porcelain spatula · stirring rod `Eq_GlassRod` · test tubes/brush/holder · pipette = `Eq_Dropper_1–4`.

**⛔ Six items REMOVED from the bench (menu `Remove VR-Inappropriate Apparatus`) — a DELIBERATE, documented exception to the "all tools always present" rule; prefabs kept in the project so any later module can re-place them. Do NOT let an all-tools audit re-add them.**

| Removed | Why it is not a VR interaction |
|---|---|
| Iron stand (`kit-retortstand`) | Support scaffold to hold a flask on a rod. VR heats **zone-free** — carry the flask to the water bath; there is no clamp-rig to assemble. |
| Iron clamp + S-clamp (`kit-utilityclamp`) | The clamps that fix glassware to that stand — same scaffold, no VR action. |
| Aspirator (`kit-aspirator`) | Vacuum suction for filtration/transfer. The game moves liquid by **pour/decant**, never suction. |
| Condenser (`kit-condenser`) | Distillation cooling. Distillation is an **abstracted heat + collect** sim (lenient 70–80 °C window); the player never assembles a condenser train. |
| Thermometer (`kit-thermometer`) | Temperature is shown live by the floating **ProcessReadout** / water-bath label, so the physical instrument is decorative. |
| Iron ring ×2 (`Eq_IronRing` + `IronRing_2`, no itemId) | Both clamp onto the retort stand (already removed) → orphaned. Matched by name CONTAINS so both go. |
| Clay triangle (`kit-claytriangle`) | Cradles a crucible over an open flame — the crucible is gone and the kept tripod+gauze already cover "hold over the burner". |
| Crucible (`kit-crucible`) + tongs (`kit-crucibletongs`) | Strong >500 °C ignition/ashing vessel + its handler — **0 manuscript & 0 game usage**; the water bath caps at 100 °C. |
| Alcohol burner (`kit-alcoholburner`) | Redundant flame source — the Bunsen burner beside the water bath covers all heating. |
| Empty vials ×8 (`Kit_Vial_0–3` = `kit-vial`; `Eq_Vial_Brown` + `Vial_Brown_2/3/4`, no itemId) | **0 manuscript, 0 game usage** — leftover reagent-staging containers from the retired-battery layout; reagents now come from the `Raw_` bottles. |
| Forceps (`Eq_Forceps`, no itemId) | **0 manuscript, 0 game usage** — only generic metadata refs (size/physics/hover), no task or verb reads it; VR grab replaces it. |

**KEPT** (user 2026-07-18): **tripod (`kit-tripod`) + wire gauze (`kit-wiregauze`)** — a genuine heat platform the player sets over a burner to hold a vessel for heating.

The chemistry apparatus (flasks, delivery tube, watch glass, graduated cylinder, droppers, tubes, spatula, rod) all stay interactive.

### ⏳ Time-skip (fermentation) — reusable across ALL experiments
The manuscript ferments **one full week**. VR compresses this: once the must is prepared and sealed, the completing task is authored `longProcess` → `TimeSkipController` **fades to black ~2 s and returns with a "time has passed" success message**. This is the standard treatment for every lengthy real-world wait in any module (overnight dries, hour-long crystallisations) — author the closing task `longProcess=true` + a message.

### ✅ BUILT + SIMULATED CLEAN 2026-07-17 (8/8 · 0 mistakes · 0 bugs)
Zone-free, bench-bound (like Exp 2). **8 tasks**: prepare-must → ferment (CO₂/limewater + **week time-skip**) → adjust-ph → distill → test-combustion/iodoform/ester → record-yield (auto-completes). **Vessels:** `FlorenceFlask` (fermentation, `fermentTaskId`) · limewater `Kit_TestTube_5` · `DistillingFlask` (`heatToC 70`) · `Eq_WatchGlass` (combustion) · iodoform `Kit_TestTube_6` (`heatToC 60`) · ester `Kit_TestTube_7` (`heatToC 50`). **The CO₂→limewater mechanic** = `FermentationController` (zone-free, mirrors the water bath): once prepare-must is done the flask evolves CO₂ into any nearby limewater vessel → `Limewater_CO2` clouds it milky → ferment completes → time-skip. CO₂ is a REACTION DRIVER, added via `AddLiquid(notify:false)` so it drives the reaction without being graded a reagent. Distillation + warm tests reuse `heatToC` + the water bath. **VR simplifications** (documented): bulk solids use the scoopula (2 g), sub-gram the spatula; the "wash" poured into the distilling flask is bench Ethanol standing in for the decanted ferment; combustion completes on the ethanol-on-watch-glass pour (match = guidance). Generator: scratchpad `gen_ethyl.py`.

The pre-polish stub graph is retained below for history.

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `prepare-must` | ReagentPrep | Prepare the fermentation must (sugar + yeast) | - | Dissolve brown sugar, add dry yeast and ammonium phosphate. |
| 2 | `ferment` | Synthesis | Ferment; confirm CO2 with limewater | prepare-must | Seal with a delivery tube into limewater; watch it turn milky. |
| 3 | `distill` | Synthesis | Distil; collect the 70\u201380\xB0C fraction | ferment | Heat gently; collect only the 70\u201380\xB0C cut. |
| 4 | `test-combustion` | ChemicalTests | Combustion test (blue flame) | distill | Ignite a sample; a blue flame means complete combustion. |
| 5 | `test-iodoform` | ChemicalTests | Iodoform test (yellow precipitate) | distill | Add KI + NaOCl, warm to 60\xB0C; look for a yellow precipitate. |
| 6 | `test-ester` | ChemicalTests | Ester formation (fruity odor) | distill | Warm with acetic acid + H2SO4; note the fruity smell. |
| 7 | `record-yield` | DataSheet | Record yield and observations | test-combustion, test-iodoform, test-ester | Enter the distillate volume and test results. |

### Stage layout

**Stations:** `distill` (Heat to 78 C) ; `test-combustion` (zone-touch)

**Reagents staged (pourable):** Brown Sugar (Vial_Brown, auto-supply) ; Yeast (Vial_Brown, auto-supply) ; Sodium Hypochlorite (Vial_Brown, auto-supply) ; Glacial Acetic Acid (Vial_Brown, auto-supply) ; Potassium Iodide 10% (Vial_Brown, auto-supply)

**Tools staged:** Distillation Flask ; Lit Splint

**Vessel Beaker_500mL** (Fermentation Beaker) - starts EMPTY
  - pour **Brown Sugar** >= 50 ml -> completes `prepare-must`
  - pour **Yeast** >= 50 ml -> completes `ferment`
  - pour **Potassium Iodide 10%** >= 50 ml -> completes `test-iodoform`

**Vessel TestTube_WithLiquid** (Distillate Test Tube) - starts with Ethanol
  - pour **Sodium Hypochlorite** >= 50 ml -> completes `test-iodoform`
  - pour **Diluted Acetic Acid** >= 50 ml -> completes `test-ester`

### Reactions & expected observations

- **EsterFormation**: Ethanol + Diluted Acetic Acid -> Ethyl Ester - "Sweet fruity ester odour"
- **Ferment**: Brown Sugar + Yeast -> Ethanol - "Steady CO2 bubbling; fermentation underway"
- **Iodoform**: Ethanol + Sodium Hypochlorite - "Yellow iodoform precipitate"

### Quiz (Documentation score)

1. **Fermentation of sugar by yeast produces ethanol and:**
   - Oxygen
   - Carbon dioxide [CORRECT]
   - Hydrogen
   - Methane
   - *why:* CO2 is the by-product, confirmed by limewater turning milky.
2. **At roughly what temperature is the ethanol fraction distilled?**
   - 30-40 C
   - 100-110 C
   - 70-80 C [CORRECT]
   - 150-160 C
   - *why:* Ethanol boils ~78 degrees; the 70-80 cut is collected.
3. **A positive iodoform test gives a precipitate that is:**
   - Yellow [CORRECT]
   - White
   - Black
   - Blue
   - *why:* Iodoform (CHI3) is a yellow precipitate.

---

## midterm-benzoic-acid

**moduleId** `midterm-benzoic-acid` | **period** Midterm | **prerequisite** `prelim-ethyl-alcohol` | **manuscript** Exp 4 (printed procedure is errata; game route = benzaldehyde + KMnO4) | **end product** Benzoic Acid

**Official ILOs:** (1) Synthesize benzoic acid. (2) Determine its identity through chemical tests.

**Manuscript Equipment & Apparatus:** S-clamp; Aspirator; Stirring rod; Beaker; Test tubes; Condenser; Test tube brush; Cork; Test tube holder; Distilling flask; Thermometer; Erlenmeyer flask; Iron clamp; Iron stand; Watch glass; Pipette; Water; Porcelain spatula; bath Rubber tubing

**Manuscript Reagents:** Benzoic acid; 0.1% Potassium permanganate; Concentrated sulfuric acid; 6N Hydrochloric acid; Methanol; 6N Sodium hydroxide; Propyl; 10% Sodium hydroxide; alcohol Benzaldehyde


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `prepare-permanganate` | ReagentPrep | Prepare 0.1% KMnO4 solution (0.1 g / 100 mL) | - | Dissolve 0.1 g potassium permanganate in 100 mL purified water. |
| 2 | `oxidise-benzaldehyde` | Synthesis | Add benzaldehyde to KMnO4 and warm to oxidise | prepare-permanganate | Add benzaldehyde to the permanganate; warm until the violet colour is discharged. |
| 3 | `filter-mno2` | Synthesis | Filter off the brown MnO2 sludge | oxidise-benzaldehyde | Filter the hot mixture to remove the brown manganese dioxide. |
| 4 | `acidify` | Synthesis | Acidify the filtrate with 6N HCl to precipitate benzoic acid | filter-mno2 | Add 6N HCl until acidic; white benzoic acid crystals separate. |
| 5 | `crystallise` | Synthesis | Cool in an ice bath; recrystallise from hot water | acidify | Chill to complete crystallisation, then recrystallise from the minimum hot water. |
| 6 | `test-litmus` | ChemicalTests | Litmus test: turns blue litmus red (acidic) | crystallise | Dissolve a little product; blue litmus turning red confirms a carboxylic acid. |
| 7 | `test-fecl3` | ChemicalTests | FeCl3 test: buff / salmon precipitate | crystallise | Neutralise to sodium benzoate then add FeCl3; a buff precipitate confirms benzoate. |
| 8 | `test-ester` | ChemicalTests | Ester test with propyl alcohol (fruity odour) | crystallise | Warm with propyl alcohol + a drop of H2SO4; a fruity ester odour confirms the acid. |
| 9 | `record-yield` | DataSheet | Record yield and observations on the data sheet | test-litmus, test-fecl3, test-ester | Enter the crystal mass, % yield and each test result. |

### Stage layout

**Stations:** `filter-mno2` (Filter) ; `crystallise` (Crystallise) ; `test-litmus` (zone-touch)

**Reagents staged (pourable):** Potassium Permanganate (Vial_Brown, auto-supply) ; Benzaldehyde (Vial_Brown, auto-supply) ; Hydrochloric Acid 6N (Vial_Brown, auto-supply) ; Ferric Chloride 10% (Vial_Brown, auto-supply) ; Sulfuric Acid (Vial_Brown, auto-supply) ; Propyl Alcohol (Vial_Brown, auto-supply)

**Tools staged:** Funnel ; Watch Glass ; Dropper

**Vessel Beaker_500mL** (Reaction Beaker) - starts EMPTY
  - pour **Potassium Permanganate** >= 50 ml -> completes `prepare-permanganate`
  - pour **Benzaldehyde** >= 50 ml -> completes `oxidise-benzaldehyde`
  - pour **Hydrochloric Acid 6N** >= 50 ml -> completes `acidify`

**Vessel TestTube_WithLiquid** (Benzoate Test Tube) - starts with Benzoic Acid
  - pour **Ferric Chloride 10%** >= 50 ml -> completes `test-fecl3`
  - pour **Sulfuric Acid** >= 50 ml -> completes `test-ester`
  - pour **Propyl Alcohol** >= 50 ml -> completes `test-ester`

### Reactions & expected observations

- **BenzoicOxidation**: Benzaldehyde + Potassium Permanganate -> Benzoic Acid - "Violet discharged; brown MnO2 forms"
- **Draft_BenzoateFeCl3**: Benzoic Acid + Ferric Chloride 10% -> Benzoic Acid - "Buff / salmon precipitate confirms benzoate (standard FeCl3"
- **Test_BenzoateEster**: Benzoic Acid + Propyl Alcohol -> Ethyl Ester - "Sweet aromatic ester odour \u2014 benzoic acid + propyl alcohol"

### Quiz (Documentation score)

1. **Benzoic acid is prepared here by oxidising:**
   - Toluene
   - Benzaldehyde [CORRECT]
   - Phenol
   - Aniline
   - *why:* Benzaldehyde is oxidised by dilute KMnO4 to benzoic acid.
2. **The brown solid filtered off after oxidation is:**
   - Carbon
   - Iron oxide
   - Manganese dioxide [CORRECT]
   - Silver
   - *why:* Reduced permanganate leaves manganese dioxide (MnO2).
3. **Why acidify the filtrate with HCl?**
   - To precipitate benzoic acid [CORRECT]
   - To dissolve crystals
   - To add colour
   - To neutralise nothing
   - *why:* It converts soluble benzoate back to insoluble benzoic acid.

---

## midterm-acetanilide

**moduleId** `midterm-acetanilide` | **period** Midterm | **prerequisite** `midterm-benzoic-acid` | **manuscript** Exp 5 | **end product** Acetanilide

**Official ILOs:** (1) Synthesize acetanilide. (2) Determine its identity through chemical tests.

**Manuscript Equipment & Apparatus:** S-clamp; Aspirator; Stirring rod; Beaker; Test tubes; Condenser; Test tube brush; Cork; Test tube holder; Distilling flask; Thermometer; Erlenmeyer flask; Iron clamp; Iron stand; Watch glass; Pipette; Water; Porcelain spatula; bath Rubber tubing

**Manuscript Reagents:** Bromine water; 0.1N Hydrochloric acid; Concentrated hydrochloric acid; Acetyl chloride; Glacial acetic acid; Aniline


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `prep-hcl` | ReagentPrep | Prepare 0.1N HCl (2.1 mL conc HCl \u2192 250 mL) | - | Slowly dilute 2.1 mL concentrated HCl to 250 mL in a volumetric flask. |
| 2 | `measure-aniline` | ReagentPrep | Measure 2 mL aniline in the fume hood | prep-hcl | Aniline is toxic - always work in the fume hood. |
| 3 | `add-acetic` | Synthesis | Add 2 mL glacial acetic acid | measure-aniline | Add glacial acetic acid to the aniline. |
| 4 | `add-acylating` | Synthesis | Carefully add 2 mL acetyl chloride (fume hood) | add-acetic | Add acetyl chloride slowly; it reacts vigorously. (Client flag: acetic anhydride is the safer alternative.) |
| 5 | `heat-bath` | Synthesis | Heat gently ~5 min in a water bath | add-acylating | Warm in a water bath about 5 minutes, then cool. |
| 6 | `ice-crystallise` | Synthesis | Add 20 mL ice-cold water to crystallise | heat-bath | Pour into ice-cold water to precipitate acetanilide. |
| 7 | `filter-wash` | Synthesis | Filter, wash 3\xD7 with water, dry | ice-crystallise | Filter the crystals, wash three times, and dry. |
| 8 | `test-hydrolysis` | ChemicalTests | Hydrolysis with conc HCl (crystals reappear) | filter-wash | Boil with conc HCl; on cooling crystals of the amine salt appear. |
| 9 | `test-bromination` | ChemicalTests | Bromination vs aniline (bromine uptake) | filter-wash | Add bromine water; compare the amount needed against free aniline. |
| 10 | `record-yield` | DataSheet | Record % yield and observations | test-hydrolysis, test-bromination | Enter crystal mass, % yield and test results. |

### Stage layout

**Stations:** `heat-bath` (Heat to 85 C) ; `ice-crystallise` (Crystallise) ; `filter-wash` (Filter) ; `test-hydrolysis` (zone-touch)

**Reagents staged (pourable):** Hydrochloric Acid 0.1N (Vial_Brown, auto-supply) ; Aniline (Vial_Brown, auto-supply) ; Glacial Acetic Acid (Vial_Brown, auto-supply) ; Acetyl Chloride (Vial_Brown, auto-supply) ; Bromine Water (Vial_Brown, auto-supply)

**Tools staged:** Bunsen Burner ; Watch Glass ; Funnel ; Test Tube

**Vessel ErlenmeyerFlask_400mL** (Reaction Flask) - starts EMPTY
  - pour **Hydrochloric Acid 0.1N** >= 50 ml -> completes `prep-hcl`
  - pour **Aniline** >= 50 ml -> completes `measure-aniline`
  - pour **Glacial Acetic Acid** >= 50 ml -> completes `add-acetic`
  - pour **Acetyl Chloride** >= 50 ml -> completes `add-acylating`

**Vessel TestTube_WithLiquid** (Bromination Test Tube) - starts with Acetanilide
  - pour **Bromine Water** >= 50 ml -> completes `test-bromination`

### Reactions & expected observations

- **Acetanilide**: Aniline + Acetyl Chloride - "White acetanilide plates"
- **Test_AcetanilideBromination**: Acetanilide + Bromine Water -> Acetanilide - "Bromine water is decolorised to a yellow solution \u2014"

### Quiz (Documentation score)

1. **Acetanilide is formed by acetylating:**
   - Phenol
   - Aniline [CORRECT]
   - Benzene
   - Toluene
   - *why:* Aniline's -NH2 is acetylated to the amide.
2. **Why must aniline be handled in the fume hood?**
   - It is flammable only
   - It is harmless
   - It is toxic and volatile [CORRECT]
   - It is radioactive
   - *why:* Aniline is toxic and volatile.
3. **Acetanilide contains which functional group?**
   - Amide [CORRECT]
   - Alcohol
   - Ketone
   - Ester
   - *why:* The product is an amide.

---

## midterm-acetone

**moduleId** `midterm-acetone` | **period** Midterm | **prerequisite** `midterm-acetanilide` | **manuscript** Exp 6 | **end product** Acetone

**Official ILOs:** (1) Synthesize acetone. (2) Determine its identity through chemical tests.

**Manuscript Equipment & Apparatus:** S-clamp; Aspirator; Stirring rod; Beaker; Test tubes; Condenser; Test tube brush; Cork; Test tube holder; Distilling flask; Thermometer; Erlenmeyer flask; Iron clamp; Iron stand; Watch glass; Pipette; Water; Porcelain spatula; bath Rubber tubing

**Manuscript Reagents:** 5% Sodium hypochlorite; Saturated sodium; bisulfite 10% Potassium hydroxide solution Schiff's reagent; Calcium acetate; Sodium acetate; Ethyl alcohol; Tollen's reagent


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `prep-koh` | ReagentPrep | Prepare 10% KOH (10 g / 100 mL) | - | Dissolve 10 g KOH in 100 mL purified water. |
| 2 | `weigh-acetates` | ReagentPrep | Weigh & mix 7 g calcium acetate + 7 g sodium acetate | prep-koh | Mix the two dry acetate powders in a test tube. |
| 3 | `setup-distill` | Synthesis | Set the tube horizontal, connect the condenser | weigh-acetates | Tap a channel along the tube and connect it to a condenser. |
| 4 | `heat-glow` | Synthesis | Heat until the acetates glow, turning the tube | setup-distill | Heat strongly, rotating the tube for even decomposition. |
| 5 | `collect-56` | Synthesis | Distil and collect the fraction at 56 \xB0C | heat-glow | Acetone boils at 56 \xB0C - collect only that cut. |
| 6 | `test-tollens` | ChemicalTests | Tollen's test (no silver mirror \u2014 ketone) | collect-56 | Ketones do not reduce Tollen's reagent, unlike aldehydes. |
| 7 | `test-schiff` | ChemicalTests | Schiff's reagent | collect-56 | Note the colour after standing. |
| 8 | `test-iodoform` | ChemicalTests | Iodoform-type test (KI + NaOCl \u2192 yellow) | collect-56 | Warm with KI and sodium hypochlorite; a yellow precipitate/odour is positive. |
| 9 | `test-bisulfite` | ChemicalTests | Bisulfite addition product | collect-56 | Shake acetone with saturated sodium bisulfite; a crystalline adduct forms. |
| 10 | `record-yield` | DataSheet | Record % yield and observations | test-tollens, test-schiff, test-iodoform, test-bisulfite | Enter distillate mass, % yield and test results. |

### Stage layout

**Stations:** `prep-koh` (zone-touch) ; `weigh-acetates` (Weigh) ; `setup-distill` (zone-touch) ; `heat-glow` (Heat to 150 C) ; `collect-56` (Collect) ; `test-schiff` (zone-touch) ; `test-bisulfite` (zone-touch)

**Reagents staged (pourable):** Silver Nitrate (Vial_Brown, auto-supply) ; Sodium Hypochlorite (Vial_Brown, auto-supply) ; Schiff's Reagent (Vial_Brown, auto-supply) ; Potassium Iodide 10% (Vial_Brown, auto-supply)

**Tools staged:** KOH Spatula ; Acetate Scoop ; Retort Stand ; Bunsen Burner ; Collection Beaker ; Bisulfite Dish

**Vessel ErlenmeyerFlask_400mL** (Distillation Flask) - starts EMPTY

**Vessel TestTube_WithLiquid** (Product Test Tube) - starts with Acetone
  - pour **Silver Nitrate** >= 50 ml -> completes `test-tollens`
  - pour **Sodium Hypochlorite** >= 50 ml -> completes `test-iodoform`
  - pour **Schiff's Reagent** >= 50 ml -> completes `test-schiff`
  - pour **Potassium Iodide 10%** >= 50 ml -> completes `test-iodoform`

### Reactions & expected observations

- **Test_AcetoneIodoform**: Acetone + Sodium Hypochlorite - "Yellow iodoform precipitate with antiseptic odour (haloform test, manual Exp 6)."
- **Test_AcetoneSchiff**: Acetone + Schiff's Reagent -> Acetone - "No magenta colour restored — acetone is a ketone (negative"
- **Test_AcetoneTollens**: Acetone + Silver Nitrate -> Acetone - "No silver mirror \u2014 acetone is a ketone (negative Tollen's"

### Quiz (Documentation score)

1. **Acetone is prepared by dry distillation of:**
   - Sodium chloride
   - Acetate salts [CORRECT]
   - Sugar
   - Limestone
   - *why:* Calcium/sodium acetate decompose to acetone.
2. **The acetone fraction is collected at about:**
   - 100 C
   - 78 C
   - 56 C [CORRECT]
   - 150 C
   - *why:* Acetone boils at 56 degrees.
3. **Acetone is a ketone, so Tollen''s test is:**
   - Negative [CORRECT]
   - Positive
   - Explosive
   - Yellow
   - *why:* Ketones do not reduce Tollen's reagent (no silver mirror).

---

## midterm-chloroform

**moduleId** `midterm-chloroform` | **period** Midterm | **prerequisite** `midterm-acetone` | **manuscript** Exp 7 | **end product** Chloroform

**Official ILOs:** (1) Synthesize chloroform. (2) Determine its identity through chemical tests.

**Manuscript Equipment & Apparatus:** S-clamp; Aspirator; Stirring rod; Beaker; Test tubes; Condenser; Test tube brush; Cork; Test tube holder; Distilling flask; Thermometer; Erlenmeyer flask; Iron clamp; Iron stand; Watch glass; Pipette; Water; Porcelain spatula; bath Rubber tubing

**Manuscript Reagents:** Concentrated sulfuric acid; Acetone; Potassium; Alcoholic silver nitrate; dichromate Bleaching powder


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `prep-bleach` | ReagentPrep | Dissolve 150 g bleaching powder in 400 mL water (1 L flask) | - | Shake until the bleaching powder dissolves. |
| 2 | `prep-acetone-mix` | ReagentPrep | Mix 12 g acetone + 50 mL water in a separatory funnel | prep-bleach | Prepare the acetone-water mixture for dropwise addition. |
| 3 | `reflux-setup` | Synthesis | Connect the flask to a reflux condenser | prep-bleach | Fit the reflux condenser before adding acetone. |
| 4 | `add-dropwise` | Synthesis | Add the acetone-water mixture dropwise | reflux-setup, prep-acetone-mix | Add slowly through the condenser; the reaction is exothermic. |
| 5 | `distil` | Synthesis | Distil off the crude chloroform | add-dropwise | Rearrange for distillation and collect the dense chloroform layer. |
| 6 | `decant-wash` | Synthesis | Wash by decantation with water (\xD72) | distil | Decant off the water twice to wash the chloroform. |
| 7 | `dry-redistil` | Synthesis | Dry over CaCl2 and redistil until clear | decant-wash | Dry with anhydrous calcium chloride, then redistil to a clear liquid. |
| 8 | `test-flammability` | ChemicalTests | Non-flammability test | dry-redistil | Chloroform does not readily burn - unlike the acetone feedstock. |
| 9 | `test-oxidation` | ChemicalTests | Oxidation test (dichromate + H2SO4) | dry-redistil | Warm chloroform with potassium dichromate and conc. H2SO4 - the pungent phosgene-like odour confirms it. Fume hood! |
| 10 | `test-agno3` | ChemicalTests | Alcoholic AgNO3 (no precipitate cold) | dry-redistil | Alcoholic silver nitrate gives no precipitate in the cold with chloroform. |
| 11 | `record-yield` | DataSheet | Record % yield and observations | test-flammability, test-agno3, test-oxidation | Enter chloroform mass, % yield and test results. |

### Stage layout

**Stations:** `reflux-setup` (zone-touch) ; `add-dropwise` (zone-touch) ; `distil` (Heat to 78 C) ; `decant-wash` (Filter) ; `dry-redistil` (Heat to 78 C) ; `test-flammability` (zone-touch)

**Reagents staged (pourable):** Bleaching Powder (Vial_Brown, auto-supply) ; Acetone (Vial_Brown, auto-supply) ; Silver Nitrate (Vial_Brown, auto-supply) ; Potassium Dichromate (Vial_Brown, auto-supply)

**Tools staged:** Retort Stand ; Dropper ; Distillate Beaker ; Separating Funnel ; Watch Glass ; Lit Splint

**Vessel ErlenmeyerFlask_400mL** (Reflux Flask) - starts EMPTY
  - pour **Bleaching Powder** >= 50 ml -> completes `prep-bleach`
  - pour **Acetone** >= 50 ml -> completes `prep-acetone-mix`

**Vessel TestTube_WithLiquid** (AgNO3 Test Tube) - starts with Chloroform
  - pour **Silver Nitrate** >= 50 ml -> completes `test-agno3`
  - pour **Potassium Dichromate** >= 50 ml -> completes `test-oxidation`

### Reactions & expected observations

- **Chloroform**: Acetone + Bleaching Powder -> Chloroform - "Dense, sweet, non-flammable chloroform"
- **Test_AcetoneTollens**: Acetone + Silver Nitrate -> Acetone - "No silver mirror \u2014 acetone is a ketone (negative Tollen's"
- **Test_ChloroformAgNO3**: Chloroform + Silver Nitrate -> Chloroform - "White precipitate (AgCl) forms on warming with alcoholic silver"
- **Test_ChloroformOxidation**: Chloroform + Potassium Dichromate - "Pungent, suffocating odour (phosgene-like) \u2014 oxidation"

### Quiz (Documentation score)

1. **Chloroform is made by reacting bleaching powder with:**
   - Ethanol
   - Acetone [CORRECT]
   - Benzene
   - Phenol
   - *why:* The haloform reaction uses acetone.
2. **A distinctive physical property of chloroform is that it is:**
   - Highly flammable
   - A gas at room temperature
   - Non-flammable and dense [CORRECT]
   - Water-soluble in all ratios
   - *why:* Chloroform is dense and non-flammable.
3. **The acetone-water mix is added to the flask:**
   - Drop by drop [CORRECT]
   - All at once
   - After distillation
   - Frozen
   - *why:* Dropwise, to control the exothermic reaction.

---

## final-benzamide

**moduleId** `final-benzamide` | **period** Final | **prerequisite** `midterm-chloroform` | **manuscript** Exp 8 | **end product** Benzamide

**Official ILOs:** (1) Synthesize benzamide. (2) Determine its identity through chemical tests.

**Manuscript Equipment & Apparatus:** S-clamp; Aspirator; Stirring rod; Beaker; Test tubes; Condenser; Test tube brush; Cork; Test tube holder; Distilling flask; Thermometer; Erlenmeyer flask; Iron clamp; Iron stand; Watch glass; Pipette; Water; Porcelain spatula; bath Rubber tubing

**Manuscript Reagents:** Concentrated; 10% Sodium hydroxide; Diluted hydrochloric; ammonia 10% Sodium nitrate solution; acid Benzoyl chloride


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `measure-ammonia` | ReagentPrep | Place 5 mL concentrated ammonia in a 50 mL flask (fume hood) | - | Measure concentrated ammonia in the fume hood. |
| 2 | `ice-bath` | ReagentPrep | Cool the flask in an ice bath | measure-ammonia | Chilling controls the vigorous reaction. |
| 3 | `add-benzoyl` | Synthesis | Add 1 mL benzoyl chloride dropwise, shaking | ice-bath | Add benzoyl chloride drop by drop, shaking frequently. |
| 4 | `stand` | Synthesis | Let stand 10\u201315 minutes | add-benzoyl | Allow the benzamide to crystallise fully. |
| 5 | `filter-dry` | Synthesis | Filter, wash with cold water, oven-dry | stand | Collect the precipitate and dry it in the oven. |
| 6 | `test-alkali` | ChemicalTests | Alkaline hydrolysis (ammonia vapour \u2192 blue litmus) | filter-dry | Boil with 10% NaOH; ammonia vapour turns moist litmus blue. |
| 7 | `test-acid` | ChemicalTests | Acid hydrolysis (dil HCl) | filter-dry | Boil with dilute HCl; test the vapour with litmus. |
| 8 | `test-nitrous` | ChemicalTests | Nitrous-acid test (dil HCl + sodium nitrite) | filter-dry | Add sodium nitrite to the acidified sample. (Manual's 'nitrate' is a typo - use nitrite.) |
| 9 | `record-yield` | DataSheet | Record % yield and observations | test-alkali, test-acid, test-nitrous | Enter crystal mass, % yield and test results. |

### Stage layout

**Stations:** `ice-bath` (Crystallise) ; `stand` (Stir) ; `filter-dry` (Filter) ; `test-alkali` (zone-touch) ; `test-nitrous` (zone-touch)

**Reagents staged (pourable):** Ammonia Solution (Vial_Brown, auto-supply) ; Benzoyl Chloride (Vial_Brown, auto-supply) ; Diluted Hydrochloric Acid (Vial_Brown, auto-supply) ; Sodium Hydroxide (Vial_Brown, auto-supply) ; Sodium Nitrite (Vial_Brown, auto-supply)

**Tools staged:** Ice Bath Dish ; Stirring Rod ; Funnel ; Alkali Test Tube ; Dropper

**Vessel Beaker_500mL** (Reaction Beaker) - starts EMPTY
  - pour **Ammonia Solution** >= 50 ml -> completes `measure-ammonia`
  - pour **Benzoyl Chloride** >= 50 ml -> completes `add-benzoyl`

**Vessel TestTube_WithLiquid** (Product Test Tube) - starts with Benzamide
  - pour **Diluted Hydrochloric Acid** >= 50 ml -> completes `test-acid`
  - pour **Sodium Hydroxide** >= 50 ml -> completes `test-alkali`
  - pour **Sodium Nitrite** >= 50 ml -> completes `test-nitrous`

### Reactions & expected observations

- **Benzamide**: Benzoyl Chloride + Ammonia Solution - "White benzamide solid"
- **Test_BenzamideAcid**: Benzamide + Diluted Hydrochloric Acid - "White benzoic acid crystals separate on boiling with acid"
- **Test_BenzamideAlkali**: Benzamide + Sodium Hydroxide -> Benzamide - "Ammonia gas evolved on boiling \u2014 turns red litmus blue"
- **Test_BenzamideNitrous**: Benzamide + Sodium Nitrite -> Benzamide - "Brisk effervescence \u2014 nitrogen gas is evolved with nitrous"

### Quiz (Documentation score)

1. **Benzamide is prepared from ammonia and:**
   - Benzoic acid
   - Benzoyl chloride [CORRECT]
   - Benzaldehyde
   - Aniline
   - *why:* Benzoyl chloride reacts with ammonia to give benzamide.
2. **Why is the reaction run in an ice bath?**
   - To freeze the product
   - To speed it up
   - To control the vigorous reaction [CORRECT]
   - To colour it
   - *why:* Benzoyl chloride reacts vigorously; cooling controls it.
3. **Alkaline hydrolysis of benzamide releases which gas?**
   - Ammonia [CORRECT]
   - Hydrogen
   - Chlorine
   - Oxygen
   - *why:* Boiling with NaOH liberates ammonia.

---

## final-winemaking

**moduleId** `final-winemaking` | **period** Final | **prerequisite** `final-benzamide` | **manuscript** Exp 9 (real-world group activity; grapes EXCLUDED) | **end product** Wine

**Official ILOs:** (1) Learn the basic methodology in preparation and synthesis of alcohol using the fermentation technique with basic household ingredients.


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `seal-airlock` | Synthesis | Seal the vessel with an airlock / balloon | prepare-must | Fit the airlock so CO2 escapes but air cannot enter. |
| 2 | `confirm-co2` | ChemicalTests | Confirm fermentation \u2014 CO2 turns limewater milky | seal-airlock | Bubble the evolved gas through limewater; it turns milky. |
| 3 | `ferment-timeskip` | Synthesis | Ferment \u20187 days & 7 nights\u2019 (time-skip) | confirm-co2 | Let fermentation run to completion (time-skip montage). |
| 4 | `rack` | Synthesis | Rack the wine off the lees | ferment-timeskip | Siphon the clear wine off the sediment. |
| 5 | `test-clarity` | ChemicalTests | Assess clarity and appearance | rack | Judge colour and clarity against the standard. |
| 6 | `test-tasting` | ChemicalTests | Tasting finale \u2014 evaluate flavour | rack | Evaluate aroma and flavour at the tasting finale. |
| 7 | `record-label` | DataSheet | Create the label and record volume & observations | test-clarity, test-tasting | Design the label and log the final volume and notes. |

### Stage layout

**Stations:** `seal-airlock` (zone-touch) ; `ferment-timeskip` (zone-touch) ; `rack` (zone-touch) ; `test-clarity` (zone-touch) ; `test-tasting` (zone-touch)

**Reagents staged (pourable):** Brown Sugar (Vial_Brown, auto-supply) ; Limewater (Vial_Brown, auto-supply)

**Tools staged:** Airlock Tube ; Stirring Rod ; Racking Funnel ; Watch Glass ; Tasting Glass

**Vessel Beaker_500mL** (Fermentation Jar) - starts with Mixed Fruit Juice
  - pour **Brown Sugar** >= 50 ml -> completes `prepare-must`

**Vessel TestTube_WithLiquid** (CO2 Collection Tube) - starts with Carbon Dioxide
  - pour **Limewater** >= 50 ml -> completes `confirm-co2`

### Reactions & expected observations

- **Limewater_CO2**: Limewater + Carbon Dioxide - "Limewater turns milky (CaCO3)"

### Quiz (Documentation score)

1. **In winemaking, yeast converts sugar into ethanol and:**
   - Oxygen
   - Carbon dioxide [CORRECT]
   - Methane
   - Water only
   - *why:* Fermentation releases CO2.
2. **The airlock on the vessel allows:**
   - Air in freely
   - Nothing to move
   - CO2 out but no air in [CORRECT]
   - Sugar in
   - *why:* CO2 out while keeping air/contaminants from entering.
3. **Evolved gas turning limewater milky confirms:**
   - Carbon dioxide is being produced [CORRECT]
   - The wine is finished
   - Oxygen is present
   - The yeast is dead
   - *why:* Milky limewater confirms carbon dioxide from fermentation.