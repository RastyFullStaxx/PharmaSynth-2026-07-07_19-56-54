# Manuscript Apparatus Not Implemented in the VR Build

**Purpose.** This document lists every item of equipment named in the client manuscript (Appendix C, Experiments 1–9) that is **not present in the PharmaSynth VR laboratory**, and explains why each was omitted and what the student uses in its place.

**Method.** The manuscript's "Equipment and Apparatus" and "Reagents" lists for all nine experiments were extracted from `manuscript.pdf`, combined into a single de-duplicated list, and compared against the actual contents of the VR laboratory scene (bench apparatus and stocked reagent cabinets).

**Date of audit:** 19 July 2026.

---

## 1. Reagents — complete, no omissions

**All 51 distinct reagents named in Appendix C are implemented** and available to the student as usable bottles on the laboratory shelves. There is no missing reagent in any of the nine experiments.

Two clarifications worth recording:

- The manuscript's Experiment 8 reagent list names **"10% Sodium nitrate solution"**. This is a transcription error: the nitrous-acid test it is used for requires sodium **nitrite**. The VR build implements sodium nitrite, which is the chemically correct reagent for that test.
- Experiment 9 does not specify a fruit. Per the manuscript's own exclusion ("Grapes will be excluded in the choices"), the build supplies a generic **Mixed Fruit Juice**.

---

## 2. Apparatus named in the manuscript but not implemented

Of the 19 distinct pieces of equipment listed across Appendix C, nine are implemented and ten are not. The ten omissions are listed below with the reason for each.

### 2.1 Clamp-and-stand scaffolding

| Manuscript item | Used for | Why it is not in the VR build | What the student uses instead |
|---|---|---|---|
| **Iron stand** (retort stand) | Holding glassware above a flame | These exist only to hold apparatus still. In VR the student's own hands do that, and the tracked controllers make clamping a vessel to a vertical pole a fiddly alignment exercise that teaches nothing about chemistry. Attempting it repeatedly is a common source of frustration in VR chemistry titles. | The vessel is held in hand, or set on the **tripod and wire gauze** (both implemented) over the burner. |
| **Iron clamp** | Securing a flask to the stand | As above — it is a fastener, not a chemistry decision. | Not required; the heat platform holds the vessel. |
| **S-clamp** | Securing tubing/condenser joints | As above. | Not required; no assembled glass train is used. |

### 2.2 Apparatus superseded by a clearer in-game system

| Manuscript item | Used for | Why it is not in the VR build | What the student uses instead |
|---|---|---|---|
| **Thermometer** | Reading the temperature of a heated mixture | A physical thermometer in VR must be read at an angle, at small scale, and is easily misread or lost in the vessel. The build replaces it with a **live temperature readout attached to the vessel itself**, which is always legible and additionally states the *target* temperature (e.g. "62 °C → warm to 65 °C"). This is strictly more informative than the physical instrument. | The floating vessel readout and the vessel's name tag. |
| **Condenser** | Condensing vapour during distillation | A glass condenser requires a water-hose assembly to be built and connected. That is a plumbing task, not a chemistry task, and it would gate every distillation behind an assembly puzzle. Distillation is instead modelled directly: a heated source gives off vapour that condenses into a receiving vessel held at its mouth. | The **receiving vessel** (beaker, flask or test tube) held at the mouth of the heated source, with the distillate volume shown as it collects. |

### 2.3 Apparatus no procedure actually uses

| Manuscript item | Used for | Why it is not in the VR build |
|---|---|---|
| **Aspirator** | Suction / vacuum filtration | The aspirator appears in the standard equipment list that the manuscript repeats for Experiments 2–9, but **no procedure in Appendix C performs a vacuum filtration**. All filtration in the manuscript is gravity filtration through a funnel, which is implemented. The aspirator is therefore unused equipment inherited from the list header. |

### 2.4 Assembly connectors with no interactive decision

| Manuscript item | Used for | Why it is not in the VR build |
|---|---|---|
| **Cork** | Stoppering flasks, joining delivery tubes | Corks and tubing exist only to join two other pieces of apparatus. Since the build does not require the student to assemble a glass train (see *Condenser* above), a separate connector item would be an inventory object with no choice attached to it. Where the manuscript's chemistry depends on a *sealed* vessel — Experiment 9's fermentation airlock — the seal is represented as part of the step itself rather than as a part to fit. |
| **Rubber tubing** | Carrying gas or condenser water | As above. |

### 2.5 Apparatus substituted by an equivalent tool

