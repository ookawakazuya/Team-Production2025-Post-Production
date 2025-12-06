using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSliderManager : MonoBehaviour
{
    // === 【追加】定数の定義とゼロ値の処理をより確実にする ===
    // 音量ゼロ（スライダー最小値）に設定するデシベル値
    private const float MIN_DB = -80f;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Slider voiceSlider;

    private void Start()
    {
        // Start()でも変換ロジックを適用することで、初期設定でサイレントになるのを防ぎます。
        // Master
        if (audioMixer != null && masterSlider != null && audioMixer.GetFloat("Master", out float masterDB))
        {
            masterSlider.value = Mathf.Pow(10, masterDB / 20f);
        }

        // BGM
        if (audioMixer != null && bgmSlider != null && audioMixer.GetFloat("BGM", out float bgmDB))
        {
            bgmSlider.value = Mathf.Pow(10, bgmDB / 20f);
        }

        // SE
        if (audioMixer != null && seSlider != null && audioMixer.GetFloat("SE", out float seDB))
        {
            seSlider.value = Mathf.Pow(10, seDB / 20f);
        }

        // Voice
        if (audioMixer != null && voiceSlider != null && audioMixer.GetFloat("Voice", out float voiceDB))
        {
            voiceSlider.value = Mathf.Pow(10, voiceDB / 20f);
        }
    }


    /// <summary>
    /// スライダー値 (0.0～1.0) を AudioMixer 用のデシベル値 (-80.0～0.0) に変換する
    /// </summary>
    private float VolumeToDB(float volume)
    {
        // volumeが0または非常に小さい場合、MIN_DBを返す
        // float.Epsilon (floatで表現できる最小の正の数) を使うことでより確実なゼロチェック
        if (volume <= float.Epsilon)
        {
            return MIN_DB;
        }

        // リニア値 -> dB への変換: 20 * log10(volume)
        return Mathf.Log10(volume) * 20f;
    }

    public void SetMaster(float volume)
    {
        audioMixer.SetFloat("Master", VolumeToDB(volume));
    }

    public void SetBGM(float volume)
    {
        audioMixer.SetFloat("BGM", VolumeToDB(volume));
    }

    public void SetSE(float volume)
    {
        audioMixer.SetFloat("SE", VolumeToDB(volume));
    }

    // パラメータ名が "Voice"（小文字）であることを確認
    public void SetVOICE(float volume)
    {
        audioMixer.SetFloat("Voice", VolumeToDB(volume));
    }
}