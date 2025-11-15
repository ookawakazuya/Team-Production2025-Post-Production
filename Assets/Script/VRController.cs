using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static VRHookActions;

public class VRController : MonoBehaviour
{
    [Header("XR Controllers")]
    [SerializeField] GameObject rightController;   // 右手コントローラー
    [SerializeField] GameObject leftController;    // 左手コントローラー

    [Header("フック関連設定")]
    [SerializeField] Camera mainCamera;
    [SerializeField] CharacterController characterController;

    [SerializeField] float maxWireLength = 300f;   // レイの最大距離
    [SerializeField] float maxMoveSpeed = 30f;     // 最大移動速度
    [SerializeField] float acceleration = 20f;     // 加速度
    [SerializeField] float stopDistance = 1f;      // フック地点停止距離

    [Header("LineRenderer 設定")]
    [SerializeField] Transform rayOrigin;           //レイの基準点
    [SerializeField] LineRenderer hookLine;        // フック（ワイヤー）描画用
    [SerializeField] LineRenderer aimLine;         // 照準用の描画ライン

    [Header("マーカー設定")]
    [SerializeField] XRRayInteractor rayInteractor;
    [SerializeField] XRInteractorLineVisual lineVisual;
    [SerializeField] GameObject markerPrefab;      // マーカーのプレハブ
    private GameObject aimMarkerInstance;          // 照準レイの終点マーカー
    private GameObject hookMarkerInstance;         // ワイヤーの終点マーカー

    [Header("壁張り付き設定")]
    [SerializeField] float clingDuration = 5f;     // 壁に留まれる最大時間
    bool isClinging = false;                       // 現在壁に張り付き中か
    float clingTimer = 0f;                         // 壁滞在タイマー
    GameObject grappledObject;                     // 命中したオブジェクトの参照

    [Header("重力設定")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float maxFallSpeed = -50.0f;
    [SerializeField] float fallSpeed = 0f;

    [Header("視点移動設定")]
    [SerializeField] float rotationSpeed = 45f;    // 視点回転速度
    [SerializeField] Transform playerRoot;         // プレイヤー本体のTransform
    bool stickPressed;

    // 内部制御フラグ
    bool isGrappling = false;      // フック発射中か
    bool isRetracting = false;     // ワイヤー巻取り中か
    bool wasGripPressed = false;   // 前フレームのグリップ状態
    float currentSpeed = 0f;       // 現在の移動速度
    Vector3 grapplePoint;          // フックの到達座標

    public VRHookActions HookMap;
    [SerializeField] string wallTag = "Wall";      // 壁判定用タグ

    public bool IsRetracting => isRetracting; // ワイヤー巻取り中か？
    public bool IsClinging => isClinging;     // 壁に張り付き中か？

    void Awake()
    {
        HookMap = new VRHookActions();

        // --- LineRenderer初期化 ---
        if (hookLine == null)
        {
            hookLine = gameObject.AddComponent<LineRenderer>();
            hookLine.startWidth = 0.02f;
            hookLine.endWidth = 0.02f;
            hookLine.material = new Material(Shader.Find("Sprites/Default"));
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
        }
        hookLine.enabled = false;

        if (aimLine == null)
        {
            GameObject aimObj = new GameObject("AimLine");
            aimObj.transform.SetParent(transform);
            aimLine = aimObj.AddComponent<LineRenderer>();
            aimLine.startWidth = 0.01f;
            aimLine.endWidth = 0.01f;
            aimLine.material = new Material(Shader.Find("Sprites/Default"));
            aimLine.startColor = Color.green;
            aimLine.endColor = Color.green;
        }
        aimLine.enabled = true;

        // --- マーカー初期化 ---
        if (markerPrefab == null)
        {
            markerPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerPrefab.transform.localScale = Vector3.one * 0.1f;
            Destroy(markerPrefab.GetComponent<Collider>());
            var renderer = markerPrefab.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = new Color(1f, 0f, 0f, 0.5f);
        }

        aimMarkerInstance = Instantiate(markerPrefab);
        aimMarkerInstance.name = "AimMarker";
        aimMarkerInstance.SetActive(true);

        hookMarkerInstance = Instantiate(markerPrefab);
        hookMarkerInstance.name = "HookMarker";
        hookMarkerInstance.SetActive(false);
    }

    private void OnEnable() => HookMap.Enable();
    private void OnDisable() => HookMap.Disable();

    void Update()
    {
    // 入力の取得
        bool triggerPressed = HookMap.VR.HookShoot.ReadValue<float>() > 0.5f;
        bool gripPressed = HookMap.VR.Retract.ReadValue<float>() > 0.5f;
        CameraRotation();

        // --- 壁張り付き中 ---
        if (isClinging)
        {
            fallSpeed = 0f;
            clingTimer -= Time.deltaTime;

            if (clingTimer <= 0f)
            {
                Debug.Log("張り付き解除 → 落下開始");
                isClinging = false;
                ReleaseHook();
            }

            // トリガーで新しいフック射出
            if (triggerPressed && !isRetracting && !isGrappling)
                ShootHook();

            // グリップで張り付き解除
            if (gripPressed)
            {
                Debug.Log("グリップで張り付き解除");
                isClinging = false;
                ReleaseHook();
            }

            aimLine.enabled = true;
            UpdateAimLine();
            return;
        }

        // --- 通常のフック処理 ---
        if (triggerPressed && !isRetracting && !isGrappling)
        {
            ShootHook();
        }

        // グリップ押下で移動開始（トリガー状態は関係なし）
        if (isGrappling && gripPressed && !isRetracting)
        {
            StartRetract();
        }

        // トリガーを放しても、巻き取り中は解除しない
        if (!triggerPressed && !isClinging)
        {
            if (isGrappling && !isRetracting)
                ReleaseHook();
        }

        // --- 移動・落下 ---
        if (isRetracting)
            AccelerateTowardsHook();
        else
            ApplyGravity();

        // --- レイ描画 ---
        if (isGrappling)
            UpdateHookLine();
        else
            UpdateAimLine();
    }

    // -------------------------------
    // 視点回転処理
    // -------------------------------
    void CameraRotation()
    {
        Vector2 rightStickInput = HookMap.VR.RightStick.ReadValue<Vector2>();

        if (Mathf.Abs(rightStickInput.x) > 0.2f)
            playerRoot.Rotate(Vector3.up * rightStickInput.x * rotationSpeed * Time.deltaTime);

        bool stickPressed = HookMap.VR.RightStickPress.ReadValue<bool>();
        if (stickPressed)
        {
            Vector3 euler = playerRoot.eulerAngles;
            euler.y = 0f;
            playerRoot.eulerAngles = euler;
        }
    }

    // -------------------------------
    // 重力適用処理
    // -------------------------------
    void ApplyGravity()
    {
        if (characterController.isGrounded)
        {
            fallSpeed = 0f;
        }
        else
        {
            fallSpeed += gravity * Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, maxFallSpeed);
            characterController.Move(new Vector3(0, fallSpeed, 0) * Time.deltaTime);
        }
    }

