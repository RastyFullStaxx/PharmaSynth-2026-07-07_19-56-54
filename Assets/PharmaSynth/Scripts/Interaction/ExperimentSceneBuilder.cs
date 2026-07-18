using System.Collections.Generic;
using UnityEngine;
using TMPro;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;
using XRSocket = UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor;
using TeleAnchor = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationAnchor;
using TeleArea = UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation.TeleportationArea;

/// Spawns an experiment's physical setup (stations, grabbable props, reagent vessels)
/// from its ExperimentLayout when a module loads — so all 11 experiments live in one
/// lab scene. The hand-built Methane objects stay as a grouped stage that is simply
/// toggled; every other experiment is built into a DynamicStage that is cleared and
/// rebuilt on each module change.
public class ExperimentSceneBuilder : MonoBehaviour
{
    [SerializeField] private ExperimentRunner runner;
    [SerializeField] private SceneAssetLibrary assets;
    [SerializeField] private ReactionRegistry registry;
    [SerializeField] private List<ExperimentLayout> layouts = new List<ExperimentLayout>();
    [SerializeField] private GameObject methaneStage;   // existing hand-built Methane objects
    [SerializeField] private Transform labelsRoot;      // WorldLabels
    [SerializeField] private string methaneModuleId = "tutorial-methane";

    private Transform _stage;

    public void SetRefs(ExperimentRunner r, SceneAssetLibrary a, ReactionRegistry reg, List<ExperimentLayout> ls)
    { runner = r; assets = a; registry = reg; if (ls != null) layouts = ls; }

    public ExperimentLayout FindLayout(string moduleId)
    {
        foreach (var l in layouts) if (l != null && l.moduleId == moduleId) return l;
        return null;
    }

    /// Hook for ExperimentLauncher.onModuleLoaded.
    public void OnModuleLoaded(ExperimentModuleDefinition m) { if (m != null) Build(m.moduleId); }

