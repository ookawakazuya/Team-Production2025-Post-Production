using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class HookController : MonoBehaviour
{
    [Header("プレイヤー関連")]
    [SerializeField] Camera mainCamera;
    [SerializeField] CharacterController characterController;
    [SerializeField] Transform originalCameraParent;
    [SerializeField] Transform moveCameraParent;

    [Header("ワイヤー関連設定")]
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] LayerMask hookableLayers;
    [SerializeField] LayerMask interactiveLayers;
    [SerializeField] float maxWireLength = 15f; //フックの長さ
    [SerializeField] float extendSpeed = 20f;
    [SerializeField] float retractSpeed = 25f;
    [SerializeField] float moveSpeed = 30f;
    [SerializeField] int curveSegments = 20;

    // 共通
    bool canShootHook = true;
    float coolDownTime = 1f;

    // 移動用フック
    bool isGrappling = false;
    bool isReturning = false;
    bool isRetractingAndMoving = false; // R1による巻き取り移動
    public bool IsRetetractingAndMoving => isRetractingAndMoving;
    Vector3 grapplePoint;
    Vector3 lastPosition;
    Coroutine stayOnWallCoroutine;

    // ギミック用フック
    Transform grabbedObject;

    // ダブルタップ関連
    float lastR2PressTime = 0f;
    float doubleTapTime = 0.3f; // ダブルタップと判定する時間

    void Update()
    {
        if (Gamepad.current == null) return;

        if (Gamepad.current.rightTrigger.isPressed && !isGrappling && !isRetractingAndMoving && !isReturning)
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = true;
                lineRenderer.positionCount = 2;
                Vector3 origin = mainCamera.transform.position;
                Vector3 direction = mainCamera.transform.forward;
                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, origin + direction * maxWireLength);
            }
        }
        else
        {
            if (lineRenderer != null && lineRenderer.enabled)
            {
                lineRenderer.enabled = false;
            }
        }


        // ×ボタンでワイヤーモード切替
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            Debug.Log("フックモード切り替え");
        }
        // R2でフックを射出、またはダブルタップで取り消し
        if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            // ダブルタップ判定
            if (Time.time - lastR2PressTime < doubleTapTime)
            {
                // ダブルタップが検出されたら移動を取り消し
                CancelGrapple();
            }
            else if (canShootHook)
            {
                StartCoroutine(ShootHook());
            }

            lastR2PressTime = Time.time;
        }

        // R2を離したらフックを解除
        if (Gamepad.current.rightTrigger.wasReleasedThisFrame)
        {
            ReleaseHook();
        }

        // R1押したら巻き取り（移動）を開始
        if (Gamepad.current.rightShoulder.wasPressedThisFrame)
        {
            if (isGrappling)
            {
                // 既にフックが当たっている場合、巻き取り移動を開始
                isGrappling = false;
                isRetractingAndMoving = true;
            }
            else if (grabbedObject != null)
            {
                // ギミックを巻き取る
                StartCoroutine(RetractObject(grabbedObject));
            }
        }
    }

    void LateUpdate()
    {
        // 巻き取り移動の処理
        if (isRetractingAndMoving)
        {
            Vector3 direction = grapplePoint - transform.position;
            characterController.Move(direction.normalized * moveSpeed * Time.deltaTime);
            DrawWire(mainCamera.transform.position, grapplePoint);

            // 目標地点に到達したか判定
            if (Vector3.Distance(transform.position, grapplePoint) < 1f)
            {
                isRetractingAndMoving = false;
                stayOnWallCoroutine = StartCoroutine(StayOnWall(5f));
            }
        }
        else if (isReturning)
        {
            Vector3 direction = lastPosition - transform.position;
            characterController.Move(direction.normalized * moveSpeed * Time.deltaTime);
            DrawWire(mainCamera.transform.position, lastPosition);

            if (Vector3.Distance(transform.position, lastPosition) < 1f)
            {
                isReturning = false;
                ReleaseHook();
            }
        }
    }

    private void CancelGrapple()
    {
        // フックが発射中、または移動中であれば強制的に解除
        if (isGrappling || isRetractingAndMoving || isReturning)
        {
            Debug.Log("フック移動を取り消しました");
            ReleaseHook();
        }
    }

    private void ReleaseHook()
    {
        lineRenderer.enabled = false;
        isGrappling = false;
        isReturning = false;
        isRetractingAndMoving = false;
        grabbedObject = null;
        if (stayOnWallCoroutine != null)
        {
            StopCoroutine(stayOnWallCoroutine);
        }

        mainCamera.transform.SetParent(originalCameraParent);
    }

    private IEnumerator ShootHook()
    {
        canShootHook = false;
        RaycastHit hit;
        Vector3 origin = mainCamera.transform.position;
        Vector3 direction = mainCamera.transform.forward;
        if (Physics.Raycast(origin, direction, out hit, maxWireLength, hookableLayers | interactiveLayers))
        {
            if (interactiveLayers == (interactiveLayers | (1 << hit.collider.gameObject.layer)))
            {
                Debug.Log("ギミックに当たった");
                grabbedObject = hit.transform;
            }
            else
            {
                Debug.Log("移動用フックが当たった");
                grapplePoint = hit.point;
                lastPosition = transform.position;
                isGrappling = true;

                mainCamera.transform.SetParent(moveCameraParent);
            }
        }
        else
        {
            Debug.Log("何にも当たらなかった");
            ReleaseHook();
        }

        yield return new WaitForSeconds(coolDownTime);
        canShootHook = true;
    }

    private IEnumerator StayOnWall(float stayTime)
    {
        yield return new WaitForSeconds(stayTime);
        ReleaseHook();
    }

    private IEnumerator RetractObject(Transform obj)
    {
        yield return null;
    }

    private void DrawWire(Vector3 start, Vector3 end)
    {
        lineRenderer.positionCount = curveSegments;
        for (int i = 0; i < curveSegments; i++)
        {
            float t = (float)i / (curveSegments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            float sag = Mathf.Sin(t * Mathf.PI) * 0.2f;
            pos.y -= sag;

            float sway = Mathf.Sin(Time.time * 10f + t * 5f) * 0.02f;
            pos.x += sway;

            lineRenderer.SetPosition(i, pos);
        }
    }
}