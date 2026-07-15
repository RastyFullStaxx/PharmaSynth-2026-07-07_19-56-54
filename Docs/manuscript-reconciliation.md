# Manuscript Reconciliation (Appendix C = the lab manual)

Extracted from `Docs/Documentations/manuscript.pdf` (2026-07-10) — the client's official course manuscript. Its **Appendix C** (pp. 27–80 of the PDF, "Experiment No. 1–9") is the laboratory manual and the **chemistry authority**. This doc reconciles it against the game so the client sign-offs (§7) can be closed with evidence, and records the official ILOs + theoretical-yield references.

**Tooling note:** the Read tool can't open PDFs on this machine (TEMP hijack); text was extracted with `pdftotext -layout` → `manuscript.txt` (gitignored alongside the PDF).

---

## 0. Apparatus evidence — splint vs delivery ("bent") tube (searched 2026-07-15)

| Item | Manuscript verdict | Evidence |
|---|---|---|
| **Wooden splint** | **NEVER used — 0 occurrences of "splint"** anywhere in the manuscript. Every combustion/flame test uses a **"lighted matchstick"**. | Exp 3 (`manuscript.txt:2447, 2456`): *"Pour 1 ml of the distillate into a watch glass and apply a lighted matchstick… blue flame indicates complete combustion… yellow flame indicates incomplete."* |
| **Delivery / "draw" tube** | **REQUIRED — but only in Exp 3 (Ethyl Alcohol)**, where it is called a **"bent tube"**: fitted to a **stopper** to bubble evolved **CO₂ into limewater**, led into a test tube topped with a **cotton swab**. | `manuscript.txt:2430–2434`. Reappears verbatim at `:2695` inside the **Exp 4** section — that is the known **copy-paste errata** (Exp 4's procedure duplicates Exp 3's fermentation; the game correctly uses benzaldehyde + KMnO₄). |
| **Methane (tutorial)** | **Not an experiment in the manuscript at all** — "methane" appears only as a molecular-weight worked example (CH₄, `:1995`). The tutorial is 100% game-authored, so neither splint nor delivery tube is mandated for it. | `:1995–1996` |
| **Exp 9 Wine Making** | A **group ACTIVITY**, not a bench procedure: no apparatus list, no bent tube, no limewater — take-home fermentation (≥250 mL, **grapes excluded**) + video documentation + wine-tasting event with a presentation rubric. | `:3808–3843` |

**Consequences applied (2026-07-15):** the `lit-splint` prop was **deleted** (`Remove Lit Splint Prop`); the gas test fires off a **lit Matchstick**, matching the manuscript (the `SplintShouldFire` method name is legacy/suite-pinned only). The **delivery ("bent") tubes are KEPT** — they are real Exp 3 equipment (CO₂→limewater), not decoys. **Flag to client:** our winemaking CO₂/limewater test is game-authored (borrowed from Exp 3) since Exp 9 specifies no bench chemistry.

---

## 1. Experiment ↔ game-module map

| Manuscript (Appendix C) | Game module | Notes |
|---|---|---|
| Exp 1 — Stoichiometry | *(none)* | Foundational computation lab; not a synthesis module. |
| Exp 2 — Chemical Reactions of Organic Compounds | **Chemical Compounding** | Identification/tests lab. |
| Exp 3 — Ethyl Alcohol Synthesis | **Ethyl Alcohol** | |
| Exp 4 — Benzoic Acid Synthesis | **Benzoic Acid** | ⚠ manuscript procedure is DEFECTIVE — see §3. |
| Exp 5 — Acetanilide Synthesis | **Acetanilide** | |
| Exp 6 — Acetone Synthesis | **Acetone** | |
| Exp 7 — Chloroform Synthesis | **Chloroform** | |
| Exp 8 — Benzamide Synthesis | **Benzamide** | |
| Exp 9 — Wine Making Activity | **Wine Making** | Real-world group activity; see §4. |
| *(not in manuscript)* | **Methane** (tutorial) | Game-authored onboarding experiment. |
| *(not in manuscript)* | **Aspirin** | Named in the manuscript intro (esterification → acetylsalicylic acid, recrystallisation) but has **no Appendix C experiment** — game-authored procedure + ILOs. |
| *(not in manuscript)* | **Caffeine** | Not in the manuscript at all — game-authored (extraction + murexide/melting-point). |

**Action for client:** confirm the game-authored ILOs/chemistry for **Methane, Aspirin, Caffeine** (no manuscript source to verify against).

