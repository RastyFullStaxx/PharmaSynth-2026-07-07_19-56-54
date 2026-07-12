# CLAUDE.md — PharmaSynth (VR / Meta Quest 3)

Read this first each session. It tells you what the game IS, how it flows, and **which doc to open for which work** — the details live in `Docs/`.

## Project summary
- **PharmaSynth: "Gear Up, Synth It Up!"** — a first-person guided VR chemistry-lab education game for **Meta Quest 3**. Unity **6000.5.2f1**, URP 17.5, OpenXR 1.17.1 + XRI 3.5.1, Input System. Client handoff (audit-and-continue). **Hard contract deadline: 2026-08-31.**
- **Chemistry authority = the client manuscript, Appendix C** (`Docs/Documentations/manuscript.pdf` → extract via `pdftotext -layout`; the Read tool can't open PDFs here). The storyboard is a reference to EXCEED, never a chemistry source. All known manuscript deviations + client flags: `Docs/experiments-reference.md` header table.
- Everything runtime lives under `Assets/PharmaSynth/Scripts/` (global namespace); experiments are **DATA** (ScriptableObjects), not scenes. Master plan (design spec): `C:\Users\MSI\.claude\plans\you-are-the-best-cozy-possum.md`.

## ⭐ SESSION START (first 5 minutes)
1. `git log --oneline -3` on **main** (the user commits checkpoints themselves; `feature/asset-intake` is a dead stub — never use it).
2. Menu **Tools ▸ PharmaSynth ▸ Run Self-Tests** → expect **969/969 ALL GREEN** (+3 deliberate warnings: two W5.9 guard tests + the Unknown-moduleId negative test) + zero-error console. **EDIT MODE ONLY** — in Play mode ~7 isPlaying-gated assertions legitimately fail; check `Unity_ManageEditor GetState` first. MCP down? → write `Temp/selftest-autorun-request.txt` (suite on next domain reload) or `Logs/menu-autorun-request.txt` (menu list; headless via `Unity.exe -batchmode -quit -projectPath <proj> -executeMethod MenuAutoRun.RunNow` with the editor CLOSED; Logs/ not Temp/ — Unity wipes Temp).
3. Open work lives in ONE tracker: **`Docs/remaining-work-checklist.md`** (§13 = the user's queued playtest issues). Check items off as they land.

## THE GAME FLOW (canonical; full detail → `Docs/gameplay-flow.md`)
Boot → **cube spawn room** (MainMenu scene: Laboratory / Settings / Quit; amber Demo button if config-enabled) → fade into **SampleScene** at the front door, Pharmee greets. Pharmee **guards the lab door** (pure FSM `GatekeeperModel`): approach → **Lab Tour** (ungraded guided tour) or **Campaign** → explain → **episode picker** (locked periods dimmed) → don **coat+goggles+gloves** at the in-lab locker → "I'm ready" → fade + stage builds **ARMED** (clock held) → threshold warning → door opens → **crossing the threshold starts the timer**. The player runs the experiment (pours/verbs/sims complete TaskGraph tasks in prerequisite order; mistakes are graded; reagents are finite → starvation offers a restart). Chemical tests done → clock FREEZES → fade-teleport to the **review corner** → Jimenez briefs → **quiz tablet** (3 MCQs + record-only yield; never score-gated) → submit = `Finish` → **grade screen** (floored %, PASSED/TRY AGAIN) + success/failure **outro cutscene** + spoken verdicts. **Pass** → Continue → one-fade lab reset + teleport home → entrance debrief → **unlock announcement** (all-11-passed = campaign-complete celebration) → door re-blocks, loop repeats. **Fail** → Retry (clean re-armed attempt) or **Choose Another** (back to the picker). HUD Restart aborts un-graded and fully resets. Demo sessions: separate save, all unlocked, skip buttons, infinite supply, end products visible.
- Two-part gate: **rubric grade ≥90 AND BKT mastery ≥0.90** per module. Progression = linear 11-module chain, period doors.

## THE 11 EXPERIMENTS (full data → `Docs/experiments-reference.md`)
| # | moduleId | Period | Manuscript | Product | Signature verbs |
|---|----------|--------|------------|---------|-----------------|
| 1 | tutorial-methane | Tutorial | game-authored | (gas; splint pop) | grind, heat, collect, splint |
| 2 | prelim-chemical-compounding | Prelim | Exp 2 ⚠ diverges (client flag) | — (ID lab) | test battery |
| 3 | prelim-ethyl-alcohol | Prelim | Exp 3 | Ethanol | ferment, distill, iodoform/ester |
| 4 | midterm-benzoic-acid | Midterm | Exp 4 (errata; game = benzaldehyde+KMnO₄) | Benzoic Acid | oxidise, filter, acidify, ester(propyl) |
| 5 | midterm-acetanilide | Midterm | Exp 5 | Acetanilide | acylate (acetyl chloride), crystallise |
| 6 | midterm-acetone | Midterm | Exp 6 | Acetone | WEIGH, dry-distill, 4 tests |
| 7 | midterm-chloroform | Midterm | Exp 7 | Chloroform | haloform, decant, dichromate oxidation |
| 8 | final-benzamide | Final | Exp 8 | Benzamide | ice bath, STIR, nitrous (nitrite!) |
| 9 | final-aspirin | Final | game-authored | Aspirin | WEIGH on scale, crystallise, FeCl₃ |
| 10 | final-caffeine | Final | game-authored | Caffeine | grind (edu), extract, sublime, murexide |
| 11 | final-winemaking | Final | Exp 9 (NON-grape) | Wine | ferment Mixed Fruit Juice, CO₂/limewater |

## Architecture map (mechanics detail → `Docs/systems-reference.md`)
All under `Assets/PharmaSynth/Scripts/`, **thin MonoBehaviours over pure suite-tested cores** (mandatory pattern; every component has a `Bind()` seam — edit-mode AddComponent fires no Awake/OnEnable).
- `Experiment/` TaskGraph(+Model) · ExperimentModuleDefinition · ExperimentRunner (StartExperiment / PrepareExperiment+StartRun armed seam / Finish / **Abort** / FreezeClock) · MistakeLog · QuizBank(+Library)
- `Scoring/` MasteryModel (BKT) · ScoreCalculator · ExperimentGrader — the two-part gate
- `Progression/` ProgressionService (JSON save) · ExperimentCatalog (11-chain) · ProgressionFlow · GameFlow · ResultRecorder · UnlockDiff · DemoMode/DemoSession · ResultsExport
- `Chemistry/` LiquidPhysics (wake-from-empty + Ledger) · LiquidPourer (PourTick/ResolveTarget) · LiquidTaskBinding (requiredMl, completesTask) · ReagentSupplyMonitor (Unlatch) · ReactionRule/Registry · TemperatureSim · Gas/Crystallise/Filter controllers · HazardousMix(+Reactor) · ShelfPourWiring · PharmaLiquid shader
- `Interaction/` ExperimentSceneBuilder (stage spawner; EnsureLiquidVisual; RackKit/spares) · ExperimentTaskStation · ZoneItemSensor/ZoneSimStation (ignition gate) · verbs: OrbitMath+Stir/GrindController, WeighMath/Station(+ScaleController), Matchstick/MatchStriker/BurnerController · Mishandling/BreakableGlassware · DropRespawn (settle-freeze) · PhysicsProfiles/RealSizes/GrabTuning/GrabPhysicsPolicy · MethaneApparatusRig (SplintShouldFire) · feedback: VesselStatus/MixFeedback/FloatingText/StationStatusLabel/HoverInspector · LayoutTidyMath/WorkspaceShelfMath · ExperimentLauncher · DoorOpener · DevExperimentDriver (B/1-5/F/R)
- `NPC/` GatekeeperModel (**the flow FSM**, IsReviewState) · PharmeeGatekeeper (driver; ReviewFlowActive; ResetToEntrance; OnAbandonAfterFail) · PharmeeBrain · PharmeeLines (ALL dialogue pools + CampaignComplete) · NPCNarrationController · ExaminerNPC · ProctorRoamer · LabTourGuide · CutsceneDirector (SkipNextOutro) · Pharmee face/mood/poke/attitude
- `UI/` HudRig/HudDialogueBar · ChoicePanel · ScreenFader (composing callbacks) · GradeScreenController (+backButton) · GradeDisplay (floored %) · PostLabController (quiz; Open freezes clock; Close) · WristWatchController (holo checklist = THE procedures panel) · LabInfoDatabase · GlyphSafe · VesselStatusMath · SettingsService/ComfortApplier · ProximityLabel
- `Safety/` PPESetModel/PPEController · FumeHoodZone · HazardZone | `Audio/` AudioService · SoundBank | `Editor/` PharmaSelfTests (969) · all builder menus · DevCapture · W5.8/W5.9 data appliers · MenuAutoRun/SelfTestAutoRun

**Key scene objects (SampleScene):** ExperimentSystems (runner/launcher/builder/monitor/recorder) · HudRig · RobotNPC (Pharmee + gatekeeper) at the corridor corner · LabDoorController + door triggers + FrontDoorSpawn · PPELocker · ScreenFader · Services · PostLabTablet + GradeScreen (review corner) · MethaneStage + DynamicStage · ReagentShelf (west 3x4 cubby: raws + gated end products) · ReagentCabinets (east) · WorkspaceShelf (gantry platforms) · WorldLabels · XR Origin (CC r0.25 + HeadCollisionPushback). MainMenu: cube room + MenuCanvas. Build Settings = [MainMenu(0), SampleScene(1)].

## 📚 DOC INDEX — open the right doc for the work
| Working on… | Open |
|---|---|
| An experiment's chemistry/steps/reagents/tests/quiz/layout | `Docs/experiments-reference.md` |
| Flow, gate states, review, grading, restarts, demo mode | `Docs/gameplay-flow.md` |
| Any mechanic (liquids/verbs/breakage/feedback/NPC), **builder menus + rebuild orders**, adding content | `Docs/systems-reference.md` |
| Running, testing, simulator keys, Quest build config, headset toggle | `Docs/build-and-run.md` |
| What's still to do (incl. §13 queued user playtest issues) | `Docs/remaining-work-checklist.md` |
| Manuscript evidence / deviations detail | `Docs/manuscript-reconciliation.md` (+ `storyboard-reconciliation.md`) |
| Client decisions pending | `Docs/client-signoff-request.md` |
| Art/audio/video assets still to produce | `Docs/asset-production-spec.md` |
| Quest 3 day-1 device pass | `Docs/on-device-test-plan.md` |
| Why/when something changed (W1→W5.10 history) | `Docs/changelog.md` |

## Known gotchas (carry forward)
- `Unity_RunCommand` compiles inside `Unity.AI.*` → fully-qualify/alias `UnityEngine.UI.*` (`UImage`, `UButton`); `System.Reflection`/`ISet`/`GetInstanceID` blocked. File writes + `AssetDatabase.DeleteAsset` flagged "requires user interaction" → use Bash for files, load-or-overwrite for assets. **Menu items are exempt** (DevCapture works).
- **Never RunCommand while the user is in Play mode** (`OpenScene` throws; scene edits force-exit play) — check `Unity_ManageEditor GetState` first. One scene per command; `AssetDatabase.Refresh()` before cross-scene asset loads; a `Refresh:true` menu execute can swallow the run in the domain reload.
- Edit mode: `OnEnable`/`Awake` don't fire on `AddComponent` → `Bind()` seams everywhere. Never `renderer.material` in edit mode (use `sharedMaterial`/MPB — TMP `outlineWidth` also instances materials!). `AddComponent<LiquidPhysics>` needs a Renderer host.
- Before deleting a script, grep ALL of `Assets/` for the type name. FBX imports have no colliders. ChemLab `_WithLiquid` prefabs: verify mesh names, not GO names.
- MCP "named pipe not found" = editor busy → wait for `Logs/Editor.log` quiet, retry. "Connection revoked" = AI-seat licensing hiccup → re-approve under Project Settings → AI, or use the request-file fallbacks. `Unity_Camera_Capture` broken → use DevCapture (**yaw 0–360 only**, negative misparses).
- Transparent geometry doesn't z-write → text/canvas visibility = **sortingOrder** (HUD 30000, bubble 29000, world panels 4000–5000, TMP labels 20000).
- Poppler/PDF: TEMP hijacked → Read can't open PDFs; use `"C:/Program Files/Git/mingw64/bin/pdftotext.exe"` or pypdf. Internet via `curl` (github-raw 429s). Windows FS is case-insensitive: case-only asset renames need `AssetDatabase.RenameAsset`.
- Dialogue copy edits invalidate `VoiceBank` text-hashes → re-export the voice manifest after copy changes. Suite warnings that are EXPECTED: the 3 listed in SESSION START.
- The suite pins behavior (Mishandling lists, ContentSuite task counts, layout spacing…) — move pinned assertions IN THE SAME change as the behavior.

## Environment & conventions
- Windows 11; PowerShell primary, Git Bash available. Active build target **Android** (IL2CPP/ARM64/ASTC, Quest features configured). **Headset play**: Tools ▸ PharmaSynth ▸ Headset Play Mode (OpenXR on Play) — ON drives a Quest-Link headset in editor Play (currently ON); OFF = headless keyboard/simulator (init with no headset can stall Play). **No Cinemachine — never animate the XR camera.**
- **Unity MCP** = official Assistant server (AI seat). MCP tools cost no credits; `Unity_AssetGeneration_*` and Assistant chat DO — flag before spending. **Art creation is IN scope** (user has credits; flag AI-generation spends first). Tripo swap convention: save model prefabs to `Art/Generated/Refs/<ExactName>.prefab` (see systems-reference §9).
- Git: work on **main**; commit only when asked; no destructive ops. Off-repo backups: `C:\Users\MSI\PharmaSynth-handoff-backup\`.
- Every change: suite green + zero-error console + DevCapture for visual work. Quest 3 not delivered — test in-editor (or headset via Link); a human play-tests by pressing Play (MCP play mode is unreliable). Escalate to client if no headset by W5 (Aug 4–10).
- Experiments are DATA; confirm game-design changes with the user; builders/menus for scene edits (idempotent, re-runnable); match inherited code style.

## Current state (2026-07-12, end of W5.11 — suite 969/969)
Engine, all 11 experiments, the full client-confirmed loop, the W5.8 interaction overhaul (real pours/verbs/feedback), the W5.9 flow-smoothness + manuscript fixes, and the W5.10 workspace cleanup are DONE and regression-locked. **Pending human play-pass + queued playtest fixes: `Docs/remaining-work-checklist.md` §10–§13** (top items: in-headset pour debugging, items-return-home-mid-run, break-respawn contents, apparatus kits + snap/stick assembly, UI wrap/scroll + dialogue polish). Blocked buckets: on-device week (no headset), client sign-offs, voice generation (needs ELEVENLABS_API_KEY).
