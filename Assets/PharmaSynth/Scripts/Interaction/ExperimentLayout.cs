using System;
using System.Collections.Generic;
using UnityEngine;

/// Data description of one experiment's physical setup: where its stations, grabbable
/// props and reagent vessels go. The ExperimentSceneBuilder spawns this on module load,
/// so all 11 experiments share one lab scene instead of 11 hand-built scenes.
/// Positions are WORLD-space (the lab is a fixed room).
/// How a station completes. None = the prop simply entering the zone completes the
/// step. Heat/Crystallise/Filter/Collect run a sustained chemistry sim while the
/// prop occupies the zone. Stir/Grind/Weigh (W5.8, append-only — serialized ints
/// stay valid) are TOOL verbs: circle the rod in the vessel, work the pestle in
/// the mortar, rest the right load on the balance pan.
public enum StationSim { None, Heat, Crystallise, Filter, Collect, Stir, Grind, Weigh }

[CreateAssetMenu(fileName = "ExperimentLayout", menuName = "PharmaSynth/Experiment Layout")]
public class ExperimentLayout : ScriptableObject
{
    [Serializable]
    public class Station
    {
        public string taskId;
        public string label;
        public string requiredItemId;     // grabbable prop that completes it (empty = any)
        public Vector3 pos;               // pad centre, world space (y = surface)
        [Tooltip("Real-verb sim driven while the prop occupies the zone. None = instant zone-touch completion.")]
        public StationSim sim = StationSim.None;
        [Tooltip("Target °C for Heat stations (distillation cut-off / water-bath).")]
        public float simTargetC = 80f;
    }

    [Serializable]
    public class Prop
    {
        public string prefabName;         // ChemLabEquipment prefab (by name) via the library
        public string itemId;             // matches a Station.requiredItemId
        public string displayName;        // proximity label
        public Vector3 pos;
        public float targetHeight = 0.16f;
        public bool pourable = false;     // add LiquidPourer + fill
        public string fillChemical = "";  // ChemicalData name to fill a pourable bottle with
        [Tooltip("Finite supply in ml (0 = auto: 2.5x the summed need, so ~2 spare pours).")]
        public float supplyMl = 0f;
    }

    [Serializable]
    public class Vessel
    {
        [Serializable] public class Bind
        {
            public string reagentChemical;
            public string taskId;
            [Tooltip("Minimum ml poured before the step completes (0 = any amount).")]
            public float requiredMl;
            [Tooltip("False = pouring only ACCUMULATES (expected, never a wrong-reagent mistake); something else — e.g. the weigh station — completes the task. (W5.8)")]
            public bool completesTask = true;
        }
        public string prefabName;
        public string displayName;
        public Vector3 pos;
        public float targetHeight = 0.2f;
        public string startChemical = "";     // what the vessel starts holding (optional)
        public List<Bind> bindings = new List<Bind>();   // reagent poured in → task completed
        [Tooltip("Vessels sharing a rackGroup are ONE SET: a step they share only completes once EVERY member has had its reagent (RackTaskGroup). Exp 2's five enol tubes, its butyl alcohols, acetone-vs-acetaldehyde. Empty = a standalone vessel. Members author their shared bindings completesTask:false — the group completes them.")]
        public string rackGroup = "";
    }

    public string moduleId;
    public List<Station> stations = new List<Station>();
    public List<Prop> props = new List<Prop>();
    public List<Vessel> vessels = new List<Vessel>();
}
