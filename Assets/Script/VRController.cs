using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using System.Collections;
using UnityEngine.XR;

public class VRController : MonoBehaviour
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
    [SerializeField] float maxWireLength = 15f;
    [SerializeField] float extendSpeed = 20f;
    [SerializeField] float retractSpeed = 25f;
    [SerializeField] float moveSpeed = 30f;
    [SerializeField] int curveSegments = 20;

    bool canShootHook = true;
    float coolDownTime = 1f;
    bool isGrappling = false;
    bool isReturning = false;
    bool isRetractingAndMoving = false;
    Vector3 grapplePoint;
    Vector3 lastPosition;
    Coroutine stayOnWallCoroutine;
    Transform grabbedObject;
    float lastR2PressTime = 0f;
    float doubleTapTime = 0.3f;

    private XRController rightHandController;

    [Header("デバッグ関連")]
    [SerializeField] private LineRenderer debugLineRenderer;

    void Awake()
    {
        var vrControllers = InputSystem.devices;
        foreach (var device in vrControllers)
        {
            if (device is XRController && ((XRController)device).characteristics.HasFlag(InputDeviceCharacteristics.Right))
            {
                rightHandController = (XRController)device;
                break;
            }
        }

        if (rightHandController == null)
        {
            Debug.LogError("右手VRコントローラーが見つかりませんでした。VRデバイスが接続されているか確認してください。");
        }
    }

    void Update()
    {
        if (rightHandController == null) return;

        // R2が押されている間だけデバッグ用のレイを表示
        if (rightHandController.trigger.isPressed && !isGrappling && !isRetractingAndMoving && !isReturning)
        {
            if (debugLineRenderer != null)
            {
                debugLineRenderer.enabled = true;
                debugLineRenderer.positionCount = 2;
                Vector3 origin = rightHandController.device.transform.position;
                Vector3 direction = rightHandController.device.transform.forward;
                debugLineRenderer.SetPosition(0, origin);
                debugLineRenderer.SetPosition(1, origin + direction * maxWireLength);
            }
        }
        else
        {
            if (debugLineRenderer != null && debugLineRenderer.enabled)
            {
                debugLineRenderer.enabled = false;
            }
        }

        if (rightHandController.trigger.wasPressedThisFrame)
        {
            if (Time.time - lastR2PressTime < doubleTapTime)
            {
                CancelGrapple();
            }
            else if (canShootHook)
            {
                StartCoroutine(ShootHook());
            }
            lastR2PressTime = Time.time;
        }

        if (rightHandController.trigger.wasReleasedThisFrame)
        {
            ReleaseHook();
        }

        if (rightHandController.grip.wasPressedThisFrame)
        {
            if (isGrappling)
            {
                isGrappling = false;
                isRetractingAndMoving = true;
            }
            else if (grabbedObject != null)
            {
                StartCoroutine(RetractObject(grabbedObject));
            }
        }
    }

    void LateUpdate()
    {
        if (isRetractingAndMoving)
        {
            Vector3 direction = grapplePoint - transform.position;
            characterController.Move(direction.normalized * moveSpeed * Time.deltaTime);
            DrawWire(mainCamera.transform.position, grapplePoint);

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

        Vector3 origin = rightHandController.device.transform.position;
        Vector3 direction = rightHandController.device.transform.forward;

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
        lineRenderer.enabled = true;
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