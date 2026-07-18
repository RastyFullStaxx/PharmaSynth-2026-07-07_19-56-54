#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// Builds the corner music speaker in the lab (user 2026-07-10): a floor-standing
/// speaker cabinet in the empty back-right corner that plays the Background_Music/Lab
/// playlist as a 3D positional source (louder as you approach) and fades in/out with
/// the screen fade on menu<->lab transitions. Also disables the old 2D LabMusicPlayer
/// bed and re-points the menu-room music to the user's supplied track.
///
/// Tools ▸ PharmaSynth ▸ Build Lab Music Speaker (SampleScene, edit mode, idempotent).
public static class MusicSpeakerBuilder
{
    const string LabDir = "Assets/Background_Music/Lab";
    const string MenuTrack = "Assets/Background_Music/MenuRoom/music_1.mp3";
    const string GroupName = "LabSpeaker";

    [MenuItem("Tools/PharmaSynth/Build Lab Music Speaker")]
    public static void Build()
    {
        if (Application.isPlaying) { Debug.LogWarning("[MusicSpeaker] exit Play mode first."); return; }

        // ---- load the lab playlist ------------------------------------------------
        var clips = LoadClips(LabDir);
        if (clips.Count == 0) Debug.LogWarning("[MusicSpeaker] no clips in " + LabDir + " — speaker will be silent until tracks import.");

        // ---- place in the empty back-right corner, facing room centre -------------
        var old = GameObject.Find(GroupName);
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject(GroupName);
        Undo.RegisterCreatedObjectUndo(root, "Build Lab Music Speaker");
        Vector3 corner = new Vector3(4.3f, 0.2f, -7.3f);          // inset from the floor's max x / min z
        // Rest it on whatever surface is in the corner (the bench top, else the floor).
        if (Physics.Raycast(new Vector3(corner.x, 2.6f, corner.z), Vector3.down, out var floorHit, 3f, ~0, QueryTriggerInteraction.Ignore))
            corner.y = floorHit.point.y;
        root.transform.position = corner;
        Vector3 toCentre = new Vector3(-0.3f, corner.y, -2.5f) - corner; toCentre.y = 0f;
        root.transform.rotation = Quaternion.LookRotation(toCentre.sqrMagnitude > 0.01f ? toCentre.normalized : Vector3.forward, Vector3.up);

        BuildCabinet(root.transform);

        var speaker = root.AddComponent<MusicSpeaker>();
        speaker.Configure(clips.ToArray(), 0.6f, 3.5f, 24f);
        EditorUtility.SetDirty(speaker);

        // ---- retire the old 2D music bed (avoid double music) ---------------------
        var oldBed = GameObject.Find("LabMusicPlayer");
        if (oldBed != null) { oldBed.SetActive(false); EditorUtility.SetDirty(oldBed); }

        // ---- re-point the menu-room music to the user's track ---------------------
        RepointMenuMusic();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("<color=#4CD07D>[MusicSpeaker] speaker built at " + corner.ToString("F1") +
                  " with " + clips.Count + " lab track(s)</color>; old LabMusicPlayer disabled.");
    }

    static List<AudioClip> LoadClips(string dir)
    {
        var list = new List<AudioClip>();
        if (!Directory.Exists(dir)) return list;
        var files = new List<string>(Directory.GetFiles(dir));
        files.Sort();                                            // music_1..5 order
        foreach (var f in files)
        {
            if (f.EndsWith(".meta")) continue;
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(f.Replace('\\', '/'));
            if (clip != null) list.Add(clip);
        }
        return list;
    }

    static void RepointMenuMusic()
    {
        var menu = AssetDatabase.LoadAssetAtPath<AudioClip>(MenuTrack);
        if (menu == null) { Debug.LogWarning("[MusicSpeaker] menu track not found: " + MenuTrack); return; }
        foreach (var g in AssetDatabase.FindAssets("t:SoundBank"))
        {
            var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(AssetDatabase.GUIDToAssetPath(g));
            var e = bank != null ? bank.Get("music-menu") : null;
            if (e != null) { e.clip = menu; EditorUtility.SetDirty(bank); Debug.Log("[MusicSpeaker] music-menu -> " + menu.name); }
            break;
        }
    }

    // ---- procedural speaker cabinet ------------------------------------------------

    static Material _cab, _drv, _led;

    static void BuildCabinet(Transform parent)
    {
        var cabMat = CabinetMat();
        var drvMat = DriverMat();
        var ledMat = LedMat();

        // Bookshelf cabinet resting on the surface (keeps its box collider → solid).
        const float H = 0.48f;                                    // cabinet height
        var cab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cab.name = "Cabinet";
        cab.transform.SetParent(parent, false);
        cab.transform.localPosition = new Vector3(0f, H * 0.5f, 0f);
        cab.transform.localScale = new Vector3(0.30f, H, 0.30f);
        cab.GetComponent<Renderer>().sharedMaterial = cabMat;

        // Drivers on the +Z (front) face — cylinders rotated so their flat caps face out.
        Driver(parent, "Woofer", drvMat, new Vector3(0f, 0.16f, 0.152f), 0.20f);
        Driver(parent, "Tweeter", drvMat, new Vector3(0f, 0.34f, 0.152f), 0.075f);

        // Cyan power LED (emissive) — blinks like a machine's standby light rather
        // than sitting flat-on (user 2026-07-19); driven via MPB off the shared mat.
        var led = GameObject.CreatePrimitive(PrimitiveType.Cube);
        led.name = "LED";
        Object.DestroyImmediate(led.GetComponent<Collider>());
        led.transform.SetParent(parent, false);
        led.transform.localPosition = new Vector3(0.10f, 0.42f, 0.152f);
        led.transform.localScale = new Vector3(0.026f, 0.026f, 0.012f);
        led.GetComponent<Renderer>().sharedMaterial = ledMat;
        led.AddComponent<SpeakerLedBlink>().Bind(new Color(0.3f, 0.85f, 1f), 1.15f, 0.22f, 6f);
    }

    static void Driver(Transform parent, string name, Material mat, Vector3 localPos, float diameter)
    {
        var d = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d.name = name;
        Object.DestroyImmediate(d.GetComponent<Collider>());
        d.transform.SetParent(parent, false);
        d.transform.localPosition = localPos;
        d.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);   // caps face +/-Z
        d.transform.localScale = new Vector3(diameter, 0.02f, diameter);
        d.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static Material CabinetMat()
    {
        if (_cab != null) return _cab;
        _cab = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SpeakerCabinet" };
        _cab.SetColor("_BaseColor", new Color(0.09f, 0.10f, 0.13f));
        if (_cab.HasProperty("_Smoothness")) _cab.SetFloat("_Smoothness", 0.35f);
        return _cab;
    }

    static Material DriverMat()
    {
        if (_drv != null) return _drv;
        _drv = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SpeakerDriver" };
        _drv.SetColor("_BaseColor", new Color(0.03f, 0.03f, 0.04f));
        if (_drv.HasProperty("_Smoothness")) _drv.SetFloat("_Smoothness", 0.15f);
        return _drv;
    }

    static Material LedMat()
    {
        if (_led != null) return _led;
        _led = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = "SpeakerLED" };
        var cyan = new Color(0.3f, 0.85f, 1f);
        _led.SetColor("_BaseColor", cyan);
        _led.EnableKeyword("_EMISSION");
        _led.SetColor("_EmissionColor", cyan * 3f);
        return _led;
    }
}
#endif
