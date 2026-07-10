# Storyboard Reconciliation

Extracted from `Docs/handoff_assets/PharmaSynth-Storyboard.pdf` (2026-07-10). Per CLAUDE.md the storyboard is a **reference to EXCEED** — never a source for chemistry or labels (those come from the manuscript / Appendix C). Page images are in `Docs/digests/images/storyboard-1..6/`.

## What it confirms the game already does
- **Boot + PPE flow:** title → Laboratory/Settings/Quit; Pharmee asks you to gear up at the locker (coat + eye protection + gloves) before entering. ✔ implemented (cube room → PPE gate).
- **HUD:** Progress % (top-left), timer `00:00:00` (top-centre), Settings (top-right), Pharmee dialogue. ✔ matches `HudRig`.
- **Dr. Jimenez:** introduced as the examiner who "checks on your activity… won't give clues," assessment framing, and **redo-the-step on every mistake** (can't proceed until a step is done right). ✔ matches the forced-step gating + `ExaminerNPC`.
- **Period structure:** Prelim (Chemical Compounding, Ethyl Alcohol) · Midterm (Benzoic Acid, Acetanilide, Acetone, Chloroform) · Final (Benzamide, Aspirin, **Caffeine "from tea"**, Wine Making). ✔ matches the catalog. *(Storyboard confirms Caffeine is a tea extraction.)*
- **Grade screen:** Time · Number of Mistakes · Grade, with Retry / Continue. ✔ (game shows PASSED/TRY AGAIN + the two-part gate — a superset).

## Gap the storyboard revealed → NOW ADDRESSED
- **Guided Lab Tour — LOCATION-TRIGGERED.** The storyboard's tour is a *narrated walkthrough* — Pharmee points out the tablet, workbench, equipment cabinet, reagent shelf, progress/timer, Settings, and "follow the markers." The game's Lab Tour was a single line + free roam. **Implemented 2026-07-10, then upgraded to exceed the storyboard:** `LabTourGuide` on Pharmee narrates each area **as the player physically walks up to it** — an intro beat on start, then a beat when they enter range of the `LabTablet` / `EquipmentShelf` / `ReagentShelf` landmarks (workbench folded into the intro; each beat folds in a UI tip: progress+timer, wrist checklist, Settings), then a sign-off once all are seen. Pure `FirstUnvisitedInRange` proximity core is tested; falls back to the timed `PharmeeLines.TourBeats` sequence if no landmarks resolve; poking Pharmee ends the tour anytime. 4 assertions; suite 572.

## Still open (storyboard-informed, production tasks)
- **Cutscene staging:** the storyboard marks every experiment intro as a generic `[CUTSCENE: EXPLAINING THE TASK AND THE OBJECTIVE OUTCOME]` placeholder — i.e. it does NOT script the cutscenes, so the game's 44 authored cutscene SOs already exceed it. Per-beat prop staging + the wine time-skip montage remain the open polish (§5).
- **ILO / title cards:** the storyboard shows the experiment title splash ("Synthesis of X") before each `[CUTSCENE]`; the card *art* is the open production item (copy is now sourced from the manuscript — see `manuscript-reconciliation.md`).
