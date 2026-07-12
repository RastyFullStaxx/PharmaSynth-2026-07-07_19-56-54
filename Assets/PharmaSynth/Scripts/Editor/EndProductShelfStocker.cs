#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using XRGrab = UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

/// Stocks the finished experiment PRODUCTS onto the west-wall ReagentShelf so
/// EndProductVisibility can hide them in regular lab mode and reveal them in a
/// demo session (user 2026-07-12: every end-game reagent lives on the shelf,
/// never as a floating vial by the door). Idempotent: products already on the
/// shelf are left exactly where they are (manual placement preserved); only the
/// missing ones are added, greedily into the roomiest free cubby slots.
///
/// Product set = the manuscript's bottleable syntheses (Appendix C §1). Methane
/// is a gas and Chemical Compounding is an identification lab, so neither has a
/// shelf product; Sodium Acetate and Grape Juice are FEEDSTOCKS (not products)
/// and stay always-visible. The Wine Making product is Wine (Grape Juice is only
/// its feedstock), so this also mints Chem_Wine if it doesn't exist yet.
///
/// Run order after this: Generate Reagent Labels → Wire End-Product Gate →
/// Wire Shelf Pourers → Re-Home Scene Items (Adopt Current).
public static class EndProductShelfStocker
{
    const string LibPath = "Assets/PharmaSynth/ScriptableObjects/SceneAssetLibrary.asset";
    const string RegPath = "Assets/PharmaSynth/ScriptableObjects/Reactions/MasterReactionRegistry.asset";
    const string ChemDir = "Assets/PharmaSynth/ScriptableObjects/Chemicals/";

    /// The nine finished products with a bottleable chemical. Matches
    /// DemoMode.IsEndProduct exactly (kept in sync by the self-tests).
    static readonly string[] Products =
    {
        "Ethanol", "Benzoic Acid", "Acetanilide", "Acetone", "Chloroform",
        "Benzamide", "Aspirin", "Caffeine", "Wine",
    };

    /// Vial-centre heights of the four ledges inside the Environment/3x4 cubby
    /// (read off the existing shelf bottles: 0.861 / 1.252 / 1.643 / 2.034).
    static readonly float[] LedgeY = { 0.861f, 1.252f, 1.643f, 2.034f };

