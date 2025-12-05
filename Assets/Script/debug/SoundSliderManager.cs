using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSliderManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private Slider voiceSlider;

    private const string MASTER = "MasterVolume";
    private const string BGM = "BGMVolume";
    private const string SE = "SEVolume";
    private const string VOICE = "VoiceVolume";

    private void Start()
    {
        Init(masterSlider, MASTER);
        Init(bgmSlider, BGM);
        Init(seSlider, SE);
        Init(voiceSlider, VOICE);
    }

    private void Init(Slider slider, string param)
    {
        if (slider == null) return;

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = 1f;

        slider.onValueChanged.AddListener(value =>
        {
            audioMixer.SetFloat(param, Mathf.Log10(value) * 20f);
        });

        // ‰Šú’l‚ğ“K—p
        audioMixer.SetFloat(param, Mathf.Log10(slider.value) * 20f);
    }
}
