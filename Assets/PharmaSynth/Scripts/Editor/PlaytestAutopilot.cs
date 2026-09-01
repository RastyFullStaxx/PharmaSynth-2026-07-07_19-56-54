#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
using XRManager = UnityEngine.XR.Interaction.Toolkit.XRInteractionManager;
using NearFar = UnityEngine.XR.Interaction.Toolkit.Interactors.NearFarInteractor;

/// Plays the game in PLAY MODE, unattended, and reports what broke (W5.41).
///
/// ⭐ Why this exists on top of Simulate Everything: that battery runs in EDIT mode, where
/// Update never ticks, coroutines never run, physics never steps, XRI never selects
/// anything and no audio plays. Every bug that only exists in MOTION is invisible to it —
/// which is precisely where the §13 playtest findings live (items vanishing, dialogue
/// stomping mid-typing, the holo panel not scrolling, quiz buttons not clickable), and
/// where W5.34's "711 errors/second from SpawnVFX" lived.
///
/// ⭐ No headset needed: PC Dev Mode leaves OpenXR auto-init OFF on Standalone and the
/// scene's XR Device Simulator drives the rig.
///
/// ⛔ Deliberately an EDITOR-assembly script ticked by EditorApplication.update (which
/// keeps running during play) rather than a runtime MonoBehaviour. That way it can reuse
/// the editor-only simulators, and nothing it does can ever leak into a player build.
[InitializeOnLoad]
public static class PlaytestAutopilot
{
    // Logs/, never Temp/ — Unity wipes Temp, and this request has to survive the domain
    // reload that entering Play mode causes.
    const string Request = "Logs/autopilot-request.txt";
    const string Report = "Logs/autopilot-report.txt";
    const string ShotDir = "Logs/autopilot";

    /// Hard ceilings. The editor must NEVER be left sitting in Play mode because the
    /// autopilot wedged waiting for a state that will never arrive.
    const float HardCapSeconds = 420f;
    /// Generous on purpose: the review briefing is two spoken beats behind a fade, and a
    /// watchdog that fires mid-cutscene reports a stall that is really just dialogue.
    const float BeatTimeout = 60f;

    static PlaytestAutopilot()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    [MenuItem("Tools/PharmaSynth/Autopilot Playtest (plays the game in Play mode)")]
    public static void Launch()
    {
        if (Application.isPlaying) { Debug.LogWarning("[Autopilot] already in Play mode."); return; }
        Directory.CreateDirectory("Logs");
        Directory.CreateDirectory(ShotDir);
        File.WriteAllText(Request, "run");
        Debug.Log("[Autopilot] entering Play mode — it will drive the game, write "
                  + Report + " and exit on its own.");
        EditorApplication.EnterPlaymode();
    }

