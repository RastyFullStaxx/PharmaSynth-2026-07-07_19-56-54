#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Re-runnable regression suite for the PharmaSynth engine. Run via
/// menu: Tools ▸ PharmaSynth ▸ Run Self-Tests. Consolidates the assertions that
/// were verified incrementally during W2–W3 into one permanent, one-click check.
/// (Kept as an Editor-menu suite rather than an NUnit asmdef to avoid restructuring
/// the runtime assembly; a formal EditMode asmdef migration can layer on later.)
public static class PharmaSelfTests
{
    static int _fail, _total;
    static readonly List<string> _log = new List<string>();

    static void A(string name, bool cond)
    {
        _total++;
        if (!cond) { _fail++; _log.Add("FAIL  " + name); }
    }
    static bool Near(float a, float b, float e = 0.01f) => Math.Abs(a - b) < e;

    [MenuItem("Tools/PharmaSynth/Run Self-Tests")]
    public static void Run()
    {
        _fail = 0; _total = 0; _log.Clear();

        TaskGraphSuite();
        MasterySuite();
        ScoreSuite();
        GraderSuite();
        RunnerSuite();
        ProgressionSuite();
        ChemVisualSuite();
        UISuite();

        string summary = $"PharmaSynth Self-Tests: {_total - _fail}/{_total} passed";
        if (_fail == 0) Debug.Log("<color=#4CD07D>" + summary + " — ALL GREEN</color>");
        else Debug.LogError(summary + " — " + _fail + " FAILED:\n" + string.Join("\n", _log));
    }

    static ExperimentTask T(string id, TaskPhase ph, float w, LabSkill sk, RubricCategory rc, params string[] pre)
    {
        var t = new ExperimentTask { taskId = id, label = id, phase = ph, progressWeight = w, skill = sk, rubricCategory = rc, required = true };
        t.prerequisites = new List<string>(pre);
        return t;
    }

    static void TaskGraphSuite()
    {
        var g = new TaskGraph(new List<ExperimentTask> {
            T("prep", TaskPhase.ReagentPrep, 1, LabSkill.Measuring, RubricCategory.Procedure),
            T("mix", TaskPhase.Synthesis, 2, LabSkill.Transfer, RubricCategory.Procedure, "prep"),
            T("test", TaskPhase.ChemicalTests, 1, LabSkill.TestInterpretation, RubricCategory.ChemicalTests, "mix"),
        });
        A("graph: mix blocked before prep", g.TryComplete("mix") == TaskCompletionResult.BlockedByPrerequisite);
        A("graph: prep completes", g.TryComplete("prep") == TaskCompletionResult.Completed);
        A("graph: progress 0.25", Near(g.Progress01, 0.25f));
        A("graph: double-complete guarded", g.TryComplete("prep") == TaskCompletionResult.AlreadyComplete);
        g.TryComplete("mix");
        A("graph: progress 0.75", Near(g.Progress01, 0.75f));
        bool sensor = false; g.RegisterCondition("test", () => sensor); g.Tick();
        A("graph: auto-check waits", !g.IsComplete("test"));
        sensor = true; g.Tick();
        A("graph: auto-check fires", g.IsComplete("test"));
        A("graph: progress 1.0", Near(g.Progress01, 1f));
    }

    static void MasterySuite()
    {
        var m = new MasteryModel(new BktParameters(), new[] { LabSkill.Measuring });
        A("bkt: starts at pL0", Near(m.OverallMastery(), 0.25f));
        for (int i = 0; i < 6; i++) m.Observe(LabSkill.Measuring, true);
        A("bkt: converges past gate", m.IsMastered(0.90f));
        float before = m.OverallMastery(); m.Observe(LabSkill.Measuring, false);
        A("bkt: slip lowers mastery", m.OverallMastery() < before);
    }

    static void ScoreSuite()
    {
        var sc = new ScoreCalculator(new RubricWeights());
        var perfect = new Dictionary<RubricCategory, float>();
        foreach (RubricCategory c in Enum.GetValues(typeof(RubricCategory))) perfect[c] = 1f;
        A("score: perfect = 100", Near(sc.Compute(perfect).Total, 100f, 0.2f));
        var w = new RubricWeights { procedure = 0.4f, chemicalTests = 0.4f, materialsAndPPE = 0, timeManagement = 0, sanitation = 0, documentation = 0 };
        var sc2 = new ScoreCalculator(w);
        A("score: weights normalized", Near(sc2.Compute(new Dictionary<RubricCategory, float> { { RubricCategory.Procedure, 1 }, { RubricCategory.ChemicalTests, 1 } }).Total, 100f, 0.2f));
        A("score: time under par", Near(ScoreCalculator.TimeSubScore(300, 600), 1f));
        A("score: time 2x par", Near(ScoreCalculator.TimeSubScore(1200, 600), 0f));
    }

