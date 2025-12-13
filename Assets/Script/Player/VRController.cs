using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
     [SerializeField] float acceleration = 20f;         // 引き寄せ加速度
    [SerializeField] float maxMoveSpeed = 30f;         // 移動時の最大速度
    [SerializeField] float stopDistance = 1f;          // 到達判定距離

    [Header("張り付き / 重力")]
    [SerializeField] float clingDuration = 5f;         // 壁に張り付ける時間
    [SerializeField] float gravity = -9.81f;           // 重力
    [SerializeField] float maxFallSpeed = -50f;

    [Header("ワイヤー移動遅延設定")]
    [SerializeField] float retractStartDelay = 0.3f;
    float retractDelayTime = 0f;
    bool waitingRetractStart = false;

    [Header("その他")]
    [SerializeField] string wallTag = "Wall";          // 張り付き判定用タグ

    [Header("フック解除条件")]
    [SerializeField] float hookBreakDistance = 2.0f;   // プレイヤーがこの距離以上離れたら自動で解除                        

    [Header("フック無効化タグ")]
    [SerializeField] string[] hookInvalidTags;  //無効化するタグ

    [SerializeField] XRInteractorLineVisual lineVisual;
    [SerializeField] HapticController haptic;
    [SerializeField] bool isRightHand = true;   //左右の判断

    // 入力アセット（自作の VRHookActions）
    VRHookActions HookMap;

    // 状態フラグ
    bool isGrappling = false;      // フックが刺さっている（命中している）状態（見た目用）
    bool isRetracting = false;     // ワイヤー移動中（引き寄せ中）
    bool isClinging = false;       // 壁に張り付いている
    bool isGameRayEnabled = true;  // ゲーム用レイ有効フラグ（UI切替用）

    // 張り付き中にトリガーで出した「一時フック」フラグ
    bool tempGrappleFromCling = false;

    // 状態パラメータ
    Vector3 grapplePoint;          // 命中位置（フック）
    Vector3 aimHitPoint;           // Aim時にRaycastが当たった位置（固定用）
    bool hasAimHitPoint = false;   // Aim時にヒットを保持しているか
    bool wasGrounded = true;       //前フレームの接地状態

    float clingTimer = 0f;
    float currentSpeed = 0f;
    float fallSpeed = 0f;



    [Header("視点移動")]
    [SerializeField] float rotationSpeed = 45f; // 右スティック横倒しの回転速度

    [Header("メニュー連携 / 衝突回避")]
    [SerializeField] VRMenuManager menuManager;         // VRMenuManagerを参照
                                                        // [SerializeField] GameObject menuCanvasObject;     // 必要であればキャンバス自体の参照
    [SerializeField] LayerMask wallLayer;             // 壁や障害物のレイヤーを設定

    // 【重要】safetyDistance を「メニューキャンバスを配置したいプレイヤーからの距離」として設定
    [SerializeField] float menuCanvasDistance = 0.5f;
    [SerializeField] float forcedRotationAngle = 180f; // 強制回転させる角度

    // 状態フラグ
    private bool isMenuRotationLocked = false;


    // 入力保持
    Vector2 rightStickInput = Vector2.zero;
    bool rightStickPressed = false;
    bool triggerPressed = false;
    bool prevTriggerPressed = false; // 前フレームのトリガー状態
    bool gripPressed = false;
    bool prevGripPressed = false;    // 前フレームのグリップ状態
    bool cancelPressed = false;

    [Header("視覚警告（ビネット）")]
    [SerializeField] Volume volume;
    Vignette vignette;
    [SerializeField] float warningStartRate = 3.0f;
    // clingDuration の何割以下になったら暗くするか
    [SerializeField] float maxVignetteIntensity = 0.45f;


    [Header("サウンド関係")]
    [SerializeField] float minLandingSpeed = -1.0f;


    [Header("エフェクト")]
    [SerializeField] ParticleSystem hookHitParticle;




    // プロパティ
    public bool IsRetracting => isRetracting;
    public bool IsClinging => isClinging;
    public bool IsGrappling => isGrappling;

    void Awake()
    {
        HookMap = new VRHookActions();

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

        if (volume != null)
            volume.profile.TryGet(out vignette);

        if (volume != null)
            volume.profile.TryGet(out vignette);

        //パーティクルシステムの初期設定 
        if (hookHitParticle == null)
        {
            Debug.LogWarning("[VRController] hookHitParticle が Inspector に割り当てられていません。");
        }
    }

    void OnEnable() => HookMap?.Enable();
    void OnDisable() => HookMap?.Disable();

    void Update()
    {
        ReadInputs();
        HandleCameraRotation();

        // UIメニュー中はゲームレイを消して入力停止（VRMenuManager が制御）
        if (!isGameRayEnabled)
        {
            SetCommonLineEnabled(false);
            prevTriggerPressed = triggerPressed; // 不整合防止
            prevGripPressed = gripPressed;
            return;
        }

        // トリガーの優先処理（Down / Up）
        HandleTriggerPriority();

        // トリガー優先処理で処理している場合、以降は通常処理へ進む
        UpdateStateMachine(); // グリップ処理など

        // フック継続判定（ヒット地点から遠ざかったら解除）
        HandleHookBreakCheck();

        // 重力・移動
        if (isClinging)
        {
            fallSpeed = 0f;
            clingTimer -= Time.deltaTime;

            // float rate = clingTimer / clingDuration; // この行は削除（またはコメントアウト）

            // 警告が開始される残り時間（秒）を計算
            // warningStartRate が 3.0 で clingDuration が 5.0 なら、timeer = 1.66秒
            float timeer = clingDuration / warningStartRate;

            if (vignette != null)
            {
                // 残り時間 (clingTimer) が警告開始の閾値 (timeer) を下回ったかチェック（秒 vs 秒）
                if (clingTimer <= timeer)
                {
                    // clingTimer を timeer (0%の暗さ) から 0f (100%の暗さ) の間で逆補間して t を求める
                    // t は clingTimer が少なくなるにつれて 0 から 1 に変化します
                    float t = Mathf.InverseLerp(timeer, 0f, clingTimer);

                    // t を使ってビネットの強度を 0f から maxVignetteIntensity へ変化させる
                    vignette.intensity.value = Mathf.Lerp(0f, maxVignetteIntensity, t);

                    // 【デバッグ用】値が変化しているか確認
                    Debug.Log($"[Vignette] Timer: {clingTimer:F2}, t: {t:F2}, Intensity: {vignette.intensity.value:F2}");
                }
                else
                {
                    // まだ余裕あるなら暗さリセット
                    vignette.intensity.value = 0f;
                }
            }
            if (clingTimer <= 0f)
            {
                Debug.Log("[VRController] 張り付き時間終了 -> 落下開始");
                EndClingAndFall();

                if (vignette != null)
                    vignette.intensity.value = 0f; // リセット
            }
        }
        else
        {
            if (isRetracting)
                AccelerateTowardsHook();
            else
                ApplyGravity();
        }


            UpdateRayVisuals();

        if (waitingRetractStart)
        {
            retractDelayTime += Time.deltaTime;
            if(retractDelayTime >= retractStartDelay)
            {
                Debug.Log("[VRController] 巻取り遅延");
                waitingRetractStart = false;
                retractDelayTime = 0f;
                StartRetract();
            }
        }
        
        // 前フレーム状態の更新（最後）
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
    // トリガー優先処理（Down/Up）※トリガー最優先
    // -----------------------------
    void HandleTriggerPriority()
    {
        // Down
        if (triggerPressed && !prevTriggerPressed)
        {
            OnTriggerDown();
        }

        // Up
        if (!triggerPressed && prevTriggerPressed)
        {
            OnTriggerUp();
        }
    }

    // トリガー押下（Down）の処理
    void OnTriggerDown()
    {
        lineVisual.enabled = false;
        SoundManager.Instance.PlaySE("SE_Hook_01");
        // Cling 中かつトリガーDownなら「一時フック」を狙う（Cling 維持）
        if (isClinging)
        {
            Debug.Log("[VRController] Cling中のトリガーDown -> 一時フック発射");
            ShootHook_FromCling();
            return;
        }

        // 通常時はトリガーでフック射出（まだ刺さっておらず巻取り中でない場合）
        if (!isGrappling && !isRetracting)
        {
            Debug.Log("[VRController] TriggerDown -> ShootHook");
            ShootHook();
            return;
        }
    }

    // トリガー解放（Up）の処理
    void OnTriggerUp()
    {
        lineVisual.enabled = true;
        // Cling 中に出した一時フックがあるならそれだけ解除して Cling 維持
        if (tempGrappleFromCling)
        {
            Debug.Log("[VRController] Cling中の一時フックを解除（TriggerUp） -> Cling 継続");
            tempGrappleFromCling = false;
            isGrappling = false;
            isRetracting = false;
            if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;
            return;
        }

        // 通常時：刺さっているが巻取り中でなければトリガー放しでフック解除
        if (isGrappling && !isRetracting && !isClinging)
        {
            Debug.Log("[VRController] TriggerUp -> ReleaseHook");
            ReleaseHook();
            return;
        }

        // 巻取り中にトリガーを放したらキャンセル（仕様）
        if (isRetracting && !triggerPressed)
        {
            Debug.Log("[VRController] 巻取り中にトリガー放し -> ReleaseHook (巻取りキャンセル)");
            ReleaseHook();
            return;
        }
    }

    // -----------------------------
    // カメラ回転（右スティック横倒しのみ）
    // -----------------------------
    void HandleCameraRotation()
    {
        if (playerRoot == null) return;

        float x = rightStickInput.x;
        
        //メニュー回転ロック中の処理
        if (isMenuRotationLocked)
        {
            // 回転操作がある場合
            if (Mathf.Abs(x) > 0.2f)
            {
                // 次のフレームで到達する目標角度を計算
                float currentY = playerRoot.eulerAngles.y;
                float deltaAngle = x * rotationSpeed * Time.deltaTime;
                float futureYRotation = currentY + deltaAngle;

                // その角度に回転したら衝突するかを予測チェック
                if (IsFutureRotationColliding(futureYRotation))
                {
                    // 衝突するため、回転を阻止 (何もしない)
                    Debug.Log("[VRController] 回転を阻止: 壁にめり込む角度です。");
                    return;
                }
            }
            // 回転ロック中は、軸リセット以外の回転操作は上記でチェック済みのため、以降の処理へ進む
        }
        if (Mathf.Abs(x) > 0.2f)
        {
            playerRoot.Rotate(Vector3.up * x * rotationSpeed * Time.deltaTime);
        }

        if (rightStickPressed)
        {
            Vector3 e = playerRoot.eulerAngles;
            e.y = 0f;
            playerRoot.eulerAngles = e;
            Debug.Log("[VRController] プレイヤーY軸をリセット (スティック押し込み)");
        }
    }

    // -----------------------------
    // トリガー優先通過後の状態処理（グリップ系など）
    // -----------------------------
    void UpdateStateMachine()
    {
        //  グリップ押下での巻き取り開始（Down の瞬間 を検出）
        if (isGrappling && gripPressed && !prevGripPressed && !isRetracting)
        {
            Debug.Log("[VRController] GripDown -> 巻取り待機開始");
            waitingRetractStart = true;
            retractDelayTime = 0f;
            return;
        }

        //  Cling 中のグリップ処理（優先順注意）
        //    - もし Cling 中かつ一時フックが存在する (tempGrappleFromCling==true)
        //      -> グリップDown で巻き取り開始（Cling を解除して移動へ）
        if (isClinging)
        {
            if (gripPressed && !prevGripPressed)
            {
                if (tempGrappleFromCling && isGrappling)
                {
                    // トリガーで一時フックを出してからグリップ押下したケース（期待される動作）
                    Debug.Log("[VRController] Cling中 + tempGrapple -> GripDown -> StartRetract");
                    StartRetract();
                    return;
                }
                else
                {
                    // 一時フックなしの単独グリップ押下は「張り付き解除（落下）」にする
                    Debug.Log("[VRController] Cling中にグリップDown -> 張り付き解除して落下");
                    EndClingAndFall();
                    return;
                }
            }
            // Cling中はそれ以外の通常処理をしない（Aimレイは表示）
            return;
        }

        //  巻取り中にトリガーを離したら既に OnTriggerUp 側で解除しているが二重チェックとして
        if (isRetracting && !triggerPressed)
        {
            Debug.Log("[VRController] 巻取り中にトリガーが離れている -> ReleaseHook (二重チェック)");
            ReleaseHook();
            return;
        }

        //  通常時のトリガー押し（Hold中）での発射は HandleTriggerPriority で処理済み
    }

    // -----------------------------
    // フック射出（通常）
    // -----------------------------
    void ShootHook()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            //フックショットが刺さらなくする条件
            if (IsTagInvalidForHook(hit.collider.tag))
            {
                Debug.Log($"[VRController] ShootHook: Tag '{hit.collider.tag}' is in the hook invalid list. Treating as miss.");
                return; // ヒットを無視し、フックが刺さらない
            }

            SoundManager.Instance.PlaySE("SE_Hook_02");
            grapplePoint = hit.point;
            aimHitPoint = hit.point;     // Aim 先端をセット（先端固定用）
            hasAimHitPoint = true;

            isGrappling = true;
            isRetracting = false;
            isClinging = false;
            tempGrappleFromCling = false;

            if (hookMaterial != null) commonLine.material = hookMaterial;

            if (haptic != null)
                haptic.VibrateWallHit(isRightHand);

            PlayHookHitParticle(grapplePoint, hit.normal);

            Debug.Log($"ShootHook Hit: {hit.collider.name} at {grapplePoint}");
        }
        else
        {
            Debug.Log("[VRController] ShootHook: Raycast miss");
        }
    }

    // Cling中の「一時フック」を発射（トリガーDown 時）
    void ShootHook_FromCling()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            //フックショットの無効化
            if (IsTagInvalidForHook(hit.collider.tag))
            {
                Debug.Log($"[VRController] ShootHook_FromCling: Tag '{hit.collider.tag}' is in the hook invalid list. Treating as miss.");
                return; // ヒットを無視し、フックが刺さらない
            }

            SoundManager.Instance.PlaySE("SE_Hook_02");
            grapplePoint = hit.point;
            aimHitPoint = hit.point;
            hasAimHitPoint = true;

            isGrappling = true;
            isRetracting = false;
            // Cling 維持
            tempGrappleFromCling = true;

            //  Cling中にワイヤー発射した時の弱振動追加
            if (haptic != null)
                haptic.VibrateWallHit(isRightHand);


            if (hookMaterial != null) commonLine.material = hookMaterial;

            PlayHookHitParticle(grapplePoint, hit.normal);

            Debug.Log($"ShootHook_FromCling Hit: {hit.collider.name} (temp)");
        }
        else
        {
            Debug.Log("[VRController] ShootHook_FromCling: Raycast miss");
        }
    }

    // -----------------------------
    // 巻き取り開始（グリップDown）
    // -----------------------------
    void StartRetract()
    {
        if (!isGrappling) return;
        SoundManager.Instance.PlaySELoop("SE_Hook_03");//ループSEに変更予定
        isRetracting = true;
        currentSpeed = 0f;
        hasAimHitPoint = false; // 移動開始したら Aim の保持は不要

        // 一時フックからの巻取りなら、張り付きフラグはキャンセルしておく
        tempGrappleFromCling = false;
        isClinging = false;
        SoundManager.Instance.StopSE();
        //パーティクルの停止
        StopHookHitParticle();
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
            // 巻取り中の連続弱振動 here 
            if (haptic != null)
                haptic.VibrateRetracting(isRightHand);

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
                SoundManager.Instance.StopSELoop();//のちにループ用に置き換え
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

        if (haptic != null)
        {
            haptic.VibrateArrivedWall(isRightHand);   
        }
        SoundManager.Instance.PlaySE("SE_Harituki");
        Debug.Log("[VRController] StartCling: 壁に張り付き開始");
    }

    // 張り付き解除＆落下開始
    void EndClingAndFall()
    {
        isClinging = false;
        ReleaseHook();

        SoundManager.Instance.StopSE();
        // 落下は ApplyGravity で処理される
    }

    // フック解除
    void ReleaseHook()
    {
        isGrappling = false;
        isRetracting = false;
        isClinging = false;
        tempGrappleFromCling = false;
        currentSpeed = 0f;
        hasAimHitPoint = false;

        if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;
        SoundManager.Instance.StopSELoop();//のちにループ停止を挟む
        StopHookHitParticle();
        Debug.Log("[VRController] ReleaseHook: フック解除");
    }

    // -----------------------------
    // 重力適用
    // -----------------------------
    void ApplyGravity()
    {
        if (characterController == null) return;
        bool isCurrentlyGrounded = characterController.isGrounded;

        if(isCurrentlyGrounded)
        {
            if (!wasGrounded && fallSpeed < minLandingSpeed)
            {
                // 着地時のSEを再生
                SoundManager.Instance.PlaySE("SE_Player_01");
            }
            if (fallSpeed < 0f)
            {
                fallSpeed = 0f;
            }
            else
            {
                fallSpeed = 0f;
            }
        }
        else
        {
            fallSpeed += gravity * Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, maxFallSpeed);
            characterController.Move(new Vector3(0, fallSpeed, 0) * Time.deltaTime);
        }
        // 最後に、現在の接地状態を次のフレームのために保存
        wasGrounded = isCurrentlyGrounded;
    }

    // -----------------------------
    // レイ描画の切り替え・更新
    //   先端（SetPosition(0)） = aimHitPoint or grapplePoint
    //   末端（SetPosition(1)） = rayOrigin.position
    // -----------------------------
    void UpdateRayVisuals()
    {

        if (commonLine == null || rayOrigin == null)
            return;

        commonLine.positionCount = 2;

        // Hook / Retract 中：先端は固定（grapplePoint or aimHitPointが優先）
        if (isGrappling || isRetracting)
        {
            commonLine.enabled = true;
            if (hookMaterial != null) commonLine.material = hookMaterial;

            Vector3 start = hasAimHitPoint ? aimHitPoint : grapplePoint;
            commonLine.SetPosition(0, start);
            commonLine.SetPosition(1, rayOrigin.position);
            return;
        }

        // Cling 中：Aim材質で先端固定（grapplePoint）
        if (isClinging)
        {
            commonLine.enabled = true;
            if (aimMaterial != null) commonLine.material = aimMaterial;

            Vector3 start = hasAimHitPoint ? aimHitPoint : grapplePoint;
            commonLine.SetPosition(0, start);
            commonLine.SetPosition(1, rayOrigin.position);
            return;
        }

        // 通常 Aim 表示
        UpdateAimRayFixed();
    }

    // Aim 更新（ヒット点を保持する）
    void UpdateAimRayFixed()
    {
        if (commonLine == null || rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            aimHitPoint = hit.point;
            hasAimHitPoint = true;

            commonLine.material = aimMaterial;
            commonLine.SetPosition(0, aimHitPoint);            // ここも先端を 0 に揃える
            commonLine.SetPosition(1, rayOrigin.position);
            return;
        }

        if (hasAimHitPoint)
        {
            float dist = Vector3.Distance(rayOrigin.position, aimHitPoint);
            if (dist <= maxWireLength)
            {
                commonLine.material = aimMaterial;
                commonLine.SetPosition(0, aimHitPoint);
                commonLine.SetPosition(1, rayOrigin.position);
                return;
            }
            hasAimHitPoint = false;
            aimHitPoint = Vector3.zero;
        }

        Vector3 endPoint = rayOrigin.position + rayOrigin.forward * maxWireLength;
        commonLine.material = aimMaterial;
        commonLine.SetPosition(0, endPoint); // no hit -> start at fixed forward point
        commonLine.SetPosition(1, rayOrigin.position);
    }

    // UI / ゲームレイ切替
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

    // ヒット地点からの離脱で自動解除
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

    // ヒット地点でパーティクルを再生
    void PlayHookHitParticle(Vector3 position, Vector3 normal)
    {
        if (hookHitParticle == null) return;

        // パーティクルの位置と向き（壁の法線方向）を設定
        hookHitParticle.transform.position = position;
        hookHitParticle.transform.rotation = Quaternion.LookRotation(normal); // 法線方向に回転

        // 一度停止し、すぐに再生することで、パーティクルをリセット
        hookHitParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hookHitParticle.Play();
    }

    // パーティクルを停止
    void StopHookHitParticle()
    {
        if (hookHitParticle == null) return;

        // パーティクルを停止（必要に応じて、終了するまで残りのエミットを待つ）
        // 今回は移動開始・解除時なので即座に消すか、自然に消えるまでエミットのみ停止するなどが考えられます
        hookHitParticle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    /// <summary>
    /// ヒットしたオブジェクトのタグがフック無効リストに含まれているかチェックする
    /// </summary>
    bool IsTagInvalidForHook(string tag)
    {
        if (hookInvalidTags == null) return false;

        foreach (string invalidTag in hookInvalidTags)
        {
            // ヒットしたタグが除外リスト内のタグと一致するかチェック
            if (tag.Equals(invalidTag, System.StringComparison.Ordinal))
            {
                return true; // 除外タグに含まれる
            }
        }
        return false; // 除外タグに含まれない
    }

    /// <summary>
    /// 現在の視点位置にメニューを表示した際に壁と衝突するかをチェックする
    /// </summary>
    private bool IsMenuCollidingWithWall()
    {
        if (playerRoot == null) return false;

        Ray ray = new Ray(playerRoot.position, playerRoot.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, menuCanvasDistance, wallLayer))
        {
            // 壁がキャンバスの配置距離よりも手前にあるため、レイを遮断します。
            Debug.Log($"[VRController] Collision Check: Wall hit at {hit.distance:F2}m. Menu is blocked by the wall.");
            return true;
        }
        return false;
    }

    /// <summary>
    /// 壁にめり込む場合、安全な方向に視点（リグ）を強制的に回転させる
    /// </summary>
    private void ForciblyRotateToSafeDirection()
    {
        if (playerRoot == null) return;

        // 現在のY軸回転から指定角度分（例: 180度）回転
        Quaternion safeRotation = Quaternion.Euler(
            0,
            playerRoot.rotation.eulerAngles.y + forcedRotationAngle,
            0
        );

        playerRoot.rotation = safeRotation;
        Debug.Log("[VRController] 壁を避けるため、視点を強制的に回転させました。");
    }

    /// <summary>
    /// 仮想的に回転させた場合に壁に衝突するかを予測チェックする
    /// </summary>
    private bool IsFutureRotationColliding(float futureYRotation)
    {
        if (playerRoot == null) return false;

        // 一時的にリグの回転を保存
        Quaternion originalRotation = playerRoot.rotation;

        // 一時的に回転を適用
        playerRoot.rotation = Quaternion.Euler(0, futureYRotation, 0);

        bool collision = IsMenuCollidingWithWall();

        // 回転を元に戻す
        playerRoot.rotation = originalRotation;

        return collision;
    }

    /// <summary>
    /// メニューが開いた時、壁に衝突してないかの確認
    /// </summary>
    /// <param name="isOpen"></param>
    public void SetMenuRotationState(bool isOpen)
    {
        isMenuRotationLocked = isOpen;

        if (isOpen)
        {
            if (IsMenuCollidingWithWall())
            {
                ForciblyRotateToSafeDirection();
            }
        }
    }

    // 外部強制解除
    public void ForceReleaseHook()
    {
        ReleaseHook();
    }
}

