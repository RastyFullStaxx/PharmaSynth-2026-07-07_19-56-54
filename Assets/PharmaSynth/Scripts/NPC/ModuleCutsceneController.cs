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
    private PlayableDirector activeDirector;
    private bool directorStopped;
    private bool hasFinished;

    private void Start()
    {
        if (playOnStart)
            PlayCutscenes();
    }

    public void PlayCutscenes()
    {
        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);
        DetachActiveDirector();

        hasFinished = false;
        sequenceRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    public void SkipCutscenes()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }
        DetachActiveDirector();

        if (cutsceneSequence != null)
        {
            for (int i = 0; i < cutsceneSequence.Length; i++)
            {
                if (cutsceneSequence[i] != null)
                    cutsceneSequence[i].Stop();
            }
        }

        Finish();
    }

    private IEnumerator PlaySequenceRoutine()
    {
        if (skipButton != null)
            skipButton.SetActive(true);

        if (cutsceneSequence != null)
        {
            for (int i = 0; i < cutsceneSequence.Length; i++)
            {
                PlayableDirector director = cutsceneSequence[i];
                if (director == null)
                    continue;

                directorStopped = false;
                activeDirector = director;
                director.stopped += OnDirectorStopped;
                director.Play();

                // director.stopped covers normal completion; the duration check
                // covers Hold wrap mode, which never leaves PlayState.Playing.
                while (!directorStopped && director.time < director.duration)
                    yield return null;

                DetachActiveDirector();
            }
        }

        sequenceRoutine = null;
        Finish();
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        directorStopped = true;
    }

    private void DetachActiveDirector()
    {
        if (activeDirector != null)
        {
            activeDirector.stopped -= OnDirectorStopped;
            activeDirector = null;
        }
    }

    private void Finish()
    {
        if (hasFinished)
            return;
        hasFinished = true;

        if (skipButton != null)
            skipButton.SetActive(false);

        onCutscenesFinished?.Invoke();
    }
}
