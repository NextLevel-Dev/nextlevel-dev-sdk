using System;
using UnityEngine;
using System.Collections;

public class UnityRuntime : AutoMonoBehaviour<UnityRuntime>
{
    public delegate void ApplicationStart();
    public event ApplicationStart OnApplicationStart;
    private static bool _applicationStarted;

    protected override void Start()
    {
        if (_applicationStarted)
            return;

        OnApplicationStart?.Invoke();

        _applicationStarted = true;
    }

    public static Coroutine StartRoutine(IEnumerator routine)
    {
        return IsRunning ? Instance.StartCoroutine(routine) : null;
    }

    public static void StopRoutine(IEnumerator routine)
    {
        Instance.StopCoroutine(routine);
    }

    public static void StopRoutine(Coroutine routine)
    {
        if (routine == null)
            return;

        Instance.StopCoroutine(routine);
    }

    public static Coroutine Invoke(Action work, float delay)
    {
        return StartRoutine(InvokeLater(work, delay));
    }

    private static IEnumerator InvokeLater(Action work, float delay)
    {
        yield return new WaitForSeconds(delay);
        try
        {
            work();
        }
        catch (MissingReferenceException)
        {
        }
    }

    public static void InvokeNextFrame(Action work)
    {
        StartRoutine(InvokeLaterFrame(work));
    }

    private static IEnumerator InvokeLaterFrame(Action work)
    {
        yield return null;
        try
        {
            work();
        }
        catch (MissingReferenceException)
        {
        }
    }

    public static Coroutine UpdateLimited(Action work, float duration, Action onEnd = null)
    {
        return StartRoutine(InvokeLimited(work, duration, onEnd));
    }

    private static IEnumerator InvokeLimited(Action work, float duration, Action onEnd)
    {
        var elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            try
            {
                work();
            }
            catch (MissingReferenceException)
            {
                yield break;
            }
            yield return null;
        }

        onEnd?.Invoke();
    }
}