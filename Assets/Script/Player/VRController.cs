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
            SoundManager.Instance.PlaySE("SE_Hook_02");
            grapplePoint = hit.point;
            aimHitPoint = hit.point;
            hasAimHitPoint = true;
            isGrappling = true;
            isRetracting = false;
            isHookActive = true;
            tempGrappleFromCling = true;
            if (haptic != null) haptic.VibrateWallHit(isRightHand);
            if (hookMaterial != null) commonLine.material = hookMaterial;
            PlayHookHitParticle(grapplePoint, hit.normal);
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

    // ==========================================
    // 4. ビジュアル・エフェクト・モデル制御
    // ==========================================

    private void UpdateRayVisuals(float length)
    {
        if (targetInteractor != null) targetInteractor.maxRaycastDistance = length;
        if (commonLine != null)
        {
            commonLine.enabled = isGameRayEnabled;
            if (commonLine.positionCount != 2) commonLine.positionCount = 2;
            Vector3 currentTipPosition = Vector3.zero;

            if (isHookActive || isRetracting)
            {
                commonLine.enabled = false;
                if (rayVisualObject != null)
                {
                    rayVisualObject.SetActive(true);
                    currentTipPosition = hasAimHitPoint ? aimHitPoint : grapplePoint;
                    float distance = Vector3.Distance(rayOrigin.position, currentTipPosition);
                    rayVisualObject.transform.position = (rayOrigin.position + currentTipPosition) / 2f;
                    rayVisualObject.transform.LookAt(currentTipPosition);
                    rayVisualObject.transform.localScale = new Vector3(initialWireScale.x, initialWireScale.y, distance * wireModelScaleFactor);
                }
                else currentTipPosition = hasAimHitPoint ? aimHitPoint : grapplePoint;

                if (hookObject != null)
                {
                    hookObject.position = currentTipPosition;
                    hookObject.LookAt(rayOrigin.position);
                    hookObject.Rotate(-90f, 0f, 0f);
                    hookObject.localScale = new Vector3(HookScale, HookScale, HookScale);
                }
            }
            else
            {
                if (rayVisualObject != null) rayVisualObject.SetActive(false);
                if (gameLineVisual != null && !gameLineVisual.enabled) gameLineVisual.enabled = true;
                if (aimMaterial != null) commonLine.material = aimMaterial;

                Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, length))
                {
                    currentTipPosition = hit.point;
                    if (aimMaterial != null) commonLine.material = aimMaterial;
                }
                else
                {
                    if (NullMaterial != null) commonLine.material = NullMaterial;
                    else if (aimMaterial != null) commonLine.material = aimMaterial;
                    currentTipPosition = rayOrigin.position + rayOrigin.forward * length;
                }
                commonLine.SetPosition(0, rayOrigin.position + rayOrigin.forward * length);
                commonLine.SetPosition(1, rayOrigin.position);

                if (hookObject != null)
                {
                    hookObject.position = rayOrigin.position;
                    hookObject.forward = rayOrigin.forward;
                    hookObject.Rotate(90f, 0f, 0f);
                    hookObject.localScale = new Vector3(HookScaleOrigin, HookScaleOrigin, HookScaleOrigin);
                }
            }
        }
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
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            if (hit.collider.CompareTag(chestTag))
            {
                ChestLid foundLid = hit.collider.GetComponentInParent<ChestLid>();
                if (foundLid != null && currentChestLid != foundLid)
                {
                    if (currentChestLid != null) currentChestLid.StopInteracting();
                    currentChestLid = foundLid;
                    return;
                }
                if (foundLid != null) return;
            }
        }
        if (currentChestLid != null && !triggerPressed)
        {
            currentChestLid.StopInteracting();
            currentChestLid = null;
        }
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
                if (Mathf.Abs(deltaY) < 1.0f) currentChestLid.UpdateRotation(deltaY);
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