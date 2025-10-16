using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject optionPanel;
    public GameObject controlGuidePanel;

    [Header("UI Elements")]
    [SerializeField] CanvasGroup menuCanvas;
    [SerializeField] XRRayInteractor rightHandRay;
    [SerializeField] Slider masterSlider;       //音量
    [SerializeField] Slider bgmSlider;          //BGM
    [SerializeField] Slider sfxsSlider;         //効果音
    [SerializeField] Slider voiceSlider;        //音声
    [SerializeField] Slider rotationSpeedSlider;//視点回転速度
    [SerializeField] Button instructionButton;
    [SerializeField] Button restartButton;
    [SerializeField] Button titleButton;
    [SerializeField] Image instructionImage;


    [Header("Audio Mixers (任意)")]
    [SerializeField] AudioSource bgmSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource voiceSource;

    [Header("VR Input")]
    [SerializeField] VRController vrController;
    public VRHookActions inputActions;

    bool isMenuOpen = false;

    private void Awake()
    {
        inputActions = new VRHookActions();
        // 初期非表示設定
        menuCanvas.alpha = 0f;
        menuCanvas.interactable = false;
        menuCanvas.blocksRaycasts = false;
        instructionImage.gameObject.SetActive(false);

        //スライダー設定
        if (masterSlider) masterSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        if (bgmSlider) bgmSlider.onValueChanged.AddListener(OnBGMChanged);
        if (sfxsSlider) sfxsSlider.onValueChanged.AddListener(OnSFXChanged);
        if (voiceSlider) voiceSlider.onValueChanged.AddListener(OnVoiceChanged);
        if (rotationSpeedSlider) rotationSpeedSlider.onValueChanged.AddListener(OnRotationSpeedChanged);

        // ボタンイベント登録
        if (instructionButton) instructionButton.onClick.AddListener(OnInstructionToggle);
        if (restartButton) restartButton.onClick.AddListener(OnRestartStage);
        if (titleButton) titleButton.onClick.AddListener(OnGoToTitle);

    }

    void OnEnable() => inputActions.Enable();
     void OnDisable() => inputActions.Disable();

    void Update()
    {
        if (inputActions.Menu.menuButton.triggered)
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;
        menuCanvas.alpha = isMenuOpen ? 1f : 0f;
        menuCanvas.interactable = isMenuOpen;
        menuCanvas.blocksRaycasts = isMenuOpen;

        if (rightHandRay != null)
            rightHandRay.enabled = isMenuOpen;

        // メニューを開いた時に現在値をUIに反映
        if (isMenuOpen && vrController != null && rotationSpeedSlider != null)
        {
            rotationSpeedSlider.value = vrController.rotationSpeed;
        }

        // ゲームの一時停止
        Time.timeScale = isMenuOpen ? 0f : 1f;
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioListener.volume = value / 100.0f;
    }

    public void OnBGMChanged(float value)
    {
        if (bgmSource) bgmSource.volume = value / 100.0f;
    }

    public void OnSFXChanged(float value)
    {
        if (sfxSource) sfxSource.volume = value / 100.0f;
    }

    public void OnVoiceChanged(float value)
    {
        if (voiceSource) voiceSource.volume = value / 100.0f;
    }
    public void OnRotationSpeedChanged(float value)
    {
        if (vrController != null)
        {
            vrController.rotationSpeed = value;
        }
    }

    public void OnInstructionToggle()
    {
        instructionImage.gameObject.SetActive(!instructionImage.gameObject.activeSelf);
    }

    public void OnRestartStage()
    {
        Debug.Log("未実装の機能(ステージ初めから再開)");
    }

    public void OnGoToTitle()
    {
        SceneManager.LoadScene("TitkeScene");
    }
}
