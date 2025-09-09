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


    bool isGrappling = false;
    bool isRetracting = false;
    bool wasgripPressed = false;//前フレームの状態
    Vector3 grapplePoint;


    float currentSpeed = 0f;//現在の移動速度

    public VRHookActions HookMap;

    void Awake()
    {
        HookMap = new VRHookActions();
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
            if(!isGrappling && !isRetracting)
            ShootHook();
        }
        else if (!triggerPressed && (isGrappling || isRetracting))
        {
            ReleaseHook();
        }
        //ワイヤー移動開始
        if (isGrappling && triggerPressed&&gripPressed&&!wasgripPressed&&!isRetracting)
        {
            StartRetract();
        }
        //実際の移動処理
        if (isRetracting)
        {
            AccelerateTowardsHook();
        }
        wasgripPressed = gripPressed;
    }

        

    void ShootHook()
    {
        Debug.Log("フック射出");
        Ray ray = new Ray(rightController.transform.position, rightController.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            Debug.Log($"フックショット命中:{hit.collider.name}");
        }
        else
        {
            Debug.Log("フック未明中");
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
            isRetracting = false;
            ReleaseHook();
        }
    }

    void ReleaseHook()
    {
        Debug.Log("フックの解除");
        isGrappling = false;
        isRetracting = false;
        currentSpeed = 0f;
    }
}