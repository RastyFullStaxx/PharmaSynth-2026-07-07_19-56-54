using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;

public class ModuleCutsceneController : MonoBehaviour
{
    [SerializeField] private PlayableDirector[] cutsceneSequence;
    [SerializeField] private GameObject skipButton;
    [SerializeField] private bool playOnStart;

    public UnityEvent onCutscenesFinished;

    private Coroutine sequenceRoutine;
    private bool isSkipping;

    private void Start()
    {
        if (playOnStart)
            PlayCutscenes();
    }

    public void PlayCutscenes()
    {
        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        isSkipping = false;
        sequenceRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    public void SkipCutscenes()
    {
        isSkipping = true;

        for (int i = 0; i < cutsceneSequence.Length; i++)
        {
            if (cutsceneSequence[i] != null)
                cutsceneSequence[i].Stop();
        }

        if (skipButton != null)
            skipButton.SetActive(false);

        onCutscenesFinished?.Invoke();
    }

    private IEnumerator PlaySequenceRoutine()
    {
        if (skipButton != null)
            skipButton.SetActive(true);

        for (int i = 0; i < cutsceneSequence.Length; i++)
        {
            if (isSkipping)
                yield break;

            PlayableDirector director = cutsceneSequence[i];
            if (director == null)
                continue;

            director.Play();
            while (director.state == PlayState.Playing && !isSkipping)
                yield return null;
        }

        if (skipButton != null)
            skipButton.SetActive(false);

        onCutscenesFinished?.Invoke();
    }
}
