#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Wires the 2026-07-10 NPC/audio polish batch into SampleScene:
///   1. Pharmee expressions — PharmeeFace re-pointed at the robot's EYES + MOUTH
///      meshes (was Ears_Black_Matt_0), default-happy; PharmeeMood resets the face
///      after every line; the gatekeeper's faceBehaviour drives gate moods.
///   2. Dr. Jimenez proctor roaming — ProctorRoamer + observation points at the
///      reagent shelf, equipment shelf, dynamic stage and fume hood.
///   3. AC proximity hum — ProximityHum on the air-con / vent assets (falls back to
///      the fume hood if no AC mesh exists).
///
/// Tools ▸ PharmaSynth ▸ Wire NPC Polish (SampleScene, edit mode, idempotent).
public static class LabNpcPolishBuilder
{
    [MenuItem("Tools/PharmaSynth/Wire NPC Polish")]
    public static void Build()
    {
        if (Application.isPlaying) { Debug.LogWarning("[NpcPolish] exit Play mode first."); return; }

        // ---- 1. Pharmee face ------------------------------------------------
        var robot = GameObject.Find("RobotNPC");
        if (robot == null) { Debug.LogError("[NpcPolish] no RobotNPC"); return; }
        var face = robot.GetComponentInChildren<PharmeeFace>(true);
        if (face == null) face = robot.AddComponent<PharmeeFace>();

        var faceParts = new List<Renderer>();
        foreach (var r in robot.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.name.ToLower();
            if (n.StartsWith("eyes") || n.StartsWith("mouth")) faceParts.Add(r);
        }
        if (faceParts.Count > 0) face.BindRenderers(faceParts.ToArray());
        Debug.Log("[NpcPolish] face renderers: " + faceParts.Count + " (" + string.Join(", ", faceParts.ConvertAll(r => r.name)) + ")");

        var narration = robot.GetComponentInChildren<NPCNarrationController>(true);
        var mood = robot.GetComponent<PharmeeMood>();
        if (mood == null) mood = robot.AddComponent<PharmeeMood>();
        mood.Bind(narration, face);

        var gk = robot.GetComponentInChildren<PharmeeGatekeeper>(true);
        if (gk != null)
        {
            var soGk = new SerializedObject(gk);
            soGk.FindProperty("faceBehaviour").objectReferenceValue = face;
            soGk.ApplyModifiedProperties();
        }
        var brain = robot.GetComponentInChildren<PharmeeBrain>(true);
        if (brain != null)
        {
            var soBr = new SerializedObject(brain);
            var p = soBr.FindProperty("faceBehaviour");
            if (p != null && p.objectReferenceValue == null)
            { p.objectReferenceValue = face; soBr.ApplyModifiedProperties(); }
        }

        // ---- 2. Jimenez roaming ---------------------------------------------
        var jim = GameObject.Find("DrJimenez");
        if (jim == null) jim = GameObject.Find("RiggedDrjimenez");
        if (jim == null)
        {
            foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                if (t.name.ToLower().Contains("jimenez") && t.parent == null) { jim = t.gameObject; break; }
        }
        if (jim != null)
        {
            var runnerGo = GameObject.Find("ExperimentSystems");
            var runner = runnerGo != null ? runnerGo.GetComponent<ExperimentRunner>() : null;
            var animator = jim.GetComponentInChildren<Animator>(true);

            // Observation points: proud of each landmark, standing height on the floor.
            var group = GameObject.Find("ProctorPoints");
            if (group != null) Object.DestroyImmediate(group);
            group = new GameObject("ProctorPoints");
            Undo.RegisterCreatedObjectUndo(group, "Wire NPC Polish");
            var points = new List<Transform>();
            void Point(string landmark, Vector3 fallback, Vector3 offset)
            {
                var lm = GameObject.Find(landmark);
                Vector3 p = (lm != null ? lm.transform.position : fallback) + offset;
                p.y = jim.transform.position.y;                        // keep his standing height
                var pt = new GameObject("Watch_" + landmark).transform;
                pt.SetParent(group.transform, false);
                pt.position = p;
                points.Add(pt);
            }
            Point("ReagentShelf", new Vector3(-4.5f, 0f, -3f), new Vector3(0.9f, 0f, 0f));
            Point("DynamicStage", new Vector3(-2f, 0f, -5f), new Vector3(0.8f, 0f, 0.8f));
            Point("EquipmentShelf", new Vector3(-4.8f, 0f, -6.5f), new Vector3(0.9f, 0f, 0.3f));
            Point("FumeHood_StandIn", new Vector3(2f, 0f, -6.5f), new Vector3(0f, 0f, 1.0f));

            var roamer = jim.GetComponent<ProctorRoamer>();
            if (roamer == null) roamer = jim.AddComponent<ProctorRoamer>();
            roamer.Bind(animator, runner, points);
            EditorUtility.SetDirty(jim);
            Debug.Log("[NpcPolish] Jimenez roamer wired (animator=" + (animator != null) + ", runner=" + (runner != null) + ", points=" + points.Count + ")");
        }
        else Debug.LogWarning("[NpcPolish] Dr. Jimenez not found — roamer skipped");

        // ---- 3. AC hum --------------------------------------------------------
        int hums = 0;
        var humHosts = new List<GameObject>();
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            string n = t.name.ToLower();
            if ((n.Contains("aircon") || n.Contains("air_cond") || n.Contains("airconditioner")
                 || n.Contains("ac_unit") || n.Contains("hvac") || n.Contains("vent"))
                && t.GetComponentInChildren<Renderer>() != null)
                humHosts.Add(t.gameObject);
        }
        if (humHosts.Count == 0)
        {
            var hood = GameObject.Find("FumeHood_StandIn");
            if (hood != null) humHosts.Add(hood);   // the hood's fan IS the room's machine noise
        }
        foreach (var host in humHosts)
        {
            if (host.GetComponent<ProximityHum>() == null)
            {
                var hum = host.AddComponent<ProximityHum>();
                hum.Bind("ambient-lab", 0.5f);
                EditorUtility.SetDirty(host);
            }
            hums++;
        }
        Debug.Log("[NpcPolish] proximity hums on " + hums + " host(s): "
            + string.Join(", ", humHosts.ConvertAll(h => h.name)));

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(robot.scene);
        Debug.Log("<color=#4CD07D>[NpcPolish] done</color>");
    }
}
#endif
