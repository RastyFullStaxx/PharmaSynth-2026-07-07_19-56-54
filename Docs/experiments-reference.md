# Experiments Reference — the 11 PharmaSynth modules

**Generated 2026-07-12 (W5.11) from the live ScriptableObject data + manuscript Appendix C.** This is the canonical per-experiment reference: open it whenever a task touches an experiment's chemistry, steps, reagents, tests, quiz or stage layout. Regenerate the data body after content changes with the extraction script pattern (see `Docs/systems-reference.md` §Authoring) or update by hand and keep the suite's ContentSuite counts in sync.

**Authority:** the client manuscript (`Docs/Documentations/manuscript.pdf`, extract via `pdftotext -layout` → `manuscript.txt`; the Read tool cannot open PDFs on this machine). Appendix C (Experiments 1–9) is the lab manual. The storyboard is a reference to EXCEED — never a source for chemistry or labels. Evidence trail for every deviation: `Docs/manuscript-reconciliation.md`.

## Deviations & client flags (authoritative summary)

| Topic | Manuscript says | Game does | Status |
|---|---|---|---|
| Benzoic Acid route | Exp 4 "procedure" is a verbatim copy-paste of the ethanol fermentation (confirmed print defect); materials list names Benzaldehyde | Benzaldehyde + KMnO₄ oxidation | ✅ correct-by-intent; client rubber-stamp pending (§7 signoff) |
| Acetanilide acylating agent | Appendix C procedure: acetyl chloride (intro prose wrongly says anhydride) | Acetyl chloride | ✅ matches; optional safer-anhydride swap = client call |
| Benzamide nitrous test | Reagent header "sodium nitrate" (typo); procedure body: 10% sodium NITRITE | Sodium Nitrite | ✅ matches |
| Chemical Compounding battery | Multi-substrate ID lab: FeCl₃ enol, KMnO₄ rate-of-oxidation (3 butyl alcohols), Tollen's, ester formation, aspirin hydrolysis (+Benedict's on data sheet) | Single-substrate (ethanol): combustion / sodium / bromine water / KMnO₄ — only KMnO₄ overlaps | ⚠ **CLIENT DECISION** — full module redesign if restored (all needed chemicals already exist as assets); reconciliation §7 |
| Wine fruit | Grapes EXCLUDED (L3830-31) | Ferments **Mixed Fruit Juice** (renamed from Grape Juice, W5.9) | ✅ fixed |
| Chloroform oxidation test | Dichromate + conc H₂SO₄ (procedure + results sheet) | Added W5.9: `test-oxidation` + rule | ✅ fixed |
| Methane / Aspirin / Caffeine | Not in Appendix C (Aspirin named in intro prose only) | Game-authored procedures + ILOs | ⚠ pending client confirmation |
| Wine rubric | Bespoke tasting/presentation rubric (group activity) | Standard 6-category rubric | ✅ client-resolved 2026-07-09 |
| Yield | Data-sheet records yield | Record-only, never graded (±5 stepper on the quiz tablet) | ✅ client-resolved |

## How to read each section
- **Task graph** = the module's `graphTasks` in authored (play) order; `phase` drives the loop (ReagentPrep → Synthesis → ChemicalTests → DataSheet); prerequisites gate order (violation = WrongStep mistake); hints surface in the wrist checklist.
- **Stage layout** = what `ExperimentSceneBuilder` spawns on the center table from `Layout_*.asset`. Station sims: Heat/Crystallise/Filter/Collect run while the required prop occupies the zone; Stir/Grind/Weigh are tool verbs (W5.8); zone-touch completes on prop contact. Every stage ALSO auto-spawns: a test-tube RackKit (6 tubes), 2 spare beakers + 1 spare flask, and (Heat modules) 2 matches + a striker.
- **Vessel bindings** = pour ≥N ml of the named reagent into that vessel to complete the task (works held or on the table). `completesTask:false` = the pour is expected but the WEIGH station completes the task.
- **Reactions** = MasterReactionRegistry rules that can fire from this stage's chemical set; `expectedObservation` pops as floating text on reaction (W5.8 feedback layer).
- **Manuscript lists** are OCR-extracted verbatim; minor column-merge artifacts (e.g. "bath Rubber tubing" = "water bath" + "rubber tubing") are the PDF's two-column layout, kept as-is for fidelity. **These lists are the source for apparatus grouping/kits** (queued work, checklist §13a).

