using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    public Transform targetCamera;
    public float distance = 2.0f;
    public float height = 1.5f;
    public float followSpeed = 5f;

    private void Update()
    {
        if (targetCamera == null) return;

        // カメラの正面方向を基準に位置を計算
        Vector3 targetPos = targetCamera.position + targetCamera.forward * distance;
        targetPos.y = targetCamera.position.y + height;

        // スムーズに位置を補間
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // 常にカメラの方向を向く
        transform.LookAt(targetCamera);
        transform.rotation = Quaternion.Euler(0, transform.rotation.eulerAngles.y, 0);
        // 水平回転だけに制限（上下に首を振ってもUIが傾かない）
    }
}