using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.UI;

public class VRMenuManager : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private GameObject menuCanvas; // メニュー全体のキャンバス

    [Header("コントローラー設定")]
    [SerializeField] GameObject rightController;     //右手コントローラー
    [SerializeField] MonoBehaviour vrControllerScripts; //通常操作スクリプト

    [Header("依存コンポーネント")]
    [SerializeField] private VRController vrController; // VRController参照（視点制御を維持するため）

    VRHookActions inputActions;
    XRUIInputModule uiInputModule;

    [SerializeField] XRRayInteractor uiRayInteractor;
    [SerializeField] XRInteractorLineVisual uiLineVisual;

    private bool isMenuOpen = false;
    public bool IsMenuOpen => isMenuOpen; // 外部から参照可能

    private void Awake()
    {
        inputActions = new VRHookActions();
        uiInputModule = new XRUIInputModule();
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.VR.Menu.performed += OnMenuPressed;
        uiInputModule.Enable();
    }

    private void OnDisable()
    {
        inputActions.VR.Menu.performed -= OnMenuPressed;
        inputActions.Disable();
        uiInputModule.Disable();
    }

    private void OnMenuPressed(InputAction.CallbackContext ctx)
    {
        ToggleMenu();
    }

    /// <summary>
    /// メニューの開閉をトグル
    /// </summary>
    private void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        // メニューキャンバスを表示 / 非表示
        if (menuCanvas != null)
            menuCanvas.SetActive(isMenuOpen);

        // VRControllerに通知（内部操作を止める or 再開）
        //    Debug.Log("コントローラーの停止");

        if (vrControllerScripts != null)
            vrControllerScripts.enabled = !isMenuOpen;

        //  UI操作用レイをメニュー中だけ有効化
        if (uiRayInteractor != null)
            uiRayInteractor.gameObject.SetActive(isMenuOpen);

        if (uiLineVisual != null)
            uiLineVisual.gameObject.SetActive(isMenuOpen);

        // 時間停止（必要なら）
        Time.timeScale = isMenuOpen ? 0f : 1f;

        Debug.Log(isMenuOpen ? "メニュー表示中：操作停止" : "メニュー終了：操作再開");
    } 
}
