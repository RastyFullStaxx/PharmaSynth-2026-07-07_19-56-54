using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class NPCNarrationController : MonoBehaviour
{
    [System.Serializable]
    public class NarrationLine
    {
        [TextArea(2, 4)] public string subtitle;
        public AudioClip voiceClip;
        public float fallbackSeconds = 3f;
    }

    [Header("References")]
    [SerializeField] private AudioSource narratorAudioSource;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private GameObject skipButton;
    [Tooltip("The visible bubble/panel behind the subtitle — shown only while a line is speaking.")]
    [SerializeField] private GameObject panelRoot;

    [Header("Sequence")]
    [SerializeField] private List<NarrationLine> tutorialLines = new List<NarrationLine>();
    [SerializeField] private bool playOnStart = true;

    [Header("Typewriter (user 2026-07-10: type lines out + talking blips)")]
    [SerializeField] private bool typewriter = true;
    // 32 cps ≈ 384 wpm — far past comfortable reading, and it read as Pharmee
    // "speaking rapidly" (user 2026-07-19). 22 cps ≈ 260 wpm: brisk but legible.
    [SerializeField] private float charsPerSecond = 22f;
    [SerializeField] private int blipEveryChars = 4;            // a voice blip every N revealed non-space chars
    [SerializeField] private string voiceBlipKey = "";          // this speaker's talking blip (SoundBank key)
    [SerializeField, Range(0f, 1f)] private float blipVolume = 0.5f;

    [Header("Voice-over (user 2026-07-10: NPCs speak their lines)")]
    [SerializeField] private VoiceBank voiceBank;               // hash-keyed clip lookup (optional)
    [SerializeField] private VoiceSpeaker speaker = VoiceSpeaker.Pharmee;

    [Header("Events")]
    public UnityEvent onNarrationFinished;

    /// Per-line hooks: the overhead bubble AND the HUD dialogue bar both mirror these.
    public event Action<string, float> LineStarted;
    public event Action LineEnded;

    /// True while a line is on screen (between BeginLine and EndLine).
    public bool IsSpeaking { get; private set; }

    /// Typewriter state — the HUD dialogue bar mirrors these to stay in sync.
    public bool Typewriter => typewriter;
    public float TypeCps() => typewriter ? Mathf.Max(1f, charsPerSecond) : 100000f;

    /// Characters currently revealed (the HUD bar reads this each frame so the two
    /// displays — and a skip — stay perfectly in sync). int.MaxValue = show all.
    public int VisibleCount { get; private set; } = int.MaxValue;

    /// True while the line is still typing out (before it's fully revealed).
    public bool IsRevealing { get; private set; }

    private Coroutine narrationRoutine;
    private AudioSource _blip;
    private bool _skipReveal;
    private bool _voiceActive;   // a real voice clip is playing → suppress blips

    // Lines that arrive while another is still TYPING are queued, not stomped
    // (user 2026-07-12: Pharmee's dialogue cut off mid-sentence — every new line
    // used to kill the reveal in progress). Bounded so stale chatter can't pile up.
    private readonly Queue<(string subtitle, float seconds, AudioClip clip)> _queued
        = new Queue<(string, float, AudioClip)>();
    private const int MaxQueued = 2;

    [Header("Pacing (user 2026-07-19: lines felt rapid + ran together)")]
    [Tooltip("Minimum time a FULLY revealed line stays up, whatever the authored dwell.")]
    [SerializeField] private float minHoldSeconds = 1.9f;
    [Tooltip("Clear beat between consecutive lines so they never run together.")]
    [SerializeField] private float interLineGap = 1.25f;

    /// Post-reveal read time: the authored dwell minus what typing consumed,
    /// floored so a long line always holds ≥ minHold once fully shown (long
    /// lines used to vanish 0.6 s after the last character). Pure + tested.
    public static float HoldSecondsAfterReveal(float authoredWait, float revealSeconds, float minHold = 1.9f)
        => Mathf.Max(minHold, authoredWait - revealSeconds);

    /// Edit-mode/test binding for the auto-hidden bubble panel.
    public void SetPanelRoot(GameObject g) => panelRoot = g;

    // Coroutines die on disable — reset the reveal flag and drop queued lines
    // so a re-enabled narrator can't deadlock queueing behind a dead reveal.
    private void OnDisable()
    {
        IsRevealing = false;
        if (s_floor == this) s_floor = null;   // never strand the floor on a dead narrator
        _queued.Clear();
        // A disable mid-line used to leave IsSpeaking TRUE and never raise
        // LineEnded, so the HUD bar kept a half-revealed line on screen with no
        // owner to finish or clear it (2026-07-19).
        if (IsSpeaking) { IsSpeaking = false; LineEnded?.Invoke(); }
        VisibleCount = int.MaxValue;
    }

    /// Voice-over seam: the hash-keyed clip bank + which character this channel is.
    public void BindVoice(VoiceBank bank, VoiceSpeaker who) { voiceBank = bank; speaker = who; }

    /// The voice clip for a subtitle, if the bank has one (null = blip fallback).
    public AudioClip ResolveVoice(string subtitle)
        => voiceBank != null ? voiceBank.Get(speaker, VoiceLineId.For(subtitle)) : null;

    /// Assign this speaker's talking blip (SoundBank key) — played per few chars as the line types.
    public void SetVoiceBlip(string key, float volume = 0.5f) { voiceBlipKey = key; blipVolume = Mathf.Clamp01(volume); }

    private void Start()
    {
        if (panelRoot != null) panelRoot.SetActive(false); // silent until spoken to
        // Long lines must wrap and never truncate — some bubble/HUD texts were
        // authored NoWrap/Ellipsis and visually cut lines off (user 2026-07-12).
        if (subtitleText != null)
        {
            subtitleText.textWrappingMode = TextWrappingModes.Normal;
            subtitleText.overflowMode = TextOverflowModes.Overflow;
        }
        if (playOnStart)
            PlayTutorialNarration();
    }

    public void PlayTutorialNarration()
    {
        if (narrationRoutine != null)
            StopCoroutine(narrationRoutine);

        narrationRoutine = StartCoroutine(PlayLinesRoutine());
    }

    /// Reactive single-line narration. A line still TYPING is never stomped —
    /// the new one queues behind it; a fully-revealed line (in its hold) is
    /// interrupted as before. Used by PharmeeBrain for instructions/warnings.
    public void Say(string subtitle, float seconds = 3f, AudioClip clip = null)
    {
        if (!isActiveAndEnabled) return; // edit-mode / disabled: no coroutine
        // QUEUE while a line is still typing OR while we're in the inter-line beat.
        // The gap used to be a hole: for its 1.25 s neither IsSpeaking nor
        // IsRevealing was true, so an arriving Say() stopped the outer SayRoutine
        // and the line already queued behind it was silently destroyed, never
        // shown at all (2026-07-19).
        if ((IsRevealing && IsSpeaking) || _chaining)
        {
            while (_queued.Count >= MaxQueued) _queued.Dequeue();   // drop the stalest
            _queued.Enqueue((subtitle, seconds, clip));
            return;
        }
        if (narrationRoutine != null)
            StopCoroutine(narrationRoutine);
        // ANOTHER narrator is mid-sentence — wait for them rather than speaking
        // over the top (the two NPCs share no timeline of their own).
        if (FloorBusy && s_floor != this)
        {
            narrationRoutine = StartCoroutine(WaitForFloorThenSay(subtitle, seconds, clip));
            return;
        }
        narrationRoutine = StartCoroutine(SayRoutine(subtitle, seconds, clip));
    }

    /// Coroutine-free line start: shows text + bubble + skip, raises LineStarted.
    /// Public so it is edit-mode testable and callable by cutscene staging.
    public void BeginLine(string subtitle, float seconds)
    {
        // Activate BEFORE writing text — same TMP-on-inactive-object trap as
        // RevealAndHold (a stale textInfo here would mis-measure any mirror).
        if (panelRoot != null) panelRoot.SetActive(true);
        if (skipButton != null) skipButton.SetActive(true);
        if (subtitleText != null) { subtitleText.text = subtitle; subtitleText.maxVisibleCharacters = int.MaxValue; }
        IsSpeaking = true;
        VisibleCount = int.MaxValue;      // instant path shows the whole line
        LineStarted?.Invoke(subtitle, seconds);
    }

    /// Coroutine-free line end: clears text, hides bubble + skip, raises LineEnded.
    /// EXTERNAL interrupt — also kills the reveal coroutine, because an orphaned
    /// RevealAndHold used to write VisibleCount/maxVisibleCharacters back over the
    /// line this just cleared (2026-07-19).
    public void EndLine() => EndLine(true);

    private void EndLine(bool stopRoutine)
    {
        // ⚠ Only an EXTERNAL caller may stop the routine: RevealAndHold calls this
        // at its own natural end from INSIDE narrationRoutine, and stopping it
        // there would kill SayRoutine's queued-line chaining (dropped lines).
        if (stopRoutine && narrationRoutine != null)
        { StopCoroutine(narrationRoutine); narrationRoutine = null; _chaining = false; _queued.Clear(); }
        if (subtitleText != null) subtitleText.text = string.Empty;
        if (panelRoot != null) panelRoot.SetActive(false);
        if (skipButton != null) skipButton.SetActive(false);
        IsRevealing = false; VisibleCount = int.MaxValue;
        if (s_floor == this) s_floor = null;         // release the floor
        if (!IsSpeaking) return; // idempotent — visuals reset above either way
        IsSpeaking = false;
        LineEnded?.Invoke();
    }

    private IEnumerator SayRoutine(string subtitle, float seconds, AudioClip clip)
    {
        if (clip == null) clip = ResolveVoice(subtitle);   // voice-over by text hash
        float waitSeconds = Mathf.Max(0.1f, seconds);
        _voiceActive = false;
        if (narratorAudioSource != null && clip != null)
        {
            narratorAudioSource.clip = clip;
            narratorAudioSource.Play();
            waitSeconds = Mathf.Max(waitSeconds, clip.length + 0.2f);
            _voiceActive = true;                            // real speech → no robot blips
        }
        yield return RevealAndHold(subtitle, waitSeconds);
        _voiceActive = false;
        // Chain any line that queued while this one was typing — but leave a clear
        // BEAT between lines (user 2026-07-13: Pharmee fired lines back-to-back too
        // fast to follow). The pause lets the bubble/HUD fully clear before the next.
        if (_queued.Count > 0)
        {
            var next = _queued.Dequeue();
            _chaining = true;                       // Say() must QUEUE, not stomp, during the beat
            yield return new WaitForSeconds(Mathf.Max(0f, interLineGap));
            _chaining = false;
            narrationRoutine = StartCoroutine(SayRoutine(next.subtitle, next.seconds, next.clip));
        }
    }

    /// True during the inter-line beat (line finished, next one pending).
    private bool _chaining;

    // ---- CROSS-NPC FLOOR -----------------------------------------------------
    // Pharmee and Dr. Jimenez own SEPARATE controllers with no shared timeline,
    // so either could start a sentence on top of the other's (user 2026-07-19:
    // "pharmee is still speaking ... where dr jimenez now speaks"). Whoever is
    // mid-REVEAL holds the floor; another speaker waits for them to finish
    // instead of talking over. Only the reveal is protected — the post-reveal
    // read hold is interruptible, so conversation still feels responsive.
    private static NPCNarrationController s_floor;

    /// True while ANY narrator is still typing a line out.
    public static bool FloorBusy => s_floor != null && s_floor.IsRevealing;

    /// The longest another speaker will be made to wait before going anyway —
    /// a safety valve so a stuck reveal can never mute the game.
    private const float MaxFloorWait = 8f;

    private IEnumerator WaitForFloorThenSay(string subtitle, float seconds, AudioClip clip)
    {
        float t = 0f;
        while (FloorBusy && s_floor != this && t < MaxFloorWait)
        { t += Time.deltaTime; yield return null; }
        yield return new WaitForSeconds(Mathf.Max(0f, interLineGap));
        narrationRoutine = StartCoroutine(SayRoutine(subtitle, seconds, clip));
    }

    /// How long a line will actually occupy the floor: the typewriter reveal plus
    /// the read hold. Schedulers MUST use this rather than a fixed dwell — a line
    /// longer than (dwell x cps) chars overruns its slot and the next speaker
    /// starts on top of it, which is precisely how the review beats collided.
    public float SecondsFor(string subtitle, float authoredSeconds)
    {
        int n = string.IsNullOrEmpty(subtitle) ? 0 : subtitle.Length;
        float reveal = typewriter ? n / Mathf.Max(1f, charsPerSecond) : 0f;
        return reveal + HoldSecondsAfterReveal(authoredSeconds, reveal, minHoldSeconds);
    }

    /// Type the line out character-by-character (with per-few-chars talking blips),
    /// then hold the finished line for the remaining dwell time, then end it. The HUD
    /// dialogue bar mirrors the reveal via LineStarted + the shared TypeCps().
    private IEnumerator RevealAndHold(string subtitle, float waitSeconds)
    {
        // ⛔ ORDER IS LOAD-BEARING (the mid-sentence truncation bug, 2026-07-19).
        // The panel must be ACTIVE before ForceMeshUpdate(): TMP does not rebuild
        // textInfo on an inactive GameObject, so characterCount came back STALE
        // FROM THE PREVIOUS LINE (or 0 on the very first). A shorter previous line
        // meant `total` was too small, the reveal loop exited early, and the line
        // sat permanently cut off mid-sentence for its whole dwell — exactly the
        // "text doesn't get completely written" the user kept seeing on both the
        // world bubble and the HUD bar (the HUD mirrors VisibleCount).
        if (panelRoot != null) panelRoot.SetActive(true);
        if (skipButton != null) skipButton.SetActive(true);
        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
            subtitleText.maxVisibleCharacters = typewriter ? 0 : int.MaxValue;
            subtitleText.ForceMeshUpdate();
        }
        IsSpeaking = true;
        VisibleCount = typewriter ? 0 : int.MaxValue;
        LineStarted?.Invoke(subtitle, waitSeconds);

        int total = CharacterTotal(subtitle);
        float revealTime = 0f;
        if (typewriter && total > 0)
        {
            IsRevealing = true; _skipReveal = false;
            s_floor = this;                 // claim the spoken floor while typing
            float cps = TypeCps();
            int sinceBlip = 0, shown = 0;
            // TIME-ACCUMULATOR, not WaitForSeconds(1/cps) per char: a per-character
            // yield is frame-quantised (at 72 Hz a 1/32 s wait really costs 1/24 s),
            // so the true reveal ran ~33% slower than the cps the hold math assumed
            // — the line then lost that time off its READ hold.
            float carry = 0f;
            while (shown < total && IsSpeaking && !_skipReveal)   // skip fills instantly
            {
                yield return null;
                carry += Time.deltaTime * cps;
                int step = Mathf.FloorToInt(carry);
                if (step <= 0) continue;
                carry -= step;
                for (int k = 0; k < step && shown < total; k++)
                {
                    shown++;
                    if (!char.IsWhiteSpace(CharAt(shown - 1)) && ++sinceBlip >= Mathf.Max(1, blipEveryChars))
                    { sinceBlip = 0; PlayBlip(); }
                }
                if (subtitleText != null) subtitleText.maxVisibleCharacters = shown;
                VisibleCount = shown;
            }
            if (!IsSpeaking) yield break;      // EndLine ran underneath us — don't re-show
            if (subtitleText != null) subtitleText.maxVisibleCharacters = total;
            VisibleCount = total;
            IsRevealing = false; _skipReveal = false;
            revealTime = total / cps;
        }

        yield return new WaitForSeconds(HoldSecondsAfterReveal(waitSeconds, revealTime, minHoldSeconds));
        EndLine(false);   // natural end — never stop our own routine (kills chaining)
    }

    /// Character count for the reveal. Prefers TMP's parsed count (rich-text tags
    /// are not characters) but NEVER trusts a zero/stale value — falls back to the
    /// raw string so a line can always finish typing.
    private int CharacterTotal(string subtitle)
    {
        int raw = subtitle != null ? subtitle.Length : 0;
        if (subtitleText == null || subtitleText.textInfo == null) return raw;
        int parsed = subtitleText.textInfo.characterCount;
        return parsed > 0 ? parsed : raw;
    }

    private char CharAt(int i)
    {
        if (subtitleText != null && subtitleText.textInfo != null
            && i >= 0 && i < subtitleText.textInfo.characterCount)
            return subtitleText.textInfo.characterInfo[i].character;
        return 'x';
    }

    private void PlayBlip()
    {
        if (_voiceActive) return;   // a spoken clip owns this line
        if (string.IsNullOrEmpty(voiceBlipKey) || AudioService.Instance == null) return;
        var e = AudioService.Instance.EntryOf(voiceBlipKey);
        if (e == null || e.clip == null) return;
        if (_blip == null)
        {
            var go = new GameObject("BlipSource");
            go.transform.SetParent(transform, false);
            _blip = go.AddComponent<AudioSource>();
            _blip.playOnAwake = false; _blip.spatialBlend = 0f;   // 2D — reads with the HUD line
        }
        _blip.pitch = AudioService.JitteredPitch(0.12f, UnityEngine.Random.value);
        _blip.PlayOneShot(e.clip, Mathf.Clamp01(e.volume) * blipVolume);
    }

    /// Skip button / fast-forward: the FIRST press completes the current typewriter
    /// reveal instantly (fills the line); a SECOND press (line already fully shown)
    /// ends/advances it. So fast readers aren't forced to wait, but one tap never
    /// skips text they haven't seen.
    public void SkipNarration()
    {
        if (IsRevealing) { _skipReveal = true; return; }   // first tap → fill the line

        if (narrationRoutine != null)
            StopCoroutine(narrationRoutine);

        if (narratorAudioSource != null)
            narratorAudioSource.Stop();

        // Take the queue BEFORE EndLine — the external path clears it.
        bool hasNext = _queued.Count > 0;
        var next = hasNext ? _queued.Dequeue() : default;
        EndLine();
        if (hasNext)
        {
            narrationRoutine = StartCoroutine(SkipChain(next));
            return;
        }
        onNarrationFinished?.Invoke();
    }

    /// A skipped line still leaves the beat before the next one (skip used to
    /// chain with NO gap, so two lines ran together on a fast tap).
    private IEnumerator SkipChain((string subtitle, float seconds, AudioClip clip) next)
    {
        _chaining = true;
        yield return new WaitForSeconds(Mathf.Max(0f, interLineGap));
        _chaining = false;
        narrationRoutine = StartCoroutine(SayRoutine(next.subtitle, next.seconds, next.clip));
    }

    private IEnumerator PlayLinesRoutine()
    {
        for (int i = 0; i < tutorialLines.Count; i++)
        {
            NarrationLine line = tutorialLines[i];
            if (line == null)
                continue;

            float waitSeconds = Mathf.Max(0.1f, line.fallbackSeconds);
            var clip = line.voiceClip != null ? line.voiceClip : ResolveVoice(line.subtitle);
            _voiceActive = false;
            if (narratorAudioSource != null && clip != null)
            {
                narratorAudioSource.clip = clip;
                narratorAudioSource.Play();
                waitSeconds = Mathf.Max(waitSeconds, clip.length + 0.2f);
                _voiceActive = true;
            }

            yield return RevealAndHold(line.subtitle, waitSeconds);
            _voiceActive = false;
        }

        onNarrationFinished?.Invoke();
    }
}
