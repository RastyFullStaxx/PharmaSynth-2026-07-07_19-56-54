# Gameplay Flow — start to finish

**Written 2026-07-12 (W5.11), reflecting the W5.9-audited flow (suite 963+).** This is the canonical description of everything the player experiences, in order, plus every branch (fail, retry, abandon, starvation, restart). Open this when working on flow, states, transitions, grading, progression, NPC staging or the review sequence. The pure state machine is `NPC/GatekeeperModel.cs`; the scene driver is `NPC/PharmeeGatekeeper.cs`.

## 1. Boot & scenes
- Build order: `MainMenu` (0) → `SampleScene` (1). In-editor, Play ALWAYS boots into MainMenu via `PlayFromMenuBootstrap` (toggle: Tools ▸ PharmaSynth ▸ Play Starts In Cube Room).
- **MainMenu = the cube spawn room**: solid futuristic cube (cyan trim, launch pad, neon frames, walkable rig with move+turn providers, `music-menu` bed). Panel = **Laboratory / Settings / Quit** only. If `demo-config.json` enables it, an amber **Demo Mode** button also appears (see §10).
- **Laboratory** → fade → load SampleScene. The player lands at `FrontDoorSpawn` (spawn routed through the same teleport path Restart uses, inside the start fade), HUD appears, Pharmee greets (`SpeakWelcome` + `pharmee-greet`), `SpawnBurstFX` cyan materialize.

