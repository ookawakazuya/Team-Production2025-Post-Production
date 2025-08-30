using UnityEngine;
using UnityEngine.InputSystem;

public class Shotgun : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] float rayDistance = 10f;

    [SerializeField] Camera camera;
    private float cameraSpeed = 0.125f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        //お試し
        // マウスの位置をスクリーン座標で取得
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        // スクリーン座標をワールド座標に変換
        Vector3 worldMousePosition = camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, camera.nearClipPlane));

        // カメラの位置とマウスの位置から方向を計算
        Vector3 direction = worldMousePosition - transform.position;

        // 方向から回転角度を計算（マウスがいる方向）
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 回転を滑らかに補間
        float smoothAngle = Mathf.LerpAngle(transform.eulerAngles.z, angle, cameraSpeed * Time.deltaTime);

        // Z軸回転のみ
        transform.rotation = Quaternion.Euler(new Vector3(0, 0, smoothAngle));

        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = target.position - rayOrigin;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out hit, rayDistance))
        {
            Debug.Log("Ray hit:");
        }
    }
}