**Wine Making (Exp 9) materials note:** the manuscript frames it as a real-world group activity — 250–500 mL of a **non-grape** fruit juice, sugar, yeast, sealed fermentation vessel + airlock, ~1 week ferment, video documentation. Adapted for solo VR per client resolution (standard rubric); see reconciliation §4.

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


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `gather-ethanol` | ReagentPrep | Gather ethanol and test reagents | - | Collect ethanol, sodium, bromine water and KMnO4. |
| 2 | `test-combustion` | ChemicalTests | Combustion test (clean flame) | gather-ethanol | Ignite a little ethanol; note the flame. |
| 3 | `test-sodium` | ChemicalTests | Reaction with sodium (H2 evolved) | gather-ethanol | Add a small piece of sodium; watch for effervescence. |
| 4 | `test-bromine` | ChemicalTests | Bromine water test | gather-ethanol | Add bromine water; note any decolourisation. |
| 5 | `test-kmno4` | ChemicalTests | Oxidation with KMnO4 (purple\u2192brown) | gather-ethanol | Add dilute KMnO4; watch the colour change. |
| 6 | `record-observations` | DataSheet | Record observations on the data sheet | test-combustion, test-sodium, test-bromine, test-kmno4 | Note each test result on your tablet. |

### Stage layout

**Stations:** `test-combustion` (zone-touch) ; `test-sodium` (zone-touch)

**Reagents staged (pourable):** Ethanol (Vial_Brown, auto-supply) ; Bromine Water (Vial_Brown, auto-supply) ; Potassium Permanganate (Vial_Brown, auto-supply)

**Tools staged:** Lit Splint ; Sodium Scoop

**Vessel Beaker_100mL** (Test Beaker) - starts EMPTY
  - pour **Ethanol** >= 50 ml -> completes `gather-ethanol`
  - pour **Bromine Water** >= 50 ml -> completes `test-bromine`
  - pour **Potassium Permanganate** >= 50 ml -> completes `test-kmno4`

### Reactions & expected observations

- **Draft_EthanolOxidation**: Ethanol + Potassium Permanganate -> Ethanol - "Purple KMnO4 is decolorised as the alcohol is oxidised (manual"

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


### Task graph (play order)

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

## final-aspirin

**moduleId** `final-aspirin` | **period** Final | **prerequisite** `final-benzamide` | **manuscript** (game-authored; named in manuscript intro only) | **end product** Aspirin

**Official ILOs:** (1) (game-authored - pending client confirmation)


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `weigh-salicylic` | ReagentPrep | Weigh salicylic acid (10 g) | - | Weigh 10 g of salicylic acid into a dry conical flask. |
| 2 | `add-anhydride` | ReagentPrep | Add acetic anhydride + a few drops conc. H2SO4 (fume hood) | weigh-salicylic | In the fume hood, add 25 mL acetic anhydride and 5 drops conc. sulfuric acid. |
| 3 | `warm-waterbath` | Synthesis | Warm gently in a water bath (do NOT overheat) | add-anhydride | Warm in a water bath ~10 min. Overheating chars the product and forces a redo. |
| 4 | `crystallise-ice` | Synthesis | Add cold water and crystallise in an ice bath | warm-waterbath | Pour into cold water to destroy excess anhydride; chill on ice to crystallise. |
| 5 | `buchner-filter` | Synthesis | Collect crystals by Buchner (vacuum) filtration | crystallise-ice | Filter under vacuum and wash with a little cold water. |
| 6 | `test-fecl3` | ChemicalTests | FeCl3 test: no violet colour = acetylation complete | buchner-filter | Add FeCl3 to a sample; a violet colour means unreacted salicylic acid remains. |
| 7 | `record-yield` | DataSheet | Record % yield and observations on the data sheet | test-fecl3 | Enter the aspirin mass and calculate % yield from 10 g salicylic acid. |

### Stage layout

**Stations:** `weigh-salicylic` (Weigh) ; `warm-waterbath` (Heat to 85 C) ; `crystallise-ice` (Crystallise) ; `buchner-filter` (Filter)

**Reagents staged (pourable):** Salicylic Acid (Vial_Brown, auto-supply) ; Acetic Anhydride (Vial_Brown, auto-supply) ; Ferric Chloride 10% (Vial_Brown, auto-supply)

**Tools staged:** Bunsen Burner ; Watch Glass ; Buchner Funnel

**Vessel ErlenmeyerFlask_400mL** (Synthesis Flask) - starts EMPTY
  - pour **Salicylic Acid** >= 50 ml -> completes `weigh-salicylic` *(pour expected; the WEIGH station completes it)*
  - pour **Acetic Anhydride** >= 50 ml -> completes `add-anhydride`

**Vessel TestTube_WithLiquid** (Product Test Tube (FeCl3)) - starts with Salicylic Acid
  - pour **Ferric Chloride 10%** >= 50 ml -> completes `test-fecl3`

### Reactions & expected observations

- **AspirinSynthesis**: Salicylic Acid + Acetic Anhydride -> Aspirin - "White aspirin crystals on cooling"
- **FeCl3_Salicylate**: Ferric Chloride 10% + Salicylic Acid -> Ferric Chloride 10% - "Violet colour (free phenol present)"

### Quiz (Documentation score)