    /// The stage root. ADOPTS an existing "DynamicStage" child before making one:
    /// `_stage` is a plain field, so it is null after every domain reload — and this
    /// used to answer that by creating a SECOND DynamicStage, leaving the first
    /// orphaned with the previous build's objects still in it. Build() then cleared
    /// only the new one, so nothing ever tidied the old stage and its contents stayed
    /// on the bench forever. (Found 2026-07-16: two DynamicStage siblings, one with 46
    /// leftover objects.) Duplicates are collapsed here rather than left to rot.
    private Transform Stage()
    {
        if (_stage != null) return _stage;

        Transform found = null;
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var c = transform.GetChild(i);
            if (c.name != "DynamicStage") continue;
            if (found == null) found = c;
            else Kill(c.gameObject);        // collapse an earlier orphaned stage
        }
        if (found == null)
        {
            var go = new GameObject("DynamicStage");
            go.transform.SetParent(transform, false);
            found = go.transform;
        }
        _stage = found;
        return _stage;
    }

    private static void Kill(GameObject go)
    {
        if (Application.isPlaying) Object.Destroy(go); else Object.DestroyImmediate(go);
    }

    private static void Kill(Component c)
    {
        if (Application.isPlaying) Object.Destroy(c); else Object.DestroyImmediate(c);
    }

    /// Build the setup for a module. Returns the number of spawned root objects.
    /// Public + returns count so edit-mode self-tests can verify it.
    /// The module currently being built — vessel wiring that needs the module's
    /// identity (the vapor stream's product chemical) reads it here.
    private string moduleBeingBuilt = "";

    public int Build(string moduleId)
    {
        moduleBeingBuilt = moduleId;
        var stage = Stage();
        for (int i = stage.childCount - 1; i >= 0; i--) Kill(stage.GetChild(i).gameObject);
        // Bench-bound vessels wire task logic onto PERMANENT objects, which the line
        // above cannot reach — strip it or the last module's bindings complete this
        // one's steps. Must run every build, before anything is wired.
        ClearBenchBindings();
        // Clear the dynamic labels we spawned last time.
        if (labelsRoot != null)
        {
            var dead = new List<GameObject>();
            foreach (Transform t in labelsRoot) if (t.name.StartsWith("DynLabel_")) dead.Add(t.gameObject);
            foreach (var d in dead) Kill(d);
        }

        bool methane = moduleId == methaneModuleId;
        // Methane stage visibility is owned by MethaneStageVisibility now (shows
        // during Lab Tour + the Methane attempt only) — the builder no longer
        // toggles it, so the two never fight. (W5.12, user 2026-07-13.)
        // Demo sessions no longer spawn a floating ready-made vial here (user
        // 2026-07-12): the finished products live on the ReagentShelf and are
        // revealed there by EndProductVisibility while a demo session is active.
        if (methane) return 0;                      // Methane uses its hand-built stage

        var layout = FindLayout(moduleId);
        if (layout == null) { Debug.LogWarning("[SceneBuilder] no layout for " + moduleId); return 0; }

        int n = 0;
        _currentLayout = layout;
        foreach (var s in layout.stations) { BuildStation(stage, s); n++; }
        foreach (var p in layout.props)    { BuildProp(stage, p); n++; }
        _rackBindings.Clear();
        foreach (var v in layout.vessels)  { BuildVessel(stage, v); n++; }
        WireRackGroups(stage, layout);        // "all five tubes" steps (2026-07-16) — needs vessels spawned
        WireVerbControllers(stage, layout);   // stir/grind/burner-gate (W5.8) — needs props+vessels spawned
        // ⛔ SpawnRackKit / SpawnSpares / StageConsumables REMOVED 2026-07-16.
        // They date from before the permanent bench and duplicated it on EVERY module:
        // another test-tube rack + 6 tubes, 2 spare beakers + a flask, and a "MatchStriker"
        // cube that is redundant because the matchbox itself has been the striker since
        // W5.8. The bench already carries all of it (Kit_TestTube_0-18, Eq_Beaker_100mL/
        // 500mL, ErlenmeyerFlask, racks, Raw_Matchsticks). The user deleted the clones by
        // hand — re-spawning them here would silently undo that on the next module load.
        // (user 2026-07-16: "we'll be using the existing tools already laid out")
        return n;
    }

    /// A fixed test-tube rack near the vessels, pre-filled with 6 grabbable
    /// tubes (full receiver treatment; each tube's DropRespawn home = its slot,
    /// so the rack refills itself when tubes are abandoned).
    private void SpawnRackKit(Transform stage)
    {
        var prefab = assets != null ? assets.GetPrefab("TestTubeRack") : null;
        if (prefab == null) return;
        var rack = Instantiate(prefab, stage);
        rack.name = "RackKit";
        Normalise(rack, "TestTubeRack", 0.18f);
        Seat(rack.transform, LayoutTidyMath.RackPos);
        var grab = rack.GetComponent<XRGrab>(); if (grab != null) Kill(grab);
        var rrb = rack.GetComponent<Rigidbody>(); if (rrb != null) Kill(rrb);

        var b = WB(rack);
        var tubePrefab = assets.GetPrefab("TestTube");
        if (tubePrefab == null) return;
        for (int i = 0; i < 6; i++)
        {
            int col = i % 3, row = i / 3;
            var pos = new Vector3(
                Mathf.Lerp(b.min.x + 0.03f, b.max.x - 0.03f, col / 2f),
                b.max.y + 0.02f,
                Mathf.Lerp(b.min.z + 0.02f, b.max.z - 0.02f, row));
            SpawnReceiver(stage, tubePrefab, "TestTube", "RackTube_" + i, "Test Tube", pos, 0.15f);
        }
    }

    /// Spare vital glassware on the free front strip (user: "enough duplicates
    /// of things that are vital like beakers").
    private void SpawnSpares(Transform stage)
    {
        if (assets == null) return;
        var specs = new (string prefab, string label)[]
        {
            ("Beaker_100mL", "Spare Beaker"),
            ("Beaker_100mL", "Spare Beaker"),
            ("ErlenmeyerFlask_400mL", "Spare Flask"),
        };
        for (int i = 0; i < specs.Length; i++)
        {
            var prefab = assets.GetPrefab(specs[i].prefab);
            if (prefab == null) continue;
            SpawnReceiver(stage, prefab, specs[i].prefab, "Spare_" + specs[i].prefab + "_" + i,
                          specs[i].label, LayoutTidyMath.SparePos(i), 0.14f);
        }
    }

    /// A grabbable empty receiver vessel with the full W5.8 treatment.
    private void SpawnReceiver(Transform stage, GameObject prefab, string prefabName, string name,
                               string label, Vector3 pos, float targetHeight)
    {
        var swap = VesselPrefabFor(prefabName);
        var inst = Instantiate(swap != null ? swap : prefab, stage);
        inst.name = name;
        Normalise(inst, prefabName, targetHeight);
        Seat(inst.transform, pos);
        var item = inst.GetComponent<LabItem>() ?? inst.AddComponent<LabItem>();
        item.itemId = name; item.displayName = label;
        var rb = PhysicsProfiles.EnsurePhysics(inst, prefabName);
        inst.AddComponent<GrabPhysicsPolicy>();
        GrabTuning.Apply(inst.GetComponent<XRGrab>());
        inst.AddComponent<HoverHighlight>().Bind(inst.GetComponent<XRGrab>());
        var respawn = inst.AddComponent<DropRespawn>();
        respawn.SetHome(inst.transform.position, inst.transform.rotation);
        if (Mishandling.IsBreakable(prefabName))
        {
            var breakable = inst.AddComponent<BreakableGlassware>();
            breakable.Bind(runner, respawn, rb, label);
            inst.AddComponent<ImpactSound>().Bind(rb, Mishandling.DropSoundKey(prefabName), Mishandling.DefaultBreakSpeed);
        }
        else inst.AddComponent<ImpactSound>().Bind(rb, Mishandling.DropSoundKey(prefabName));
        var lp = inst.GetComponent<LiquidPhysics>() ?? inst.AddComponent<LiquidPhysics>();
        lp.registry = registry;
        lp.SetContents(null, 0f);
        EnsureLiquidVisual(inst, lp);
        inst.AddComponent<HazardousMixReactor>().Bind(lp, runner);
        inst.AddComponent<CleanableVessel>().Bind(lp);   // used vessels get dirty (W5.12)
        var pl = inst.AddComponent<ProximityLabel>(); pl.SetLabel(label, 1.6f);
        inst.AddComponent<VesselStatus>().Bind(lp, pl, label, 1.6f);
        inst.AddComponent<MixFeedback>().Bind(lp);
    }

    /// Heat experiments stage two ready matchsticks + a striker block on the
    /// table (the cabinet dispenser remains the endless source). Cloned from
    /// the dispenser's hidden template when it exists; skipped silently if not.
    private void StageConsumables(Transform stage, ExperimentLayout layout)
    {
        bool hasHeat = false;
        foreach (var s in layout.stations) if (s.sim == StationSim.Heat) hasHeat = true;
        if (!hasHeat) return;

        // Striker block (always useful next to the matches).
        var striker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        striker.name = "MatchStriker";
        striker.transform.SetParent(stage, false);
        striker.transform.localScale = new Vector3(0.09f, 0.02f, 0.06f);
        Seat(striker.transform, LayoutTidyMath.StrikerPos);
        striker.AddComponent<MatchStrikerSurface>();
        var spl = striker.AddComponent<ProximityLabel>(); spl.SetLabel("Striker", 1.2f);

        GameObject template = null;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == "Template_Raw_Matchsticks") { template = t.gameObject; break; }
        if (template == null) return;
        for (int i = 0; i < 2; i++)
        {
            var match = Instantiate(template, stage);
            match.name = "StagedMatch_" + i;
            match.SetActive(true);
            Seat(match.transform, LayoutTidyMath.MatchPos(i));
            var respawn = match.GetComponent<DropRespawn>() ?? match.AddComponent<DropRespawn>();
            respawn.Bind(match.GetComponent<Rigidbody>(), match.GetComponent<XRGrab>());
            respawn.SetHome(match.transform.position, match.transform.rotation);
        }
    }

    private ExperimentLayout _currentLayout;

    /// Post-pass (W5.8): wire the tool verbs that span a station + a prop + a
    /// vessel — the rod stirs the bindings vessel, the pestle grinds the mortar,
    /// and a Heat station whose required prop is a burner only heats while LIT.
    private void WireVerbControllers(Transform stage, ExperimentLayout layout)
    {
        foreach (var s in layout.stations)
        {
            if (s.sim == StationSim.Heat)
            {
                var prop = FindLayoutProp(layout, s.requiredItemId);
                if (prop == null || (prop.prefabName != "BunsenBurner" && prop.prefabName != "AlcoholBurner")) continue;
                var propGo = FindStageChild(stage, "Prop_" + s.requiredItemId);
                var padGo = FindStageChild(stage, "Station_" + s.taskId);
                if (propGo == null || padGo == null) continue;
                var burner = propGo.GetComponent<BurnerController>() ?? propGo.AddComponent<BurnerController>();
                var rig = padGo.GetComponent<ZoneSimStation>();
                if (rig != null) rig.SetIgnitionGate(() => burner != null && burner.IsLit);
                var status = padGo.GetComponent<StationStatusLabel>();
                if (status != null) status.SetIgnitionHint(() => burner != null && burner.IsLit);
                // The burner base doubles as a match striker.
                if (propGo.GetComponent<MatchStrikerSurface>() == null) propGo.AddComponent<MatchStrikerSurface>();
            }
            else if (s.sim == StationSim.Stir)
            {
                var rodGo = FindStageChild(stage, "Prop_" + s.requiredItemId);
                var vesselLp = FirstBindingsVessel(stage);
                if (rodGo == null || vesselLp == null) continue;
                var stir = vesselLp.GetComponent<StirController>() ?? vesselLp.gameObject.AddComponent<StirController>();
                stir.Bind(runner, s.taskId, vesselLp, rodGo.transform);
            }
            else if (s.sim == StationSim.Grind)
            {
                WireGrind(stage, s.taskId);
            }
        }

        // A staged mortar+pestle with no Grind station still grinds (educational).
        bool hasGrindStation = false;
        foreach (var s in layout.stations) if (s.sim == StationSim.Grind) hasGrindStation = true;
        if (!hasGrindStation) WireGrind(stage, null);
    }

    private void WireGrind(Transform stage, string taskId)
    {
        GameObject mortar = null, pestle = null;
        foreach (Transform t in stage)
        {
            if (t.name.StartsWith("Prop_"))
            {
                var li = t.GetComponent<LabItem>();
                string dn = li != null ? (li.displayName ?? "") : "";
                if (mortar == null && (dn.Contains("Mortar") || t.name.Contains("Motar") || dn.Contains("Motar"))) mortar = t.gameObject;
                if (pestle == null && dn.Contains("Pestle")) pestle = t.gameObject;
            }
        }
        if (mortar == null || pestle == null) return;
        var grind = mortar.GetComponent<GrindController>() ?? mortar.AddComponent<GrindController>();
        grind.Bind(runner, taskId, pestle.transform);
    }

    private static ExperimentLayout.Prop FindLayoutProp(ExperimentLayout layout, string itemId)
    {
        if (string.IsNullOrEmpty(itemId)) return null;
        foreach (var p in layout.props) if (p.itemId == itemId) return p;
        return null;
    }

    private static GameObject FindStageChild(Transform stage, string name)
    {
        foreach (Transform t in stage) if (t.name == name) return t.gameObject;
        return null;
    }

    private LiquidPhysics FirstBindingsVessel(Transform stage)
    {
        foreach (Transform t in stage)
        {
            if (!t.name.StartsWith("Vessel_")) continue;
            var bind = t.GetComponent<LiquidTaskBinding>();
            if (bind != null && bind.ExpectedSteps.Count > 0) return t.GetComponent<LiquidPhysics>();
        }
        return null;
    }

    // ---- builders ---------------------------------------------------------

    private void BuildStation(Transform stage, ExperimentLayout.Station s)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Station_" + s.taskId;
        pad.transform.SetParent(stage, false);
        pad.transform.position = new Vector3(s.pos.x, s.pos.y + 0.006f, s.pos.z);
        pad.transform.localScale = new Vector3(0.3f, 0.012f, 0.3f);
        var box = pad.GetComponent<BoxCollider>();
        box.isTrigger = true; box.size = new Vector3(1.2f, 40f, 1.2f); box.center = new Vector3(0f, 20f, 0f);
        // W5.8 (user): the visible pads cluttered the table — hide the cosmetic
        // cube but keep its collider column + every sensor/sim living on it.
        // The socket ghost + billboard label remain the "place here" cues.
        var padMr = pad.GetComponent<MeshRenderer>();
        if (padMr != null) padMr.enabled = false;

        // Zone sims run a sustained chemistry sim; verb stations (Stir/Grind/
        // Weigh, W5.8) complete through their own tool controllers. Only plain
        // stations still complete on zone-touch.
        bool zoneSim = s.sim == StationSim.Heat || s.sim == StationSim.Crystallise
                    || s.sim == StationSim.Filter || s.sim == StationSim.Collect;
        var st = pad.AddComponent<ExperimentTaskStation>();
        st.Configure(runner, s.taskId, s.requiredItemId, s.sim == StationSim.None, false);
        if (s.sim == StationSim.Weigh) BuildWeighStation(stage, s);

        TemperatureSim temp = null; CrystallizationController cryst = null;
        FiltrationController filt = null; GasCollection gas = null;
        if (zoneSim)
        {
            var sensor = pad.AddComponent<ZoneItemSensor>();
            sensor.SetItemId(s.requiredItemId);
            switch (s.sim)
            {
                case StationSim.Heat:       temp  = pad.AddComponent<TemperatureSim>(); break;
                case StationSim.Crystallise: cryst = pad.AddComponent<CrystallizationController>(); break;
                case StationSim.Filter:     filt  = pad.AddComponent<FiltrationController>(); break;
                case StationSim.Collect:    gas   = pad.AddComponent<GasCollection>(); break;
            }
            var rig = pad.AddComponent<ZoneSimStation>();
            rig.Bind(runner, s.taskId, s.sim, sensor, temp, cryst, filt, gas, s.simTargetC);
            var loop = pad.AddComponent<SimLoopAudio>();
            loop.Bind(SimLoopAudio.KeyFor(s.sim));
            rig.SetLoopAudio(loop);
            var vfx = pad.AddComponent<StationVfx>();   // steam/frost/drip/bubbles while occupied
            vfx.Bind(s.sim);
            rig.SetVfx(vfx);

            // Overheat consequence (error-effects pass): smoke + ruined batch +
            // alarm + Overheat mistake when the sim crosses its threshold.
            if (s.sim == StationSim.Heat && temp != null)
            {
                var overheat = pad.AddComponent<OverheatEffects>();
                overheat.Bind(temp, runner, assets != null ? assets.GetChemical("Ruined Mixture") : null);
            }

            // Hot-surface hazard (§1): touching a HEAT station once it is
            // actually hot records a handling mistake. Player-only (props
            // placed on the pad never trigger it) and armed above 50 °C.
            if (s.sim == StationSim.Heat && temp != null)
            {
                var hot = new GameObject("HotSurface_" + s.taskId);
                hot.transform.SetParent(stage, false);
                hot.transform.position = new Vector3(s.pos.x, s.pos.y + 0.06f, s.pos.z);
                var hotCol = hot.AddComponent<SphereCollider>();
                hotCol.isTrigger = true; hotCol.radius = 0.16f;
                var hz = hot.AddComponent<HazardZone>();
                hz.Configure(runner, LabErrorType.HazardousAction, "Hot surface — don't touch heated apparatus!");
                var tempRef = temp;
                hz.SetArmedCheck(() => tempRef != null && tempRef.AtLeast(50f));
                var cam = Camera.main;
                if (cam != null) hz.SetPlayerRoot(cam.transform.root);
            }
        }

        // Teleport anchor: a floor pad in front of the station so thumbstick
        // teleporters land at each workstation (§2 — only the room-wide floor
        // area existed). Mirrors the existing area's layers/provider.
        var anchorGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        anchorGo.name = "TeleAnchor_" + s.taskId;
        anchorGo.transform.SetParent(stage, false);
        Vector3 toward = new Vector3(0.2f, 0f, -2.5f) - new Vector3(s.pos.x, 0f, s.pos.z);
        toward.y = 0f;
        toward = toward.sqrMagnitude > 0.01f ? toward.normalized : Vector3.right;
        anchorGo.transform.position = new Vector3(s.pos.x, 0.01f, s.pos.z) + toward * 0.75f;
        anchorGo.transform.localScale = new Vector3(0.5f, 0.008f, 0.5f);
        var anchor = anchorGo.AddComponent<TeleAnchor>();
        anchor.teleportAnchorTransform = anchorGo.transform;
        var floorArea = FindAnyObjectByType<TeleArea>();
        if (floorArea != null)
        {
            anchor.interactionLayers = floorArea.interactionLayers;
            anchor.teleportationProvider = floorArea.teleportationProvider;
        }

        // Snap socket: releasing the required prop near the pad clicks it into
        // place (parented to the stage — the pad's non-uniform scale would
        // distort a child). Wrong items are rejected by the filter.
        var sockGo = new GameObject("Socket_" + s.taskId);
        sockGo.transform.SetParent(stage, false);
        sockGo.transform.position = new Vector3(s.pos.x, s.pos.y + 0.02f, s.pos.z);
        var sockCol = sockGo.AddComponent<SphereCollider>();
        sockCol.isTrigger = true; sockCol.radius = 0.10f;
        var filter = sockGo.AddComponent<StationSocketFilter>();
        filter.requiredItemId = s.requiredItemId;
        var sock = sockGo.AddComponent<XRSocket>();
        sock.selectFilters.Add(filter);
        sock.attachTransform = sockGo.transform;
        // Ghost preview: bringing the correct item near shows where it snaps.
        sock.showInteractableHoverMeshes = true;
        sock.interactableHoverMeshMaterial = SocketGhostMaterial();
        sockGo.AddComponent<SelectSfx>().Bind(sock, "socket-snap");

        var labelTmp = MakeLabel(s.label, new Vector3(s.pos.x, s.pos.y + 0.32f, s.pos.z), 0.13f);
        // Live status on the billboard (W5.8): Heat shows the temperature climb,
        // the other sims show percent progress — the player can SEE the verb work.
        if (zoneSim && labelTmp != null)
            pad.AddComponent<StationStatusLabel>().Bind(labelTmp, s.label, s.sim, temp, cryst, filt, gas, s.simTargetC);
    }

    /// A functional balance fixture at a Weigh station (W5.8): the Balance model
    /// (fixed, not grabbable), a live grams display, and the WeighStation whose
    /// TaskGraph condition completes the weigh task. The required measure comes
    /// from the layout's vessel binding for the same task (chemical mode); a
    /// station with no such binding falls back to its requiredItemId (tool mode).
    private void BuildWeighStation(Transform stage, ExperimentLayout.Station s)
    {
        GameObject balance;
        var prefab = assets != null ? assets.GetPrefab("Balance") : null;
        if (prefab != null)
        {
            balance = Instantiate(prefab, stage);
            Normalise(balance, "Balance", 0.18f);
        }
        else
        {
            balance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            balance.transform.SetParent(stage, false);
            balance.transform.localScale = new Vector3(0.22f, 0.05f, 0.18f);
        }
        balance.name = "Weigh_" + s.taskId;
        Seat(balance.transform, s.pos);
        // Fixed fixture: collider yes, grab/physics no (the pan must stay put).
        var grab = balance.GetComponent<XRGrab>();
        if (grab != null) Kill(grab);
        var brb = balance.GetComponent<Rigidbody>();
        if (brb != null) Kill(brb);
        if (balance.GetComponentInChildren<Collider>() == null)
        {
            var bc = balance.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.22f, 0.06f, 0.18f);
        }

        // Pan trigger just above the balance top.
        var b = WB(balance);
        var pan = new GameObject("Pan_" + s.taskId);
        pan.transform.SetParent(balance.transform, true);
        pan.transform.position = new Vector3(b.center.x, b.max.y + 0.05f, b.center.z);
        var panCol = pan.AddComponent<BoxCollider>();
        panCol.isTrigger = true;
        panCol.size = new Vector3(0.24f, 0.16f, 0.2f);

        // Grams display above the pan.
        var display = MakeLabel("0.00 g", new Vector3(b.center.x, b.max.y + 0.22f, b.center.z), 0.1f);
        var scale = balance.AddComponent<WeighingScaleController>();
        if (display != null) scale.Bind(display);

        // The required measure: the vessel binding that feeds this task.
        string chem = null; float ml = 0f;
        if (_currentLayout != null)
            foreach (var v in _currentLayout.vessels)
                foreach (var bind in v.bindings)
                    if (bind.taskId == s.taskId) { chem = bind.reagentChemical; ml = bind.requiredMl; }

        var ws = pan.AddComponent<WeighStation>();
        ws.Bind(runner, s.taskId, s.requiredItemId, chem, ml, scale);
    }

    private static Material _socketGhostMat;
    /// Shared translucent-cyan material for socket hover-mesh previews (built once).
    private static Material SocketGhostMaterial()
    {
        if (_socketGhostMat != null) return _socketGhostMat;
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        var m = new Material(sh) { name = "SocketGhost" };
        var c = new Color(0.5f, 0.9f, 1f, 0.3f);
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        m.SetOverrideTag("RenderType", "Transparent");
        if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
        if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        _socketGhostMat = m;
        return m;
    }

    private void BuildProp(Transform stage, ExperimentLayout.Prop p)
    {
        var prefab = assets != null ? assets.GetPrefab(p.prefabName) : null;
        if (prefab == null) { Debug.LogWarning("[SceneBuilder] missing prefab " + p.prefabName); return; }
        var inst = Instantiate(prefab, stage);
        inst.name = "Prop_" + p.itemId;
        Normalise(inst, p.prefabName, p.targetHeight);
        RestPoseFor(inst, p.prefabName);
        Seat(inst.transform, p.pos);
        var item = inst.GetComponent<LabItem>() ?? inst.AddComponent<LabItem>();
        item.itemId = p.itemId; item.displayName = p.displayName;
        var rb = PhysicsProfiles.EnsurePhysics(inst, p.prefabName);
        inst.AddComponent<GrabPhysicsPolicy>();
        GrabTuning.Apply(inst.GetComponent<XRGrab>());   // held items collide with the world
        inst.AddComponent<HoverHighlight>().Bind(inst.GetComponent<XRGrab>());   // hover affordance
        // Scooping tools transfer solid reagents by the scoopful (W5.12).
        if (p.prefabName == "Scoopula" || p.prefabName == "Spatula")
            inst.AddComponent<ScoopController>().Bind(inst.GetComponent<XRGrab>());
        var respawn = inst.AddComponent<DropRespawn>();
        respawn.SetHome(inst.transform.position, inst.transform.rotation);
        if (Mishandling.IsBreakable(p.prefabName))
        {
            var breakable = inst.AddComponent<BreakableGlassware>();
            breakable.Bind(runner, respawn, rb, p.displayName);
            inst.AddComponent<ImpactSound>().Bind(rb, Mishandling.DropSoundKey(p.prefabName), Mishandling.DefaultBreakSpeed);
        }
        else
        {
            inst.AddComponent<ImpactSound>().Bind(rb, Mishandling.DropSoundKey(p.prefabName));
        }
        if (p.pourable)
        {
            var lp = inst.GetComponent<LiquidPhysics>() ?? inst.AddComponent<LiquidPhysics>();
            lp.registry = registry;
            var chem = assets.GetChemical(p.fillChemical);
            if (chem != null)
                lp.SetContents(chem, SupplyFor(p, chem));   // finite: ~need + 2 spare pours
            EnsureLiquidVisual(inst, lp);                    // visible fill (W5.8)
            var pourer = inst.GetComponent<LiquidPourer>() ?? inst.AddComponent<LiquidPourer>();
            pourer.Bind(lp);
            if (pourer.spout == null)
            {
                // Mouth at the TOP of the item's real bounds — the old guessed
                // (0, 0.12, 0) sat mid-body on tall bottles and above short vials.
                var b = WB(inst);
                var spout = new GameObject("Spout").transform;
                spout.SetParent(inst.transform, true);
                spout.position = new Vector3(b.center.x, b.max.y - 0.005f, b.center.z);
                pourer.spout = spout;
            }
            var spill = inst.AddComponent<SpillMistake>();
            spill.Bind(runner, lp, inst.GetComponent<XRGrab>(), p.displayName);
            inst.AddComponent<HazardousMixReactor>().Bind(lp, runner);   // bad-mix consequences
        }
        var pl = inst.AddComponent<ProximityLabel>(); pl.SetLabel(p.displayName, 1.6f);
        if (p.pourable)
        {
            var plp = inst.GetComponent<LiquidPhysics>();
            if (plp != null)
            {
                inst.AddComponent<VesselStatus>().Bind(plp, pl, p.displayName, 1.6f);   // live supply tag (W5.8)
                inst.AddComponent<MixFeedback>().Bind(plp);
            }
        }
    }

    /// Empty ChemLab prefabs have no liquid child mesh — their `_WithLiquid`
    /// twin carries the authored PharmaLiquid setup (fill bounds, precipitate
    /// renderer). Receiving vessels swap to the twin so poured liquid RENDERS
    /// (W5.8: "pouring into a beaker showed nothing").
    private GameObject VesselPrefabFor(string prefabName)
    {
        if (assets == null) return null;
        if (!prefabName.EndsWith("_WithLiquid"))
        {
            var twin = assets.GetPrefab(prefabName + "_WithLiquid");
            if (twin != null) return twin;
        }
        return assets.GetPrefab(prefabName);
    }

    /// Vessels of one rackGroup, collected during BuildVessel.
    private readonly Dictionary<string, List<LiquidTaskBinding>> _rackBindings
        = new Dictionary<string, List<LiquidTaskBinding>>();

    /// One RackTaskGroup per (rackGroup, shared task): the step completes only
    /// once EVERY member tube that names the task has had its reagent. Members
    /// that don't name the task — Exp 2's negative control — are not counted, so
    /// leaving the control alone is correct play, and pouring into it is a
    /// wrong-reagent mistake. (2026-07-16)
    private void WireRackGroups(Transform stage, ExperimentLayout layout)
    {
        foreach (var kv in _rackBindings)
        {
            // Tasks these tubes share, in authoring order.
            var tasks = new List<string>();
            foreach (var v in layout.vessels)
            {
                if (v.rackGroup != kv.Key) continue;
                foreach (var b in v.bindings)
                    if (!b.completesTask && !tasks.Contains(b.taskId)) tasks.Add(b.taskId);
            }
            foreach (var taskId in tasks)
            {
                // Only the tubes that actually name this task are members.
                var members = new List<LiquidTaskBinding>();
                int i = 0;
                foreach (var v in layout.vessels)
                {
                    if (v.rackGroup != kv.Key) continue;
                    bool names = false;
                    foreach (var b in v.bindings) if (b.taskId == taskId) names = true;
                    if (names && i < kv.Value.Count) members.Add(kv.Value[i]);
                    i++;
                }
                if (members.Count == 0) continue;
                var go = new GameObject("Rack_" + kv.Key + "_" + taskId);
                go.transform.SetParent(stage, false);
                go.transform.position = members[0].transform.position;
                go.AddComponent<RackTaskGroup>().Bind(runner, taskId, members);
            }
        }
    }

    private void BuildVessel(Transform stage, ExperimentLayout.Vessel v)
    {
        // BENCH-BOUND vessel: the object already exists and is placed by hand, so
        // adopt it instead of spawning a twin beside it (the hard client rule keeps
        // every tool permanently out). Only the task wiring is attached; its
        // transform, physics and grab setup are the scene's, not ours.
        GameObject inst;
        if (!string.IsNullOrEmpty(v.benchItem))
        {
            inst = FindBenchItem(v.benchItem);
            if (inst == null)
            {
                Debug.LogWarning("[SceneBuilder] layout wants bench item '" + v.benchItem
                                 + "' but it is not in the scene — step cannot complete.");
                return;
            }
        }
        else
        {
            var prefab = VesselPrefabFor(v.prefabName);
            if (prefab == null) { Debug.LogWarning("[SceneBuilder] missing vessel prefab " + v.prefabName); return; }
            inst = Instantiate(prefab, stage);
            inst.name = "Vessel_" + v.prefabName;   // keep the AUTHORED name (RealSizes/Mishandling lookups)
            Normalise(inst, v.prefabName, v.targetHeight);
            Seat(inst.transform, v.pos);
            PhysicsProfiles.EnsurePhysics(inst, v.prefabName);   // vessels stay kinematic (no release policy)
            GrabTuning.Apply(inst.GetComponent<XRGrab>());       // collide while held; re-freezes on release
        }
        var lp = inst.GetComponent<LiquidPhysics>() ?? inst.AddComponent<LiquidPhysics>();
        lp.registry = registry;
        // Bench glass keeps LiquidPhysics' serialized default of 1000 ml — a "test
        // tube" the size of a bucket. Fill renders as volume/maxVolume, so 5 counted
        // drops was 0.5%: invisible, and the playtest read it as "no drop landed"
        // (2026-07-17). Adopting a bench vessel gives it its REAL capacity.
        if (!string.IsNullOrEmpty(v.benchItem))
            lp.maxVolume = BenchMaxVolumeFor(v.prefabName, lp.maxVolume);
        // ALWAYS set contents explicitly: the _WithLiquid twin serializes a
        // phantom half-fill, and a blank start must arm the wake-from-empty
        // branch (chem null + 0 ml) so the first pour adopts its chemical.
        var startChem = !string.IsNullOrEmpty(v.startChemical) ? assets.GetChemical(v.startChemical) : null;
        lp.SetContents(startChem, startChem != null ? lp.maxVolume * 0.3f : 0f);
        EnsureLiquidVisual(inst, lp);
        var bind = inst.AddComponent<LiquidTaskBinding>();
        bind.SetVesselAndRunner(lp, runner);
        // The hood ref was NEVER wired (2026-07-18) — every requiresFumeHood
        // pour graded a violation no matter where the player stood. The check
        // is now position-based: sanctioned when THIS vessel is inside the hood.
        bind.SetFumeHood(FindAnyObjectByType<FumeHoodZone>(FindObjectsInactive.Include));
        foreach (var b in v.bindings)
        {
            var reagent = assets.GetChemical(b.reagentChemical);
            if (reagent != null) bind.AddExpected(reagent, b.taskId, b.requiredMl, b.completesTask);
        }
        if (!string.IsNullOrEmpty(v.rackGroup))
        {
            if (!_rackBindings.TryGetValue(v.rackGroup, out var list))
                _rackBindings[v.rackGroup] = list = new List<LiquidTaskBinding>();
            list.Add(bind);
        }
        // ZONE-FREE heat step (2026-07-17): the vessel's deferred task completes
        // when it is served AND heated to heatToC — wherever the player does it.
        // Replaces the fixed Heat station (pad/label/teleport anchor all gone).
        // An EXPLICIT heatTaskId (Exp 6's heat-glow) wins over the inferred
        // first-deferred binding — for heat steps with no reagents of their own,
        // where the binding gate is skipped (null binding = vacuously served).
        if (v.heatToC > 0f)
        {
            string heatTask = v.heatTaskId;
            if (string.IsNullOrEmpty(heatTask))
                foreach (var b in v.bindings)
                    if (!b.completesTask && !string.IsNullOrEmpty(b.taskId)) { heatTask = b.taskId; break; }
            if (!string.IsNullOrEmpty(heatTask))
            {
                bool heatHasSteps = false;
                foreach (var b in v.bindings) if (b.taskId == heatTask) heatHasSteps = true;
                (inst.GetComponent<VesselHeatTask>() ?? inst.AddComponent<VesselHeatTask>())
                    .Bind(runner, heatTask, v.heatToC, heatHasSteps ? bind : null, lp);
            }
            else
                Debug.LogWarning("[SceneBuilder] " + v.benchItem + " sets heatToC but has no deferred (completesTask:false) binding to own.");
        }
        // WEIGH step on the bench balance (Exp 6): served AND settled on the pan.
        if (!string.IsNullOrEmpty(v.weighTaskId))
        {
            bool weighHasSteps = false;
            foreach (var b in v.bindings) if (b.taskId == v.weighTaskId) weighHasSteps = true;
            (inst.GetComponent<VesselWeighTask>() ?? inst.AddComponent<VesselWeighTask>())
                .Bind(runner, v.weighTaskId, lp, weighHasSteps ? bind : null);
        }
        // VAPOR collection source (Exp 6): the hot tube condenses the module's
        // product into the nearest vessel whose binding expects it.
        if (!string.IsNullOrEmpty(v.vaporTaskId))
        {
            var productChem = assets.GetChemical(DemoMode.ProductFor(moduleBeingBuilt));
            (inst.GetComponent<VaporCollectController>() ?? inst.AddComponent<VaporCollectController>())
                .Bind(runner, lp, v.vaporTaskId, productChem, Mathf.Max(1f, v.heatToC));
        }
        // ZONE-FREE chill step (Exp 4): completes when the vessel holds product
        // AND has been cooled to chillToC — the ice bucket, anywhere in the lab.
        // If the chill task has its own reagent steps (Exp 5's 20 ml ice-cold
        // water), the binding gates completion too: served AND cold.
        if (v.chillToC > 0f && !string.IsNullOrEmpty(v.chillTaskId))
        {
            bool chillHasSteps = false;
            foreach (var b in v.bindings) if (b.taskId == v.chillTaskId) chillHasSteps = true;
            (inst.GetComponent<VesselChillTask>() ?? inst.AddComponent<VesselChillTask>())
                .Bind(runner, v.chillTaskId, v.chillToC, lp, chillHasSteps ? bind : null);
        }
        // LITMUS confirmation (Exp 4): a strip touching this vessel while the
        // mixture reads acid completes the task — zone-free, no station.
        if (!string.IsNullOrEmpty(v.litmusTaskId))
            (inst.GetComponent<VesselLitmusTask>() ?? inst.AddComponent<VesselLitmusTask>())
                .Bind(runner, v.litmusTaskId, bind, lp);
        // FLAME confirmation (Exp 7): a lit match/burner flame held to the served
        // sample completes the task — the NEGATIVE ("won't ignite") is the result.
        if (!string.IsNullOrEmpty(v.flameTaskId))
        {
            bool flameHasSteps = false;
            foreach (var b in v.bindings) if (b.taskId == v.flameTaskId) flameHasSteps = true;
            (inst.GetComponent<VesselFlameTask>() ?? inst.AddComponent<VesselFlameTask>())
                .Bind(runner, v.flameTaskId, flameHasSteps ? bind : null, lp);
        }
        // FERMENTATION flask (Exp 3): evolves CO₂ into nearby limewater vessels.
        if (!string.IsNullOrEmpty(v.fermentTaskId))
        {
            var co2 = assets.GetChemical("Carbon Dioxide");
            var lime = assets.GetChemical("Limewater");
            (inst.GetComponent<FermentationController>() ?? inst.AddComponent<FermentationController>())
                .Bind(runner, lp, v.fermentTaskId, co2, lime);
        }
        // GetComponent-or-Add: a SPAWNED vessel is fresh, but a BENCH item survives
        // every rebuild — blind AddComponent would stack a new copy of each of these
        // on it per module load.
        (inst.GetComponent<HazardousMixReactor>() ?? inst.AddComponent<HazardousMixReactor>()).Bind(lp, runner);
        // A BENCH item is shared apparatus, so its floating label must stay NEUTRAL
        // ("Test Tube 3"), never the layout's internal role name ("TollensTube_1") —
        // that read as "this tube is only for Tollens" and would go stale the moment
        // another module adopts the same tube (user 2026-07-17).
        string labelName = !string.IsNullOrEmpty(v.benchItem)
            ? BenchDisplayNameFor(v.benchItem) : v.displayName;
        var pl = inst.GetComponent<ProximityLabel>() ?? inst.AddComponent<ProximityLabel>();
        pl.SetLabel(labelName, 1.6f);
        (inst.GetComponent<VesselStatus>() ?? inst.AddComponent<VesselStatus>()).Bind(lp, pl, labelName, 1.6f);
        (inst.GetComponent<MixFeedback>() ?? inst.AddComponent<MixFeedback>()).Bind(lp);
        // POUR-OUT ability (2026-07-18): a task vessel must also POUR — "tilt the
        // beaker through the funnel" and every draw-from-your-own-product step
        // were unplayable by hand because only shelf bottles carried a
        // LiquidPourer (the sim's direct PourOut calls masked it). WireBottle is
        // idempotent: pourer + spout + spill grading, only where missing.
        ShelfPourWiring.WireBottle(inst, runner, registry);
    }

    /// Real capacity for an adopted bench vessel, keyed off the layout's prefab kind.
    /// Pure so the suite pins the table. A 25 ml tube makes 5 drops a visible 20%
    /// fill and puts Exp 2's "to the 10 ml mark" pour at a readable 40%.
    public static float BenchMaxVolumeFor(string prefabName, float current)
    {
        if (string.IsNullOrEmpty(prefabName)) return current;
        if (prefabName.Contains("TestTube")) return 25f;
        if (prefabName.Contains("Beaker_100")) return 100f;
        if (prefabName.Contains("Beaker_500")) return 500f;
        if (prefabName.Contains("GraduatedCylinder_50")) return 50f;
        if (prefabName.Contains("ErlenmeyerFlask")) return 400f;
        if (prefabName.Contains("FlorenceFlask")) return 250f;      // Exp 3 fermentation
        if (prefabName.Contains("DistillingFlask")) return 250f;    // Exp 3 distillation
        if (prefabName.Contains("WatchGlass")) return 20f;          // combustion test drops
        return current;
    }

    /// Neutral player-facing label for a bench item, derived from its scene name —
    /// "Kit_TestTube_3" → "Test Tube 3", "Eq_Beaker_100mL" → "Beaker 100 mL". Pure
    /// so the suite pins it: shared apparatus must never carry a reagent-role name.
    public static string BenchDisplayNameFor(string benchItem)
    {
        if (string.IsNullOrEmpty(benchItem)) return benchItem;
        string n = benchItem;
        foreach (var p in new[] { "Kit_", "Eq_", "Raw_" })
            if (n.StartsWith(p)) { n = n.Substring(p.Length); break; }
        return n.Replace("Hard-GlassTestTube", "Hard-Glass Test Tube")
                .Replace("TestTube", "Test Tube")
                .Replace("GraduatedCylinder", "Graduated Cylinder")
                .Replace("mL", " mL")
                .Replace('_', ' ')
                .Replace("  ", " ")
                .Trim();
    }

    /// A permanent bench object by name (Kit_TestTube_3, Eq_Beaker_100mL…). Searched
    /// scene-wide because the bench lives OUTSIDE the stage — that is the whole point.
    private GameObject FindBenchItem(string name)
    {
        foreach (var li in FindObjectsByType<LabItem>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (li != null && li.name == name) return li.gameObject;
        foreach (var t in FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (t.name == name) return t.gameObject;
        return null;
    }

    /// Strip the task wiring off every bench item before a build.
    ///
    /// Build() clears only the STAGE's children, so bindings attached to bench objects
    /// (which live outside it) would survive into the next module and silently complete
    /// its steps with the previous experiment's reagents. Bench-bound vessels only work
    /// if their wiring is torn down first. (2026-07-16)
    private void ClearBenchBindings()
    {
        foreach (var b in FindObjectsByType<LiquidTaskBinding>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (b == null || (_stage != null && b.transform.IsChildOf(_stage))) continue;   // stage ones die with the stage
            b.Detach();   // DestroyImmediate skips OnDisable in edit mode → ghost subscriptions
            Kill(b);
        }
        foreach (var g in FindObjectsByType<RackTaskGroup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (g == null || (_stage != null && g.transform.IsChildOf(_stage))) continue;
            Kill(g);
        }
        foreach (var h in FindObjectsByType<VesselHeatTask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (h == null || (_stage != null && h.transform.IsChildOf(_stage))) continue;
            h.Detach();
            Kill(h);
        }
        foreach (var fc in FindObjectsByType<FermentationController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (fc == null || (_stage != null && fc.transform.IsChildOf(_stage))) continue;
            fc.Detach();
            Kill(fc);
        }
        foreach (var ct in FindObjectsByType<VesselChillTask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ct == null || (_stage != null && ct.transform.IsChildOf(_stage))) continue;
            ct.Detach();
            Kill(ct);
        }
        foreach (var lt in FindObjectsByType<VesselLitmusTask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lt == null || (_stage != null && lt.transform.IsChildOf(_stage))) continue;
            lt.Detach();
            Kill(lt);
        }
        foreach (var wt in FindObjectsByType<VesselWeighTask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (wt == null || (_stage != null && wt.transform.IsChildOf(_stage))) continue;
            wt.Detach();
            Kill(wt);
        }
        foreach (var vc in FindObjectsByType<VaporCollectController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (vc == null || (_stage != null && vc.transform.IsChildOf(_stage))) continue;
            vc.Detach();
            Kill(vc);
        }
        foreach (var ft in FindObjectsByType<VesselFlameTask>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (ft == null || (_stage != null && ft.transform.IsChildOf(_stage))) continue;
            ft.Detach();
            Kill(ft);
        }
    }

    /// Guarantee the vessel/prop can RENDER its liquid: if LiquidPhysics has no
    /// mainRenderer (or it points at the glass shell, which lacks the PharmaLiquid
    /// fill properties), build an inset "Liquid" child running the liquid shader.
    /// sharedMaterial only — edit-mode safe (the suite drives the builder).
    public static void EnsureLiquidVisual(GameObject inst, LiquidPhysics lp)
    {
        if (inst == null || lp == null) return;

        // Solids/powders (sodium acetate, soda lime, salicylic acid…) must NOT
        // read as a sloshing liquid vial (user 2026-07-13: "it's misleading… the
        // vial and its content is for liquid"). Render an opaque granular mound in
        // the lower part of the jar instead of the translucent liquid shader.
        var chem = lp.currentChemical;
        if (chem != null && (chem.state == PhysicalState.Solid || chem.state == PhysicalState.Powder))
        {
            EnsurePowderVisual(inst, chem);
            return;   // mainRenderer stays whatever it was; the Start guard in
                      // LiquidPhysics keeps it off the opaque vessel mesh.
        }

        bool authoredFill = lp.mainRenderer != null && lp.mainRenderer.sharedMaterial != null
            && lp.mainRenderer.sharedMaterial.HasProperty("_Fill");   // authored setup (e.g. _WithLiquid twin)
        if (!authoredFill)
        {
            // Reuse an existing child named "Liquid" when present but unwired.
            Renderer liquidR = null;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Liquid" && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Fill")) { liquidR = r; break; }

            if (liquidR == null)
                liquidR = BuildFillChild(inst, "Liquid", 0.72f);
            lp.mainRenderer = liquidR;
        }

        // PRECIPITATE layer (2026-07-18): bench-adopted glass had NO
        // precipitateRenderer — only the authored _WithLiquid twins carry one —
        // so Exp 4's MnO2 sludge, white benzoic crystals and buff ferric
        // benzoate fired their observation text with NOTHING visible in the
        // vessel. Same inset-cylinder trick, slightly wider so the settled
        // layer reads at the bottom through the liquid.
        if (lp.precipitateRenderer == null)
        {
            Renderer pptR = null;
            foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
                if (r.name == "Precipitate" && r.sharedMaterial != null && r.sharedMaterial.HasProperty("_Fill")) { pptR = r; break; }
            if (pptR == null) pptR = BuildFillChild(inst, "Precipitate", 0.76f);
            if (pptR != null) pptR.enabled = false;   // LiquidPhysics enables it while ppt volume > 1 ml
            lp.precipitateRenderer = pptR;
        }
    }

    /// Inset PharmaLiquid cylinder fitted to the vessel's glass bounds — the
    /// runtime fill surface for adopted bench glass (Liquid + Precipitate layers).
    private static Renderer BuildFillChild(GameObject inst, string childName, float widthFrac)
    {
        var shader = Shader.Find("PharmaSynth/Liquid");
        if (shader == null) return null;   // shader stripped — leave numeric-only rather than magenta
        var b = WB(inst);
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = childName;
        var col = go.GetComponent<Collider>();
        if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
        go.transform.SetParent(inst.transform, true);
        // Inset cylinder, floor to just under the rim.
        float w = Mathf.Min(b.size.x, b.size.z) * widthFrac;
        float h = Mathf.Max(0.01f, b.size.y * 0.86f);
        go.transform.position = new Vector3(b.center.x, b.min.y + h * 0.5f + b.size.y * 0.04f, b.center.z);
        go.transform.rotation = inst.transform.rotation;
        var ls = inst.transform.lossyScale;
        go.transform.localScale = new Vector3(
            w / Mathf.Max(1e-4f, Mathf.Abs(ls.x)),
            h * 0.5f / Mathf.Max(1e-4f, Mathf.Abs(ls.y)),   // cylinder mesh is 2 units tall
            w / Mathf.Max(1e-4f, Mathf.Abs(ls.z)));
        var r2 = go.GetComponent<Renderer>();
        r2.sharedMaterial = new Material(shader) { name = "PharmaLiquid_Runtime" };
        r2.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return r2;
    }

    // ---- vessel fitting (local space) ---------------------------------------
    // Contents must be placed in the vessel's LOCAL frame, never world axes — a
    // tilted/held tube would otherwise get a world-vertical fill sticking out of
    // it (user 2026-07-15: "the content isn't aligned/contained").

    /// Local-space bounds of a vessel's solid meshes, skipping fill/label children.
    public static Bounds LocalMeshBounds(Transform root, params string[] skipNames)
    {
        Bounds b = default; bool has = false;
        foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (mf == null || mf.sharedMesh == null) continue;
            if (mf.GetComponent<TMPro.TMP_Text>() != null) continue;
            bool skip = false;
            foreach (var n in skipNames) if (mf.gameObject.name == n) { skip = true; break; }
            if (skip) continue;
            var mb = mf.sharedMesh.bounds;
            for (int i = 0; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? mb.min.x : mb.max.x,
                    (i & 2) == 0 ? mb.min.y : mb.max.y,
                    (i & 4) == 0 ? mb.min.z : mb.max.z);
                var local = root.InverseTransformPoint(mf.transform.TransformPoint(corner));
                if (!has) { b = new Bounds(local, Vector3.zero); has = true; } else b.Encapsulate(local);
            }
        }
        return has ? b : new Bounds(Vector3.zero, Vector3.one * 0.05f);
    }

    /// Index of the longest axis (0=x, 1=y, 2=z) — a vessel's "up" from its base.
    public static int LongestAxis(Vector3 size)
        => (size.y >= size.x && size.y >= size.z) ? 1 : (size.z >= size.x ? 2 : 0);

    /// Rotation mapping a primitive's +Y onto the given local axis.
    public static Quaternion AxisAlign(int axis)
        => axis == 1 ? Quaternion.identity
         : axis == 0 ? Quaternion.Euler(0f, 0f, -90f)
                     : Quaternion.Euler(90f, 0f, 0f);

    /// The two extents perpendicular to `axis` → the vessel's internal bore.
    public static float BoreOf(Vector3 size, int axis)
        => axis == 0 ? Mathf.Min(size.y, size.z)
         : axis == 1 ? Mathf.Min(size.x, size.z)
                     : Mathf.Min(size.x, size.y);

    /// Opaque powder mound for solid/powder reagents: a low, wide, matte heap
    /// sitting in the bottom of the jar so it reads as granular, not liquid.
    /// Idempotent (reuses a child named "Powder"); sharedMaterial only.
    public static void EnsurePowderVisual(GameObject inst, ChemicalData chem, float fill01 = 1f)
    {
        if (inst == null) return;
        fill01 = Mathf.Clamp01(fill01);
        Renderer heap = null;
        foreach (var r in inst.GetComponentsInChildren<Renderer>(true))
        {
            if (r.name == "Powder") heap = r;
            else if (r.name == "Liquid")   // an earlier liquid-fill twin — drop it
            {
                if (Application.isPlaying) Destroy(r.gameObject); else DestroyImmediate(r.gameObject);
            }
        }

        // (The vessel is measured below in its LOCAL frame, always excluding the
        // mound itself — including it once made each re-run wrap the previous,
        // bigger mound and balloon it into a giant blob. — user 2026-07-14.)
        if (heap == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);   // squashed = a mound
            go.name = "Powder";
            var col = go.GetComponent<Collider>();
            if (col != null) { if (Application.isPlaying) Destroy(col); else DestroyImmediate(col); }
            go.transform.SetParent(inst.transform, true);
            heap = go.GetComponent<Renderer>();
            heap.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            heap.sharedMaterial = new Material(shader != null ? shader : Shader.Find("Standard"))
                { name = "PharmaPowder_Runtime" };
        }
        // Empty → hide the mound entirely (a scooped-out source shows no powder).
        if (fill01 <= 0.001f) { heap.gameObject.SetActive(false); return; }
        heap.gameObject.SetActive(true);

        // Fit the mound INSIDE the vessel in its own LOCAL frame, so a tilted or
        // held vessel (e.g. the hard-glass tube) keeps its contents aligned and
        // contained instead of a world-vertical blob poking out (user 2026-07-15).
        var lb = LocalMeshBounds(inst.transform, "Powder", "Liquid", "CollectedGas");
        int ax = LongestAxis(lb.size);
        float bore = BoreOf(lb.size, ax);
        // Size PURELY as a fraction of the vessel's own local bounds — never mix in
        // absolute metres. lb is in LOCAL units, so a metre cap here becomes
        // microscopic on an import-scaled prefab (user 2026-07-15: the powder was
        // invisible inside the glass tube). Fractions stay contained at any scale.
        float w = bore * 0.5f * Mathf.Lerp(0.6f, 1f, fill01);                       // half the bore
        float h = Mathf.Min(lb.size[ax] * 0.22f, bore * 0.6f) * Mathf.Lerp(0.4f, 1f, fill01);
        h = Mathf.Max(lb.size[ax] * 0.03f, h);                                      // always a little visible

        // A hand-placed anchor wins for placement — and, ONLY if it is a size-setting
        // anchor, for size too (user 2026-07-15: "set its size manually").
        // "PowderAnchor" = position + SIZE; "BowlAnchor" (mortar) = position ONLY.
        // A position-only anchor has scale 1, so taking its scale produced a 1-unit
        // beach-ball of powder — never size from an anchor unless it says it sizes.
        var anchor = inst.transform.Find("PowderAnchor") ?? inst.transform.Find("BowlAnchor");
        if (anchor != null)
        {
            var pa = anchor.GetComponent<PlacementAnchor>();
            bool anchorSetsSize = pa != null && pa.previewsScale;
            heap.transform.localPosition = anchor.localPosition;
            heap.transform.localRotation = anchor.localRotation;
            heap.transform.localScale = anchorSetsSize
                ? anchor.localScale * Mathf.Lerp(0.55f, 1f, fill01)   // hand-set size
                : new Vector3(w, h, w);                                // computed fraction of the bore
        }
        else
        {
            // Rest on the vessel floor, centred in the bore, along the vessel's axis.
            Vector3 lp = lb.center;
            lp[ax] = lb.min[ax] + h * 0.5f + lb.size[ax] * 0.05f;
            heap.transform.localPosition = lp;
            heap.transform.localRotation = AxisAlign(ax);
            // Local scale is in the vessel's frame → no lossyScale compensation.
            heap.transform.localScale = new Vector3(w, h, w);
        }
        var mat = heap.sharedMaterial;
        var c = chem != null ? chem.liquidColor : new Color(0.9f, 0.88f, 0.82f); c.a = 1f;
        if (mat != null)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", c);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.08f);   // matte, not glossy
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 0.08f);
        }
    }

    // ---- helpers ----------------------------------------------------------

    /// Finite bottle supply: the authored supplyMl, or 2.5x the summed requiredMl of
    /// every binding this chemical feeds (min 120 ml). A chemical no binding consumes
    /// keeps the legacy 60% fill (display/test reagents).
    private float SupplyFor(ExperimentLayout.Prop p, ChemicalData chem)
    {
        if (p.supplyMl > 0f) return p.supplyMl;
        float needed = 0f;
        var layout = FindLayout(runner != null && runner.Module != null ? runner.Module.moduleId : null);
        if (layout != null)
            foreach (var v in layout.vessels)
                foreach (var b in v.bindings)
                    if (b.reagentChemical == p.fillChemical && b.requiredMl > 0f) needed += b.requiredMl;
        return needed > 0f ? Mathf.Max(120f, needed * 2.5f) : 600f;
    }

    private static Bounds WB(GameObject g)
    {
        var rs = g.GetComponentsInChildren<Renderer>();
        Bounds b = rs.Length > 0 ? rs[0].bounds : new Bounds(g.transform.position, Vector3.one * 0.1f);
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }

    /// World bounds of a prop's SOLID mesh only — skips the given child names
    /// (the powder/liquid fill so it can't inflate its own host on a re-run) AND
    /// any non-mesh renderer: floating TMP name labels, the pour-arc LineRenderer,
    /// particle systems. Those used to balloon the measured jar size, so a tiny
    /// vial grew a beach-ball of powder (user 2026-07-14).
    private static Bounds BoundsExcluding(GameObject g, params string[] skipNames)
    {
        Bounds b = default; bool has = false;
        foreach (var r in g.GetComponentsInChildren<Renderer>(true))
        {
            if (!(r is MeshRenderer || r is SkinnedMeshRenderer)) continue;   // no text/line/particle
            if (r.GetComponent<TMPro.TMP_Text>() != null) continue;           // TMP labels use a MeshRenderer
            bool skip = false;
            foreach (var n in skipNames) if (r.name == n) { skip = true; break; }
            if (skip) continue;
            if (!has) { b = r.bounds; has = true; } else b.Encapsulate(r.bounds);
        }
        return has ? b : new Bounds(g.transform.position, Vector3.one * 0.05f);
    }

    private static void Normalise(GameObject g, float targetHeight)
        => Normalise(g, null, targetHeight);

    /// RealSizes-aware normalisation: known prefabs scale by their realistic
    /// LONGEST dimension (bounds-HEIGHT normalisation inflated flat tools 3-16x);
    /// unknown names keep the legacy height behaviour.
    private static void Normalise(GameObject g, string prefabName, float fallbackHeight)
    {
        g.transform.localScale = Vector3.one;
        var size = WB(g).size;
        if (RealSizes.TryGet(prefabName, out float target))
            g.transform.localScale = Vector3.one * RealSizes.UniformScaleFactor(size, target);
        else
            g.transform.localScale = Vector3.one * (fallbackHeight / Mathf.Max(size.y, 0.01f));
    }

    /// Rotate a spawned item into its plausible resting pose (a spatula lies
    /// flat, a glass rod on its side) BEFORE seating, so bounds-based seating
    /// uses the rotated footprint.
    private static void RestPoseFor(GameObject g, string prefabName)
    {
        if (!PhysicsProfiles.TryGet(prefabName, out var prof)) return;
        g.transform.rotation = PhysicsProfiles.RestRotation(prof.pose, WB(g).size) * g.transform.rotation;
    }

    private static void Seat(Transform t, Vector3 pos)
    {
        float surfaceY = pos.y;
        RaycastHit hit;
        if (Physics.Raycast(new Vector3(pos.x, pos.y + 0.5f, pos.z), Vector3.down, out hit, 1.0f, ~0, QueryTriggerInteraction.Ignore))
            surfaceY = hit.point.y;
        t.position = new Vector3(pos.x, surfaceY + 0.3f, pos.z);
        t.position += Vector3.up * (surfaceY + 0.005f - WB(t.gameObject).min.y);
    }

    private TextMeshPro MakeLabel(string text, Vector3 pos, float scale)
    {
        // Fall back to the stage when the scene's WorldLabels root isn't wired
        // (edit-mode builder tests) — stage teardown cleans those up anyway.
        var parent = labelsRoot != null ? labelsRoot : Stage();
        if (parent == null) return null;
        var go = new GameObject("DynLabel_" + text);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.text = text; tmp.fontSize = 6f; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white; tmp.fontStyle = FontStyles.Bold;
        // outlineWidth instances the font material — play-mode only (the edit-mode
        // suite builds stages too, and .material instancing errors there).
        if (Application.isPlaying)
        {
            tmp.outlineWidth = 0.25f; tmp.outlineColor = new Color32(6, 12, 22, 255);
        }
        go.AddComponent<FaceCamera>();
        return tmp;
    }
}
