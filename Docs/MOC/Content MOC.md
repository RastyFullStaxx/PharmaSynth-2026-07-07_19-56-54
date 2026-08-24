# Content MOC

Up: [[Home]] · Siblings: [[Architecture MOC]] · [[Systems MOC]] · [[Process MOC]]

The experiments, where their chemistry comes from, and what may and may not be
changed about them.

---

## The chemistry authority

> [!danger] Appendix C of the client manuscript is the only chemistry source
> `Docs/Documentations/manuscript.pdf`. The Read tool cannot open PDFs in this
> environment — extract with:
> ```bash
> "C:/Program Files/Git/mingw64/bin/pdftotext.exe" -layout Docs/Documentations/manuscript.pdf -
> ```
> The **storyboard is a reference to EXCEED**, never a chemistry source.

Known deviations and every client flag are tabulated in the header of
[[experiments-reference]]. Evidence and reasoning: [[manuscript-reconciliation]],
[[storyboard-reconciliation]], [[manuscript-apparatus-gap]].

---

## The 9 experiments

The chain is **9 modules**, not 11. Aspirin and Caffeine were dropped 2026-07-16.

| # | moduleId | Period | Manuscript | Product |
|---|---|---|---|---|
| 1 | `tutorial-methane` | Tutorial | game-authored | (gas; splint pop) |
| 2 | `prelim-chemical-compounding` | Prelim | Exp 2 | — (identification lab) |
| 3 | `prelim-ethyl-alcohol` | Prelim | Exp 3 | Ethanol |
| 4 | `midterm-benzoic-acid` | Midterm | Exp 4 (errata) | Benzoic Acid |
| 5 | `midterm-acetanilide` | Midterm | Exp 5 | Acetanilide |
| 6 | `midterm-acetone` | Midterm | Exp 6 | Acetone |
| 7 | `midterm-chloroform` | Midterm | Exp 7 | Chloroform |
| 8 | `final-benzamide` | Final | Exp 8 | Benzamide |
| 9 | `final-winemaking` | Final | Exp 9 (non-grape) | Wine |

Full per-experiment data — reagents, steps, quantities, chemical tests, quiz
questions, layout, apparatus, polish status — is [[experiments-reference]].

### Period grouping (client-final, 2026-07-16)

Exactly the manuscript's 8 bench labs (Exp 2–9):

- **Prelim** — chemical reactions (2), ethyl alcohol (3)
- **Midterm** — benzoic (4), acetanilide (5), acetone (6), chloroform (7)
- **Finals** — benzamide (8), wine making (9)

**Tutorial (methane) is client-confirmed to remain**, outside the graded periods.

> [!warning] Do not "re-derive" a conflict from the Appendix-C table of contents
> Manuscript **Exp 1 is STOICHIOMETRY** — a pen-and-paper calculation exercise
> (balance / MW / % yield), not a bench lab. And the TOC's Term column is a
> **vertically-centred merged cell**, so its labels land on rows 2/5/8. It does *not*
> put chloroform in Finals; it agrees with the grouping above. This has been
> mis-read before.

---

## Dropped content, and what survived

✅ **Aspirin + Caffeine dropped 2026-07-16** — chain 11 → 9. Modules, layouts,
quizzes, cutscenes and reactions deleted; catalog, libraries, scene layout list and
suite counts updated.

> [!important] Aspirin survives as a RAW REAGENT
> Exp 2 §D hydrolyses it, so it must stay on the bench outside demo mode.

**Orphaned but deliberately KEPT** — never delete bench reagents:
`Chem_Caffeine`, `Murexide Reagent`, `Label_Aspirin`, `Label_Caffeine`,
`Art/UI/Icons/aspirin.png`.

---

## End products

> [!important] Client, 2026-07-16
> *"The END products which is the GOAL of the experiments must NOT exist — we'd lose
> the very meaning of the experiment; the player will manually craft them."*

`EndProductVisibility` therefore gates **per-experiment, not per-chemical**: it hides
only the running module's own product (`DemoMode.ProductFor`).

- The 4 **pure** products (Acetanilide, Benzamide, Chloroform, Wine) were **deleted** —
  no procedure names them as a reagent, so there is no bottle to gate.
- **Ethanol, Acetone and Benzoic Acid survive** in `IsEndProduct`, because each is
  *also* a manuscript reagent for other modules (Ethanol → Exp 2, 6; Acetone → Exp 2,
  7; Benzoic → Exp 4). Exp 2 runs *before* Exp 3/6, so the old global hide stripped
  reagents it cannot run without.
- `ProductFor("prelim-chemical-compounding")` is **null** — an identification lab
  synthesises nothing.

---

## Reagents and where they live

**`ReagentCabinets` (east) is THE reagent home**: all 57 `Raw_*` bottles, 2 units ×
5 shelves × 7 slots = 70 places. Consolidated 2026-07-16; the west `ReagentShelf`
cubby is now empty.

> [!warning] Check BOTH before concluding a reagent is missing
> 60+ bottles exist. Historically `Reagent_*` = west cubby, `Raw_*` = east cabinets.

> [!danger] `Build Reagent Cabinets` destroys and recreates everything it stocks
> It preserves only UNIT transforms, never the items inside. The hand-placed **ice
> bucket and 4 consumable boxes** (filter paper, cotton swabs, litmus, matchsticks)
> are therefore **excluded** via `IsHandPlacedConsumable` — a run once wiped their
> by-hand positions. Anything hand-placed must be excluded, not stocked. → [[Gotchas]]

---

## Quizzes and learning outcomes

- 3 MCQs per module + a record-only yield field. **Never score-gated.**
- `QuizBank` / `QuizBankLibrary` in `Experiment/`.
- `intendedLearningOutcomes` on each module is synced verbatim to Appendix C. It
  reaches the player three ways: the wrist board's **OBJECTIVES** block above
  MATERIALS, the intro cutscene beats, and Dr. Jimenez's pre-quiz recap — the one
  module-specific thing he says.

---

## Assets still to produce

[[asset-production-spec]] — art, audio and video. Note that AI asset generation is
gated behind a Unity AI seat.

Voice: **343 clips, 0 unvoiced.** See the voice warning in [[Systems MOC]] before
editing any dialogue or hint copy.

---

## Client decisions

Open questions awaiting sign-off: [[client-signoff-request]].
