using UnityEngine;
using UnityEngine.InputSystem; // 新Input System

public class DualSenseRayShooter : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float maxWireLength = 30f; // 最大長
    [SerializeField] private float extendSpeed = 20f;   // 伸びる速さ
    [SerializeField] private int curveSegments = 20;    // 頂点数（カーブ滑らかさ）

    private float currentLength = 0f;
    private bool isExtending = false;
    private Vector3 targetPoint;

    void Update()
    {
        if (Gamepad.current != null)
        {
            if (Gamepad.current.rightTrigger.wasPressedThisFrame)
            {
                StartWire();
            }

            if (isExtending)
            {
                ExtendWire();
            }
        }
    }

    private void StartWire()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        // ヒット判定
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxWireLength, hitLayers))
        {
            Debug.Log("レイのヒット");
            targetPoint = hit.point;
        }
        else
        {
            targetPoint = origin + direction * maxWireLength;
        }

        currentLength = 0f;
        isExtending = true;
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
            isExtending = false; // 到達したら伸び終了
        }

        // 始点と現在の先端位置
        Vector3 start = playerCamera.transform.position;
        Vector3 end = Vector3.Lerp(start, targetPoint, currentLength / totalLength);

        // ワイヤーのカーブ計算
        for (int i = 0; i < curveSegments; i++)
        {
            float t = (float)i / (curveSegments - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // 中間を少したるませる（ワイヤー感）
            float sag = Mathf.Sin(t * Mathf.PI) * 0.2f; // 弓なり
            pos.y -= sag;

            // 少し揺れを追加
            float sway = Mathf.Sin(Time.time * 10f + t * 5f) * 0.02f;
            pos.x += sway;

            lineRenderer.SetPosition(i, pos);
        }
    }
    private void OnDrawGizmos()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxWireLength);
        }
    }

}
