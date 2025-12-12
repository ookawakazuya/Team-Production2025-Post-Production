using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

public class VignettController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;

    [Header("Vignette 設定")]
    [SerializeField, Range(0f, 1f)] private float startValue = 0f;    // 初期値
    [SerializeField, Range(0f, 1f)] private float endValue = 0.35f;   // 最終値
    [SerializeField] private float duration = 5f;   // 0→最大までにかける秒数

    private Vignette vignette;
    private float timer = 0f;
    private bool isPlaying = false;

    void Start()
    {
        if (volume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = startValue;
        }
    }

    void Update()
    {
        // ▼ デバッグ用：スペースキーで開始
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Play();
        }

        if (!isPlaying || vignette == null) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / duration);

        vignette.intensity.value = Mathf.Lerp(startValue, endValue, t);

        if (t >= 1f)
        {
            isPlaying = false;
        }
    }

    public void Play()
    {
        timer = 0f;
        isPlaying = true;
    }

    // ▼ Wall 接触時に開始
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wall"))
        {
            Play();
        }
    }
}