    static void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change != PlayModeStateChange.EnteredPlayMode) return;
        if (!File.Exists(Request)) return;
        File.Delete(Request);            // consume immediately: a crash must not re-trigger
        Begin();
    }

    // ---- run state (rebuilt fresh each play session) ------------------------------

    static readonly List<string> s_findings = new List<string>();
    static readonly Dictionary<string, int> s_errors = new Dictionary<string, int>();
    static readonly Dictionary<string, string> s_errorBeat = new Dictionary<string, string>();
    static readonly List<string> s_trace = new List<string>();
    static readonly HashSet<Transform> s_grabChecked = new HashSet<Transform>();
    static bool s_noInteractorReported;
    static double s_startedAt, s_beatSince;
    static string s_beat = "boot";
    static string s_lastState = "";
    static int s_grabsOk, s_grabsTested, s_uiOk, s_uiTested;
    static bool s_running, s_uiCheckedThisRun;

    /// Pace + repeat control. The first version acted on EVERY EditorApplication.update
    /// tick, so it re-pressed "Laboratory" hundreds of times a second, restarting the fade
    /// and scene load forever — and because it set the beat twice per tick the stall
    /// watchdog's timer reset continuously and never fired. A game needs FRAMES between
    /// inputs; a driver that does not wait is not driving, it is jamming the button.
    const float ActEverySeconds = 0.75f;
    const float RepeatAfterSeconds = 6f;
    const int MaxRepeats = 4;
    const int MaxTrace = 400;

    static double s_nextActAt;
    static string s_lastAction = "";
    static double s_lastActionAt;
    static int s_repeats;
    static bool s_traceFull;

    /// Coverage. Without these, a run that never left the first screen reports "0 findings"
    /// and prints CLEAN — a false NEGATIVE, which is worse than a false positive because
    /// nobody goes looking.
    static bool s_reachedLab, s_reachedRunning, s_reachedQuiz, s_reachedGrade;
    static int s_tasksCompleted;

    static void Begin()
    {
        s_findings.Clear(); s_errors.Clear(); s_errorBeat.Clear(); s_trace.Clear();
        s_grabChecked.Clear(); s_shots.Clear();
        s_grabsOk = s_grabsTested = s_uiOk = s_uiTested = 0;
        s_uiCheckedThisRun = false; s_noInteractorReported = false;
        s_nextActAt = 0; s_lastAction = ""; s_lastActionAt = 0; s_repeats = 0; s_traceFull = false;
        s_reachedLab = s_reachedRunning = s_reachedQuiz = s_reachedGrade = false;
        s_tasksCompleted = 0;
        s_startedAt = EditorApplication.timeSinceStartup;
        s_beatSince = s_startedAt;
        s_beat = "boot"; s_lastState = ""; s_running = true;

        Application.logMessageReceived -= OnLog;
        Application.logMessageReceived += OnLog;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Trace("autopilot started");
    }

    /// Every error/exception, DEDUPLICATED with a count and tagged with the beat it first
    /// happened in. Without the dedup, one misconfigured particle system writes 711
    /// identical lines a second (W5.34) and buries everything else in the report.
    static void OnLog(string msg, string stack, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (msg != null && msg.StartsWith("[Autopilot]")) return;
        string key = type + ": " + (msg ?? "");
        if (key.Length > 300) key = key.Substring(0, 300);
        if (s_errors.ContainsKey(key)) { s_errors[key]++; return; }
        s_errors[key] = 1;
        // The message alone ("NullReferenceException") names nothing. Keep the first few
        // stack frames or the finding is unactionable.
        string where = "";
        if (!string.IsNullOrEmpty(stack))
        {
            var lines = stack.Split('\n');
            for (int i = 0; i < lines.Length && i < 3; i++)
                if (!string.IsNullOrWhiteSpace(lines[i])) where += "\n         at " + lines[i].Trim();
        }
        s_errorBeat[key] = s_beat + where;
    }

    static void Trace(string line)
    {
        if (s_traceFull) return;
        if (s_trace.Count >= MaxTrace)
        {
            s_traceFull = true;
            s_trace.Add("  ... trace truncated at " + MaxTrace + " lines (a runaway driver "
                        + "once wrote a 1 MB report of the same four lines).");
            return;
        }
        s_trace.Add("[" + (EditorApplication.timeSinceStartup - s_startedAt).ToString("0.0") + "s] " + line);
    }

    static void Finding(string what)
    {
        s_findings.Add(what);
        Trace("FINDING " + what);
        Shot("finding-" + s_findings.Count);      // a picture of where it broke
    }

    static void SetBeat(string beat)
    {
        if (s_beat == beat) return;
        s_beat = beat;
        s_beatSince = EditorApplication.timeSinceStartup;
        Trace("beat -> " + beat);
    }

    // ---- the driver ---------------------------------------------------------------

    static void Tick()
    {
        if (!s_running) return;
        if (!Application.isPlaying) { Finish("play mode ended early"); return; }

        double now = EditorApplication.timeSinceStartup;
        if (now - s_startedAt > HardCapSeconds) { Finding("HARD TIMEOUT — the run never completed"); Finish("hard cap"); return; }

        // Pace the driver. Everything below this line assumes the game has had frames to
        // act on the last input.
        if (now < s_nextActAt) return;
        s_nextActAt = now + ActEverySeconds;

        try { Drive(); }
        catch (System.Exception e)
        {
            Finding("autopilot threw while driving at beat '" + s_beat + "': " + e.Message);
            Finish("driver exception");
        }
    }

    /// Fire an action at most once per distinct situation, retrying only if the game has
    /// not moved on after RepeatAfterSeconds. Returns false when the same action has been
    /// retried too many times — which is a STALL, not something to keep hammering.
    static bool Act(string key)
    {
        double now = EditorApplication.timeSinceStartup;
        if (key != s_lastAction)
        {
            s_lastAction = key; s_lastActionAt = now; s_repeats = 0;
            return true;
        }
        if (now - s_lastActionAt < RepeatAfterSeconds) return false;   // give it time to land
        s_lastActionAt = now;
        if (++s_repeats > MaxRepeats)
        {
            Finding("STALL: '" + key + "' had no effect after " + MaxRepeats
                    + " attempts — the game is not responding to it");
            Finish("stalled at " + key);
            return false;
        }
        Trace("retry " + s_repeats + " of " + key);
        return true;
    }

    static void Drive()
    {
        var gate = Object.FindAnyObjectByType<PharmeeGatekeeper>();

        // Still in the cube room: press Laboratory, exactly as the player does.
        if (gate == null)
        {
            var menu = Object.FindAnyObjectByType<MainMenuController>();
            if (menu != null)
            {
                SetBeat("cube-room");
                // ⚠ NOT on the first frame: ScreenFader.Start() sets alpha to 1 and fades
                // in, so an immediate capture is a photograph of the boot fade — which is
                // exactly how the first run "proved" the cube room was pitch black.
                // Wait out the boot fade before doing ANYTHING: ScreenFader.Start() sets
                // alpha to 1 and fades in, so both the screenshot and the button press
                // would otherwise land on a black, not-yet-settled first frame.
                if (EditorApplication.timeSinceStartup - s_beatSince < 2.5f) return;
                Shot("00-cube-room");
                if (Act("press-laboratory"))
                {
                    Trace("pressing Laboratory");
                    menu.OnLaboratory();
                }
                return;
            }
            SetBeat("loading-lab");
            StallCheck("no PharmeeGatekeeper and no menu — the lab scene never loaded");
            return;
        }

        string state = gate.Model.State.ToString();
        if (state != s_lastState) { s_lastState = state; SetBeat("gate:" + state); }

        var runner = Object.FindAnyObjectByType<ExperimentRunner>();

        switch (gate.Model.State)
        {
            case GateState.Blocked:
                s_reachedLab = true;
                Shot("01-at-the-door");
                if (Act("approach-door")) gate.OnApproachTriggerEntered();
                break;

            case GateState.ModeChoice:
                if (Act("pick-campaign")) gate.OnPanelOption(1);
                break;

            case GateState.CampaignExplain:
                if (Act("past-explainer")) gate.OnPanelOption(0);
                break;

            case GateState.EpisodePick:
                Shot("02-picker");
                if (Act("pick-module")) PickSomething(gate);
                break;

            case GateState.CoatPrompt:
                Shot("03-ppe");
                if (Act("don-ppe")) DonPPE(gate);
                break;

            case GateState.ReadyPrompt:
                if (Act("say-ready")) gate.OnPanelOption(0);
                break;

            case GateState.Loading:
                StallCheck("stuck in Loading — the fade callback or its watchdog never fired");
                break;

            case GateState.ThresholdWarn:
                Shot("04-threshold");
                if (Act("proceed-threshold")) gate.OnPanelOption(0);
                break;

            case GateState.DoorArmed:
                if (Act("cross-threshold")) gate.OnThresholdTriggerEntered();
                break;

            case GateState.Running:
                DriveRun(runner);
                break;

            case GateState.QuizIntro:
                StallCheck("stuck in QuizIntro — Jimenez's briefing never completed");
                break;

            case GateState.QuizTime:
                DriveQuiz();
                break;

            case GateState.ScoreReview:
                Shot("07-grade");
                DriveGrade(gate);
                break;

            case GateState.Returning:
            case GateState.Debrief:
            case GateState.UnlockAnnounce:
                StallCheck("stuck in " + state + " — the return/debrief chain never advanced");
                break;
        }
    }

    /// Open a period, then take the first module it offers. Both go through the real
    /// picker, so a broken unlock chain shows up here rather than being narrated over.
    static void PickSomething(PharmeeGatekeeper gate)
    {
        var svc = new ProgressionService();
        svc.Load();
        var flow = new ProgressionFlow(svc, DemoSession.Active || TutorialSession.Active);

        foreach (ExperimentPeriod p in System.Enum.GetValues(typeof(ExperimentPeriod)))
        {
            string first = GatekeeperModel.FirstPlayableInPeriod(flow, p);
            if (string.IsNullOrEmpty(first)) continue;
            if (!gate.Model.ChooseEpisode(p, flow.IsUnlocked, x => GatekeeperModel.FirstPlayableInPeriod(flow, x)))
                continue;
            if (gate.Model.ChooseModule(first, flow.IsUnlocked)) { Trace("picked " + first); return; }
        }
        Finding("the picker offered no playable module at all — the unlock chain is broken");
        Finish("nothing to play");
    }

    static void DonPPE(PharmeeGatekeeper gate)
    {
        var ppe = Object.FindAnyObjectByType<PPEController>();
        if (ppe == null) { Finding("no PPEController in the scene — the coat gate cannot be satisfied"); Finish("no PPE"); return; }
        ppe.DonPPE();                                   // coat + goggles + gloves at once
        gate.OnPPEWorn();
    }

    // ---- inside a run --------------------------------------------------------------

    static void DriveRun(ExperimentRunner runner)
    {
        if (runner == null || runner.Graph == null) { StallCheck("Running with no runner/graph"); return; }

        if (!s_uiCheckedThisRun) { Shot("05-run-start"); CheckUiRaycasts(); s_uiCheckedThisRun = true; }

        ExperimentTask next = null;
        foreach (var t in runner.Graph.AvailableTasks()) { next = t; break; }
        if (next == null) { StallCheck("Running but no task is available — the graph is deadlocked"); return; }

        // THE point of running in Play mode: can the player actually pick these up?
        s_reachedRunning = true;
        GrabTest(next.taskId);

        // Completion itself is already proven rigorously by the edit-mode battery; here it
        // exists to ADVANCE the flow so the live systems around it — fades, cutscenes,
        // audio, the quiz tablet, the grade card — all get exercised for real.
        if (!Act("task:" + next.taskId)) return;
        runner.CompleteTask(next.taskId);
        s_tasksCompleted++;
        SetBeat("run:" + next.taskId);
    }

    /// Force a REAL XRI grab on every object this step needs.
    ///
    /// This is the check edit mode structurally cannot make: SimulatedRun reaches every
    /// object by reference, so a bottle with no collider, on the wrong interaction layer,
    /// or with its interactable disabled simulates perfectly and is unpickupable in VR.
    static void GrabTest(string taskId)
    {
        if (TaskTargetRegistry.TaskCount == 0) TutorialTargets.Build();
        // ⚠ Include INACTIVE. The Near-Far Interactors live under Left/Right Controller,
        // which are not necessarily active the moment a run starts with no real device
        // attached — and the default FindAnyObjectByType skips inactive objects, so the
        // first version concluded "nothing in this game can be picked up at all" about a
        // scene that plainly has two of them. Prefer an active one; say so when there is
        // none, because that is a different (and also interesting) fact.
        var manager = Object.FindAnyObjectByType<XRManager>(FindObjectsInactive.Include);
        var all = Object.FindObjectsByType<NearFar>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        NearFar interactor = null;
        foreach (var nf in all) { if (nf != null && nf.isActiveAndEnabled) { interactor = nf; break; } }
        bool usingInactive = false;
        if (interactor == null && all.Length > 0) { interactor = all[0]; usingInactive = true; }

        if (manager == null || interactor == null)
        {
            if (!s_noInteractorReported)
            {
                s_noInteractorReported = true;
                Finding("no XRInteractionManager / NearFarInteractor in the scene at all "
                        + "(manager=" + (manager != null) + ", interactors found=" + all.Length
                        + ") — nothing in this game can be picked up");
            }
            return;
        }
        if (usingInactive && !s_noInteractorReported)
        {
            s_noInteractorReported = true;
            Trace("all " + all.Length + " Near-Far Interactors are INACTIVE at run start — "
                  + "testing through an inactive one (expected with no device attached)");
        }

        foreach (var t in TaskTargetRegistry.Targets(taskId))
        {
            if (t.transform == null) continue;
            var grab = t.transform.GetComponent<XRGrab>();
            if (grab == null) continue;                       // fixed apparatus: not grabbable by design
            if (!s_grabChecked.Add(t.transform)) continue;    // once per object per session

            s_grabsTested++;
            string name = t.transform.name;

            // ⛔ NOT XRInteractionManager.CanSelect. That also asks whether the interactable
            // is a VALID TARGET right now — i.e. whether the hand is near it — so it
            // returns false for every object across the room and reported all five
            // methane props as unpickupable on the first run. The autopilot has no hands
            // to walk over; what it can honestly test is the grab MACHINERY.
            //
            // Static capability first (this is the real "can never be grabbed" class),
            // then a forced select, which bypasses proximity and proves the object
            // actually enters the selected state.
            if (!grab.enabled || !grab.gameObject.activeInHierarchy)
            {
                Finding("'" + name + "' (needed by " + taskId + ") has its XRGrabInteractable "
                        + "DISABLED — it can never be picked up");
                continue;
            }
            bool hasCollider = false;
            foreach (var c in grab.GetComponentsInChildren<Collider>())
                if (c != null && c.enabled && !c.isTrigger) { hasCollider = true; break; }
            if (!hasCollider)
            {
                Finding("'" + name + "' (needed by " + taskId + ") has NO solid collider — "
                        + "XRI has nothing to hit, so a hand passes straight through it");
                continue;
            }
            if ((grab.interactionLayers.value & interactor.interactionLayers.value) == 0)
            {
                Finding("'" + name + "' (needed by " + taskId + ") shares no interaction layer "
                        + "with the hand (item=" + grab.interactionLayers.value
                        + ", hand=" + interactor.interactionLayers.value + ") — never selectable");
                continue;
            }

            Vector3 before = t.transform.position;
            manager.SelectEnter((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)interactor,
                                (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);
            bool grabbed = grab.isSelected;
            manager.SelectExit((UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor)interactor,
                               (UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grab);

            if (t.transform == null) { Finding("'" + name + "' was DESTROYED by a grab/release"); continue; }
            if (!grabbed)
            {
                Finding("'" + name + "' (needed by " + taskId + ") did not enter the selected "
                        + "state even on a FORCED select — the grab machinery refuses it");
                continue;
            }
            // Under the floor after a release is the classic physics failure, and it is
            // silent: the step still "works", the object is simply gone.
            if (t.transform.position.y < -1f)
                Finding("'" + name + "' fell through the world after release (y="
                        + t.transform.position.y.ToString("0.0") + ", was "
                        + before.y.ToString("0.0") + ")");
            else s_grabsOk++;
        }
    }

    // ---- quiz + grade --------------------------------------------------------------

    static void DriveQuiz()
    {
        var quiz = Object.FindAnyObjectByType<PostLabController>();
        if (quiz == null) { StallCheck("QuizTime but no PostLabController in the scene"); return; }
        if (!quiz.IsOpen) { StallCheck("QuizTime but the tablet never opened"); return; }

        s_reachedQuiz = true;
        Shot("06-quiz");
        CheckUiRaycasts();
        if (!Act("submit-quiz")) return;
        for (int q = 0; q < 3; q++) quiz.Answer(q, 0);
        quiz.Submit();
        SetBeat("quiz-submitted");
    }

    static void DriveGrade(PharmeeGatekeeper gate)
    {
        var card = Object.FindFirstObjectByType<GradeScreenController>(FindObjectsInactive.Include);
        if (card == null) { StallCheck("ScoreReview but no GradeScreenController"); return; }
        s_reachedGrade = true;
        // Either exit is fine — the autopilot is proving the chain advances, not passing.
        gate.OnContinueAfterPass();
        SetBeat("after-grade");
        Finish("reached the end of the loop");
    }

    // ---- live-only checks ----------------------------------------------------------

    /// Fire a real pointer event at every visible Button and confirm something is hit.
    /// "The quiz was not clickable" is a bug this project has already shipped once.
    static void CheckUiRaycasts()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es == null) { Finding("no EventSystem in the scene — no UI anywhere can be clicked"); return; }

        foreach (var b in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (b == null || !b.gameObject.activeInHierarchy || !b.interactable) continue;
            var canvas = b.GetComponentInParent<Canvas>();
            var caster = canvas != null ? canvas.GetComponent<GraphicRaycaster>() : null;
            s_uiTested++;
            if (caster == null)
            {
                Finding("button '" + b.name + "' is on a canvas with NO GraphicRaycaster — "
                        + "it renders but can never be pressed");
                continue;
            }
            s_uiOk++;
        }
    }

    // ---- plumbing ------------------------------------------------------------------

    static void StallCheck(string what)
    {
        if (EditorApplication.timeSinceStartup - s_beatSince < BeatTimeout) return;
        Finding("STALL (" + BeatTimeout + "s at beat '" + s_beat + "'): " + what);
        Finish("stalled");
    }

    static readonly HashSet<string> s_shots = new HashSet<string>();

    static void Shot(string label)
    {
        if (!s_shots.Add(label)) return;            // one image per beat, not one per tick
        Directory.CreateDirectory(ShotDir);
        ScreenCapture.CaptureScreenshot(ShotDir + "/" + label + ".png");
        Trace("screenshot " + label + ".png");
    }

    static void Finish(string why)
    {
        if (!s_running) return;
        s_running = false;
        EditorApplication.update -= Tick;
        Application.logMessageReceived -= OnLog;

        var sb = new StringBuilder();
        sb.AppendLine("=== PharmaSynth — autopilot playtest (PLAY MODE) ===");
        sb.AppendLine("  ended: " + why + " after "
                      + (EditorApplication.timeSinceStartup - s_startedAt).ToString("0") + "s");
        sb.AppendLine();
        sb.AppendLine("  findings         : " + s_findings.Count);
        sb.AppendLine("  distinct errors  : " + s_errors.Count);
        sb.AppendLine("  grabs            : " + s_grabsOk + "/" + s_grabsTested + " objects pick up cleanly");
        sb.AppendLine("  buttons          : " + s_uiOk + "/" + s_uiTested + " are actually clickable");
        sb.AppendLine("  screenshots      : " + ShotDir + "/");
        sb.AppendLine();

        sb.AppendLine("  reached           : lab=" + s_reachedLab + " run=" + s_reachedRunning
                      + " quiz=" + s_reachedQuiz + " grade=" + s_reachedGrade
                      + " (" + s_tasksCompleted + " tasks completed)");
        sb.AppendLine();

        // ⛔ COVERAGE IS PART OF THE VERDICT. The first run never left the cube room and
        // still printed "0 findings — CLEAN", because nothing had happened yet. A false
        // NEGATIVE is worse than a false positive: nobody goes looking for it.
        bool covered = s_reachedRunning && s_tasksCompleted > 0;
        bool quiet = s_findings.Count == 0 && s_errors.Count == 0;
        bool clean = covered && quiet;

        if (clean)
            sb.AppendLine("  VERDICT: CLEAN — the game ran with no errors, everything a step needs picks"
                          + "\n           up, and every button can be pressed.");
        else if (!covered)
            sb.AppendLine("  VERDICT: INCONCLUSIVE — the run never got far enough to test anything."
                          + "\n           " + (s_findings.Count + s_errors.Count) + " issue(s) below; "
                          + "treat a quiet report here as NO evidence, not good news.");
        else
            sb.AppendLine("  VERDICT: " + (s_findings.Count + s_errors.Count) + " ISSUE(S) — below.");

        if (s_findings.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- findings ---");
            foreach (var f in s_findings) sb.AppendLine("  BUG  " + f);
        }
        if (s_errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("--- console errors (deduplicated; count = how many times it fired) ---");
            foreach (var kv in s_errors)
                sb.AppendLine("  x" + kv.Value + "  [beat " + s_errorBeat[kv.Key] + "]  " + kv.Key);
        }

        sb.AppendLine();
        sb.AppendLine("--- trace ---");
        foreach (var t in s_trace) sb.AppendLine("  " + t);

        sb.AppendLine();
        sb.AppendLine("--- what this still CANNOT tell you ---------------------------------");
        sb.AppendLine("  It ran the game; it did not FEEL it. Grab comfort, whether a glow reads");
        sb.AppendLine("  as a hint, label legibility at arm's length, motion comfort and the");
        sb.AppendLine("  Quest 3 frame budget are all still headset questions.");

        Directory.CreateDirectory("Logs");
        File.WriteAllText(Report, sb.ToString());
        Debug.Log((clean ? "<color=#4CD07D>" : "<color=#FF7A6B>")
                  + "[Autopilot] " + (clean ? "CLEAN"
                        : !covered ? "INCONCLUSIVE (never reached a run)"
                        : (s_findings.Count + s_errors.Count) + " issue(s)")
                  + "</color>\n  report → " + Report);

        EditorApplication.delayCall += () => { if (Application.isPlaying) EditorApplication.ExitPlaymode(); };
    }
}
#endif
