using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance;
    private Image fadeImage;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        fadeImage = GetComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);
    }

    public void FadeOut(float duration, System.Action onComplete = null)
    {
        StartCoroutine(Fade(0f, 1f, duration, onComplete));
    }

    public void FadeIn(float duration, System.Action onComplete = null)
    {
        StartCoroutine(Fade(1f, 0f, duration, onComplete));
    }

    private IEnumerator Fade(float startAlpha, float endAlpha, float duration, System.Action onComplete)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, time / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        fadeImage.color = new Color(0, 0, 0, endAlpha);
        onComplete?.Invoke();
    }
}
