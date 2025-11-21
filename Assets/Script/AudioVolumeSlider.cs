using UnityEngine;
using UnityEngine.UI;

public class AudioVolumeSlider : MonoBehaviour
{
    //[Header("スライダー設定")]
    //[SerializeField] Slider bgmSlider;
    //[SerializeField] Slider seSlider;

    //[Header("AudioManager (外部作成)")]
    //public AudioManager audioManager; // 他の方が作成中の AudioManager を参照

    //const string BGM_KEY = "BGM_VOLUME";
    //const string SE_KEY = "SE_VOLUME";

    //private void Start()
    //{
    //    // AudioManager参照チェック
    //    if (audioManager == null)
    //    {
    //        Debug.LogWarning("AudioManagerが未設定です。後でアタッチしてください。");
    //        return;
    //    }

    //    // スライダー初期値をロード
    //    float bgm = PlayerPrefs.GetFloat(BGM_KEY, 1.0f);
    //    float se = PlayerPrefs.GetFloat(SE_KEY, 1.0f);

    //    bgmSlider.value = bgm;
    //    seSlider.value = se;

    //    // AudioManager に反映
    //    audioManager.SetBGMVolume(bgm);
    //    audioManager.SetSEVolume(se);

    //    // スライダー操作イベント登録
    //    bgmSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
    //    seSlider.onValueChanged.AddListener(OnSEVolumeChanged);
    //}

    //private void OnBGMVolumeChanged(float value)
    //{
    //    if (audioManager == null) return;

    //    audioManager.SetBGMVolume(value);
    //    PlayerPrefs.SetFloat(BGM_KEY, value);
    //}

    //private void OnSEVolumeChanged(float value)
    //{
    //    if (audioManager == null) return;

    //    audioManager.SetSEVolume(value);
    //    PlayerPrefs.SetFloat(SE_KEY, value);
    //}
}