---

## 2. Official ILOs (verbatim from Appendix C "Objectives")

Use these as the authoritative learning-outcome copy for the intro-cutscene ILO cards. The game's current `intendedLearningOutcomes` are *more granular* (step-level) and remain valid — these are the formal academic objectives to display/sign off.

- **Chemical Compounding** (Exp 2): (1) Write chemical reactions involved in some organic compounds such as alcohols, aldehydes, ketones, carboxylic acids and esters. (2) Differentiate tests for different organic compounds.
- **Ethyl Alcohol** (Exp 3): (1) Synthesize ethyl alcohol. (2) Determine its identity through chemical tests.
- **Benzoic Acid** (Exp 4): (1) Synthesize benzoic acid. (2) Determine its identity through chemical tests.
- **Acetanilide** (Exp 5): (1) Synthesize acetanilide. (2) Determine its identity through chemical tests.
- **Acetone** (Exp 6): (1) Synthesize acetone. (2) Determine its identity through chemical tests.
- **Chloroform** (Exp 7): (1) Synthesize chloroform. (2) Determine its identity through chemical tests.
- **Benzamide** (Exp 8): (1) Synthesize benzamide. (2) Determine its identity through chemical tests.
- **Wine Making** (Exp 9): Learn the basic methodology in preparation and synthesis of alcohol using the fermentation technique with basic household ingredients.
- **Stoichiometry** (Exp 1, reference only): balance a chemical reaction; compute molecular weight; compute stoichiometry via dimensional analysis; compute percentage yield.

*(Methane / Aspirin / Caffeine ILOs are game-authored — pending client confirmation.)*

---

## 3. Chemistry sign-offs (§7) — VERIFIED against Appendix C

All three long-standing flags are now confirmed from the source. **The game's implementation is correct; the manuscript itself carries the defects.**

1. **Benzoic Acid route — CONFIRMED DEFECT (manuscript Exp 4).** The Exp 4 "Benzoic Acid" *procedure body is a verbatim copy-paste of the Ethyl Alcohol experiment* — it describes a 12% brown-sugar + yeast fermentation, distilling the 70–80 °C fraction, with combustion / iodoform / ester tests (i.e. ethanol, not benzoic acid). The **materials list correctly names Benzaldehyde**. → The game's **benzaldehyde + KMnO₄ oxidation** route is the correct intent. *Sign-off: adopt benzaldehyde + KMnO₄; treat the printed Exp 4 procedure as errata.*
2. **Acetanilide acylating agent — CONFIRMED: acetyl chloride.** Appendix C Exp 5 synthesis: 2 mL aniline + 2 mL glacial acetic acid + **2 mL acetyl chloride**. (The manuscript *intro prose* said "acetic anhydride" — that prose is inconsistent with the authoritative Appendix C procedure.) → The game's **acetyl chloride** matches. *Optional client call: substitute the safer acetic anhydride for a VR audience — chemistry differs slightly but both give acetanilide.*
3. **Benzamide nitrous test — CONFIRMED: sodium nitrite (the "nitrate" is a typo).** Exp 8 reagents header lists "Sodium **nitrate** solution", but the actual test-C procedure says "Action of nitrous acid … add 3 mL of 10% Sodium **nitrite** solution." Nitrite is correct for the nitrous-acid test. Synthesis route = **benzoyl chloride + concentrated ammonia** (not "benzoic acid → benzamide" as the intro prose said). → The game's **sodium nitrite** + benzoyl-chloride route matches. *Sign-off: nitrite confirmed.*

---

## 4. Wine rubric — manuscript vs. client decision

Appendix C Exp 9 specifies a **bespoke Wine-Tasting rubric**: Workmanship 15% · General Appearance (bottle) 20% · Presentation (table, costume) 20% · Photo/Video Documentation 20% · Flavor/Smell/Taste 25%. This is a **real-world group presentation activity** (3–5 members, 15–20 min video, min 250–500 mL of a **non-grape** fruit).

The client **RESOLVED (2026-07-09) to keep the standard 6-category rubric** for a solo VR sim (the manuscript rubric is mostly presentation/documentation, not chemistry). Documented here for traceability. *Minor note: the manuscript EXCLUDES grapes; the game currently uses grape juice for simplicity — flag to client if fidelity matters.*

---

## 5. Theoretical-yield reference (derived from Appendix C quantities)