## 2. The lab & the HUD
- SampleScene: front corridor + the lab proper behind the interior door (window door at x≈−2.35). Pharmee (RobotNPC) hovers at the corridor corner and GUARDS the door.
- Screen-locked HUD: Progress (top-left), timer (top-centre), Settings/Restart/Quit (top-right), Pharmee dialogue bar with icon (bottom, only while he speaks). His overhead bubble shows only while speaking. `ScreenFader` covers every scene load/teleport; its callbacks COMPOSE (overlapping fades can't drop a transition) and Loading has a 5 s watchdog.
- In-lab Settings = 4 volume sliders + comfort (text scale/vignette/snap-turn/subtitle pace). Quit → cube room (full scene reload). Restart → §9.

## 3. The door gate (Pharmee), state by state
Pure FSM: `GatekeeperModel` (suite-pinned). Door physically opens/closes per state (`DoorOpen`); Approach never closes an open door; poking Pharmee = `TalkRequested` re-opens the conversation. If a Dismiss closes the door while the player is already inside, the driver keeps it open (never traps).

| State | What happens | Exits |
|---|---|---|
| **Blocked** | Door shut, panel hidden. Walk up (approach trigger) or poke Pharmee. | Approach/Talk → ModeChoice |
| **ModeChoice** | "What would you like to do?" → **Lab Tour / Campaign** | tour / campaign / dismiss→Blocked |
| **LabTour** | Door opens, nothing graded. Location-triggered guided tour (`LabTourGuide`: DynamicStage → EquipmentShelf → ReagentShelf beats as you reach them; timed fallback). Poke Pharmee to end. | Talk → ModeChoice |
| **CampaignExplain** | "Clear each period's experiments — 90% or better to advance." Continue. | done → EpisodePick; dismiss → ModeChoice |
| **EpisodePick** | Period picker (Tutorial/Prelims/Midterms/Finals; locked periods dimmed). Picks the first unlocked-but-unpassed module in the period (replay = first unlocked). | chosen → CoatPrompt; dismiss → ModeChoice |
| **CoatPrompt** | Wearables re-seated on their pegs; player must don **coat + goggles + gloves** (each individually clickable in the locker just INSIDE the lab — door open). Partial dress → Pharmee names what's missing. Auto-skips if already fully dressed. | PPE worn → ReadyPrompt; back → ModeChoice |
| **ReadyPrompt** | "Are you prepared to begin?" | ready → Loading; not yet → ModeChoice |
| **Loading** | Fade → stage rebuilt ARMED (`ExperimentLauncher.Launch(PrepareArmed)`: task graph built, clock held). Watchdog forces exit if the fade callback is ever lost. | loaded → ThresholdWarn |
| **ThresholdWarn** | "The period will start as soon as you walk in." Proceed / Not yet. | proceed → DoorArmed; dismiss → ModeChoice |
| **DoorArmed** | Door swings open. Crossing the threshold starts the run; if the player is ALREADY inside (locker is in-lab), auto-starts after 0.8 s. | crossed → Running |
| **Running** | `runner.StartRun()` — **the walk-in timer starts**. Unlock snapshot taken (for the later announcement). Player performs the experiment (§4–6). | tests done → QuizIntro; supply starved → SupplyPrompt |
| **SupplyPrompt** | "Not enough reagents left. Restart the period?" **Restart** = starved attempt recorded as failed (grade 0; grade screen AND failure outro suppressed), teleport home inside the Loading fade → re-armed at the door. **Keep trying** = back to Running; the monitor re-arms after ~20 s and re-prompts if still starved. | restart → Loading; dismiss → Running |
| **QuizIntro** | Clock FROZEN (never unfreezes this attempt; review time is free). Pharmee congratulates, fade-teleport to the **review corner** (`ReviewCornerSpawn`, facing the PostLabTablet + Dr. Jimenez), Jimenez speaks a 2-beat briefing. Ambient NPC chatter is suppressed for the whole review (`PharmeeGatekeeper.ReviewFlowActive`). | brief done → QuizTime |
| **QuizTime** | The quiz tablet opens (`PostLabController.Open` — freezes the clock itself too, any path). 3 MCQs + the ±5 yield stepper (yield = record-only). **Never score-gated** (manuscript). Submit → completes the DataSheet task → `runner.Finish(quizFraction)`. | graded → ScoreReview |
| **ScoreReview** | Grade screen shows (floored %, PASSED/TRY AGAIN, breakdown, confetti on pass) while the success/failure **outro cutscene** plays (always, unless data missing — suite asserts all 22 outros exist). After the outro: Jimenez speaks the pass/fail verdict, Pharmee follows up (banded pools; numbers stay on the card). Buttons: **Continue** (pass only) / **Retry** / **Choose Another** (fail only). | continue → Returning; retry → Loading (re-armed at door, teleport inside fade); abandon → Blocked (full reset, no debrief) |
| **Returning** | One fade: full lab reset (props re-seated, wearables back on pegs) + teleport home. | → Debrief |
| **Debrief** | AT the entrance, after the fade-in reveals the scene: quiz congrats + banded performance remark (campaign-flavoured on the final pass). | → UnlockAnnounce |
| **UnlockAnnounce** | Newly-unlocked experiments/periods announced. **Campaign complete** (all 11 passed): dedicated celebration lines + confetti + pass sting instead. | → Blocked (loop repeats) |

## 4. Inside a run — how tasks complete
- The module's **TaskGraph** enforces order via prerequisites; wrong-order attempts record a **WrongStep** mistake. Progress bar = weighted task completion. Phases: ReagentPrep → Synthesis → ChemicalTests → DataSheet; completing ChemicalTests fires the review flow (modules without a ChemicalTests phase route off Synthesis).
- **Pour/mixture tasks** (`LiquidTaskBinding`): pour ≥N ml of the expected reagent into the bound vessel — table OR held in hand. Unexpected reagent = WrongReagent mistake (+HazardousMix consequences if dangerous). `completesTask:false` bindings only accumulate (the Weigh station completes those).
- **Zone sims** (station occupies while prop present): Heat (to target °C; burner-prop stations require the burner LIT via a struck match), Crystallise (ice bath sets), Filter, Collect (gas fills). Live status on the station billboard ("62 C -> 150 C", percent).
- **Tool verbs** (W5.8): Stir (circle the glass rod in the vessel — held or on table), Grind (pestle in mortar; Methane's prepare-mixture is dual-path grind OR zone-touch), Weigh (right vessel/contents settled on the Balance pan). Matches strike-to-light on striker surfaces; the methane splint test fires from a lit match at the filled tube (20 s auto-fallback).
- **Mistakes matrix** (all runner-recorded, rubric-categorized): WrongReagent, WrongStep, Overheat (>120 °C ruins the batch → "Ruined Mixture" + alarm), DroppedGlassware (thin glass ≥4.5 m/s impact; item respawns home), Spill (unheld tipped bottle), FumeHoodViolation (toxic reagent outside the hood), HazardousAction (hot-surface touch), hazardous mixes (toxic gas / fire / spatter theatrics + alarm + grade penalty).
- **Reagent supply is finite** (~2.5× need). The supply monitor polls; a starved required step triggers SupplyPrompt (§3). Demo sessions refill instead.

## 5. Grading — the two-part 90% gate
Computed once at `runner.Finish` (double-finish returns the same recorded result):
- **Rubric grade** (0–100): per-category completion ratios (Procedure, ChemicalTests, MaterialsAndPPE, TimeManagement, Sanitation, Documentation) weighted per module, minus mistake penalties; TimeManagement uses par-time; **Documentation = the quiz fraction** (missing/empty bank = full credit).
- **BKT mastery** (0–1): per-skill Bayesian updates — correct step = positive evidence, mistake = negative (on tracked skills). Overall = mean.
- **PASS = grade ≥ 90 AND mastery ≥ 0.90** (module-configurable threshold). Displays FLOOR so "90%" is never shown next to TRY AGAIN.
- Result persisted (`ResultRecorder` → `ProgressionService` JSON, versioned + .bak): attempts++, best grade/mastery kept, `passed` latches. Failed attempts are recorded (incl. supply-starvation restarts, by design).

## 6. Progression
Linear 11-module chain (`ExperimentCatalog`): each unlock requires the previous module PASSED; periods unlock when every earlier period is fully passed. `ProgressionFlow` answers all gate queries. Save: `pharmasynth_progress.json` (demo sessions → `_demo` file). Campaign completion = all 11 passed → celebration (§3 UnlockAnnounce) — there is deliberately no "next challenge" text after the last one.

## 7. The review corner
Built by Tools ▸ PharmaSynth ▸ Build Review Corner: `ReviewCornerSpawn` facing the PostLabTablet + Jimenez. Jimenez (`ExaminerNPC`) has his own narration bubble + "Talking" animator bool; he roams 4 proctor points during runs (never during review; watchdogs prevent wall-escapes) and goes home when a run ends.

## 8. Cutscenes
Per module: intro (on start, with verbatim Appendix C ILO beats), reagent-prep (on that phase), success/failure outro (ALWAYS on finish — suppressed only for supply-starvation restarts via `SkipNextOutro`). `CutsceneDirector.Skip()` exists; outros run parallel to the grade screen and the review waits for them before the spoken remarks.

## 9. Restart matrix
| Path | Attempt | Stage | UI | Player |
|---|---|---|---|---|
| **HUD Restart** (confirm) | `Abort()` — ends un-graded, NOT recorded | re-seated (StageOnly) | grade card hidden, quiz closed | faded teleport home, gate → Blocked, Pharmee greets |
| **Retry** (grade screen) | fresh armed attempt | rebuilt armed | grade hidden | teleport home inside the load fade → door flow |
| **Choose Another** (fail) | already finished | full lab reset | grade+quiz hidden | faded home → Blocked → pick any unlocked module |
| **Supply restart** | recorded failed (grade 0), outro suppressed | rebuilt armed | grade hidden | teleport inside fade → door flow |
| **Quit** | discarded (scene unload) | — | — | cube room |

## 10. Demo Mode (panelists; Chapter-3 participants must never see it)
`StreamingAssets/demo-config.json` (persistentDataPath override wins) merely reveals the cube-room button. A demo SESSION: separate save, ALL periods unlocked, HUD Skip Step / Finish Exp. / Auto Quiz cluster (hidden during the review), infinite reagent refill (no SupplyPrompt), end-product bottles visible on the west shelf (hidden in regular play — `EndProductVisibility`), and the module's ready-made product available so tests can run without synthesis.

## 11. Known playtest gaps (queued)
See `Docs/remaining-work-checklist.md` §13 — user-reported issues awaiting fixes (items returning to shelves mid-run, break-respawn losing contents, in-headset pour debugging, apparatus kits, snap/stick assembly, UI wrap/scroll, dialogue polish).
