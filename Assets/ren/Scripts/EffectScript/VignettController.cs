using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using System.Collections;

public class VignettController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;

    [Header("Vignette 設定（張り付き）")]
    [SerializeField, Range(0f, 1f)] private float startValue = 0f;
    [SerializeField, Range(0f, 1f)] private float endValue = 0.35f;
    [SerializeField] private float duration = 5f;

    [Header("奈落 Vignette 設定")]
    [SerializeField] private Transform player;
    [SerializeField] private float fallStartY = -15f;
    [SerializeField] private float fallEndY = -30f;
    [SerializeField, Range(0f, 1f)] private float fallMaxValue = 1f;

    [Header("Damage Vignette Color")]
    [SerializeField] private Color damageColor = Color.red;

    private Color defaultColor;

    [Header("参照")]
    [SerializeField] private VRController vrController;

    private Vignette vignette;
    private float timer = 0f;
    private bool isPlaying = false;

    void Start()
    {

        if (volume != null && volume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = startValue;
        }

        defaultColor = vignette.color.value;

    }

    void Update()
    {
        if (vignette == null || vrController == null || player == null)
            return;

        float clingValue = GetClingVignetteValue();
        float fallValue = GetFallVignetteValue();

        // より暗い方を採用
        float finalValue = Mathf.Max(clingValue, fallValue);

        vignette.intensity.value = finalValue;

        // どちらも効いていなければリセット
        if (finalValue <= startValue)
        {
            isPlaying = false;
            timer = 0f;
        }
    }

    // =====================
    // 張り付き Vignette
    // =====================
    float GetClingVignetteValue()
    {
        if (!vrController.IsClinging)
            return startValue;

        if (!isPlaying)
        {
            timer = 0f;
            isPlaying = true;
        }

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);
        return Mathf.Lerp(startValue, endValue, t);
    }

    // =====================
    // 奈落 Vignette
    // =====================
    float GetFallVignetteValue()
    {
        float y = player.position.y;

        if (y > fallStartY)
            return startValue;

        float t = Mathf.InverseLerp(fallStartY, fallEndY, y);
        return Mathf.Lerp(startValue, fallMaxValue, t);
    }

    public void PlayDamageVignette()
    {
        StopAllCoroutines();
        StartCoroutine(DamageVignetteCoroutine());
    }

    IEnumerator DamageVignetteCoroutine()
    {
        // 色を赤に
        vignette.color.value = damageColor;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            vignette.intensity.value = Mathf.Lerp(startValue, endValue, t);
            yield return null;
        }

        yield return new WaitForSeconds(0.2f);

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            vignette.intensity.value = Mathf.Lerp(endValue, startValue, t);
            yield return null;
        }

        // 元の色に戻す
        vignette.color.value = defaultColor;
    }
}
