using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
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


    [Header("マーカ設定")]
    [SerializeField] GameObject markerPrefab;//カーソルプレハブ
    private GameObject aimMarkerInstance;//照準用
    private GameObject hookMarkerInstance;//フック用

    [Header("壁張り付き設定")]
    [SerializeField] float clingDuration = 5f;  //壁に留まれる時間

    bool isClinging = false;    //到達したか
    float clingTimer = 0f;      //落下タイマー
    GameObject grappledObject;  //命中したオブジェクト



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
        //トリガーとグリップの状態
        bool triggerPressed = HookMap.VR.HookShoot.ReadValue<float>() > 0.5f;
        bool gripPressed = HookMap.VR.Retract.ReadValue<float>() > 0.5f;

        //フック発射処理
        if (triggerPressed)
        {
            if(!isGrappling && !isRetracting&&!isClinging)
            ShootHook();
        }
        else if (!triggerPressed && (isGrappling || isRetracting))
        {
            ReleaseHook();
        }
        //ワイヤー移動開始
        if (isGrappling && triggerPressed&&gripPressed&&!wasgripPressed&&!isRetracting &&!isClinging)
        {
            StartRetract();
        }

        //wasgripPressed = gripPressed;

        //張り付き処理
        if (isClinging)
        {
            clingTimer -= Time.deltaTime;
            if(clingTimer <= 0)
            {
                Debug.Log("張り付き解除→落下開始");
                isClinging = false;
                ReleaseHook();
            }
        }
        else
        {
            //実際の移動処理
            if (isRetracting)
            {
                AccelerateTowardsHook();
            }
        }
        if(isGrappling) UpdateHookLine();
        else UpdateAimLine();
        wasgripPressed = gripPressed;
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
        //レイの描画
        aimLine.SetPosition(0, rightController.transform.position);
        aimLine.SetPosition(1, endPoint);

        if (aimMarkerInstance != null)
        {
            aimMarkerInstance.SetActive(true);
            aimMarkerInstance.transform.transform.position = endPoint;
            aimMarkerInstance.transform.rotation = Quaternion.LookRotation(normal);
        }
    }
    void StartRetract()
    {
        if (isGrappling)
        {
            Debug.Log("巻き取り開始");
            isRetracting = true;
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
    }
}