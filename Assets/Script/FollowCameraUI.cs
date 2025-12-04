using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    public Transform targetCamera;
    public float distance = 2.0f;
    public float height = 1.5f;
    public float followSpeed = 5f;

    void Update()
    {
        if (targetCamera == null) return;

        // à íuåvéZ
        Vector3 targetPos = targetCamera.position + targetCamera.forward * distance;
        targetPos.y = targetCamera.position.y + height;
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // âÒì]ï‚ê≥
        Vector3 forward = targetCamera.forward;
        forward.y = 0; // êÖïΩÇÃÇ›
        transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(0, 0, 0);
    }
}