| Manuscript item | Why it is not in the VR build | What the student uses instead |
|---|---|---|
| **Pipette** | Operating a pipette bulb or plunger is a fine-motor dexterity task rather than a chemistry one, and it does not survive translation to controller input. The build instead standardises measurement into a consistent "verb contract" that carries the same quantitative meaning. | The **dropper** for counted drops and small millilitre amounts, and a **tilt-pour into a tolerance band** for bulk volumes. The quantity asked for in the manuscript is preserved exactly. |
| **Test tube holder** | The manuscript's holder is a hand-held clamp for gripping a hot tube. In VR, hot glassware is not gripped — it is placed. | The **test tube racks** (storage racks and workspace holders) and the **water bath**, which holds tubes while they are heated. |
| **Separatory funnel** *(named in the Experiment 7 procedure)* | In Experiment 7 the separatory funnel is used only to *pre-mix* acetone with water before dropwise addition. No layer separation is performed in it, so its distinguishing function is never exercised. | The **50 mL graduated cylinder**, which performs the same mixing with a clearer volume reading. |
| **1-Litre flask** *(named in the Experiment 7 procedure)* | The manuscript's 150 g / 400 mL quantities are scaled down for a VR bench so that fills are visible and the run is a sensible length. | The **400 mL Erlenmeyer flask**, with all quantities scaled consistently. The manuscript's true quantities are still shown to the student in the step instructions. |

### 2.6 Equipment replaced by a time compression

| Manuscript item | Why it is not in the VR build | What the student sees instead |
|---|---|---|
| **Oven** *(named in the Experiment 8 procedure: "dry using an oven")* | Oven-drying a precipitate takes 30 minutes or more of real time, during which nothing is learned and nothing can be interacted with. | The "filter, wash and oven-dry" step plays as a **time-skip**: the screen fades and returns with a message confirming the crystals are dried, exactly as the manuscript's fermentation and crystallisation waits are handled. |

---

## 3. Manuscript content that is not laboratory apparatus

These parts of the manuscript are not implemented as VR bench activities. They are recorded here so the omission is explicit rather than accidental.

| Manuscript content | Status and reason |
|---|---|
| **Experiment 1 — Stoichiometry** | Not a bench experiment. Appendix C's Experiment 1 is a pen-and-paper calculation exercise (balancing equations, molecular weights, percentage yield) with no laboratory procedure. The skills it teaches are instead assessed inside the other experiments, where percentage yield is recorded on each data sheet. |
| **Experiment 9 — group-activity requirements** | Experiment 9 is defined in the manuscript as a *group activity* with no bench procedure: 3–5 member groups, assigned roles, a 15–20 minute documentary video, a table presentation with costume, and a wine-tasting event. These are classroom and assessment activities that cannot be performed by a single player in VR. The **chemistry** of the activity — preparing the must, fermentation, confirming CO₂, racking, and evaluating the product — is fully implemented; the group logistics and presentation components are not. |
| **Reagent preparation instructions** | Appendix C includes instructions for preparing stock solutions (e.g. "dissolve 10 g of potassium iodide in 100 ml of purified water"). The VR laboratory supplies these solutions ready-made at their stated concentrations, as a real teaching laboratory would. The concentration is preserved in every reagent's label and name. |

---

## 4. Appendix — equipment removed that the manuscript never required

For completeness: the following items were present in the project's 3D asset library but were removed from the laboratory because **no procedure in Appendix C calls for them**. They are not manuscript omissions; they were surplus scenery.

Iron ring (×2), clay triangle, crucible, crucible tongs, alcohol burner, empty sample vials (×8), and forceps.

The 3D models for all removed items — including the clamps, stand, condenser, thermometer and aspirator listed in section 2 — are **retained in the project's asset folders**. If a future revision of the curriculum requires any of them, they can be reinstated without new art being produced.

---

## 5. Summary

| Category | Count | Status |
|---|---|---|
| Reagents named in Appendix C | 51 | **All implemented** |
| Apparatus named in Appendix C | 19 | 9 implemented, 10 omitted (documented above) |
| Experiments with bench procedures | 8 (Exp 2–9) | **All implemented and playable end-to-end** |
| Experiments without bench procedures | 1 (Exp 1, stoichiometry) | Not applicable to a VR bench |

Every omission falls into one of five categories: **apparatus that only holds other apparatus** (stands and clamps), **apparatus replaced by a clearer digital equivalent** (thermometer, condenser), **apparatus no procedure uses** (aspirator), **apparatus substituted by a tool that performs the same measurement** (pipette, holder, separatory funnel), or **waiting time compressed into a time-skip** (oven).

No omission removes a chemical reaction, a reagent, an observation, or a learning outcome from any experiment.
