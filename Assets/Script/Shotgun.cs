using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] ActionBasedController leftHandInteractor;

    [Header("照準用 UI（例：照準マーク Image）")]
    [SerializeField] private Image crosshairImage;

    [Header("Ray 設定")]
    [SerializeField] private float rayDistance = 10f;

    [Header("弾の設定")]
    [SerializeField] private GameObject bulletPrefab; // 玉のプレハブ
    [SerializeField] private float bulletSpeed = 50.0f; // 飛ぶ速さ
    [SerializeField] private float bulletLifeTime = 0.2f;  // 玉の寿命（秒）

    [Header("銃モデルの設定")]
    [SerializeField] private GameObject gunPrefab;
    [SerializeField] private Vector3 gunOffset = new Vector3(0f, 0f, 0.1f); // コントローラーに対する位置補正

    [Header("Debug用のため削除予定")]
    [SerializeField] private LineRenderer lineRenderer;

    private int pelletCount = 8;     // ショットガンの散弾数
    private float spreadAngle = 0.2f; // 拡散角度
    private bool isShooting = false;
    private GameObject gunInstance;
    private GameObject currentBullet;
    private InputAction triggerAction;
    private Vector3 rayDirection;
    private Vector3 rayOrigin;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // --- 銃の装備 ---
        if (leftHandInteractor == null)
        {
            Debug.LogError("LeftHandInteractor が設定されていません。");
            return;
        }

        if (gunPrefab != null)
        {
            gunInstance = Instantiate(gunPrefab);
            gunInstance.transform.SetParent(leftHandInteractor.transform);
            gunInstance.transform.localPosition = gunOffset;
            gunInstance.transform.localRotation = Quaternion.identity;
        }

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.red;
        }

        // --- トリガー入力設定 ---
        triggerAction = new InputAction("Fire", binding: "<XRController>{RightHand}/trigger");
        triggerAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        // --- 照準処理 ---
        rayOrigin = leftHandInteractor.transform.position; // 自分の位置
        rayDirection = leftHandInteractor.transform.forward; // ターゲットへの方向（正規化）

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red); // Rayを可視化

        RaycastHit hit;
        Vector3 endPoint;

        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out hit, rayDistance))
        {
            endPoint = hit.point;

            Debug.Log("Ray hit:");
            crosshairImage.color = Color.green;

            // トリガーが押されたら Shoot
            if (!isShooting && leftHandInteractor.activateActionValue.action.WasPressedThisFrame())
            {
                Shoot();
                isShooting = true;
            }
        }
        else {
            endPoint = rayOrigin + rayDirection * rayDistance;
            crosshairImage.color = Color.red;
        }

        // --- LineRenderer で線を描画 ---
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, rayOrigin);
            lineRenderer.SetPosition(1, rayOrigin + rayDirection * rayDistance);
        }
    }

    void Shoot() // ショットガンをモデルに弾を飛ばす
    {
        for (int i = 0; i < pelletCount; i++)
        {
            // 玉を生成（向きもfirePointの向きに合わせる）
            GameObject bullet = Instantiate(bulletPrefab, leftHandInteractor.transform.position, leftHandInteractor.transform.rotation);
            // Rigidbodyが付いていることを確認して速度を設定
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null) {
                // ランダムな角度で拡散
                // float randomYaw = Random.Range(-spreadAngle / 2f, spreadAngle / 2f); // Yaw（左右方向）
                // float randomPitch = Random.Range(-spreadAngle / 2f, spreadAngle / 2f); // Pitch（上下方向）
                Vector3 randomOffset = Random.insideUnitSphere * spreadAngle;

                // 拡散角度を反映した方向ベクトルを計算
                Vector3 spreadDirection = (rayDirection + randomOffset).normalized;

                // 弾に速度を設定
                rb.linearVelocity = spreadDirection * bulletSpeed;
            }

            // 一定時間後に弾を消す
            Destroy(bullet, bulletLifeTime); // 一定時間後に玉を自動で消す
        }

        Invoke(nameof(ClearBulletReference), bulletLifeTime);
    }

    void ClearBulletReference()
    {
        isShooting = false;
        currentBullet = null;
    }
}
