using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine;


public class VignettController : MonoBehaviour
{
    [Header("Volume")]
    [SerializeField] private Volume volume;

    [Header("Vignette 設定")]
    [SerializeField, Range(0f, 1f)] private float startValue = 0f;   // 開始時の強度
    [SerializeField, Range(0f, 1f)] private float endValue = 0.35f;  // 最大強度
    [SerializeField] private float duration = 5f;                   // start→end までの時間（秒）

    [Header("参照")]
    [SerializeField] private VRController vrController; // 壁張り付き状態を参照する対象

    // 内部管理用
    private Vignette vignette;   // Volume から取得した Vignette
    private float timer = 0f;    // 再生時間カウンタ
    private bool isPlaying = false; // 現在再生中かどうか

    void Start()
    {
        // Volume から Vignette を取得し、初期状態を設定
        if (volume != null && volume.profile.TryGet(out vignette))
        {
            vignette.intensity.value = startValue;
        }
    }

    void Update()
    {
        // 参照切れ防止
        if (vignette == null || vrController == null)
            return;

        // VRController の張り付き状態で分岐
        if (vrController.IsClinging)
        {
            // 張り付き中 → 再生（まだなら開始）
            PlayIfNeeded();
            UpdateVignette();
        }
        else
        {
            // 張り付き解除 → 停止＆即リセット
            StopAndReset();
        }
    }

    /// <summary>
    /// 再生中でなければ Vignette の再生を開始する
    /// </summary>
    void PlayIfNeeded()
    {
        if (isPlaying) return;

        timer = 0f;
        isPlaying = true;
    }

    /// <summary>
    /// Vignette の強度を時間経過で補間する
    /// </summary>
    void UpdateVignette()
    {
        if (!isPlaying) return;

        timer += Time.deltaTime;

        // 0～1 に正規化
        float t = Mathf.Clamp01(timer / duration);

        // startValue → endValue へ補間
        vignette.intensity.value = Mathf.Lerp(startValue, endValue, t);
    }

    /// <summary>
    /// 再生を停止し、Vignette を初期状態へ戻す
    /// </summary>
    void StopAndReset()
    {
        if (!isPlaying) return;

        isPlaying = false;
        timer = 0f;
        vignette.intensity.value = startValue;
    }
}
