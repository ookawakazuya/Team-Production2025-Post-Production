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
    [SerializeField] GameObject menuCanvas; // メニュー全体のキャンバス

    [Header("コントローラー設定")]
    [SerializeField] GameObject rightController;     //右手コントローラー
    [SerializeField] MonoBehaviour vrControllerScripts; //通常操作スクリプト

    [Header("依存コンポーネント")]

    [SerializeField] LineRenderer uiRay;                    //操作レイ
    [SerializeField] XRRayInteractor uiRayInteractor;    // メニュー操作専用のレイ
    [SerializeField] XRInteractorLineVisual uiLineVisual; // UIレイの可視化用
    [SerializeField] XRRayInteractor gameRayInteractor;   // ゲーム用レイ（通常照準）

    [SerializeField] VRMenuNavigator vrMenuNavigator; // メニュー階層管理スクリプト


    [Header("照準用レイ")]
    [SerializeField] XRRayInteractor gameRayinteractor;
    [SerializeField] XRInteractorLineVisual gameLineVisual; // ゲーム用レイの可視化用
    [SerializeField] VRController vrController; // VRController参照（視点制御を維持するため）

    [Header("その他の設定")]
    [SerializeField] float uiRayLength = 5.0f;                  //UIレイの長さ

    // 【削除】: 衝突回避設定 (VRControllerに一任するため削除)
    /*
    [Header("メニュー表示時の衝突回避設定")]
    [SerializeField] LayerMask wallLayer;
    [SerializeField] float menuCanvasDistance = 0.5f; 
    [SerializeField] float rotationSpeed = 5f;
    [SerializeField] Transform vrCameraTransform;
    private bool rotationLockEngaged = false;
    */

    VRHookActions inputActions;
    XRUIInputModule uiInputModule;

    bool isMenuOpen = false;
    public bool IsMenuOpen => isMenuOpen; // 外部から参照可能

    private void Awake()
    {
        inputActions = new VRHookActions();

        // --- LineRenderer 自動生成 ---
        if (uiRay == null)
        {
            uiRay = rightController.AddComponent<LineRenderer>();
            uiRay.positionCount = 2;
            uiRay.startWidth = 0.01f;
            uiRay.endWidth = 0.01f;
            uiRay.material = new Material(Shader.Find("Unlit/Color"));
            uiRay.startColor = Color.cyan;
            uiRay.endColor = Color.cyan;
        }
    }

    private void OnEnable()
    {
        inputActions.Enable();
        inputActions.VR.Menu.performed += OnMenuPressed;
    }

    private void OnDisable()
    {
        inputActions.VR.Menu.performed -= OnMenuPressed;
        inputActions.Disable();
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
        if (isMenuOpen && vrMenuNavigator != null)
        {
            vrMenuNavigator.GoBackToMain();
            Debug.Log("[VRMenuManager] メニューを開く際に最初のパネルにリセットしました。");
        }

        // 【修正なし】VRControllerに通知（衝突チェックと回転制限の指示）
        if (vrController != null)
        {
            vrController.SetMenuRotationState(isMenuOpen);
        }


        // VRControllerに通知（内部操作を止める or 再開）
        //    Debug.Log("コントローラーの停止");

        if (vrControllerScripts != null)
            vrControllerScripts.enabled = !isMenuOpen;

        //  ゲーム用レイをOFF、UI用レイをON 
        if (gameRayInteractor != null)
            gameRayInteractor.gameObject.SetActive(!isMenuOpen);
        if (gameLineVisual != null)
            gameLineVisual.gameObject.SetActive(!isMenuOpen);

        if (uiRayInteractor != null)
            uiRayInteractor.gameObject.SetActive(isMenuOpen);
        if (uiLineVisual != null)
            uiLineVisual.gameObject.SetActive(isMenuOpen);

        // 時間停止（必要なら）
        Time.timeScale = isMenuOpen ? 0f : 1f;

        Debug.Log(isMenuOpen ? "メニュー表示中：操作停止" : "メニュー終了：操作再開");
    }

    void UpdateUIRay()
    {
        if (uiRay == null || rightController == null) return;
        {

            //レイの開始位置と方向を右手のコントローラーから取得
            Vector3 start = rightController.transform.position;
            Vector3 direction = rightController.transform.forward;
            Vector3 end = start + direction * uiRayLength;

            if (uiRay.positionCount < 2)
                uiRay.positionCount = 2;

            //LineRendererの設定
            uiRay.SetPosition(0, start);
            uiRay.SetPosition(1, end);
        }
    }

    private void Update()
    {
        if (isMenuOpen)
        {
            uiRay.enabled = true;
            UpdateUIRay();
        }
        else
        {
            uiRay.enabled = false;
        }
    }

    // 【削除】: IsMenuCollidingWithWall(), ForciblyRotateToSafeDirection(), ApplyRotationLock() の各メソッドを削除
    // これらのメソッドは VRController.cs に移管されました。
}