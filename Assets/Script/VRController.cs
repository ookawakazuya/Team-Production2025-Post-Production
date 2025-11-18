using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// VRController（関数分割版）
/// - Aim / Hook を共通 LineRenderer (commonLine) で描画（マテリアル差し替え）
/// - Aim でヒットした地点は保持し、rayOrigin からの距離が maxWireLength 内であれば先端は固定する
/// - トリガーで ShootHook（命中時に isGrappling=true）
/// - グリップで StartRetract（移動開始）。移動中はトリガーを放しても継続。
/// - 到達時にタグが wallTag のオブジェクトなら StartCling（張り付き）
/// - UI レイは別管理（VRMenuManager）に任せる。EnableGameRay / EnableUIRay で切替可能
/// 
/// 必要な Inspector 設定:
///  - rayOrigin (Transform) : レイを発生させたいコントローラの子 Transform
///  - commonLine (LineRenderer)
///  - aimMaterial / hookMaterial
///  - characterController
///  - playerRoot (視点回転の親 transform)
///  - wallTag (張り付き対象のタグ名)
///  - VRHookActions input asset がプロジェクトにあること
/// </summary>
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

    // 入力アセット（自作の VRHookActions）
    VRHookActions HookMap;

    // 内部状態フラグ
    bool isGrappling = false;      // フックが刺さっている（命中している）状態
    bool isRetracting = false;     // ワイヤー移動中（引き寄せ中）
    bool isClinging = false;       // 壁に張り付いている
    bool isGameRayEnabled = true;  // ゲーム用レイ有効フラグ（UI切替用）

    // 状態パラメータ
    Vector3 grapplePoint;          // 命中位置（フック）
    bool isGrappleRayActive = false;
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
    bool gripPressed = false;
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

        // 簡易チェック
        if (rayOrigin == null) Debug.LogWarning("[VRController] rayOrigin が割り当てられていません。");
        if (characterController == null) Debug.LogWarning("[VRController] characterController が割り当てられていません。");
        if (playerRoot == null) Debug.LogWarning("[VRController] playerRoot が割り当てられていません。右スティック回転は動作しません。");
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
            // メニュー中はレイを非表示にする（UIレイはVRMenuManagerが表示）
            SetCommonLineEnabled(false);
            return;
        }

        // --- 状態更新（張り付き処理、フック射出、巻取り開始など） ---
        UpdateStateMachine();

        // --- 重力と移動 ---
        if (isClinging)
        {
            // 張り付き中は重力無効（プレイヤーが張り付いている）
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
            {
                // ワイヤー移動処理
                AccelerateTowardsHook();
            }
            else
            {
                // 通常の重力適用
                ApplyGravity();
            }
        }

        // --- レイ描画更新 ---
        UpdateRayVisuals();
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
    // カメラ回転（右スティック横倒しのみ）
    // -----------------------------
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
    // 状態遷移（射出 / 張り付き / 巻取り開始 等）
    // -----------------------------
    void UpdateStateMachine()
    {
        // ---- 張り付き中の特殊処理（張り付き中でもトリガーで新規フック可能、移動はグリップで開始） ----
        if (isClinging)
        {
            // 張り付き中：トリガーで新規フック（張り付きは低優先）
            if (triggerPressed)
            {
                // 新規フックを撃つ（isClinging のままにするか張り付き解除するかは仕様次第）
                // ここでは張り付き状態を解除して新フック開始
                Debug.Log("[VRController] 張り付き中にトリガー -> 新規フック発射");
                EndCling();
                ShootHook();
                return; // 新規フック処理に移行
            }

            // 張り付き中にグリップが押されれば張り付き解除して移動開始しない（仕様：グリップで張り付き解除）
            if (gripPressed)
            {
                Debug.Log("[VRController] 張り付き中にグリップ -> 張り付き解除");
                EndCling();
                return;
            }

            // 張り付き中は常にAimレイを維持（UpdateRayVisualsが表示）
            return;
        }

        // ---- 通常時：トリガーでフック射出（まだフックが刺さっていない & 巻き取り中でない） ----
        if (triggerPressed && !isGrappling && !isRetracting)
        {
            ShootHook();
        }

        // ---- トリガーを放したときの解除条件 ----
        // トリガーを放したら、フックが刺さっているけど巻取り中でない場合は解除する（ただし張り付き中は解除しない）
        if (!triggerPressed && isGrappling && !isRetracting && !isClinging)
        {
            // ReleaseHook は張り付き中は呼ばれないようにしている
            ReleaseHook();
        }

        // ---- グリップで巻き取り開始（isGrappling が true のとき） ----
        if (isGrappling && gripPressed && !isRetracting)
        {
            StartRetract();
        }
    }

    // -----------------------------
    // フック発射（トリガー）
    // -----------------------------
    void ShootHook()
    {
        if (rayOrigin == null) { Debug.LogWarning("[VRController] rayOrigin が null で Shoot できません"); return; }

        // Aimヒット点は一旦クリア。新しい射出では再計算。
        hasAimHitPoint = false;
        aimHitPoint = Vector3.zero;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            // 命中
            grapplePoint = hit.point;
            isGrappling = true;
            isClinging = false;
            isRetracting = false;

            // 保持用のAimヒット点（Aim -> Hook 切替を視覚的にスムーズにするため）
            aimHitPoint = hit.point;
            hasAimHitPoint = true;

            // Line のマテリアルを Hook 用に切替（描画は UpdateRayVisuals で行う）
            if (commonLine != null && hookMaterial != null) commonLine.material = hookMaterial;

            Debug.Log($"[VRController] ShootHook: hit {hit.collider.name} at {grapplePoint}");
        }
        else
        {
            // 命中なし
            Debug.Log("[VRController] ShootHook: no hit");
            // isGrappling は立てない（未命中はフック発射が無効）
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
            // 加速度で速度を増加
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);

            // 移動（CharacterController を使って当たり判定含めて移動）
            Vector3 move = direction.normalized * currentSpeed * Time.deltaTime;
            characterController.Move(move);
        }
        else
        {
            // 到達時の処理
            Debug.Log("[VRController] 到達: hook point に到達");
            // 到達先のオブジェクトが壁かどうかで張り付き判定
            // ※ ここでは簡易のため Physics.OverlapSphere で近傍のコライダを調べる
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
                // それ以外は解除
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

        // 表示は Aim レイ（照準）に変更して、Aimマーカーを表示するようにする（commonLineの材質変更）
        if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;

        Debug.Log("[VRController] StartCling: 壁に張り付き開始");
    }

    // -----------------------------
    // 張り付き終了（落下開始）
    // -----------------------------
    void EndClingAndFall()
    {
        isClinging = false;
        // 落下は ApplyGravity が処理
        ReleaseHook(); // フックの解除（必要に応じて）
    }

    // 張り付きをただ解除（外部から呼べる）
    void EndCling()
    {
        isClinging = false;
        ReleaseHook();
    }

    // -----------------------------
    // フック解除（トリガー放し時等）
    // -----------------------------
    void ReleaseHook()
    {
        isGrappling = false;
        isRetracting = false;
        isClinging = false;
        currentSpeed = 0f;
        hasAimHitPoint = false;

        // レイは Aim マテリアルに戻す（表示は UpdateRayVisuals に任せる）
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

    // -----------------------------
    // レイ表示の切替・更新
    // -----------------------------
    void UpdateRayVisuals()
    {
        // safety checks
        if (commonLine == null || rayOrigin == null)
            return;

        // positionCount 保証
        if (commonLine.positionCount < 2)
            commonLine.positionCount = 2;

        // 張り付き中は Aim レイ表示（先端は grapplePoint）
        if (isClinging)
        {
            commonLine.enabled = true;
            if (aimMaterial != null) commonLine.material = aimMaterial;
            commonLine.SetPosition(0, rayOrigin.position);
            commonLine.SetPosition(1, grapplePoint);
            return;
        }

        // 移動中またはフック刺さり中は Hook レイ（常に grapplePoint へ）
        if (isRetracting || isGrappling)
        {
            commonLine.enabled = true;
            if (hookMaterial != null) commonLine.material = hookMaterial;
            commonLine.SetPosition(0, rayOrigin.position);
            commonLine.SetPosition(1, grapplePoint);
            return;
        }

        // 通常 Aim 表示
        UpdateAimRay();
    }

    /// <summary>
    /// Aimレイの更新。重要：Aimでヒットした地点は保持し、rayOriginからの距離がmaxWireLength以内なら先端はそのヒット地点に固定する。
    /// そうでない（ヒットなし or 範囲外）なら rayOrigin.forward * aimDistance で表示。
    /// </summary>
    void UpdateAimRay()
    {
        if (commonLine == null || rayOrigin == null) return;

        // 描画材質は Aim 用
        if (aimMaterial != null) commonLine.material = aimMaterial;
        commonLine.enabled = true;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        // 1) 新たにヒットしたか確認（毎フレーム）
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            // ヒットしている場合はヒット位置を保持（コントローラ移動しても先端はここに固定する）
            aimHitPoint = hit.point;
            hasAimHitPoint = true;
        }
        else
        {
            // ヒットしていないフレーム。だが以前の aimHitPoint が有効で、
            // rayOrigin からその点までの距離が maxWireLength 以下なら維持する（要求どおりの挙動）
            if (hasAimHitPoint)
            {
                float dist = Vector3.Distance(rayOrigin.position, aimHitPoint);
                if (dist <= maxWireLength)
                {
                    // 以前のヒット地点を維持する（コントローラーを動かしても先端は動かない）
                    commonLine.SetPosition(0, rayOrigin.position);
                    commonLine.SetPosition(1, aimHitPoint);
                    return;
                }
                else
                {
                    // 範囲外になったので保持解除
                    hasAimHitPoint = false;
                    aimHitPoint = Vector3.zero;
                }
            }
        }

        // 上の分岐で有効な aimHitPoint がない場合は、通常の forward レイを描画
        Vector3 endPoint = rayOrigin.position + rayOrigin.forward * aimDistance;
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
        // UI 側は VRMenuManager が管理する。ここではフラグだけ立てる。
        isGameRayEnabled = !enable;
        SetCommonLineEnabled(!enable);
    }

    void SetCommonLineEnabled(bool enabled)
    {
        if (commonLine == null) return;
        commonLine.enabled = enabled;
    }

    // -----------------------------
    // 外部トリガー（あれば呼べる）
    // -----------------------------
    public void ForceReleaseHook()
    {
        ReleaseHook();
    }
}
