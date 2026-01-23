using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance;

    private Image fadeImage;
    private Coroutine fadeCoroutine;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        fadeImage = GetComponent<Image>();

        if (fadeImage == null)
        {
            Debug.LogError("FadeControllerÇ…ImageÇ™Ç†ÇËÇ‹ÇπÇÒÅI");
            return;
        }

        // ç≈èâÇÕìßñæ
        SetAlpha(0f);
    }

    void SetAlpha(float a)
    {
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }

    public void FadeOut(float duration, System.Action onComplete = null)
    {
        StartFade(0f, 1f, duration, onComplete);
    }

    public void FadeIn(float duration, System.Action onComplete = null)
    {
        StartFade(1f, 0f, duration, onComplete);
    }

    void StartFade(float from, float to, float time, System.Action onComplete)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeCoroutine = StartCoroutine(Fade(from, to, time, onComplete));
    }

    IEnumerator Fade(float from, float to, float time, System.Action onComplete)
    {
        float t = 0f;

        while (t < time)
        {
            t += Time.deltaTime;

            float a = Mathf.Lerp(from, to, t / time);
            SetAlpha(a);

            yield return null;
        }

        SetAlpha(to);

        fadeCoroutine = null;
        onComplete?.Invoke();
    }
}