For the "expected-yield ranges (display/reference only)" item. Yield is **record-only, never graded** (resolved 2026-07-09). Theoretical values below are computed from the Appendix C starting quantities; realistic student yields run well under 100%.

| Experiment | Limiting reagent (Appendix C) | Product (MW) | Theoretical | Typical actual |
|---|---|---|---|---|
| Acetanilide | 2 mL aniline (~2.04 g, 0.0219 mol) | acetanilide (135.2) | ~2.96 g | ~1.8–2.4 g (60–80%) |
| Benzamide | 1 mL benzoyl chloride (~1.21 g, 0.0086 mol) | benzamide (121.1) | ~1.04 g | ~0.6–0.85 g (60–80%) |
| Acetone | 7 g Ca-acetate + 7 g Na-acetate | acetone (58.1) | ~5 g | ~1.5–2.5 g (dry-distillation is lossy) |
| Chloroform | 12 g acetone (0.207 mol) | chloroform (119.4) | ~24.7 g | ~10–15 g (40–60%) |
| Ethyl Alcohol | 12 g sucrose (fermentation) | ethanol (46.1) | ~6.4 g | ~2–5 mL distillate (fermentation + distillation losses) |
| Benzoic Acid | *(game route: benzaldehyde + KMnO₄ — not the defective Exp 4)* | benzoic acid (122.1) | per game quantities | ~60–75% |
| Aspirin / Caffeine / Methane | *(game-authored — no manuscript quantities)* | — | — | client to supply |

*(These are reference figures for the data-sheet; wiring an on-screen "expected yield" display is an optional follow-up.)*

---

## 6. Net effect on the checklist

- §7 **chemistry sign-offs** → move from *open* to **manuscript-verified**; client action is to rubber-stamp the three errata-corrections above.
- §1 **ILO cards** → the ILO *text* is now sourced from Appendix C (§2); the card *art* remains the open production task.
- §1 **yield ranges** → reference figures derived (§5); yield stays record-only.
- Still genuinely open / client-only: Methane/Aspirin/Caffeine ILO+chemistry confirmation, and the scoring-weight sign-off (no manuscript weights found).

---

## 7. W5.9 re-verification sweep (2026-07-12)

A second full pass of all 8 manuscript experiments against the game data. **Fixed in data** (menu `Tools ▸ PharmaSynth ▸ Apply W5.9 Manuscript Data`, suite-locked):
- **Benzoic Acid ester test** was chemically inert (rule paired benzoic acid with sulfuric acid — no alcohol anywhere). Propyl alcohol (named by the module's own hint) is now staged, bound, and the rule fires on benzoic acid + propyl alcohol.
- **Chloroform** gained the manuscript's missing confirmatory test: *oxidation with potassium dichromate + conc. H2SO4* (procedure L3419-21 AND the results data sheet) — new task `test-oxidation`, staged reagent, new reaction rule.
- **Wine Making** no longer ferments grape juice against the manuscript's explicit grape exclusion (L3830-31): `Chem_GrapeJuice` renamed **Mixed Fruit Juice** (all references updated).
- **Reagent fidelity:** iodoform tests (Exp 3 + 6) now stage their 10% KI alongside NaOCl; Ethyl Alcohol's ester test uses *diluted* acetic acid; Benzamide's acid test uses *diluted* HCl; Acetanilide preps 0.1N HCl as labeled — reaction-rule inputs re-pointed in lockstep so the tests still fire.

**⚠ CLIENT DECISION — Chemical Compounding (manuscript Exp 2) diverges by design.** The manuscript is a multi-substrate identification lab (FeCl3 enol test, KMnO4 rate-of-oxidation on three butyl alcohols, Tollen's, ester formation, aspirin hydrolysis — plus Benedict's on the data sheet). The game module instead runs a single-substrate (ethanol) battery: combustion / sodium metal / bromine water / KMnO4 — only KMnO4 overlaps. Restoring the manuscript battery is a full module redesign (tasks + layout + quiz + rules) on a locked, regression-tested experiment; all needed chemicals already exist as assets (`Chem_FerricChloride10`, `Chem_TollensReagent`, the three butyl alcohols, `Chem_Phenol`, `Chem_Glycerol`…). The chemistry-misfit quiz question (bromine/unsaturation on an all-saturated module) was replaced with a manuscript-aligned KMnO4 question in the meantime. **Ask the client: keep the simplified battery, or restore the manuscript's Exp 2 tests?**
