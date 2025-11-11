using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] ActionBasedController leftHandInteractor;

    [Header("照準用 UI")]
    [SerializeField] private Image crosshairImage;

    [Header("Ray 設定")]
    [SerializeField] private float rayDistance = 10f;

    [Header("弾の設定")]
    [SerializeField] private GameObject bulletPrefab; // 玉のプレハブ
    [SerializeField] private float bulletSpeed = 50.0f; // 飛ぶ速さ
    [SerializeField] private float bulletLifeTime = 0.2f;  // 玉の寿命（秒）
    [SerializeField] private int maxReserve = 5;  // 最大ストック弾数

    [Header("銃モデルの設定")]
    [SerializeField] private GameObject gunPrefab;
    [SerializeField] private Vector3 gunOffset = new Vector3(0f, 0f, 0.1f); // コントローラーに対する位置補正

    [Header("LineRenderer（デバッグ用）")]
    [SerializeField] private LineRenderer lineRenderer;

    private int pelletCount = 1; // ショットガンの散弾数
    private int currentAmmo = 1;  // 現在装填されている弾
    private int reserveAmmo; // ストック弾
    private float spreadAngle = 0.2f; // 拡散角度
    private float reloadThresholdY = 0.3f; // リロードを検出する高さ（Y位置）
    private bool isShooting = false;
    private bool isReloading = false;
    // private bool hasBullet = true; // 現在、弾が装填されているか
    // private GameObject gunInstance;
    // private GameObject currentBullet;
    private InputAction triggerAction;
    private Transform rayOrigin;
    private Vector3 rayDirection;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // --- 銃の装備 ---
        if (leftHandInteractor == null)
        {
            Debug.LogError("LeftHandInteractor が設定されていません。");
            return;
        }

        // --- 左手トリガーを入力にバインド ---
        triggerAction = new InputAction("LeftTrigger", binding: "<XRController>{LeftHand}/trigger");
        triggerAction.Enable();

        // --- ストック初期化 ---
        reserveAmmo = maxReserve;

        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.002f;
            lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
            lineRenderer.material.color = Color.red;
        }

#if false
        if (gunPrefab != null)
        {
            gunInstance = Instantiate(gunPrefab);
            gunInstance.transform.SetParent(leftHandInteractor.transform);
            gunInstance.transform.localPosition = gunOffset;
            gunInstance.transform.localRotation = Quaternion.identity;
        }
#endif
    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit hit;
        Vector3 endPoint;
 
        // --- 照準処理 ---
        rayOrigin = leftHandInteractor.transform; // コントローラーの位置情報
        rayDirection = rayOrigin.forward; // ターゲットへの方向（正規化）

        // Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red); // Rayを可視化

        // --- トリガー入力を取得 ---
        float triggerValue = triggerAction.ReadValue<float>();

        // --- LineRenderer で線を描画 ---
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, rayOrigin.position);
            lineRenderer.SetPosition(1, rayOrigin.position + rayDirection * rayDistance);

            // --- crosshairImage の色を反映 ---
            if (lineRenderer.material != null)
            {
                lineRenderer.material.color = crosshairImage.color;
            }
        }

        // --- Raycast 判定 ---
        if (Physics.Raycast(rayOrigin.position, rayDirection, out hit, rayDistance))
        {
            endPoint = hit.point;
            crosshairImage.color = Color.green;
            Debug.Log("Ray hit:");

            // --- トリガーが押されたら --- 
            if (!isShooting && triggerValue > 0.9f) { 
                isShooting = true; Shoot();
                Debug.Log("発射");

                // Invoke(nameof(ClearBulletReference), 0.3f); // 0.3秒後に撃てるように戻す

            }
            // --- トリガーが離されたら ---
            else if (isShooting && triggerValue < 0.1f)
            {
                isShooting = false;
            }
        }
        else {
            endPoint = rayOrigin.position + rayDirection * rayDistance;
            crosshairImage.color = Color.red;
        }


        // --- Y座標が一定より低くなったらリロード ---
        if (!isReloading && currentAmmo == 0 && reserveAmmo > 0 && rayOrigin.position.y < reloadThresholdY)
        {
            Reload();
        }
    }

    void Shoot() // --- 発砲用コード ---
    {
        if (reserveAmmo <= 0)
        {
            Debug.Log("ストックがありません！");
            return;
        }

        if (currentAmmo <= 0)
        {
            Debug.Log("弾がありません！リロードしてください。");
            return;
        }

        // --- 撃ったので1発減る ---
        currentAmmo--;

        for (int i = 0; i < pelletCount; i++)
        {
            // 玉を生成（向きもfirePointの向きに合わせる）
            GameObject bullet = Instantiate(bulletPrefab, rayOrigin.position, rayOrigin.rotation);
            
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
    }

    void Reload() 
    {
    if (currentAmmo > 0)
    {
        Debug.Log("すでに装填済みです。");
        return;
    }

    if (reserveAmmo > 0)
    {
        currentAmmo = 1;
        reserveAmmo--;
        Debug.Log("リロード完了！残りストック：" + reserveAmmo);
    }
    else
    {
        Debug.Log("ストックがありません！");
    }

        Invoke(nameof(ResetReload), 0.5f); // 短い待機で再リロード防止
    }

    void ClearBulletReference()
    {
        isShooting = false;
    }

    void ResetReload()
    {
        isReloading = false;
    }
}
