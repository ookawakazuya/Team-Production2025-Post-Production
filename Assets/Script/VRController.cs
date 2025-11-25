using UnityEngine;
using UnityEngine.InputSystem;
public class VRController : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] Transform rayOrigin;              // レイの発射位置（コントローラーの子）
    [SerializeField] LineRenderer commonLine;          // Aim/Hook共通のLineRenderer
    [SerializeField] Material aimMaterial;             // Aim用マテリアル
    [SerializeField] Material hookMaterial;            // Hook用マテリアル
    [SerializeField] CharacterController characterController; // 移動用
    [SerializeField] Transform playerRoot;             // 視点回転の親（右スティックで回す対象）

    [Header("フック / レイ設定")]
    [SerializeField] float maxWireLength = 50f;        // レイの最大長
    [SerializeField] float aimDistance = 30f;          // Aim時の見た目距離（ヒット無し時）
    [SerializeField] float acceleration = 20f;         // 引き寄せ加速度
    [SerializeField] float maxMoveSpeed = 30f;         // 移動時の最大速度
    [SerializeField] float stopDistance = 1f;          // 到達判定距離

    [Header("張り付き / 重力")]
    [SerializeField] float clingDuration = 5f;         // 壁に張り付ける時間
    [SerializeField] float gravity = -9.81f;           // 重力
    [SerializeField] float maxFallSpeed = -50f;

    [Header("その他")]
    [SerializeField] string wallTag = "Wall";          // 張り付き判定用タグ

    [Header("フック解除条件")]
    [SerializeField] float hookBreakDistance = 2.0f;   // プレイヤーがこの距離以上離れたら自動で解除

    // 入力アセット（自作の VRHookActions）
    VRHookActions HookMap;

    // 内部状態フラグ
    bool isGrappling = false;      // フックが刺さっている（命中している）状態
    bool isRetracting = false;     // ワイヤー移動中（引き寄せ中）
    bool isClinging = false;       // 壁に張り付いている
    bool isGameRayEnabled = true;  // ゲーム用レイ有効フラグ（UI切替用）

    // 一時フラグ：張り付き中にトリガーで出した一時フックかどうか
    bool tempGrappleFromCling = false;

    // 状態パラメータ
    Vector3 grapplePoint;          // 命中位置（フック）
    Vector3 aimHitPoint;           // Aim時にRaycastが当たった位置（固定用）
    bool hasAimHitPoint = false;   // Aim時にヒットを保持しているか
    float clingTimer = 0f;
    float currentSpeed = 0f;
    float fallSpeed = 0f;

    // カメラ回転用
    [Header("視点移動")]
    [SerializeField] float rotationSpeed = 45f; // 右スティック横倒しの回転速度

    // 入力保持（読み取りは ReadInputs()）
    Vector2 rightStickInput = Vector2.zero;
    bool rightStickPressed = false;
    bool triggerPressed = false;
    bool prevTriggerPressed = false; // 前フレームのトリガー状態（Down/Up 検出用）
    bool gripPressed = false;
    bool prevGripPressed = false;    // 前フレームのグリップ状態（Down検出用）
    bool cancelPressed = false;

    // public プロパティ（外部から参照可能に）
    public bool IsRetracting => isRetracting;
    public bool IsClinging => isClinging;
    public bool IsGrappling => isGrappling;

    void Awake()
    {
        // 入力アセット初期化
        HookMap = new VRHookActions();

        // LineRenderer の初期化（必ず2点）
        if (commonLine != null)
        {
            commonLine.positionCount = 2;
            commonLine.enabled = true;
            if (aimMaterial != null) commonLine.material = aimMaterial;
        }
        else
        {
            Debug.LogWarning("[VRController] commonLine が Inspector に割り当てられていません。");
        }

        hookBreakDistance = Mathf.Max(hookBreakDistance, 0.1f);
    }

    void OnEnable() => HookMap?.Enable();
    void OnDisable() => HookMap?.Disable();

    void Update()
    {
        // --- 入力をまとめて取得 ---
        ReadInputs();

        // --- カメラ回転（右スティック横倒しのみ） ---
        HandleCameraRotation();

        // --- UIメニュー中はゲーム入力をブロック（VRMenuManagerが呼ぶEnableGameRayで制御） ---
        if (!isGameRayEnabled)
        {
            SetCommonLineEnabled(false);
            // Update 前フレーム入力保存しておく（UIから戻ったときの不整合防止）
            prevTriggerPressed = triggerPressed;
            prevGripPressed = gripPressed;
            return;
        }

        // --- トリガーの優先処理: Down / Up を検出して即時処理（トリガー最優先） ---
        HandleTriggerPriority();

        // --- 以降はトリガー処理がなかった場合の通常状態遷移 ---
        UpdateStateMachine_PostTrigger();

        // --- フック継続判定（離脱で自動解除） ---
        HandleHookBreakCheck();

        // --- 重力と移動 ---
        if (isClinging)
        {
            // 張り付き中は重力無効
            fallSpeed = 0f;
            clingTimer -= Time.deltaTime;
            if (clingTimer <= 0f)
            {
                Debug.Log("[VRController] 張り付き時間終了 -> 落下開始");
                EndClingAndFall();
            }
        }
        else
        {
            if (isRetracting)
                AccelerateTowardsHook();
            else
                ApplyGravity();
        }

        // --- レイ描画更新 ---
        UpdateRayVisuals();

        // --- 前フレーム入力保存（最後に） ---
        prevTriggerPressed = triggerPressed;
        prevGripPressed = gripPressed;
    }

    // -----------------------------
    // 入力取得
    // -----------------------------
    void ReadInputs()
    {
        if (HookMap == null) return;

        triggerPressed = HookMap.VR.HookShoot.ReadValue<float>() > 0.5f;
        gripPressed = HookMap.VR.Retract.ReadValue<float>() > 0.5f;
        rightStickInput = HookMap.VR.RightStick.ReadValue<Vector2>();
        rightStickPressed = HookMap.VR.RightStickPress.ReadValue<float>() > 0.5f;
        cancelPressed = HookMap.VR.Cancel.ReadValue<float>() > 0.5f;
    }

    // -----------------------------
    // トリガー優先処理（Down/Up を検出して即時対応）
    // -----------------------------
    void HandleTriggerPriority()
    {
        // トリガー押下（Down）
        if (triggerPressed && !prevTriggerPressed)
        {
            OnTriggerDown();
            // Do NOT return here — we still want to potentially handle other inputs in same frame,
            // but trigger actions are performed first so we don't early-exit the whole Update.
        }

        // トリガー解放（Up）
        if (!triggerPressed && prevTriggerPressed)
        {
            OnTriggerUp();
            // same as above: handle but keep update flow
        }
    }

    // トリガーダウン時の処理
    void OnTriggerDown()
    {
        // Cling 中は「一時フック」扱いにする（後でトリガーUpで解除してCling復帰）
        if (isClinging)
        {
            Debug.Log("[VRController] Cling中のトリガーDown -> 一時フック発射");
            ShootHook_FromCling();
            return;
        }

        // 通常時はトリガーでフックを射出（まだ刺さってなくて巻取り中でない場合のみ）
        if (!isGrappling && !isRetracting)
        {
            Debug.Log("[VRController] TriggerDown -> ShootHook");
            ShootHook();
            return;
        }
    }

    // トリガーアップ時の処理
    void OnTriggerUp()
    {
        // Cling 中に一時フックを出していた場合、その「一時フック」を解除してClingに戻る
        if (tempGrappleFromCling)
        {
            Debug.Log("[VRController] Cling中の一時フックを解除（TriggerUp） -> Cling 継続");
            tempGrappleFromCling = false;
            // いま表示している一時的な isGrappling を解除して Cling を保つ
            isGrappling = false;
            isRetracting = false;
            if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;
            return;
        }

        // 通常時：トリガーを放したら（巻取り中でなければ）フック解除
        if (isGrappling && !isRetracting && !isClinging)
        {
            Debug.Log("[VRController] TriggerUp -> ReleaseHook");
            ReleaseHook();
            return;
        }

        // 巻取り中（isRetracting）の場合はトリガーを放しても移動は継続（仕様）
    }

    void HandleCameraRotation()
    {
        if (playerRoot == null) return;

        // 横入力のみ反応
        float x = rightStickInput.x;
        if (Mathf.Abs(x) > 0.2f)
        {
            playerRoot.Rotate(Vector3.up * x * rotationSpeed * Time.deltaTime);
        }

        // スティック押し込みで Y 軸を 0 にリセット
        if (rightStickPressed)
        {
            Vector3 e = playerRoot.eulerAngles;
            e.y = 0f;
            playerRoot.eulerAngles = e;
            Debug.Log("[VRController] プレイヤーY軸をリセット (スティック押し込み)");
        }
    }

    // -----------------------------
    // 状態遷移（上のトリガー優先を通過した後の処理）
    // -----------------------------
    void UpdateStateMachine_PostTrigger()
    {
        // 張り付き中のグリップ処理：Cling 中にグリップ押下で張り付き解除（落下）
        if (isClinging)
        {
            if (gripPressed && !prevGripPressed) // 押した瞬間(Down)のみ処理
            {
                Debug.Log("[VRController] Cling中にグリップDown -> 張り付き解除して落下");
                EndClingAndFall();
                return;
            }
            // Cling中はそれ以外の通常処理を行わない（Aimレイは表示）
            return;
        }

        // グリップ押下で巻き取り開始（isGrappling が true のとき）
        if (isGrappling && gripPressed && !prevGripPressed && !isRetracting)
        {
            Debug.Log("[VRController] GripDown -> StartRetract");
            StartRetract();
            return;
        }

        // 通常トリガー放し（Up）での解除は HandleTriggerPriority が処理する
    }

    // -----------------------------
    // フック発射（通常）
    // -----------------------------
    void ShootHook()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            isRetracting = false;
            isClinging = false;
            tempGrappleFromCling = false;

            if (hookMaterial != null) commonLine.material = hookMaterial;

            Debug.Log($"ShootHook Hit: {hit.collider.name} at {grapplePoint}");
        }
        else
        {
            Debug.Log("[VRController] ShootHook: Raycast miss");
        }
    }

    // Cling中の「一時フック」を発射（トリガーDown 時の挙動）
    void ShootHook_FromCling()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            grapplePoint = hit.point;
            isGrappling = true;
            isRetracting = false;
            // isClinging は true のままにしておく（Cling 維持）
            tempGrappleFromCling = true;

            if (hookMaterial != null) commonLine.material = hookMaterial;

            Debug.Log($"ShootHook_FromCling Hit: {hit.collider.name} (temp)");
        }
        else
        {
            Debug.Log("[VRController] ShootHook_FromCling: Raycast miss");
        }
    }

    // -----------------------------
    // 巻き取り開始（グリップ）
    // -----------------------------
    void StartRetract()
    {
        if (!isGrappling) return;
        isRetracting = true;
        currentSpeed = 0f;
        hasAimHitPoint = false; // 移動開始したらAimの保持は不要

        // 一時フックからの巻取りなら、張り付きフラグはキャンセルしておく
        tempGrappleFromCling = false;
        isClinging = false;

        Debug.Log("[VRController] StartRetract: 巻き取り開始");
    }

    // -----------------------------
    // 引き寄せ移動処理
    // -----------------------------
    void AccelerateTowardsHook()
    {
        if (characterController == null) return;

        Vector3 direction = grapplePoint - transform.position;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);
            Vector3 move = direction.normalized * currentSpeed * Time.deltaTime;
            characterController.Move(move);
        }
        else
        {
            Debug.Log("[VRController] 到達: hook point に到達");
            Collider[] cols = Physics.OverlapSphere(grapplePoint, 0.1f);
            bool hitWall = false;
            GameObject hitObj = null;
            foreach (var c in cols)
            {
                if (c != null && c.gameObject.CompareTag(wallTag))
                {
                    hitWall = true;
                    hitObj = c.gameObject;
                    break;
                }
            }

            if (hitWall)
            {
                StartCling(grapplePoint, hitObj);
            }
            else
            {
                ReleaseHook();
            }
        }
    }

    // -----------------------------
    // 張り付き開始
    // -----------------------------
    void StartCling(Vector3 hitPoint, GameObject hitObject = null)
    {
        isRetracting = false;
        isClinging = true;
        isGrappling = false;
        clingTimer = clingDuration;
        grapplePoint = hitPoint;
        tempGrappleFromCling = false;

        if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;

        Debug.Log("[VRController] StartCling: 壁に張り付き開始");
    }

    // -----------------------------
    // 張り付き終了（落下開始）
    // -----------------------------
    void EndClingAndFall()
    {
        isClinging = false;
        ReleaseHook();
        // 落下は ApplyGravity() が行う
    }

    // -----------------------------
    // フック解除
    // -----------------------------
    void ReleaseHook()
    {
        isGrappling = false;
        isRetracting = false;
        isClinging = false;
        tempGrappleFromCling = false;
        currentSpeed = 0f;
        hasAimHitPoint = false;

        if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;

        Debug.Log("[VRController] ReleaseHook: フック解除");
    }

    // -----------------------------
    // 重力適用
    // -----------------------------
    void ApplyGravity()
    {
        if (characterController == null) return;

        if (characterController.isGrounded)
            fallSpeed = 0f;
        else
        {
            fallSpeed += gravity * Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, maxFallSpeed);
            characterController.Move(new Vector3(0, fallSpeed, 0) * Time.deltaTime);
        }
    }

    // -----------------------------
    // レイ表示の切替・更新
    // -----------------------------
    void UpdateRayVisuals()
    {
        if (commonLine == null || rayOrigin == null)
            return;

        commonLine.positionCount = 2;

        // Cling 中は Aim材質で固定先端（grapplePoint）を表示
        if (isClinging)
        {
            commonLine.material = aimMaterial;
            commonLine.enabled = true;
            commonLine.SetPosition(0, rayOrigin.position);
            commonLine.SetPosition(1, grapplePoint);
            return;
        }

        // Grappling または Retracting 中は Hook材質で先端固定
        if (isGrappling || isRetracting)
        {
            commonLine.material = hookMaterial;
            commonLine.enabled = true;
            commonLine.SetPosition(0, rayOrigin.position);
            commonLine.SetPosition(1, grapplePoint);
            return;
        }

        // 通常 Aim 表示
        UpdateAimRayFixed();
    }

    // Aim更新（ヒット点を保持）
    void UpdateAimRayFixed()
    {
        if (commonLine == null || rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            aimHitPoint = hit.point;
            hasAimHitPoint = true;

            commonLine.material = aimMaterial;
            commonLine.SetPosition(0, rayOrigin.position);
            commonLine.SetPosition(1, aimHitPoint);
            return;
        }

        if (hasAimHitPoint)
        {
            float dist = Vector3.Distance(rayOrigin.position, aimHitPoint);
            if (dist <= maxWireLength)
            {
                commonLine.material = aimMaterial;
                commonLine.SetPosition(0, rayOrigin.position);
                commonLine.SetPosition(1, aimHitPoint);
                return;
            }
            hasAimHitPoint = false;
            aimHitPoint = Vector3.zero;
        }

        Vector3 endPoint = rayOrigin.position + rayOrigin.forward * aimDistance;
        commonLine.material = aimMaterial;
        commonLine.SetPosition(0, rayOrigin.position);
        commonLine.SetPosition(1, endPoint);
    }

    // -----------------------------
    // UI とゲームのレイ切替（VRMenuManager等から呼ぶ）
    // -----------------------------
    public void EnableGameRay(bool enable)
    {
        isGameRayEnabled = enable;
        SetCommonLineEnabled(enable);
    }

    public void EnableUIRay(bool enable)
    {
        isGameRayEnabled = !enable;
        SetCommonLineEnabled(!enable);
    }

    void SetCommonLineEnabled(bool enabled)
    {
        if (commonLine == null) return;
        commonLine.enabled = enabled;
    }

    // -----------------------------
    // 新規：ヒット地点からの離脱チェック（hook解除）
    // -----------------------------
    void HandleHookBreakCheck()
    {
        if (!(isGrappling || isRetracting || isClinging))
            return;

        float dist = Vector3.Distance(transform.position, grapplePoint);

        if (dist > hookBreakDistance)
        {
            Debug.Log($"[VRController] HookBreak: distance {dist:F2} exceeded hookBreakDistance {hookBreakDistance:F2} -> ReleaseHook()");
            ReleaseHook();
        }
    }

    // -----------------------------
    // 外部トリガー（あれば呼べる）
    // -----------------------------
    public void ForceReleaseHook()
    {
        ReleaseHook();
    }
}
