#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine;
using UnityEngine.XR.Management;

/// One-click switch for editor Play mode's XR init on the Standalone (PC) target
/// — the setting buried under Project Settings ▸ XR Plug-in Management ▸ (per
/// target) "Initialize XR on Startup".
///   • ON  → pressing Play brings up OpenXR, so a Quest connected via Quest Link
///           / Air Link drives the headset. Use this to test in the headset.
///   • OFF → Play never touches OpenXR, so the headset-less PC dev loop (XR
///           Device Simulator + keyboard DevExperimentDriver) is safe. Leaving
///           it ON with NO headset connected can stall Play mode.
/// Android is left untouched (the APK always auto-inits on the device).
public static class HeadsetPlayModeToggle
{
    const string MenuPath = "Tools/PharmaSynth/Headset Play Mode (OpenXR on Play)";

    static XRGeneralSettings Standalone()
    {
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out XRGeneralSettingsPerBuildTarget per);
        return per != null ? per.SettingsForBuildTarget(BuildTargetGroup.Standalone) : null;
    }

    [MenuItem(MenuPath, priority = 2000)]
    static void Toggle()
    {
        var s = Standalone();
        if (s == null)
        {
            Debug.LogError("[HeadsetPlayMode] No Standalone XR settings — assign OpenXR under XR Plug-in Management ▸ Desktop first.");
            return;
        }
        s.InitManagerOnStart = !s.InitManagerOnStart;
        EditorUtility.SetDirty(s);
        AssetDatabase.SaveAssets();
        Debug.Log(s.InitManagerOnStart
            ? "[HeadsetPlayMode] ON — press Play with a Quest connected (Quest Link / Air Link running) to drive the headset. Turn OFF for headless keyboard testing."
            : "[HeadsetPlayMode] OFF — headless-PC safe (XR Device Simulator + keyboard). Turn ON to test in the headset.");
    }

    [MenuItem(MenuPath, validate = true)]
    static bool ToggleValidate()
    {
        var s = Standalone();
        Menu.SetChecked(MenuPath, s != null && s.InitManagerOnStart);
        return true;
    }
}
#endif
