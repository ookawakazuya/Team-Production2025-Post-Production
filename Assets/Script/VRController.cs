using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static VRHookActions;

public class VRController : MonoBehaviour
{
    [SerializeField]  VRMenuManager menuManager;

    [Header("XR Controllers")]
    [SerializeField] GameObject rightController;
    [SerializeField] GameObject leftController;

    [Header("フック関連")]
    [SerializeField] Camera mainCamera;
    [SerializeField] CharacterController characterController;

    [SerializeField] float maxWireLength = 300f; // レイの最大距離
    [SerializeField] float maxMoveSpeed = 30f;   // 最大巻き取り速度
    [SerializeField] float acceleration = 20f;   // 加速度
    [SerializeField] float stopDistance = 1f;    // 停止判定距離

    [Header("LineRenderer設定")]
    [SerializeField] LineRenderer hookLine; // ワイヤーレイ
    [SerializeField] LineRenderer aimLine;  // 照準レイ
    // 内部状態変数

    bool isGrappling = false;   // フック発射中
    bool isRetracting = false;  // 巻き取り中
    bool isClinging = false;    // 壁張り付き中
    bool isGripCooldown = false;
    Vector3 grapplePoint;       // 命中地点
    GameObject grappledObject;

    bool inputLocked = false;    // ワイヤー移動中の操作ロック
    bool allowCameraOnly = false; // カメラ操作のみ許可フラグ

    [Header("マーカー設定")]
    [SerializeField] XRRayInteractor rayInteractor;
    [SerializeField] XRInteractorLineVisual lineVisual;
    [SerializeField] GameObject markerPrefab;
    private GameObject aimMarkerInstance;
    private GameObject hookMarkerInstance;

    [Header("壁張り付き設定")]
    [SerializeField] float clingDuration = 5f; // 壁にとどまれる時間
    float clingTimer = 0f;


    [Header("重力設定")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float maxFallSpeed = -50.0f;
    [SerializeField] float fallSpeed = 0f;
    bool useGravity = true; // ← 張り付き中などで無効化する用

    [Header("視点移動設定")]
    [SerializeField] public float  rotationSpeed = 45f; // 回転速度
    [SerializeField] Transform playerRoot;      // プレイヤーの角度操作対象

    [Header("タグ設定")]
    [SerializeField] string wallTag = "Wall"; // 壁タグ設定

    float currentSpeed = 0f; // 現在の巻き取り速度

    public bool IsRetracting => isRetracting;
    public bool IsClinging => isClinging;

    public VRHookActions HookMap;
    public VRHookActions UIMap;

    bool isMenuOpen = false;



    // 初期化
    void Awake()
    {
        HookMap = new VRHookActions();
        //UIMap = new VRHookActions();

        // LineRendererの自動設定
        if (hookLine == null)
        {
            hookLine = gameObject.AddComponent<LineRenderer>();
            hookLine.startWidth = 0.02f;
            hookLine.endWidth = 0.02f;
            hookLine.material = new Material(Shader.Find("Sprites/Default"));
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
            hookLine.enabled = false;
        }


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
            aimLine.enabled = true;
        }


        // マーカー生成
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
    private void OnDisable()
    {
        HookMap.Disable();
    }

    public void SetMenuState(bool state)
    {
        isMenuOpen = state;
    }

    // 更新処理
    void Update()
    {
        if (isMenuOpen) return;
            //  常時視点操作は有効
            CameraRotation();
                //  入力取得
        bool triggerPressed = HookMap.VR.HookShoot.ReadValue<float>() > 0.5f;
        bool gripPressed = HookMap.VR.Retract.ReadValue<float>() > 0.5f;
        bool cancelPressed = HookMap.VR.Cancel.ReadValue<float>() > 0.5f;

        //  入力ロック：ワイヤー移動中は他の操作をブロック
        if (inputLocked)
        {
            if (isRetracting)
            {
                AccelerateTowardsHook(); // ← トリガー放しても巻き取り継続
                UpdateHookLine();
            }
            else
            {
                UpdateAimLine();
            }
            return; // 他の入力は一切処理しない
        }

        // 壁張り付き処理
        if (isClinging)
        {
            
            if (gripPressed)
            {
                Debug.Log("張り付き：移動開始");
                isClinging = false;
                useGravity = true;
                UpdateAimLine();
                return;
            }
            if (triggerPressed && !isRetracting && isGrappling)
            {
                Debug.Log("張り付き　フック解除");                 
                ShootHook();
                return;
            }
            clingTimer -= Time.deltaTime;
  
            if (clingTimer <= 0f)
            {
                Debug.Log("張り付き解除 → 落下開始");
                isClinging = false;
                useGravity = true;
                ReleaseHook();
                return;
            }

            UpdateAimLine();

            return;
        }

        //  通常のフック処理

        if (triggerPressed)
        {
            if (!isGrappling && !isRetracting)
                ShootHook();

            if (isGrappling && gripPressed && !isRetracting)
                StartRetract(); // トリガー→グリップで移動開始
        }
        else
        {
            // トリガー解除でフックを解除（張り付き時以外）
            if ((isGrappling || isRetracting) && !isClinging)
                ReleaseHook();
        }

        //  通常移動・重力
        if (isRetracting)
            AccelerateTowardsHook();
        else
            ApplyGravity();

        //  レイ描画更新

        if (isGrappling)
            UpdateHookLine();
        else
            UpdateAimLine();
    }

    //  視点回転
    void CameraRotation()
    {
        Vector2 stick = HookMap.VR.RightStick.ReadValue<Vector2>();
        bool stickPressed = HookMap.VR.RightStickPress.ReadValue<bool>();

        if (Mathf.Abs(stick.x) > 0.2f)
            playerRoot.Rotate(Vector3.up * stick.x * rotationSpeed * Time.deltaTime);

        if (stickPressed)
        {
            Vector3 euler = playerRoot.eulerAngles;
            euler.y = 0f;
            playerRoot.eulerAngles = euler;
        }
    }


    //  巻き取り開始
    void StartRetract()
    {
        if(isRetracting)
        Debug.Log("巻き取り開始");
        isRetracting = true;
        isGrappling = true;
        useGravity = false;
        currentSpeed = 0f;

        inputLocked = true; // ← 移動中は操作ロック（視点のみ許可）
        allowCameraOnly = true;
    }

    //  フック射出
    void ShootHook()
    {
        Debug.Log("フック射出");
        Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            grapplePoint = hit.point;
            grappledObject = hit.collider.gameObject;
            isGrappling = true;

            aimLine.enabled = false;
            hookLine.enabled = true;
            aimMarkerInstance.SetActive(false);
            hookMarkerInstance.SetActive(true);
            Debug.Log($"命中: {hit.collider.name}");
        }
        else
        {
            Debug.Log("未命中");
        }
    }