1. **Aspirin is made by acetylating salicylic acid with:**
   - Acetone
   - Acetic anhydride [CORRECT]
   - Ethanol
   - Sodium hydroxide
   - *why:* Acetic anhydride acetylates the phenol group.
2. **A purple colour with ferric chloride means the aspirin is:**
   - Pure
   - Overheated only
   - Impure (salicylic acid remains) [CORRECT]
   - Perfectly acetylated
   - *why:* Purple indicates unreacted salicylic acid (free phenol) \u2014
3. **Overheating the water bath during synthesis causes:**
   - Decomposition of the product [CORRECT]
   - A higher yield
   - Faster crystallisation
   - A purer product
   - *why:* Excess heat chars/decomposes the product \u2014 a failed batch.

---

## final-caffeine

**moduleId** `final-caffeine` | **period** Final | **prerequisite** `final-aspirin` | **manuscript** (game-authored) | **end product** Caffeine

**Official ILOs:** (1) (game-authored - pending client confirmation)


### Task graph (play order)

| # | task | phase | label | prerequisites | hint |
|---|------|-------|-------|---------------|------|
| 1 | `brew-tea` | ReagentPrep | Boil tea leaves with sodium carbonate | - | Sodium carbonate frees caffeine from tannins during boiling. |
| 2 | `filter-brew` | Synthesis | Filter the hot tea liquor | brew-tea | Filter off the leaves while hot. |
| 3 | `extract-dcm` | Synthesis | Extract with dichloromethane in a separatory funnel (fume hood) | filter-brew | Shake with DCM; caffeine partitions into the organic layer. Work in the fume hood. |
| 4 | `dry-na2so4` | Synthesis | Dry the DCM extract over anhydrous Na2SO4 | extract-dcm | Dry the solvent with sodium sulfate, then decant. |
| 5 | `evaporate` | Synthesis | Evaporate the solvent to crude caffeine | dry-na2so4 | Evaporate the DCM to leave crude caffeine. |
| 6 | `sublime` | Synthesis | Purify crystals by sublimation | evaporate | Gently sublime to obtain pure white caffeine needles. |
| 7 | `test-murexide` | ChemicalTests | Murexide test (purple residue) | sublime | Evaporate with HCl + H2O2, then expose to ammonia; a purple colour confirms caffeine. |
| 8 | `test-meltingpoint` | ChemicalTests | Melting point (~234\u2013236 \xB0C) | sublime | A sharp melting point near 235 \xB0C confirms purity. |
| 9 | `record-yield` | DataSheet | Record % yield and observations | test-murexide, test-meltingpoint | Enter caffeine mass, % yield and test results. |

### Stage layout

**Stations:** `filter-brew` (Filter) ; `evaporate` (zone-touch) ; `sublime` (zone-touch) ; `test-murexide` (zone-touch) ; `test-meltingpoint` (zone-touch)

**Reagents staged (pourable):** Sodium Carbonate (Vial_Brown, auto-supply) ; Dichloromethane (Vial_Brown, auto-supply) ; Sodium Sulfate (Vial_Brown, auto-supply) ; Murexide Reagent (Vial_Brown, auto-supply)

**Tools staged:** Funnel ; Evaporating Dish ; Sublimation Glass ; MP Capillary ; Mortar ; Pestle

**Vessel Beaker_500mL** (Boiling Beaker) - starts EMPTY
  - pour **Sodium Carbonate** >= 50 ml -> completes `brew-tea`
  - pour **Dichloromethane** >= 50 ml -> completes `extract-dcm`
  - pour **Sodium Sulfate** >= 50 ml -> completes `dry-na2so4`

**Vessel TestTube_WithLiquid** (Product Test Tube) - starts with Caffeine
  - pour **Murexide Reagent** >= 50 ml -> completes `test-murexide`

### Reactions & expected observations

- **Test_CaffeineMurexide**: Caffeine + Murexide Reagent -> Murexide - "Purple/murexide colour on adding ammonia — caffeine confirmed"

### Quiz (Documentation score)

1. **Caffeine is extracted from tea using sodium carbonate and:**
   - Water only
   - An organic solvent [CORRECT]
   - Sulfuric acid
   - Sodium metal
   - *why:* An organic solvent (dichloromethane) extracts the caffeine.
2. **Caffeine is purified in this experiment by:**
   - Distillation
   - Filtration only
   - Sublimation [CORRECT]
   - Fermentation
   - *why:* It sublimes cleanly to needle crystals.
3. **The murexide test for caffeine produces a colour that is:**
   - Purple [CORRECT]
   - Green
   - Colourless
   - Black
   - *why:* Murexide gives a characteristic purple.

---

## final-winemaking

**moduleId** `final-winemaking` | **period** Final | **prerequisite** `final-caffeine` | **manuscript** Exp 9 (real-world group activity; grapes EXCLUDED) | **end product** Wine

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