    [MenuItem("Tools/PharmaSynth/Stock End-Product Shelf")]
    public static void Stock()
    {
        if (Application.isPlaying) { Debug.LogWarning("[EndProductShelf] exit Play mode first."); return; }
        var lib = AssetDatabase.LoadAssetAtPath<SceneAssetLibrary>(LibPath);
        if (lib == null) { Debug.LogError("[EndProductShelf] SceneAssetLibrary not found."); return; }
        var registry = AssetDatabase.LoadAssetAtPath<ReactionRegistry>(RegPath);
        var runner = Object.FindAnyObjectByType<ExperimentRunner>();

        EnsureWine(lib);

        var shelf = GameObject.Find("ReagentShelf");
        if (shelf == null) { Debug.LogError("[EndProductShelf] ReagentShelf not found."); return; }

        // Scan existing bottles: which products are already here, and every
        // occupied (y,z) so new bottles avoid them (all sit at the same x).
        var present = new HashSet<string>();
        var occupied = new List<Vector2>();
        float sumX = 0f; int nX = 0;
        foreach (var lp in shelf.GetComponentsInChildren<LiquidPhysics>(true))
        {
            occupied.Add(new Vector2(lp.transform.position.y, lp.transform.position.z));
            sumX += lp.transform.position.x; nX++;
            if (lp.currentChemical != null) present.Add(lp.currentChemical.chemicalName);
        }
        float baseX = nX > 0 ? sumX / nX : -5.02f;

        var prefab = lib.GetPrefab("Vial_WithLabel");
        if (prefab == null) { Debug.LogError("[EndProductShelf] Vial_WithLabel prefab missing from library."); return; }

        int placed = 0, skipped = 0;
        foreach (var name in Products)
        {
            if (present.Contains(name)) { skipped++; continue; }
            var chem = lib.GetChemical(name);
            if (chem == null) { Debug.LogWarning("[EndProductShelf] chemical not in library: " + name); continue; }

            Vector2 slot = BestFreeSlot(occupied);
            StockBottle(prefab, chem, new Vector3(baseX, slot.x, slot.y), shelf.transform, registry, runner);
            occupied.Add(slot);
            placed++;
        }

        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[EndProductShelf] {placed} product bottles placed, {skipped} already present " +
                  $"({Products.Length} total). Run Generate Reagent Labels → Wire End-Product Gate → " +
                  "Wire Shelf Pourers → Re-Home Scene Items next.");
    }

    /// The candidate cubby slot with the largest clearance to every occupied
    /// bottle — fills the roomiest gap. Deterministic, so re-runs are stable.
    static Vector2 BestFreeSlot(List<Vector2> occupied)
    {
        Vector2 best = new Vector2(LedgeY[LedgeY.Length - 1], -1.5f);
        float bestClear = -1f;
        foreach (float y in LedgeY)
            for (float z = -2.0f; z <= -0.99f; z += 0.10f)
            {
                var cand = new Vector2(y, z);
                float clear = float.MaxValue;
                for (int i = 0; i < occupied.Count; i++)
                    clear = Mathf.Min(clear, Vector2.Distance(cand, occupied[i]));
                if (clear > bestClear) { bestClear = clear; best = cand; }
            }
        return best;
    }

    /// Instantiate + fully wire one product bottle — the same recipe
    /// ReagentCabinetBuilder.StockRow uses for a shelf bottle.
    static void StockBottle(GameObject prefab, ChemicalData chem, Vector3 pos, Transform parent,
                            ReactionRegistry registry, ExperimentRunner runner)
    {
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = "Reagent_" + chem.chemicalName.Replace(" ", "").Replace("%", "");

        // Normalise the tallest axis to ~0.17 m (matches the shelf/cabinet vials).
        var rends = inst.GetComponentsInChildren<Renderer>(true);
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            if (b.size.y > 0.001f) inst.transform.localScale *= 0.17f / b.size.y;
        }
        inst.transform.position = pos;

        var lp = inst.GetComponent<LiquidPhysics>() ?? inst.AddComponent<LiquidPhysics>();
        lp.registry = registry;
        lp.currentChemical = chem;
        lp.currentLiquidVolume = 120f;
        TintLiquid(inst, chem);

        var rb = PhysicsProfiles.EnsurePhysics(inst, "Vial_WithLabel");
        GrabTuning.Apply(inst.GetComponent<XRGrab>());
        if (inst.GetComponent<GrabPhysicsPolicy>() == null) inst.AddComponent<GrabPhysicsPolicy>();
        var respawn = inst.GetComponent<DropRespawn>() ?? inst.AddComponent<DropRespawn>();
        respawn.SetHome(inst.transform.position, inst.transform.rotation);
        ShelfPourWiring.WireBottle(inst.gameObject, runner, registry);

        if (Mishandling.IsBreakable("Vial_WithLabel"))
        {
            var breakable = inst.GetComponent<BreakableGlassware>() ?? inst.AddComponent<BreakableGlassware>();
            breakable.Bind(runner, respawn, rb, chem.chemicalName);
            if (inst.GetComponent<ImpactSound>() == null)
                inst.AddComponent<ImpactSound>().Bind(rb, Mishandling.DropSoundKey("Vial_WithLabel"), Mishandling.DefaultBreakSpeed);
        }
        else if (inst.GetComponent<ImpactSound>() == null)
            inst.AddComponent<ImpactSound>().Bind(rb, Mishandling.DropSoundKey("Vial_WithLabel"));

        var label = inst.GetComponent<ProximityLabel>() ?? inst.AddComponent<ProximityLabel>();
        label.SetLabel(chem.chemicalName, 1.4f);
    }

    /// Create + register Chem_Wine (the fermented Wine Making product) if it does
    /// not exist. Deep-red burgundy; copies Grape Juice's non-visual fields where
    /// available so enums/flags stay sensible.
    static void EnsureWine(SceneAssetLibrary lib)
    {
        var wine = lib.GetChemical("Wine")
                   ?? AssetDatabase.LoadAssetAtPath<ChemicalData>(ChemDir + "Chem_Wine.asset");
        if (wine == null)
        {
            var juice = lib.GetChemical("Mixed Fruit Juice");   // W5.9: renamed (manuscript excludes grapes)
            wine = ScriptableObject.CreateInstance<ChemicalData>();
            wine.chemicalName = "Wine";
            wine.state = PhysicalState.Liquid;
            wine.liquidColor = new Color(0.36f, 0.04f, 0.10f, 1f);   // deep burgundy
            wine.liquidTopColor = new Color(0.50f, 0.08f, 0.16f, 1f);
            wine.pH = juice != null ? Mathf.Min(juice.pH, 3.5f) : 3.4f;
            wine.boilingPointC = 78f;                                // ethanol-bearing
            wine.viscosity = juice != null ? juice.viscosity : 0.5f;
            wine.hazard = HazardType.Flammable;
            AssetDatabase.CreateAsset(wine, ChemDir + "Chem_Wine.asset");
        }
        if (lib.chemicals != null && !lib.chemicals.Contains(wine))
        {
            lib.chemicals.Add(wine);
            EditorUtility.SetDirty(lib);
        }
        EditorUtility.SetDirty(wine);
        AssetDatabase.SaveAssets();
        Debug.Log("[EndProductShelf] Chem_Wine ensured + registered in the library.");
    }

    /// Bake the fill colour so the bottle reads in the editor and on load (the
    /// LiquidPhysics colour lerp is a play-mode coroutine). Mirrors
    /// ReagentCabinetBuilder.TintLiquid.
    static void TintLiquid(GameObject bottle, ChemicalData chem)
    {
        if (chem == null) return;
        Color c = chem.liquidColor; c.a = 1f;
        foreach (var r in bottle.GetComponentsInChildren<Renderer>(true))
        {
            string n = r.name.ToLowerInvariant();
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            if (n.Contains("liquid"))
            {
                mpb.SetColor("_LiquidColour", c);
                r.SetPropertyBlock(mpb);
            }
            else if (n.Contains("lid") || n.Contains("cap"))
            {
                Color capC = (c.r > 0.88f && c.g > 0.88f && c.b > 0.85f)
                    ? new Color(0.45f, 0.5f, 0.55f) : c;
                mpb.SetColor("_BaseColor", capC);
                mpb.SetColor("_Color", capC);
                r.SetPropertyBlock(mpb);
            }
        }
    }
}
#endif
