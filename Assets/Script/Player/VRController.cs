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
    // --- フィールド定義（変更なし） ---
    [Header("参照設定")]
    [SerializeField] Transform rayOrigin;
    [SerializeField] LineRenderer commonLine;
    [SerializeField] Material aimMaterial;
    [SerializeField] Material hookMaterial;
    [SerializeField] Material NullMaterial;
    [SerializeField] Material ChestMaterial;
    [SerializeField] CharacterController characterController;
    [SerializeField] Transform playerRoot;

    [Header("フック / レイ設定")]
    [SerializeField] float maxWireLength = 50f;
    [SerializeField] float acceleration = 20f;
    [SerializeField] float maxMoveSpeed = 30f;
    [SerializeField] float stopDistance = 1f;
    [Header("レイのオブジェクト")]
    [SerializeField] private GameObject rayVisualObject;
    [SerializeField] float wireModelScaleFactor = 0.01f;
    private Vector3 initialWireScale;

    [Header("XR Interaction Toolkit 連携")]
    [SerializeField] XRRayInteractor targetInteractor;
    private XRInteractorLineVisual gameLineVisual;

    [Header("張り付き / 重力")]
    [SerializeField] float clingDuration = 5f;
    [SerializeField] float gravity = -5f;
    [SerializeField] float maxFallSpeed = -50f;

    [Header("ワイヤー移動遅延設定")]
    [SerializeField] float retractStartDelay = 0.3f;
    float retractDelayTime = 0f;
    bool waitingRetractStart = false;

    [Header("その他")]
    [SerializeField] string wallTag = "Wall";

    [Header("フック解除条件")]
    [SerializeField] float hookBreakDistance = 2.0f;

    [Header("フック無効化タグ")]
    [SerializeField] string[] hookInvalidTags;

    [SerializeField] XRInteractorLineVisual lineVisual;
    [SerializeField] HapticController haptic;
    [SerializeField] bool isRightHand = true;

    [Header("3Dモデルの実装部分")]
    [SerializeField] Transform hookObject;
    [SerializeField] bool showHookOnly = false;
    [SerializeField] float HookScaleOrigin = 0.5f;
    [SerializeField] float HookScale = 5.0f;

    [Header("フック解除時のモデル切り替え")]
    [SerializeField] GameObject normalHookModel;
    [SerializeField] GameObject flyingHookModel;

    Transform originalHookParent;
    private bool isSwitchingModel = false;

    [Header("宝箱操作設定")]
    [SerializeField] string chestTag = "Chest";
    ChestLid currentChestLid;
    float lastControllerY;
    [SerializeField] float MaxChestRay = 0.3f;
    [SerializeField] float maxinteractionDistance = 3.0f;


    VRHookActions HookMap;

    bool isGrappling = false;
    bool isRetracting = false;
    public bool isClinging = false;
    bool isGameRayEnabled = true;
    private bool isHookActive = false;

    bool tempGrappleFromCling = false;

    Vector3 grapplePoint;
    Vector3 aimHitPoint;
    bool hasAimHitPoint = false;
    bool wasGrounded = true;

    float clingTimer = 0f;
    float currentSpeed = 0f;
    float fallSpeed = 0f;

    [Header("視点移動")]
    [SerializeField] float rotationSpeed = 45f;

    [Header("メニュー連携 / 衝突回避")]
    [SerializeField] VRMenuManager menuManager;
    [SerializeField] GameObject menuCanvasObject;
    [SerializeField] LayerMask wallLayer;
    [SerializeField] float menuCanvasDistance = 0.5f;
    [SerializeField] float forcedRotationAngle = 180f;

    private bool isMenuRotationLocked = false;

    Vector2 rightStickInput = Vector2.zero;
    bool rightStickPressed = false;
    bool triggerPressed = false;
    bool prevTriggerPressed = false;
    bool gripPressed = false;
    bool prevGripPressed = false;
    bool cancelPressed = false;
    float currentRayLength = 0f;

    [Header("サウンド関係")]
    [SerializeField] float minLandingSpeed = -1.0f;

    [Header("エフェクト")]
    [SerializeField] ParticleSystem hookHitParticle;

    [Header("シーンの初期設定")]
    [SerializeField] bool startInUIMode = false;

    public bool IsRetracting => isRetracting;
    public bool IsClinging => isClinging;
    public bool IsGrappling => isGrappling;

    // ==========================================
    // 1. ライフサイクル・初期化ルーチン
    // ==========================================

    void Awake()
    {
        HookMap = new VRHookActions();
        if (commonLine != null)
        {
            commonLine.positionCount = 2;
            commonLine.enabled = true;
            if (aimMaterial != null) commonLine.material = aimMaterial;
        }
        if (targetInteractor != null) targetInteractor.maxRaycastDistance = maxWireLength;
        hookBreakDistance = Mathf.Max(hookBreakDistance, 0.1f);
    }

    void OnEnable() => HookMap?.Enable();
    void OnDisable() => HookMap?.Disable();

    void Start()
    {
        StartCoroutine(RecenterAtStart());
        if (startInUIMode) SwitchToUIMode();
        else
        {
            UpdateRayVisuals(maxWireLength);
            SetHookModelStatus(isIdle: true);
        }
        if (rayVisualObject != null) initialWireScale = rayVisualObject.transform.localScale;
        if (hookObject != null) originalHookParent = hookObject.parent;
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
        if (centerEye.isValid && centerEye.subsystem != null) centerEye.subsystem.TryRecenter();
    }

    void ResetRotationOnStart()
    {
        if (playerRoot != null)
        {
            Vector3 currentEuler = playerRoot.eulerAngles;
            currentEuler.y = 0f;
            playerRoot.eulerAngles = currentEuler;
        }
    }

    // ==========================================
    // 2. 更新メインループ・入力管理
    // ==========================================

    void Update()
    {
        ReadInputs();
        if (isGameRayEnabled) HandleCameraRotation();

        CheckForChest();
        if (currentChestLid != null && triggerPressed)
        {
            HandleChestInteraction();
            prevTriggerPressed = triggerPressed;
            prevGripPressed = gripPressed;
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

        UpdateClingAimWhileHolding();

        if (isClinging)
        {
            fallSpeed = 0f;
            clingTimer -= Time.deltaTime;
            if (clingTimer <= 0f) EndClingAndFall();
        }
        else
        {
            if (isRetracting) AccelerateTowardsHook();
            else ApplyGravity();
        }

        float lengthToDraw = (isGrappling || isRetracting) ? Vector3.Distance(rayOrigin.position, grapplePoint) : maxWireLength;
        if (currentRayLength > 0) lengthToDraw = currentRayLength;

        UpdateRayVisuals(lengthToDraw);

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

    void ReadInputs()
    {
        if (HookMap == null) return;
        triggerPressed = HookMap.VR.HookShoot.ReadValue<float>() > 0.5f;
        gripPressed = HookMap.VR.Retract.ReadValue<float>() > 0.5f;
        rightStickInput = HookMap.VR.RightStick.ReadValue<Vector2>();
        rightStickPressed = HookMap.VR.RightStickPress.ReadValue<float>() > 0.5f;
        cancelPressed = HookMap.VR.Cancel.ReadValue<float>() > 0.5f;
    }

    void HandleTriggerPriority()
    {
        if (triggerPressed && !prevTriggerPressed) OnTriggerDown();
        if (!triggerPressed && prevTriggerPressed) OnTriggerUp();
    }

    void OnTriggerDown()
    {
        SoundManager.Instance.PlaySE("SE_Hook_01");
        if (isClinging) { ShootHook_FromCling(); return; }
        if (!isGrappling && !isRetracting) ShootHook();
    }

    void OnTriggerUp()
    {
        if (isClinging && isHookActive)
        {
            isHookActive = false;
            isGrappling = false;
            tempGrappleFromCling = false;
            return;
        }
        if (isClinging) return;
        ReleaseHook();
    }

    void UpdateStateMachine()
    {
        if (isGrappling && gripPressed && !prevGripPressed && !isRetracting)
        {
            waitingRetractStart = true;
            retractDelayTime = 0f;
            return;
        }
        if (isClinging)
        {
            if (gripPressed && !prevGripPressed)
            {
                if (tempGrappleFromCling && isGrappling) StartRetract();
                else EndClingAndFall();
            }
            return;
        }
        if (isRetracting && !triggerPressed) ReleaseHook();
    }

    // ==========================================
    // 3. フック・引き寄せ・移動ロジック
    // ==========================================

    void ShootHook()
    {
        if (rayOrigin == null) return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            if (IsTagInvalidForHook(hit.collider.tag)) { ResetHookStateOnMiss(); return; }
            SoundManager.Instance.PlaySE("SE_Hook_02");
            SetHookModelStatus(isIdle: false);
            if (hookObject != null) hookObject.SetParent(null);

            isHookActive = true;
            isGrappling = true;
            grapplePoint = hit.point;

            if (flyingHookModel != null)
            {
                flyingHookModel.transform.position = hit.point;
                flyingHookModel.transform.rotation = Quaternion.LookRotation(hit.point, Vector3.up);
                flyingHookModel.transform.Rotate(90f, 0f, 0f);
            }

            aimHitPoint = hit.point;
            hasAimHitPoint = true;
            isRetracting = false;
            isClinging = false;
            tempGrappleFromCling = false;
            if (hookMaterial != null) commonLine.material = hookMaterial;
            if (haptic != null) haptic.VibrateWallHit(isRightHand);
            PlayHookHitParticle(grapplePoint, hit.normal);
        }
        else ResetHookStateOnMiss();
    }

    void ShootHook_FromCling()
    {
        if (rayOrigin == null) return;
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            if (IsTagInvalidForHook(hit.collider.tag)) return;

            // --- 状態の確定 ---
            aimHitPoint = hit.point;    // ここで先端座標を保存
            grapplePoint = hit.point;  // 念のため同期
            hasAimHitPoint = true;     // 座標があることを明示

            isGrappling = true;
            isHookActive = true;
            tempGrappleFromCling = true; // このフラグが描画モードを切り替える
            isRetracting = false;

            // --- モデルの表示切り替え ---
            SetHookModelStatus(isIdle: false); // flyingHookModel を有効化

            SoundManager.Instance.PlaySE("SE_Hook_02");
            if (haptic != null) haptic.VibrateWallHit(isRightHand);
            //if (hookMaterial != null) commonLine.material = hookMaterial;
            PlayHookHitParticle(aimHitPoint, hit.normal);
        }
    }

    void StartRetract()
    {
        if (!isGrappling) return;
        SoundManager.Instance.PlaySELoop("SE_Hook_03");
        isRetracting = true;
        currentSpeed = 0f;
        hasAimHitPoint = false;
        tempGrappleFromCling = false;
        isClinging = false;
        SoundManager.Instance.StopSE();
        StopHookHitParticle();
    }

    void AccelerateTowardsHook()
    {
        if (characterController == null) return;
        Vector3 direction = grapplePoint - transform.position;
        float distance = direction.magnitude;

        if (distance > stopDistance)
        {
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);
            characterController.Move(direction.normalized * currentSpeed * Time.deltaTime);
            if (haptic != null) haptic.VibrateRetracting(isRightHand);
        }
        else
        {
            Collider[] cols = Physics.OverlapSphere(grapplePoint, 0.1f);
            bool hitWall = false;
            GameObject hitObj = null;
            foreach (var c in cols)
            {
                if (c != null && c.gameObject.CompareTag(wallTag)) { hitWall = true; hitObj = c.gameObject; break; }
            }
            if (hitWall) { StartCling(grapplePoint, hitObj); SoundManager.Instance.StopSELoop(); }
            else ReleaseHook();
        }
    }

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
        if (haptic != null) haptic.VibrateArrivedWall(isRightHand);
        SoundManager.Instance.PlaySE("SE_Harituki");
    }

    void EndClingAndFall()
    {
        isClinging = false;
        ReleaseHook();
        SoundManager.Instance.StopSE();
    }

    public void ReleaseHook()
    {
        isHookActive = false;
        isGrappling = false;
        isRetracting = false;
        isClinging = false;
        tempGrappleFromCling = false;
        hasAimHitPoint = false;
        currentSpeed = 0f;
        if (hookObject != null && originalHookParent != null)
        {
            hookObject.SetParent(originalHookParent);
            hookObject.localPosition = Vector3.zero;
            hookObject.localRotation = Quaternion.identity;
        }
        fallSpeed = 0f;

        SetHookModelStatus(isIdle: true);
        if (gameLineVisual != null) gameLineVisual.enabled = true;
        if (commonLine != null && aimMaterial != null) commonLine.material = aimMaterial;
        SoundManager.Instance.StopSELoop();
        StopHookHitParticle();
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

    void ApplyGravity()
    {
        if (characterController == null) return;
        bool isCurrentlyGrounded = characterController.isGrounded;
        if (isCurrentlyGrounded)
        {
            if (!wasGrounded && fallSpeed < minLandingSpeed)
            {
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
        wasGrounded = isCurrentlyGrounded;
    }

    void HandleGravity()
    {
        // フック移動中、引き寄せ中、張り付き中は重力を加算させない
        if (isRetracting || isClinging || isGrappling)
        {
            fallSpeed = -1f; // 状態維持中は常に 0 で固定
            return;
        }

        // 接地していない場合のみ重力を加算
        if (characterController.isGrounded)
        {
            if (fallSpeed < 0) fallSpeed = -5f; // 接地時は少しだけ押し付ける
        }
        else
        {
            fallSpeed += gravity * Time.deltaTime; // 落下速度の加算
        }
    }


    // ==========================================
    // 4. ビジュアル・エフェクト・モデル制御
    // ==========================================

    private void UpdateRayVisuals(float length)
    {
        if (targetInteractor != null) targetInteractor.maxRaycastDistance = length;
        if (commonLine == null) return;

        commonLine.enabled = isGameRayEnabled;
        if (commonLine.positionCount != 2) commonLine.positionCount = 2;

        Vector3 currentTipPosition = Vector3.zero;
        bool isInteractingWithChest = currentChestLid != null && triggerPressed;

        // tempGrappleFromCling 中は isClinging よりもこちらを優先
        bool isWiredState = (isHookActive || isRetracting || tempGrappleFromCling) && !isInteractingWithChest;
        if (isWiredState)
        {
            // 射出時に確定した aimHitPoint を終点として表示し続ける
            currentTipPosition = aimHitPoint;
            // --- ワイヤー・フック表示フェーズ ---
            commonLine.enabled = false;

            if (rayVisualObject != null)
            {
                rayVisualObject.SetActive(true);
                // 射出時に保存した座標をそのまま使う（動かさない）
                //currentTipPosition = aimHitPoint;

                // ワイヤー本体の配置と長さ更新
                float distance = Vector3.Distance(rayOrigin.position, currentTipPosition);
                rayVisualObject.transform.position = (rayOrigin.position + currentTipPosition) / 2f;
                rayVisualObject.transform.LookAt(currentTipPosition);
                rayVisualObject.transform.localScale = new Vector3(initialWireScale.x, initialWireScale.y, distance * wireModelScaleFactor);
            }

            // 先端にフックモデルを表示し、座標を更新する
            if (hookObject != null)
            {
                // モデルを表示状態にする
                hookObject.gameObject.SetActive(true);
                // 先端座標（aimHitPoint）に配置
                hookObject.position = currentTipPosition;
                // 手元を向くように回転
                hookObject.LookAt(rayOrigin.position);
                hookObject.Rotate(-90f, 0f, 0f);
                hookObject.localScale = new Vector3(HookScale, HookScale, HookScale);
            }
        }
        else
        {

            if (rayVisualObject != null) rayVisualObject.SetActive(false);

            // 宝箱操作中または射出していない張り付き中はモデルを手元に戻す
            if (isInteractingWithChest || isClinging) SetHookModelStatus(isIdle: true);

            // --- ここからマテリアル切り替えロジックの再実装 ---
            Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, length))
            {
                // 何かにヒットしている場合は「狙い中」のマテリアル
                currentTipPosition = hit.point;
                // ヒットしたオブジェクトのタグが「無効化タグ」に含まれているかチェック
                if (IsTagInvalidForHook(hit.collider.tag))
                {
                    // 無効なターゲットの場合は NullMaterial を適用
                    if (NullMaterial != null) commonLine.material = NullMaterial;
                    else if (aimMaterial != null) commonLine.material = aimMaterial;
                }
                else
                {
                    // 有効なターゲット（フックが刺さる場所）の場合は aimMaterial を適用
                    if (aimMaterial != null) commonLine.material = aimMaterial;
                }
            }
            else
            {
                currentTipPosition = rayOrigin.position + rayOrigin.forward * length;

                // 何もヒットしていない場合は「無効」のマテリアル
                if (NullMaterial != null)
                {
                    Debug.Log("それ以外の処理");
                    commonLine.material = NullMaterial;
                }
                else if (aimMaterial != null) commonLine.material = aimMaterial;

                            }

            if (!isInteractingWithChest)
            {
                commonLine.SetPosition(0, currentTipPosition);
                commonLine.SetPosition(1, rayOrigin.position);
            }

            // フックモデルを手元の座標（rayOrigin）に固定
            if (hookObject != null)
            {
                hookObject.position = rayOrigin.position;
                hookObject.forward = rayOrigin.forward;
                hookObject.Rotate(90f, 0f, 0f);
                hookObject.localScale = new Vector3(HookScaleOrigin, HookScaleOrigin, HookScaleOrigin);
            }
        }
    }
    // 壁に到達した時の処理（既存の移動ロジック内で呼ばれている箇所を想定）
    void OnClingToWall()
    {
        isClinging = true;
        isRetracting = false;
        isHookActive = false;
        tempGrappleFromCling = false;

        // 壁に張り付いた瞬間、先端のフックモデルを非表示（手元へ戻す）
        SetHookModelStatus(isIdle: true);
    }


    void UpdateAimRayFixed(float dynamicLength)
    {
        currentRayLength = dynamicLength;
        UpdateRayVisuals(currentRayLength);
    }

    private void SetHookModelStatus(bool isIdle)
    {
        if (normalHookModel != null) normalHookModel.SetActive(isIdle);
        if (flyingHookModel != null) flyingHookModel.SetActive(!isIdle);
    }

    void SetHookRotationCorrectly(Vector3 hitPoint, Vector3 hitNormal)
    {
        if (hookObject == null) return;
        hookObject.rotation = Quaternion.LookRotation(hitNormal, Vector3.up);
        hookObject.position = hitPoint;
    }

    void PlayHookHitParticle(Vector3 position, Vector3 normal)
    {
        if (hookHitParticle == null) return;
        hookHitParticle.transform.position = position;
        hookHitParticle.transform.rotation = Quaternion.LookRotation(normal);
        hookHitParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        hookHitParticle.Play();
    }

    void StopHookHitParticle()
    {
        if (hookHitParticle == null) return;
        hookHitParticle.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }

    // ==========================================
    // 5. カメラ回転・メニュー連携・衝突回避
    // ==========================================

    void HandleCameraRotation()
    {
        if (playerRoot == null) return;
        float x = rightStickInput.x;

        if (isMenuRotationLocked)
        {
            if (Mathf.Abs(x) > 0.2f)
            {
                float currentY = playerRoot.eulerAngles.y;
                float deltaAngle = x * rotationSpeed * Time.deltaTime;
                float futureYRotation = currentY + deltaAngle;
                if (IsFutureRotationColliding(futureYRotation)) return;
            }
        }
        if (Mathf.Abs(x) > 0.2f) playerRoot.Rotate(Vector3.up * x * rotationSpeed * Time.deltaTime);
        if (rightStickPressed)
        {
            Vector3 e = playerRoot.eulerAngles;
            e.y = 0f;
            playerRoot.eulerAngles = e;
        }
    }

    public bool CanOpenMenu()
    {
        bool isMoving = isRetracting || isClinging || isGrappling || !characterController.isGrounded;
        return !isMoving;
    }

    public void SetMenuState(bool isOpen)
    {
        isGameRayEnabled = !isOpen;
        isMenuRotationLocked = isOpen;
        if (isOpen && IsMenuCollidingWithWall()) ForciblyRotateToSafeDirection();
    }

    private bool IsMenuCollidingWithWall()
    {
        if (playerRoot == null) return false;
        Ray ray = new Ray(playerRoot.position, playerRoot.forward);
        return Physics.Raycast(ray, menuCanvasDistance, wallLayer);
    }

    private void ForciblyRotateToSafeDirection()
    {
        if (playerRoot == null) return;
        playerRoot.rotation = Quaternion.Euler(0, playerRoot.rotation.eulerAngles.y + forcedRotationAngle, 0);
        if (menuCanvasObject != null)
        {
            menuCanvasObject.transform.position = playerRoot.position + playerRoot.forward * menuCanvasDistance;
            menuCanvasObject.transform.rotation = Quaternion.LookRotation(playerRoot.forward);
        }
    }

    private bool IsFutureRotationColliding(float futureYRotation)
    {
        if (playerRoot == null) return false;
        Quaternion originalRotation = playerRoot.rotation;
        playerRoot.rotation = Quaternion.Euler(0, futureYRotation, 0);
        bool collision = IsMenuCollidingWithWall();
        playerRoot.rotation = originalRotation;
        return collision;
    }

    public void SetMenuRotationState(bool isOpen)
    {
        isMenuRotationLocked = isOpen;
        if (isOpen && IsMenuCollidingWithWall()) ForciblyRotateToSafeDirection();
    }

    public void SwitchToUIMode()
    {
        isGameRayEnabled = false;
        EnableGameRay(false);
        if (targetInteractor != null)
        {
            targetInteractor.enabled = true;
            targetInteractor.maxRaycastDistance = 10f;
        }
        SetHookModelStatus(isIdle: true);
    }

    public void SwitchToGameMode()
    {
        if (startInUIMode) return;
        isGameRayEnabled = true;
        EnableGameRay(true);
        if (targetInteractor != null) targetInteractor.maxRaycastDistance = maxWireLength;
    }

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
        if (commonLine != null) commonLine.enabled = enabled;
    }

    // ==========================================
    // 6. 宝箱・インタラクション
    // ==========================================
    void CheckForChest()
    {
        //  すでにトリガーを引いて操作中の場合は、判定を更新せずにそのまま維持する
        if (triggerPressed && currentChestLid != null)
        {
            return;
        }

        // レイを飛ばしてヒット確認
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxWireLength))
        {
            // ヒットしたオブジェクト、またはその親に ChestLid が付いているか確認
            // アンカーに MeshRenderer がある場合、その周辺のコライダーが反応します
            ChestLid foundLid = hit.collider.GetComponentInParent<ChestLid>();

            if (foundLid != null && foundLid.RayAnchorpoint != null)
            {
                //プレイヤーと宝箱の距離
                float distanceToChest = Vector3.Distance(transform.position, foundLid.transform.position);
                // 宝箱側で設定した「操作可能半径」以内かチェック
                if (distanceToChest <= foundLid.interactionRadius)
                {
                    // 宝箱から見たプレイヤーへの方向ベクトル（水平方向のみで計算）
                    Vector3 playerDir = (transform.position - foundLid.transform.position);
                    playerDir.y = 0; // 高低差を無視して水平方向で比較
                    playerDir.Normalize();

                    // 宝箱の正面ベクトル
                    Vector3 chestForward = foundLid.transform.forward;
                    chestForward.y = 0;
                    chestForward.Normalize();

                    // 2つのベクトルの内積（Dot Product）を計算
                    // 1.0 = 完全に同じ向き（0度）, 0.7 = 約45度, 0 = 90度
                    float dot = Vector3.Dot(chestForward, playerDir);

                    // dotが0.7以上（正面から左右約45度以内）の時だけ操作を許可
                    if (dot > 0.7f)
                    {
                        // アンカーポイントへのエイム判定
                        float distanceToAnchor = Vector3.Distance(hit.point, foundLid.RayAnchorpoint.position);
                        if (distanceToAnchor < MaxChestRay)
                        {
                            if (currentChestLid != foundLid)
                            {
                                if (currentChestLid != null) currentChestLid.StopInteracting();
                                currentChestLid = foundLid;
                                if (commonLine != null) commonLine.material = ChestMaterial;
                            }
                            return;
                        }
                    }
                }
            }
        }

        //  操作中でなく、かつアンカーからレイが外れた場合は操作対象をクリア
        if (!triggerPressed && currentChestLid != null)
        {
            currentChestLid.StopInteracting();
            currentChestLid = null;
        }
        // レイの色を通常（aimMaterial）に戻す
        if (commonLine != null) commonLine.material = aimMaterial;
    }

    void HandleChestInteraction()
    {
        float currentY = rayOrigin.position.y;
        if (currentChestLid != null)
        {
            if (currentChestLid.RayAnchorpoint != null) UpdateRayToChestAnchor(currentChestLid.RayAnchorpoint.position);
            if (triggerPressed && !prevTriggerPressed) lastControllerY = currentY;
            if (triggerPressed)
            {
                float deltaY = currentY - lastControllerY;
                if (Mathf.Abs(deltaY) < 0.5f) currentChestLid.UpdateRotation(deltaY);
            }
            else if (prevTriggerPressed && !triggerPressed) currentChestLid.StopInteracting();
        }
        lastControllerY = currentY;
    }

    void UpdateRayToChestAnchor(Vector3 anchorPos)
    {
        if (commonLine != null)
        {
            commonLine.enabled = true;
            commonLine.positionCount = 2;
            commonLine.SetPosition(0, anchorPos);
            commonLine.SetPosition(1, rayOrigin.position);
        }
    }

    void UpdateClingAimWhileHolding()
    {
        // 張り付き中 ＆ トリガー押しっぱなし
        if (!isClinging || !triggerPressed|| tempGrappleFromCling)
            return;

        if (rayOrigin == null)
            return;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            if (IsTagInvalidForHook(hit.collider.tag))
                return;

            aimHitPoint = hit.point;
            grapplePoint = hit.point;

            // flyingHookModel を直接動かしたい場合（保険）
            if (flyingHookModel != null && flyingHookModel.activeSelf)
            {
                hookObject.gameObject.SetActive(true); // 非表示になっていた場合に備えて
                flyingHookModel.transform.position = hit.point;
                flyingHookModel.transform.LookAt(rayOrigin.position);
                flyingHookModel.transform.Rotate(-90f, 0f, 0f);
            }
        }
    }


    // ==========================================
    // 7. 補助判定・外部インターフェース
    // ==========================================

    void HandleHookBreakCheck()
    {
        if (!(isGrappling || isRetracting || isClinging)) return;
        if (Vector3.Distance(transform.position, grapplePoint) > hookBreakDistance) ReleaseHook();
    }

    bool IsTagInvalidForHook(string tag)
    {
        if (hookInvalidTags == null) return false;
        foreach (string invalidTag in hookInvalidTags)
        {
            if (tag.Equals(invalidTag, System.StringComparison.Ordinal)) return true;
        }
        return false;
    }

    public void ForceReleaseHook()
    {
        ReleaseHook();
    }
}