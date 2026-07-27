#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// Exports the full voice-over manifest (user 2026-07-10: NPCs speak): every
/// code-authored line (VoiceCorpus) plus every cutscene beat, one row per
/// unique (speaker, text) with its stable id. Tools/voice/generate-voice.ps1
/// consumes the manifest; changed lines re-key and regenerate individually.
public static class VoiceManifestExporter
{
    const string OutPath = "Assets/PharmaSynth/Audio/Voice/voice-manifest.json";

    [System.Serializable]
    public class ManifestLine { public string id; public string speaker; public string text; public int chars; }

    /// Generation ORDER (user 2026-07-27: "prioritize dr jimenez before we generate
    /// pharmee. stop only when we ran out of tokens"). The PowerShell generator
    /// walks the manifest top-to-bottom and skips what already exists, so writing
    /// Jimenez's rows first means a run that dies on an exhausted quota has spent
    /// every credit on him — no ordering logic needed in the script itself.
    /// Pure + pinned: lower sorts first.
    public static int SpeakerPriority(string speaker) => speaker == "Jimenez" ? 0 : 1;

    [System.Serializable]
    public class Manifest { public List<ManifestLine> lines = new List<ManifestLine>(); }

    [MenuItem("Tools/PharmaSynth/Voice/Export Voice Manifest")]
    public static void Export()
    {
        var manifest = new Manifest();
        var seen = new HashSet<string>();

        void Add(VoiceSpeaker speaker, string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            string id = VoiceLineId.For(text);
            string key = speaker + ":" + id;
            if (!seen.Add(key)) return;
            manifest.lines.Add(new ManifestLine
            {
                id = id,
                speaker = speaker.ToString(),
                text = VoiceLineId.Normalize(text),
                chars = VoiceLineId.Normalize(text).Length,
            });
        }

        foreach (var l in VoiceCorpus.CodeLines()) Add(l.speaker, l.text);

        // Cutscene beats (Pharmee narrates all four cutscenes per module).
        int beats = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:CutsceneData", new[] { "Assets/PharmaSynth/ScriptableObjects/Cutscenes" }))
        {
            var cs = AssetDatabase.LoadAssetAtPath<CutsceneData>(AssetDatabase.GUIDToAssetPath(guid));
            if (cs == null || cs.beats == null) continue;
            foreach (var b in cs.beats)
                if (b != null) { Add(VoiceSpeaker.Pharmee, b.subtitle); beats++; }
        }

        // Jimenez first (stable within each speaker — OrderBy is a stable sort, so
        // the corpus order is preserved and re-exports don't shuffle the queue).
        manifest.lines = new List<ManifestLine>(
            System.Linq.Enumerable.OrderBy(manifest.lines, l => SpeakerPriority(l.speaker)));

        Directory.CreateDirectory(Path.GetDirectoryName(OutPath));
        File.WriteAllText(OutPath, JsonUtility.ToJson(manifest, true), new UTF8Encoding(false));
        AssetDatabase.ImportAsset(OutPath);

        int chars = 0, pharmee = 0, jimenez = 0, jChars = 0;
        foreach (var l in manifest.lines)
        {
            chars += l.chars;
            if (l.speaker == "Pharmee") pharmee++; else { jimenez++; jChars += l.chars; }
        }
        // eleven_flash_v2_5 ≈ 0.5 credits per character.
        Debug.Log($"[VoiceManifest] {manifest.lines.Count} unique lines ({jimenez} Jimenez FIRST, {pharmee} Pharmee, "
                  + $"{beats} cutscene beats folded in), {chars:n0} characters ≈ {chars / 2:n0} ElevenLabs credits "
                  + $"on eleven_flash_v2_5. Jimenez alone: {jChars:n0} characters ≈ {jChars / 2:n0} credits — "
                  + $"generate him first and the run can stop anywhere. Wrote {OutPath}.");
    }
}
#endif
