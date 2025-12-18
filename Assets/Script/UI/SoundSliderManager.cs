using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using System.Collections; // コルーチンに必要

public class SoundSliderManager : MonoBehaviour
{
    private const float MIN_DB = -80f;

    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Slider voiceSlider;

    [Header("数字画像 (0~9)")]
    [SerializeField] private Sprite[] numberSprites;

    [Header("表示用Image")]
    [SerializeField] private Image master100, master10, master1;
    [SerializeField] private Image bgm100, bgm10, bgm1;
    [SerializeField] private Image se100, se10, se1;
    [SerializeField] private Image voice100, voice10, voice1;

    // StartをIEnumeratorにすることで、Unityが自動的にコルーチンとして開始します
    private IEnumerator Start()
    {
        // 1. AudioMixerがシステムに完全にロードされるまで少し待機
        yield return new WaitForEndOfFrame();

        // 2. 各スライダーの初期化
        InitSlider(masterSlider, "Master", master100, master10, master1);
        InitSlider(bgmSlider, "BGM", bgm100, bgm10, bgm1);
        InitSlider(seSlider, "SE", se100, se10, se1);
        InitSlider(voiceSlider, "VOICE", voice100, voice10, voice1);
    }

    void InitSlider(Slider slider, string paramName, Image img100, Image img10, Image img1)
    {
        if (audioMixer == null || slider == null) return;

        if (audioMixer.GetFloat(paramName, out float dbValue))
        {
            float val = Mathf.Pow(10, dbValue / 20f);

            // 重要：スライダーの値を書き換える際、OnValueChangedイベントが
            // 連鎖して音量を書き換えないよう、一時的にリスナーを外すか値を直接代入
            slider.onValueChanged.RemoveAllListeners();

            slider.value = val;
            UpdateDisplay(val, img100, img10, img1);

            // 初期化後にイベントを再登録（Inspectorではなくコードで登録する場合）
            // ※Inspectorで登録している場合は、このRemoveAllListenersは注意が必要
            // その場合は「初期化中フラグ」を立てる等の対策が必要です。
            AddListenerToSlider(slider, paramName);
        }
    }

    // 各スライダーに正しいメソッドを再登録する
    void AddListenerToSlider(Slider slider, string paramName)
    {
        if (paramName == "Master") slider.onValueChanged.AddListener(SetMaster);
        if (paramName == "BGM") slider.onValueChanged.AddListener(SetBGM);
        if (paramName == "SE") slider.onValueChanged.AddListener(SetSE);
        if (paramName == "VOICE") slider.onValueChanged.AddListener(SetVOICE);
    }

    // --- 以下、既存のSetメソッドとUpdateDisplay ---
    public void SetMaster(float volume) { audioMixer.SetFloat("Master", VolumeToDB(volume)); UpdateDisplay(volume, master100, master10, master1); }
    public void SetBGM(float volume) { audioMixer.SetFloat("BGM", VolumeToDB(volume)); UpdateDisplay(volume, bgm100, bgm10, bgm1); }
    public void SetSE(float volume) { audioMixer.SetFloat("SE", VolumeToDB(volume)); UpdateDisplay(volume, se100, se10, se1); }
    public void SetVOICE(float volume) { audioMixer.SetFloat("VOICE", VolumeToDB(volume)); UpdateDisplay(volume, voice100, voice10, voice1); }

    private float VolumeToDB(float volume)
    {
        if (volume <= float.Epsilon) return MIN_DB;
        return Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
    }

    void UpdateDisplay(float volume, Image img100, Image img10, Image img1)
    {
        if (img100 == null || img10 == null || img1 == null || numberSprites.Length < 10) return;
        int intValue = Mathf.RoundToInt(Mathf.Clamp(volume, 0f, 1f) * 100f);
        img100.sprite = numberSprites[intValue / 100];
        img10.sprite = numberSprites[(intValue % 100) / 10];
        img1.sprite = numberSprites[intValue % 10];
    }
}