using UnityEngine;
using UnityEngine.EventSystems;

public class VRPointer : MonoBehaviour
{
    public LineRenderer lineRenderer;     // レーザー描画用
    public float rayLength = 10f;         // レーザーの長さ
    public Camera eventCamera;            // UI用カメラ（通常はVRのメインカメラ）

    private GameObject currentTarget;

    void Update()
    {
        // Ray を飛ばす
        Ray ray = new Ray(transform.position, transform.forward);
        lineRenderer.SetPosition(0, ray.origin);
        lineRenderer.SetPosition(1, ray.origin + ray.direction * rayLength);

        // UIにRaycastする
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = eventCamera.WorldToScreenPoint(ray.origin + ray.direction * rayLength);

        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        if (results.Count > 0)
        {
            GameObject hitUI = results[0].gameObject;

            if (hitUI != currentTarget)
            {
                currentTarget = hitUI;
                Debug.Log("UI Hover: " + currentTarget.name);
            }

            // トリガーでクリックを再現（例: Spaceキーでテスト）
            if (Input.GetKeyDown(KeyCode.Space))
            {
                ExecuteEvents.Execute(currentTarget, eventData, ExecuteEvents.pointerClickHandler);
                Debug.Log("UI Clicked: " + currentTarget.name);
            }
        }
        else
        {
            currentTarget = null;
        }
    }
}
