using UnityEngine;

/// <summary>
/// 【司令塔】入力を監視し、各機能クラスへ命令を出す
/// </summary>
public class VRcontroller : MonoBehaviour
{
    [Header("各機能担当クラス")]
    [SerializeField] private VRMovementHandler movement;
    [SerializeField] private VRHookVisualizer visualizer;
    [SerializeField] private VRViewRotator rotator;
    [SerializeField] private ChestInteractor chest;

    [Header("設定・参照")]
    [SerializeField] private Transform rayOrigin;
    [SerializeField] private float maxWireLength = 50f;
    [SerializeField] private string[] invalidTags;

    private VRHookActions inputActions;
    private bool isGrappling = false;
    private Vector3 grapplePoint;

    private void Awake()
    {
        inputActions = new VRHookActions();
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Start()
    {
        // 初期リセンターの実行
        StartCoroutine(rotator.RecenterAtStart());
    }

    private void Update()
    {
        // 1. 入力値の取得
        float triggerValue = inputActions.VR.HookShoot.ReadValue<float>();
        float gripValue = inputActions.VR.Retract.ReadValue<float>();
        Vector2 stickValue = inputActions.VR.RightStick.ReadValue<Vector2>();

        bool triggerPressed = triggerValue > 0.5f;
        bool gripPressed = gripValue > 0.5f;

        // 2. 宝箱操作 (操作中は他をキャンセル)
        // 宝箱操作を優先チェック
        bool isChestInteracting = chest.HandleChestInteraction(rayOrigin, triggerPressed);

        if (isChestInteracting)
        {
            // 宝箱を触っている間は、フックを解除して処理を抜ける
            if (isGrappling) CancelHook();

            // 宝箱のアンカーポイントにレイを吸着させる（見た目の向上）
            // visualizer.UpdateVisuals(rayOrigin, true, currentChestLid.RayAnchorpoint.position);
            return;
        }

        // 3. 視点回転
        rotator.HandleRotation(stickValue.x);

        // 4. 移動・物理更新
        movement.Tick(isGrappling);

        // 5. フックロジック
        HandleHookProcess(triggerPressed, gripPressed);

        // 6. 描画更新
        UpdateVisuals();
    }

    private void HandleHookProcess(bool triggerPressed, bool gripPressed)
    {
        // 発射判定
        if (triggerPressed && !isGrappling)
        {
            ExecuteShoot();
        }
        // 解除
        else if (!triggerPressed && isGrappling)
        {
            CancelHook();
        }

        // 引き寄せ開始
        if (isGrappling && gripPressed && !movement.IsRetracting)
        {
            movement.StartRetracting(grapplePoint);
        }
    }

    private void ExecuteShoot()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            if (System.Array.IndexOf(invalidTags, hit.collider.tag) == -1)
            {
                isGrappling = true;
                grapplePoint = hit.point;
                visualizer.SetHookModelStatus(isIdle: false);
                visualizer.PlayHitEffect(hit.point, hit.normal);
                // Haptic(振動)などもここで呼ぶ
            }
        }
    }

    private void CancelHook()
    {
        isGrappling = false;
        movement.ResetMovement();
        visualizer.SetHookModelStatus(isIdle: true);
        visualizer.StopHitEffect();
    }

    private void UpdateVisuals()
    {
        Vector3 target = isGrappling ? grapplePoint : rayOrigin.position + (rayOrigin.forward * maxWireLength);
        visualizer.UpdateVisuals(rayOrigin, isGrappling, target);
    }
}