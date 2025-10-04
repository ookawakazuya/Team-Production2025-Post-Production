using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using static VRHookActions;

public class VRController : MonoBehaviour
{
    [Header("XR Controllers")]
    [SerializeField] GameObject rightController;
    [SerializeField] GameObject leftController;

    [Header("フック関連")]
    [SerializeField] Camera mainCamera;
    [SerializeField] CharacterController characterController;

    [SerializeField] float maxWireLength = 300f;//レイの長さ
    [SerializeField] float maxMoveSpeed = 30f;//最大速度
    [SerializeField] float acceleration = 20f;//加速度
    [SerializeField] float stopDistance = 1f;//停止判定の距離


    [Header("LineRendererの設定")]
    [SerializeField] LineRenderer hookLine;//ワイヤーレイ
    [SerializeField] LineRenderer aimLine;//照準用


    bool isGrappling = false;
    bool isRetracting = false;
    bool wasgripPressed = false;//前フレームの状態
    Vector3 grapplePoint;
    public bool isMoving {  get; private set; }


    [Header("マーカ設定")]
    [SerializeField] XRRayInteractor rayInteractor;
    [SerializeField] XRInteractorLineVisual lineVisual;
    [SerializeField] GameObject markerPrefab;//カーソルプレハブ
    private GameObject aimMarkerInstance;//照準用
    private GameObject hookMarkerInstance;//フック用

    [Header("壁張り付き設定")]
    [SerializeField] float clingDuration = 5f;  //壁に留まれる時間
    bool isClinging = false;    //到達したか
    float clingTimer = 0f;      //落下タイマー
    GameObject grappledObject;  //命中したオブジェクト
    

    [Header("重力設定")]
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float maxFallSpeed = -50.0f;
    [SerializeField] float fallSpeed = 0f;

    bool useGravity = true;

    [Header("視点移動設定")]
    [SerializeField] float rotationSpeed = 90f; //回転速度
    [SerializeField] Transform playerRoot;      //プレイヤーの角度
    bool stickPressed;



    float currentSpeed = 0f;//現在の移動速度

    public VRHookActions HookMap;

    [SerializeField] string wallTag;

    void Awake()
    {
        HookMap = new VRHookActions();
        // LineRendererの初期設定（インスペクタでセットしていない場合、自動追加）
        if (hookLine == null)
        {
            hookLine = gameObject.AddComponent<LineRenderer>();
            hookLine.startWidth = 0.02f;
            hookLine.endWidth = 0.02f;
            hookLine.material = new Material(Shader.Find("Sprites/Default"));
            hookLine.startColor = Color.white;
            hookLine.endColor = Color.white;
        }
        hookLine.enabled = false; // 初期は非表示
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
        aimLine.enabled = true; // 初期は非表示

        if (lineVisual != null && markerPrefab != null)
        {
            lineVisual.reticle = markerPrefab; // 終端にマーカーを出す
        }

        if (rayInteractor != null) rayInteractor.enabled = true;
        if (lineVisual != null) lineVisual.enabled = true;


        // マーカー生成（プレハブ未指定なら自動生成）
        if (markerPrefab == null)
        {
            markerPrefab = GameObject.CreatePrimitive(PrimitiveType.Quad);
            markerPrefab.transform.localScale = Vector3.one * 0.1f;
            Destroy(markerPrefab.GetComponent<Collider>());
            var renderer = markerPrefab.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Unlit/Color"));
            renderer.material.color = new Color(1f, 0f, 0f, 0.5f);
        }

        // Aim 用マーカー
        aimMarkerInstance = Instantiate(markerPrefab);
        aimMarkerInstance.name = "AimMarker";
        aimMarkerInstance.SetActive(true);

        // Hook 用マーカー
        hookMarkerInstance = Instantiate(markerPrefab);
        hookMarkerInstance.name = "HookMarker";
        hookMarkerInstance.SetActive(false);

    }
    private void OnEnable() => HookMap.Enable();
    void OnDisable() => HookMap.Disable();

    private void Update()
    {
        bool triggerPressed = HookMap.VR.HookShoot.ReadValue<float>() > 0.5f;
        bool gripPressed = HookMap.VR.Retract.ReadValue<float>() > 0.5f;
        CameraRotation();
        stickPressed = HookMap.VR.RightStickPress.ReadValue<float>() > 0.5f;
        // グリップ先行中のブロック
        bool blockTriggerByGrip = gripPressed && !isGrappling && !isRetracting;

        // ---------------- 張り付き中の処理 ----------------
        if (isClinging)
        {
            clingTimer -= Time.deltaTime;
            if (clingTimer <= 0f)
            {
                Debug.Log("張り付き解除→落下開始");
                isClinging = false;
                isMoving = true;
                ReleaseHook();
            }

            // 張り付き中も照準レイ常時更新
            UpdateAimLine();

            // ★ 張り付き中でもフック射出を許可（グリップ先行時は無効）
            if (triggerPressed && !blockTriggerByGrip)
            {
                // 新しいフックを撃つ
                if (!isGrappling && !isRetracting)
                    ShootHook();

                // フック中にグリップ押下で巻き取り開始
                if (isGrappling && gripPressed && !isRetracting)
                    StartRetract();
            }
            else if (!triggerPressed && (isGrappling || isRetracting))
            {
                // トリガーを放したら解除
                ReleaseHook();
            }

            // ★ 張り付き中でも巻き取り中は移動処理を継続
            if (isRetracting)
            {
                AccelerateTowardsHook();
            }

            return; // 張り付き中は重力落下だけ止める
        }

        // ---------------- 通常時の処理 ----------------
        if (triggerPressed && !blockTriggerByGrip)
        {
            if (!isGrappling && !isRetracting)
                ShootHook();

            if (isGrappling && gripPressed && !isRetracting)
                StartRetract();
        }
        else if (!triggerPressed)
        {
            if ((isGrappling || isRetracting) && !isClinging)
                ReleaseHook();
        }

        // 通常移動／落下処理
        if (isRetracting)
            AccelerateTowardsHook();
        else
            ApplyGravity();

        // レイ描画
        if (isGrappling) UpdateHookLine();
        else UpdateAimLine();
    }

