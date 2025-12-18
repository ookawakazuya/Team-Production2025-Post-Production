using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSliderManager : MonoBehaviour
{
    // === 定数の定義とゼロ値の処理をより確実にする ===
    // 音量ゼロ（スライダー最小値）に設定するデシベル値
    private const float MIN_DB = -80f;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Slider voiceSlider;


    [Header("数字画像")]
    [SerializeField] private Sprite[] numberSprites;

    [Header("表示用image")]
    [SerializeField] private Image master100, master10, master1;
    [SerializeField] private Image bgm100, bgm10, bgm1;
    [SerializeField] private Image se100, se10, se1;
    [SerializeField] private Image voice100, voice10, voice1;

    private void Start()
    {
        InitSlider(masterSlider, "Master",master100, master10, master1);
        InitSlider(bgmSlider,"BGM", bgm100, bgm10, bgm1);
        InitSlider(seSlider, "SE", se100, se10, se1);
        InitSlider(voiceSlider, "Voice", voice100, voice10, voice1);
    }

    void InitSlider(Slider slider,string paramName,Image imag100,Image imag10,Image imag1　)
    {
        if(audioMixer != null && slider != null)
        {
            float dbValue;
            // AudioMixerから現在の値を取得できているか確認
            if (audioMixer.GetFloat(paramName, out dbValue))
            {
                // デシベルから 0.0～1.0 への変換
                float val = Mathf.Clamp01(Mathf.Pow(10, dbValue / 20f));
                slider.value = val;
                UpdateDisplay(val, imag100, imag10, imag1);
            }
        


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
        UpdateDisplay(volume,master100, master10, master1);
    }

    public void SetBGM(float volume)
    {
        audioMixer.SetFloat("BGM", VolumeToDB(volume));
        UpdateDisplay(volume ,bgm100, bgm10, bgm1);
    }

    public void SetSE(float volume)
    {
        audioMixer.SetFloat("SE", VolumeToDB(volume));
        UpdateDisplay (volume ,se100, se10, se1);
    }

    // パラメータ名が "Voice"（小文字）であることを確認
    public void SetVOICE(float volume)
    {
        audioMixer.SetFloat("Voice", VolumeToDB(volume));
        UpdateDisplay(volume,voice100,voice10, voice1);
    }

    void UpdateDisplay(float volume,Image imag100,Image imag10,Image imag1)
    {
        if (imag10 == null || imag1 == null || numberSprites.Length < 10) return;
        //0.0～1.0を0～100に変換
        int intValue = Mathf.RoundToInt(Mathf.Clamp(volume, 0f, 1f) * 100f);

        int hundreds = intValue / 100;      //100
        int tens = (intValue%100) / 10;     //10
        int ones = intValue % 10;           //1
        //10の位表示制御
        imag100.sprite = numberSprites[hundreds];
        imag10.sprite = numberSprites[tens];
        imag1.sprite = numberSprites[ones];
    }
}