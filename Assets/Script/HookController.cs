using UnityEngine;
using UnityEngine.InputSystem;

public class HookContlore : MonoBehaviour
{
    [SerializeField] private Camera playerCamera; // プレイヤー視点カメラ
    [SerializeField] private LineRenderer lineRenderer; // レイ可視化用
    [SerializeField] private LayerMask hitLayers; // ヒット判定対象（壁など）

    private void Update()
    {
        if (Gamepad.current != null)
        {
            // R2の押し込み量（0.0～1.0）
            float r2 = Gamepad.current.rightTrigger.ReadValue();

            if (r2 > 0.1f) // 少しでも押されていたら
            {
                ShootRay();
            }
            else
            {
                lineRenderer.enabled = false; // トリガーを離したら非表示
            }
        }
    }

    private void ShootRay()
    {
        Vector3 origin = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;

        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, hitLayers))
        {
            // レイを壁まで表示
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, hit.point);
        }
        else
        {
            // 当たらなかったら100m先まで表示
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, origin + direction * 100f);
        }
    }
}