    void CameraRotation()
    {
        Vector2 rightStickInput = HookMap.VR.RightStick.ReadValue<Vector2>();

        // 横方向に倒していたらカメラ回転
        if (Mathf.Abs(rightStickInput.x) > 0.2f)
        {
            Debug.Log($"横方向の入力:{rightStickInput}");
            playerRoot.Rotate(Vector3.up * rightStickInput.x * rotationSpeed * Time.deltaTime);

        }

        bool stickPressed = HookMap.VR.RightStickPress.ReadValue<bool>();
        if (stickPressed)
        {
            Debug.Log("Y軸をリセット！");
            Vector3 forward = mainCamera.transform.forward;
            forward.y = 0f; // カメラの向きに合わせてY軸を修正
            if (forward.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(forward);
                playerRoot.rotation = targetRotation; // カメラの向きに合わせて回転
            }
        }
    }


    void ApplyGravity()
    {
        if (!useGravity) return;

        if (characterController.isGrounded)
        {
            fallSpeed = 0f;
        }
        else
        {
            fallSpeed += gravity * Time.deltaTime;//自由落下加速
            fallSpeed = Mathf.Max(fallSpeed, maxFallSpeed);//最大落下速度制限
            characterController.Move(new Vector3(0, fallSpeed, 0) * Time.deltaTime);
        }
    }



    void ShootHook()
    {
        if (isClinging)
        {
            isClinging = false;
            clingTimer = 0f;
        }

        Debug.Log("フック射出");
        Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            grapplePoint = hit.point;
            grappledObject = hit.collider.gameObject;//命中したオブジェクトの保存
            isGrappling = true;
            // ライン開始
            aimLine.enabled = false;
            hookLine.enabled = true;
            aimMarkerInstance.SetActive(false);
            hookMarkerInstance.SetActive(true);
            Debug.Log($"フックショット命中:{hit.collider.name}");
        }
        else
        {
            Debug.Log("フック未明中");
        }
    }

    void UpdateHookLine()
    {
        if (hookLine.enabled)
        {
            hookLine.SetPosition(0, rightController.transform.position);
            hookLine.SetPosition(1, grapplePoint);

            if(hookMarkerInstance != null)
            {
                hookMarkerInstance.SetActive(true);
                hookMarkerInstance.transform.position = grapplePoint;
                hookMarkerInstance.transform.rotation =
                    Quaternion.LookRotation((transform.position - grapplePoint).normalized);
            }

        }
    }
    void UpdateAimLine()
    {
        Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);
        Vector3 endPoint = ray.origin + ray.direction * maxWireLength;
        Vector3 normal = -ray.direction;

        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            endPoint = hit.point; // ヒットしたらそこまで
            normal = hit.normal;
        }

        // レイの描画（もしXRInteractorLineVisualを使うなら不要）
        aimLine.SetPosition(0, rightController.transform.position);
        aimLine.SetPosition(1, endPoint);

        // マーカー更新
        if (aimMarkerInstance != null)
        {
            aimMarkerInstance.SetActive(true);
            aimMarkerInstance.transform.position = endPoint; 
            aimMarkerInstance.transform.rotation = Quaternion.LookRotation(normal);
        }
    }
    void StartRetract()
    {
        if (isGrappling)
        {
            Debug.Log("巻き取り開始");
            isRetracting = true;
            isMoving = false;
            useGravity = true;
            currentSpeed = 0f;
        }
    }

    void AccelerateTowardsHook()
    {
        if (isClinging) return;
        Vector3 direction = grapplePoint - transform.position;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            //加速度でスピードアップ
            currentSpeed += acceleration * Time.deltaTime;
            //最大速度制限
            currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);

            characterController.Move(direction.normalized * currentSpeed * Time.deltaTime);
        }
        else
        {
            Debug.Log("フック地点に到達");
            //到達時に特定のタグが付いている場合
            if(grappledObject != null && grappledObject.CompareTag(wallTag))
            {
                StartCling();
            }
            else
            {   //それ以外
                isRetracting = false;
                ReleaseHook();
            }
        }
    }

    void ReleaseHook()
    {
        Debug.Log("フックの解除");
        isGrappling = false;
        isRetracting = false;
        isClinging = false;
        useGravity = true;
        currentSpeed = 0f;
        grappledObject = null;

        hookLine.enabled = false;
        aimLine.enabled = true;

        hookMarkerInstance.SetActive(false);
        aimMarkerInstance.SetActive(true);
    }
    void StartCling()
    {
        Debug.Log("壁に張り付いた");
        isRetracting = false;
        isClinging = true;
        clingTimer = clingDuration;
        useGravity = false;//重力の無効化

        //  レイ切り替え 
        hookLine.enabled = false;          // フックレイ非表示
        aimLine.enabled = true;            // 照準レイ再表示

        // マーカー切り替え
        hookMarkerInstance.SetActive(false);
        aimMarkerInstance.SetActive(true);
    }
}