    static void GraderSuite()
    {
        var tasks = new List<ExperimentTask> {
            T("prep", TaskPhase.ReagentPrep, 1, LabSkill.Measuring, RubricCategory.Procedure),
            T("synth", TaskPhase.Synthesis, 1, LabSkill.Transfer, RubricCategory.Procedure, "prep"),
            T("t", TaskPhase.ChemicalTests, 1, LabSkill.TestInterpretation, RubricCategory.ChemicalTests, "synth"),
            T("clean", TaskPhase.DataSheet, 1, LabSkill.Transfer, RubricCategory.Sanitation, "t"),
        };
        var g = new TaskGraph(tasks);
        g.TryComplete("prep"); g.TryComplete("synth"); g.TryComplete("t"); g.TryComplete("clean");
        var grader = new ExperimentGrader(new ScoreCalculator(new RubricWeights()), new GradingConfig());
        A("grader: perfect run = 100", Near(grader.Grade(g, new MistakeLog(), 300, 600, 1f).Total, 100f, 0.2f));
        var log = new MistakeLog();
        log.Record(LabErrorType.WrongReagent, ""); log.Record(LabErrorType.WrongStep, "");
        log.Record(LabErrorType.MissingPPE, ""); log.Record(LabErrorType.DroppedGlassware, "");
        A("grader: mistakes = 81.08", Near(grader.Grade(g, log, 900, 600, 2f / 3f).Total, 81.08f, 0.3f));
        A("mistakelog: category mapping", MistakeLog.CategoryFor(LabErrorType.DroppedGlassware) == RubricCategory.Sanitation);
    }

    static void RunnerSuite()
    {
        var module = ScriptableObject.CreateInstance<ExperimentModuleDefinition>();
        module.graphTasks = new List<ExperimentTask> {
            T("a", TaskPhase.ReagentPrep, 1, LabSkill.Measuring, RubricCategory.Procedure),
            T("b", TaskPhase.Synthesis, 1, LabSkill.Transfer, RubricCategory.Procedure, "a"),
        };
        module.trackedSkills = new List<LabSkill> { LabSkill.Measuring, LabSkill.Transfer };
        var go = new GameObject("SelfTestRunner");
        try
        {
            var runner = go.AddComponent<ExperimentRunner>();
            runner.SetModule(module);
            runner.StartExperiment();
            A("runner: out-of-order = wrong step", runner.CompleteTask("b") == TaskCompletionResult.BlockedByPrerequisite && runner.MistakeCount == 1);
            runner.CompleteTask("a"); runner.CompleteTask("b");
            var res = runner.Finish(1f);
            A("runner: two-part gate computed", res.grade.Total > 0 && (res.passed == (res.gradePassed && res.masteryPassed)));
            A("runner: retry resets", RetryResets(runner));
        }
        finally { UnityEngine.Object.DestroyImmediate(go); UnityEngine.Object.DestroyImmediate(module); }
    }
    static bool RetryResets(ExperimentRunner r) { r.Retry(); return Near(r.Progress01, 0f) && r.MistakeCount == 0; }

    static void ProgressionSuite()
    {
        var svc = new ProgressionService("selftest-mem");
        var b = new GradeBreakdown { Total = 94 };
        svc.RecordResult("m", new ExperimentResult { grade = b, overallMastery = 0.93f, passed = true }, false);
        A("progression: passes latch", svc.IsPassed("m"));
        A("progression: unlock next", svc.IsUnlocked("m2", "m"));
        var json = JsonUtility.ToJson(svc.Data);
        A("progression: json round-trips", JsonUtility.FromJson<ProgressSaveData>(json).modules[0].passed);
    }

    static void ChemVisualSuite()
    {
        var h = new HeatModel(22f) { HeatRate = 0.35f };
        h.SetHeating(true, 100f); for (int i = 0; i < 60; i++) h.Step(1f);
        A("heat: rises toward source", h.Current > 90f);
        var a1 = new HeatModel(22f); a1.SetHeating(true, 100f); a1.Step(10f);
        var a2 = new HeatModel(22f); a2.SetHeating(true, 100f); for (int i = 0; i < 100; i++) a2.Step(0.1f);
        A("heat: timestep-independent", Near(a1.Current, a2.Current, 0.1f));
        var gg = new GameObject("gas");
        try { var gc = gg.AddComponent<GasCollection>(); gc.AddGas(50f); A("gas: fill fraction", Near(gc.FillFraction, 0.5f)); }
        finally { UnityEngine.Object.DestroyImmediate(gg); }
    }

    static void UISuite()
    {
        A("ui: time format", ExperimentHudController.FormatTime(920) == "00:15:20");
        A("ui: percent format", ExperimentHudController.FormatPercent(0.754f) == "75%");
        A("ui: progress drops on mistakes", Near(ExperimentHudController.DisplayedProgress(0.8f, 2, 0.05f), 0.7f));
        A("ui: instruction from hint", PharmeeBrain.InstructionFor(new ExperimentTask { label = "X", hint = "do x" }) == "do x");
    }
}
#endif
