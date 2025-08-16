using UnityEngine;
using UnityEngine.InputSystem;

public class DualSenseWireController : MonoBehaviour
{
    [Header("プレイヤー関連")]
    [SerializeField] private Camera playerCamera;   // プレイヤー視点用カメラ

    [Header("ワイヤー関連設定")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float maxWireLength = 15f;
    [SerializeField] private float extendSpeed = 20f;
    [SerializeField] private float retractSpeed = 25f;
    [SerializeField] private int curveSegments = 20;

    private bool wireMode = false;
    private bool isExtending = false;
    private bool isRetracting = false;

    private Vector3 targetPoint;
    private float currentLength;

    void Update()
    {
        if (Gamepad.current == null) return;

        // ================================
        // ×ボタンでワイヤーモード切替
        // ================================
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            wireMode = !wireMode;
            Debug.Log($"ワイヤーモード: {(wireMode ? "ON" : "OFF")}");
        }

        if (!wireMode)
        {
            lineRenderer.enabled = false;
            return;
        }

        // ================================
        // R2で射出開始
        // ================================
        if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            StartWire();
        }

        // ================================
        // ワイヤー伸び処理
        // ================================
        if (isExtending && !isRetracting && Gamepad.current.rightTrigger.isPressed)
        {
            ExtendWire();
        }

        // ================================
        // R2離したら解除
        // ================================
        if (Gamepad.current.rightTrigger.wasReleasedThisFrame)
        {
            ReleaseWire();
        }

        // ================================
        // R1押したら「巻き取りモード開始」
        // （伸び中でも即巻き取りに切替）
        // ================================
        if (Gamepad.current.rightShoulder.wasPressedThisFrame && lineRenderer.enabled)
        {
            isExtending = false;   // 伸びるのを中断
            isRetracting = true;   // 巻き取り開始
        }

        // ================================
        // 巻き取りモード中なら自動巻き取り
        // ================================
        if (isRetracting)
        {
            RetractWire();
        }
    }

    private void StartWire()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxWireLength, hitLayers))
        {
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = origin + direction * maxWireLength;
        }

        currentLength = 0f;
        isExtending = true;
        isRetracting = false;

        lineRenderer.positionCount = curveSegments;
        lineRenderer.enabled = true;
    }

    private void ExtendWire()
    {
        currentLength += extendSpeed * Time.deltaTime;
        float totalLength = Vector3.Distance(playerCamera.transform.position, targetPoint);

        if (currentLength >= totalLength)
        {
            currentLength = totalLength;
            isExtending = false; // 自然に伸び切ったら終了
        }

        DrawWire(playerCamera.transform.position, Vector3.Lerp(playerCamera.transform.position, targetPoint, currentLength / totalLength));
    }

    private void ReleaseWire()
    {
        lineRenderer.enabled = false;
        isExtending = false;
        isRetracting = false;
    }

    private void RetractWire()
    {
        if (!lineRenderer.enabled) return;

        currentLength -= retractSpeed * Time.deltaTime;

        if (currentLength <= 0f)
        {
            currentLength = 0f;
            ReleaseWire(); // 完全に巻き取りきったら解除
            return;
        }

        DrawWire(playerCamera.transform.position, Vector3.Lerp(playerCamera.transform.position, targetPoint, currentLength / Vector3.Distance(playerCamera.transform.position, targetPoint)));
    }

    private void DrawWire(Vector3 start, Vector3 end)
    {
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