    // -------------------------------
    // フック発射処理
    // -------------------------------
    void ShootHook()
    {
        Debug.Log("フック射出");
        Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            grapplePoint = hit.point;
            grappledObject = hit.collider.gameObject;
            isGrappling = true;

            // レイ切り替え
            aimLine.enabled = false;
            hookLine.enabled = true;
            aimMarkerInstance.SetActive(false);
            hookMarkerInstance.SetActive(true);

            Debug.Log($"フック命中: {hit.collider.name}");
        }
        else
        {
            Debug.Log("フック未命中");
        }
    }

    // -------------------------------
    // ワイヤー巻取り開始
    // -------------------------------
    void StartRetract()
    {
        if (isGrappling)
        {
            Debug.Log("巻き取り開始");
            isRetracting = true;
            currentSpeed = 0f;
        }
    }

    // -------------------------------
    // フック到達点へ加速移動
    // -------------------------------
    void AccelerateTowardsHook()
    {
        Vector3 direction = grapplePoint - transform.position;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);
            characterController.Move(direction.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            Debug.Log("フック地点に到達");
            if (grappledObject != null && grappledObject.CompareTag(wallTag))
                StartCling();
            else
                ReleaseHook();
        }
    }

    // -------------------------------
    // 張り付き開始
    // -------------------------------
    void StartCling()
    {
        Debug.Log("壁に張り付いた");
        isRetracting = false;
        isClinging = true;
        clingTimer = clingDuration;

        // 重力を無効化
        fallSpeed = 0f;

        // レイ切り替え
        hookLine.enabled = false;
        aimLine.enabled = true;
        hookMarkerInstance.SetActive(false);
        aimMarkerInstance.SetActive(true);
    }

    // -------------------------------
    // ワイヤー解除処理
    // -------------------------------
    void ReleaseHook()
    {
        Debug.Log("フック解除");

        // 張り付き中なら照準レイを維持
        if (isClinging)
        {
            aimLine.enabled = true;
            aimMarkerInstance.SetActive(true);
            hookLine.enabled = false;
            hookMarkerInstance.SetActive(false);
            return; // ← 張り付き中は他の状態を変えない
        }

        // 通常の解除処理
        isGrappling = false;
        isRetracting = false;
        grappledObject = null;
        hookLine.enabled = false;
        aimLine.enabled = true;
        hookMarkerInstance.SetActive(false);
        aimMarkerInstance.SetActive(true);
    }
    /// <summary>
    /// 外部からゲームレイをON/OFFする
    /// </summary>
    /// <param name="enable"></param>
    public void EnableGameRay(bool enable)
    {
        aimLine.enabled = enable;
        if (aimMarkerInstance != null) { 
        aimMarkerInstance.SetActive(enable);
        }
    }

    /// <summary>
    /// 外部からUIレイをON/OFFにする
    /// </summary>
    /// <param name="enable"></param>
    public void EnableUIRay(bool enable)
    {
        // XRRayInteractor側でVisibilityを操作する想定
        rayInteractor.enabled = enable;
        lineVisual.enabled = enable;
    }

    // -------------------------------
    // レイ描画更新
    // -------------------------------
    void UpdateHookLine()
    {
        if (!hookLine.enabled) return;

        if (rayOrigin == null)
            rayOrigin = rightController.transform;

        hookLine.SetPosition(0, rayOrigin.position);
        hookLine.SetPosition(1, grapplePoint);

        hookMarkerInstance.transform.position = grapplePoint;
        hookMarkerInstance.transform.rotation =
            Quaternion.LookRotation((transform.position - grapplePoint).normalized);
    }

    void UpdateAimLine()
    {
        if (rayOrigin == null)
            rayOrigin = rightController.transform;   // 念のため保険

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        Vector3 endPoint = ray.origin + ray.direction * maxWireLength;
        Vector3 normal = -ray.direction;

        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            endPoint = hit.point;
            normal = hit.normal;
        }

        // LineRenderer 更新
        aimLine.SetPosition(0, rayOrigin.position);
        aimLine.SetPosition(1, endPoint);

        // マーカー更新
        aimMarkerInstance.transform.position = endPoint;
        aimMarkerInstance.transform.rotation = Quaternion.LookRotation(normal);
    }
}
