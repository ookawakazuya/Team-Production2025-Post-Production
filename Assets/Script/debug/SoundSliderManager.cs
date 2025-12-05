/*using UnityEngine;
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

    private const string MASTER = "Master";
    private const string BGM = "BGM";
    private const string SE = "SE";
    private const string VOICE = "Voice";

    private void Update()
    {
       // audioMixer.SetFloat(MASTER, -80f);
        init(masterslider, MASTER);
        init(bgmslider, BGM);
        init(seslider, SE);
        init(voiceslider, VOICE);
    }

    public void Init(Slider slider, string param)
    {
        if (slider == null) return;

        slider.minValue = 0.0001f;
        slider.maxValue = 1f;
        slider.value = 1f;

        slider.onValueChanged.AddListener(value =>
        {
            audioMixer.SetFloat(param, Mathf.Log10(value) * 20f);
        });

        // èâä˙ílÇìKóp
        audioMixer.SetFloat(param, Mathf.Log10(slider.value) * 20f);
    }

    public void SliderUpdate()
    {
        audioMixer.SetFloat(MASTER, Mathf.Log10(masterSlider.value) * 20f);
        audioMixer.SetFloat(BGM, Mathf.Log10(bgmSlider.value) * 20f);
        audioMixer.SetFloat(SE, Mathf.Log10(seSlider.value) * 20f);
        audioMixer.SetFloat(VOICE, Mathf.Log10(voiceSlider.value) * 20f);
    }
}*/

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

    private void Start()
    {
        audioMixer.GetFloat("Master", out float Maset);
        masterSlider.value = Maset;

        audioMixer.GetFloat("BGM", out float Bgm);
        masterSlider.value = Bgm;

        audioMixer.GetFloat("SE", out float Se);
        masterSlider.value = Se;

        audioMixer.GetFloat("Voice", out float Voice);
        masterSlider.value = Voice;
    }

    public void SetMaster(float volume)
    {
        audioMixer.SetFloat("Master",volume);
    }

    public void SetBGM(float volume)
    {
        audioMixer.SetFloat("BGM", volume);
    }

    public void SetSE(float volume)
    {
        audioMixer.SetFloat("SE", volume);
    }
    public void SetVOICE(float volume)
    {
        audioMixer.SetFloat("VOICE", volume);
    }
}