using UnityEngine;
using UnityEngine.UI;

public class SoundDebugManager : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] AudioSource targetAudioSource;   // デバッグ用オーディオ
    [SerializeField] Slider volumeSlider;             // UI スライダー

    void Start()
    {
        if (targetAudioSource == null)
            Debug.LogWarning("[SoundDebug] targetAudioSource が未設定です");

        if (volumeSlider == null)
            Debug.LogWarning("[SoundDebug] volumeSlider が未設定です");
        else
        {
            // 初期値をAudioSourceに同期
            volumeSlider.value = targetAudioSource.volume;

            // スライダー変更時に音量を反映
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }
    }

    void OnVolumeChanged(float value)
    {
        if (targetAudioSource != null)
            targetAudioSource.volume = value;
    }
}
