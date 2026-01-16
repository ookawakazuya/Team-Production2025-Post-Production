using UnityEditor.TerrainTools;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using System.Collections;
using UnityEngine.EventSystems;
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
    [Header("レイのオブジェクト")]
    [SerializeField] private GameObject rayVisualObject;//フックレイのオブジェクト

    [Header("XR Interaction Toolkit 連携")]
    [SerializeField] XRRayInteractor targetInteractor;  //インスペクターでレイを表示しているデフォルトのスクリプトを参照する
    private XRInteractorLineVisual gameLineVisual;

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

    [Header("3Dモデルの実装部分")]
    [SerializeField] Transform hookObject;
    [SerializeField] bool showHookOnly = false;
    [SerializeField] float HookScaleOrigin = 0.5f;
    [SerializeField] float HookScale = 5.0f;


    [Header("フック解除時のモデル切り替え")]
    [SerializeField] GameObject normalHookModel;         //通常モデル
    [SerializeField] GameObject flyingHookModel;       //解除モデル

    private bool isSwitchingModel = false;


    [Header("宝箱操作設定")]
    [SerializeField] string chestTag = "Chest";
    ChestLid currentChestLid;
    float lastControllerY;


    // 入力アセット（自作の VRHookActions）
    VRHookActions HookMap;

    // 状態フラグ
    bool isGrappling = false;      // フックが刺さっている（命中している）状態（見た目用）
    bool isRetracting = false;     // ワイヤー移動中（引き寄せ中）
    public bool isClinging = false;       // 壁に張り付いている
    bool isGameRayEnabled = true;  // ゲーム用レイ有効フラグ（UI切替用）
    private bool isHookActive = false;


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
    [SerializeField] GameObject menuCanvasObject;     // 必要であればキャンバス自体の参照
    [SerializeField] LayerMask wallLayer;             // 壁や障害物のレイヤーを設定
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
    float currentRayLength = 0f;    //内部計算用//

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

        if (targetInteractor != null)
        {
            targetInteractor.maxRaycastDistance = maxWireLength;
            Debug.Log($"レイの最大距離を{maxWireLength}に変更しました。");
        }

        hookBreakDistance = Mathf.Max(hookBreakDistance, 0.1f);

        //パーティクルシステムの初期設定 
        if (hookHitParticle == null)
        {
            Debug.LogWarning("[VRController] hookHitParticle が Inspector に割り当てられていません。");
        }
    }

    void OnEnable() => HookMap?.Enable();
    void OnDisable() => HookMap?.Disable();

    private void Start()
    {

        //ResetRotationOnStart();
        StartCoroutine(RecenterAtStart());
        // XRDevice（旧式）や最新の XRInputSubsystem を使ったリセンター処理
        //UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye).subsystem.TryRecenter();
        UpdateRayVisuals(maxWireLength);
    }

    private void SetHookModelStatus(bool isIdle)
    {
        if(normalHookModel != null) normalHookModel.SetActive(isIdle);
        if(flyingHookModel != null) flyingHookModel.SetActive(!isIdle);
    }


    /// <summary>
    /// ゲーム開始時にプレイヤーの向きを正面にする
    /// </summary>
    void ResetRotationOnStart()
    {
        if (playerRoot != null)
        {
            Vector3 currentEuler = playerRoot.eulerAngles;

            //Y軸の固定
            currentEuler.y = 0f;

            playerRoot.eulerAngles = currentEuler;
            Debug.Log("[VRController] ゲーム開始に伴い、プレイヤーのY軸回転を0にリセットしました。");
        }

        {
            Debug.LogWarning("[VRController] playerRootが割り当てられていないため、回転のリセットに失敗しました。");
        }
    }

    private IEnumerator RecenterAtStart()
    {
        yield return null;

        if (playerRoot != null)
        {
            Vector3 euler = playerRoot.eulerAngles;
            euler.y = 0f;
            playerRoot.eulerAngles = euler;
        }

        var centerEye = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(UnityEngine.XR.XRNode.CenterEye);

        if (centerEye.isValid && centerEye.subsystem != null)
        {
            centerEye.subsystem.TryRecenter();
            Debug.Log("[VRController] HMDのリセンターに成功しました。");

        }

    }

    void Update()
    {
        ReadInputs();
        HandleCameraRotation();

        CheckForChest();

        if (currentChestLid != null && triggerPressed)
        {
            HandleChestInteraction();
            return;
        }


        if (!isGameRayEnabled)
        {
            SetCommonLineEnabled(false);
            prevTriggerPressed = triggerPressed;
            prevGripPressed = gripPressed;
            return;
        }

        HandleTriggerPriority();
        UpdateStateMachine();
        HandleHookBreakCheck();

        if (isClinging)
        {
            fallSpeed = 0f;
            clingTimer -= Time.deltaTime;
            if (clingTimer <= 0f) { EndClingAndFall(); }
        }
        else
        {
            if (isRetracting) AccelerateTowardsHook();
            else ApplyGravity();
        }

        // --- レイの表示更新 ---
        // 通常時は maxWireLength、フック中などは currentRayLength (またはヒット点までの距離)
        float lengthToDraw = (isGrappling || isRetracting) ? Vector3.Distance(rayOrigin.position, grapplePoint) : maxWireLength;

        // 外部（アニメーション等）から currentRayLength が指定されている場合はそちらを最優先
        if (currentRayLength > 0)
        {
            lengthToDraw = currentRayLength;
        }

        UpdateRayVisuals(lengthToDraw);
        // ----------------------

        if (waitingRetractStart)
        {
            retractDelayTime += Time.deltaTime;
            if (retractDelayTime >= retractStartDelay)
            {
                waitingRetractStart = false;
                retractDelayTime = 0f;
                StartRetract();
            }
        }

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
        SoundManager.Instance.PlaySE("SE_Hook_01");
        // Cling 中かつトリガーDownなら「一時フック」を狙う（Cling 維持）
        if (isClinging)
        {
              ShootHook_FromCling();
            return;
        }

        // 通常時はトリガーでフック射出（まだ刺さっておらず巻取り中でない場合）
        if (!isGrappling && !isRetracting)
        {
              ShootHook();
            return;
        }
    }

    // トリガー解放（Up）の処理
    void OnTriggerUp()
    {
        if (isClinging && isHookActive)
        {
            isHookActive = false;
            isGrappling = false;
            tempGrappleFromCling = false;
            // isClinging は true のままなので、UpdateRayVisuals が自動で AimMaterial に戻します
            return;
        }

        if (isClinging) return;

        // 通常時：刺さっているが巻取り中でなければトリガー放しでフック解除
        if (!isRetracting )
        {
            ReleaseHook();
        }
        // 巻取り中にトリガーを放したらキャンセル（仕様）
        else
        {
            Debug.Log("[VRController] 巻取り中にトリガー放し -> ReleaseHook (巻取りキャンセル)");
            ReleaseHook();
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


    //宝箱がレイ上にあるかのチェック
    void CheckForChest()
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out  hit, maxWireLength))
        {
            if (hit.collider.CompareTag(chestTag))
            {
                ChestLid foundLid = hit.collider.GetComponentInParent<ChestLid>();

                if (foundLid != null)
                {
                    currentChestLid = foundLid;
                    return;
                }
            }
        }
        // 何もヒットしていないときは、保持していた蓋のフラグを折る
        if (currentChestLid != null && !triggerPressed)
        {
            currentChestLid.StopInteracting();
            currentChestLid = null;
        }
    }


    void HandleChestInteraction()
    {
        //前フレームからの高さ差分を計算
        float currentY = rayOrigin.position.y;
        if (currentChestLid != null)
        {
            if(currentChestLid.RayAnchorpoint != null)
            {
                UpdateRayToChestAnchor(currentChestLid.RayAnchorpoint.position);
            }

            if (triggerPressed)
            {
                float deltaY = currentY - lastControllerY;
                    currentChestLid.UpdateRotation(deltaY);
            }
            else if (prevTriggerPressed && !triggerPressed)
            {
                currentChestLid.StopInteracting();
            }
        }
            //現在の高さを保存
            lastControllerY = currentY;
    }

    void UpdateRayToChestAnchor(Vector3 anchorPos)
    {
        if (commonLine != null)
        {
            commonLine.enabled = true;
            commonLine.positionCount = 2;

            commonLine.SetPosition(0, anchorPos);
            commonLine.SetPosition(1,rayOrigin.position);
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
                ResetHookStateOnMiss();
                return; // ヒットを無視し、フックが刺さらない
            }

            SoundManager.Instance.PlaySE("SE_Hook_02");

            //ヒット時モデルを切り替え
            SetHookModelStatus(isIdle: false);
            //回転の修正
            if (flyingHookModel != null) 
            {
                flyingHookModel.transform.position = hit.point;
                flyingHookModel.transform.rotation = Quaternion.LookRotation(hit.point, Vector3.up);
                flyingHookModel.transform.Rotate(90f, 0f, 0f);
            }


            isHookActive = true;
            isGrappling = true;
            grapplePoint = hit.point;
            aimHitPoint = hit.point;     // Aim 先端をセット（先端固定用）
            hasAimHitPoint = true;
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
            ResetHookStateOnMiss();
            Debug.Log("[VRController] ShootHook: Raycast miss");
        }
    }

    void ResetHookStateOnMiss()
    {
        isGrappling = false;
        isRetracting = false;
        hasAimHitPoint = false;

        if (commonLine != null)
        {
            commonLine.enabled = false;
            if (aimMaterial != null) commonLine.material = aimMaterial;
        }
        StopHookHitParticle();
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
            isHookActive = true;
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
        isGrappling = false;
        isHookActive = false;
        isClinging = true;

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
        isHookActive = false;
        isGrappling = false;
        isRetracting = false;
        isClinging = false;
        tempGrappleFromCling = false;
        hasAimHitPoint = false;

        currentSpeed = 0f;

        SetHookModelStatus(isIdle: true);

        // スクリプトを確実に有効に戻す
        if (gameLineVisual != null) gameLineVisual.enabled = true;

        if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;
        SoundManager.Instance.StopSELoop();
        StopHookHitParticle();
        Debug.Log("[VRController] ReleaseHook: フック解除して標準ビジュアルを再有効化");
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
    private void UpdateRayVisuals(float length)
    {
        // インターラクターの最大距離のみ同期
        if (targetInteractor != null) targetInteractor.maxRaycastDistance = length;

        if (commonLine != null)
        {
            // UIメニュー中でなければLineRendererを表示
            commonLine.enabled = isGameRayEnabled;

            // 頂点数を2に固定
            if (commonLine.positionCount != 2) commonLine.positionCount = 2;

            Vector3 currentTipPosition;

            // ワイヤー発射中（isGrappling）または 移動中（isRetracting）
            // ※トリガーを押して命中した瞬間からここに入ります
            if (isHookActive || isRetracting)
            {
                if (gameLineVisual != null) gameLineVisual.enabled = false;
                if (rayVisualObject != null) rayVisualObject.SetActive(true);


                currentTipPosition = hasAimHitPoint ? aimHitPoint : grapplePoint;
                //オブジェクトをヒット視点と始点の中心に配置する
                Vector3 midPoint = (rayOrigin.position + currentTipPosition) / 2;

                if (rayVisualObject != null)
                {
                    rayVisualObject.transform.position = midPoint;
                    //オブジェクトを中心点の方向に向ける
                    rayVisualObject.transform.LookAt(currentTipPosition);

                    //距離に合わせてスケールの変更
                    float distance = Vector3.Distance(rayOrigin.position, currentTipPosition);
                    Vector3 newScale = rayVisualObject.transform.localScale;

                    newScale.z = distance;
                    rayVisualObject.transform.localScale = newScale;
                }
                if(hookObject != null)
                {
                    hookObject.position = currentTipPosition;
                    // フックの向きを手元に向ける（必要に応じて調整）
                   // hookObject.rotation = Quaternion.LookRotation(hasAimHitPoint, Vector3.up);
                    hookObject.Rotate(-90f, 0f, 0f);

                    // スケールを5に変更
                    hookObject.localScale = new Vector3(HookScale, HookScale, HookScale);
                }

            }
            else
            {
                // それ以外（通常待機、または張り付き中の照準状態）は AimMaterial
                if (rayVisualObject != null) rayVisualObject.SetActive(false);
                if (gameLineVisual != null && !gameLineVisual.enabled) gameLineVisual.enabled = true;
                if (aimMaterial != null) commonLine.material = aimMaterial;

                Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, length))
                {
                    currentTipPosition = hit.point;
                }
                else
                {
                    currentTipPosition = rayOrigin.position + rayOrigin.forward * length;
                }
                commonLine.SetPosition(0, rayOrigin.position + rayOrigin.forward * length);
                commonLine.SetPosition(1, rayOrigin.position);

                if (hookObject != null)
                {
                    // 通常時は RayOrigin（コントローラー）の位置に配置
                    hookObject.position = rayOrigin.position;
                    // 向きはレイの進行方向（前方）を向かせる
                    hookObject.forward = rayOrigin.forward;

                    hookObject.Rotate(90f, 0f, 0f); // X軸に90度回転を追加

                    hookObject.localScale = new Vector3(HookScaleOrigin, HookScaleOrigin, HookScaleOrigin);
                }
            }
        }
    }

    // Aim 更新（ヒット点を保持する）
    void UpdateAimRayFixed(float dynamicLength)
    {

        currentRayLength = dynamicLength;
        UpdateRayVisuals(currentRayLength);
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

    //private IEnumerator SwitchHookModelRoutine()
    //{
    //    if(isSwitchingModel) yield break;
    //    isSwitchingModel = true;

    //    if (normalHookModel != null) normalHookModel.SetActive(false);
    //    if(flyingHookModel != null) flyingHookModel.SetActive(true);

    //    if(normalHookModel != null) normalHookModel.SetActive(true);
    //    if (flyingHookModel != null) flyingHookModel.SetActive(false);

    //    isSwitchingModel =false;

    //}

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

    void SetHookRotationCorrectly(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hookObject == null) return;

        //壁の法線を正面とする方向を作成
        Quaternion targetRotation = Quaternion.LookRotation(hitNormal, Vector3.up);

        hookObject.rotation = targetRotation;

        //
        hookObject.position = hitPoint;


    }

    /// <summary>
    /// 現在の視点位置にメニューを表示した際に壁と衝突するかをチェックする
    /// </summary>
    private bool IsMenuCollidingWithWall()
    {
        /*
        if (playerRoot == null) return false;

        Ray ray = new Ray(playerRoot.position, playerRoot.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, menuCanvasDistance, wallLayer))
        {
            // 壁がキャンバスの配置距離よりも手前にあるため、レイを遮断します。
            Debug.Log($"[VRController] Collision Check: Wall hit at {hit.distance:F2}m. Menu is blocked by the wall.");
            return true;
        }
        return false;*/
        if (playerRoot == null) return false;
        Ray ray = new Ray(playerRoot.position, playerRoot.forward);
        return Physics.Raycast(ray, menuCanvasDistance, wallLayer);
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
        if (menuCanvasObject != null)
        {
            // キャンバスの位置を、リグから前方に設定距離分移動した場所に設定
            Vector3 newCanvasPosition = playerRoot.position + playerRoot.forward * menuCanvasDistance;

            // キャンバスのZ軸はプレイヤーの視線方向（forward）に向ける
            Quaternion newCanvasRotation = Quaternion.LookRotation(playerRoot.forward);

            // Canvasの位置と回転を適用
            menuCanvasObject.transform.position = newCanvasPosition;
            menuCanvasObject.transform.rotation = newCanvasRotation;

            Debug.Log("[VRController] メニューキャンバスを新しい視点位置に移動しました。");
        }
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