    //  巻き取り処理
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
            if (grappledObject != null && grappledObject.CompareTag(wallTag))
                StartCling();
            else
                ReleaseHook();
        }
    }

    //  重力処理
    void ApplyGravity()
    {
        if (!useGravity) return; // ← ★張り付き中は無効化
        if (characterController.isGrounded)
            fallSpeed = 0f;
        else
        {
            fallSpeed += gravity * Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, maxFallSpeed);
            characterController.Move(new Vector3(0, fallSpeed, 0) * Time.deltaTime);
        }
    }

    //  レイ更新系
    void UpdateHookLine()
    {
        hookLine.SetPosition(0, rightController.transform.position);
        hookLine.SetPosition(1, grapplePoint);
        hookMarkerInstance.transform.position = grapplePoint;
    }

    void UpdateAimLine()
    {
        Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);
        Vector3 endPoint = ray.origin + ray.direction * maxWireLength;
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
            endPoint = hit.point;

        aimLine.SetPosition(0, ray.origin);
        aimLine.SetPosition(1, endPoint);
        aimMarkerInstance.transform.position = endPoint;
    }

    //  壁張り付き
    void StartCling()
    {
        Debug.Log("壁に張り付いた");
        isRetracting = false;
        isGrappling = true;
        isClinging = true;
        useGravity = false;
        clingTimer = clingDuration;

        // レイ切替
        hookLine.enabled = false;
        aimLine.enabled = true;
        hookMarkerInstance.SetActive(false);
        aimMarkerInstance.SetActive(true);

        // 操作ロック解除
        inputLocked = false;
        allowCameraOnly = false;
    }

    //  フック解除
    void ReleaseHook()
    {
        Debug.Log("フック解除");
        isGrappling = false;
        isRetracting = false;
        useGravity = true;

        inputLocked = false;
        allowCameraOnly = false;

        hookLine.enabled = false;
        aimLine.enabled = true;
        hookMarkerInstance.SetActive(false);
        aimMarkerInstance.SetActive(true);
    }
}
