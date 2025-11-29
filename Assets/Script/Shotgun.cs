using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR;

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] Transform leftHandInteractor;

    [Header("照準用 UI")]
    [SerializeField] private Image crosshairImage;

    [Header("Ray 設定")]
    [SerializeField] private Transform rayOriginObject;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float rayDistance = 50f;

    [Header("弾の設定")]
    [SerializeField] private GameObject bulletPrefab; // 玉のプレハブ
    [SerializeField] private float bulletSpeed = 50.0f; // 飛ぶ速さ
    [SerializeField] private float bulletLifeTime = 1.0f;  // 玉の寿命（秒）
    [SerializeField] private int maxReserve = 5000;  // 最大ストック弾数

    [Header("銃モデルの設定")]
    [SerializeField] private GameObject gunPrefab;
    [SerializeField] private Vector3 gunOffset = new Vector3(0f, 0f, 0.1f); // コントローラーに対する位置補正

    private GameObject gunInstance;
    // private GameObject currentBullet;
    // private InputAction triggerAction;
    private Transform rayOrigin;
    private Vector3 rayDirection;
    private UnityEngine.XR.InputDevice leftHandDevice;

    private int pelletCount = 1; // ショットガンの散弾数
    private int currentAmmo = 5000;  // 現在装填されている弾
    private int reserveAmmo; // ストック弾

    private float spreadAngle = 0.2f; // 拡散角度
    private float reloadThresholdY = 0.3f; // リロードを検出する高さ（Y位置）

    private bool isShooting = false;
    private bool isReloading = false;

    private void Awake()
    {
        // --- 左コントローラーの初期設定 ---
        if (leftHandInteractor == null)
        {
            Debug.LogError("LeftHandInteractor が設定されていません。");
            return;
        }

        // --- LeftHand の InputDevice を取得 ---
        var devices = new List<UnityEngine.XR.InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
        if (devices.Count > 0)
        {
            leftHandDevice = devices[0];
            Debug.Log("LeftHand デバイス検出：" + leftHandDevice.name);
        }
        else
        {
            Debug.LogWarning("LeftHand デバイスが見つかりません！");
        }

        // --- 銃を装備 ---
        if (gunPrefab != null)
        {
            gunInstance = Instantiate(gunPrefab);
            gunInstance.transform.SetParent(leftHandInteractor.transform);
            gunInstance.transform.localPosition = gunOffset;
            gunInstance.transform.localRotation = Quaternion.identity;
        }

        // --- 射線の可視化 ---
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = 0.01f;
            lineRenderer.endWidth = 0.002f;

            // ✅ 透明対応のマテリアルを生成
            Material transparentMat = new Material(Shader.Find("Unlit/Transparent"));

            // --- 🔧 透明描画設定を強制適用 ---
            // transparentMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            // transparentMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            // transparentMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            // transparentMat.SetInt("_ZWrite", 0); // 深度書き込みを無効
            // transparentMat.DisableKeyword("_ALPHATEST_ON");
            // transparentMat.EnableKeyword("_ALPHABLEND_ON");
            // transparentMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");

            lineRenderer.material = transparentMat;

            // --- 初期は透明にしておく ---
            SetLineAlpha(0f, Color.red);
            // lineRenderer.enabled = false;
        }

        // --- ストック初期化 ---
        reserveAmmo = maxReserve;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // --- 左手コントローラーの確認 ---
        if (!leftHandDevice.isValid)
        {
            var devices = new List<UnityEngine.XR.InputDevice>();
            InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
            if (devices.Count > 0) { leftHandDevice = devices[0]; }
        }

        // --- トリガー入力を取得 ---
        float triggerValue = 0f;
        leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out triggerValue);

        RaycastHit hit;
        // Vector3 endPoint;

        // --- 照準処理 ---
        rayOrigin = leftHandInteractor.transform; // コントローラーの位置情報
        rayDirection = rayOrigin.forward; // ターゲットへの方向（正規化）

        // --- Raycast 判定 ---
        if (Physics.Raycast(rayOrigin.position, rayDirection, out hit, rayDistance))
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                Debug.Log("Ray hit:");
                lineRenderer.enabled = true;
                // endPoint = hit.point;
                // crosshairImage.color = Color.red;

                // 🟢 LineRenderer の始点・終点を毎フレーム更新
                lineRenderer.SetPosition(0, rayOrigin.position);
                lineRenderer.SetPosition(1, hit.point);

                // 敵に当たった：赤色・非透明
                //SetLineAlpha(1f, Color.red);

                // --- トリガーが押されたら --- 
                if (!isShooting && triggerValue > 0.9f)
                {
                    isShooting = true; Shoot();
                    Debug.Log("発射");
                }
                // --- トリガーが離されたら ---
                else if (isShooting && triggerValue < 0.1f) { isShooting = false; }
            }
            else
            {
                // 敵以外に当たった：透明
                //SetLineAlpha(0f, Color.red);
                lineRenderer.enabled = false;
            }

        }

        // --- Y座標が一定より低くなったらリロード ---
        if (reserveAmmo > 0 && triggerValue < 0.1f && rayOrigin.position.y < reloadThresholdY)
        {
            Reload();
        }
    }

    void LateUpdate()
    {
        // 何にも当たってない：透明
        SetLineAlpha(1f, Color.red);
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

            if (rb != null)
            {
                // === 真っすぐ発射 ===
                Vector3 shootDirection = rayOrigin.forward.normalized;

                // 弾に速度を与える
                rb.linearVelocity = shootDirection * bulletSpeed;

#if false
                // === 円形拡散の計算 ===
                // ランダムな角度で拡散
                // float randomYaw = Random.Range(-spreadAngle / 2f, spreadAngle / 2f); // Yaw（左右方向）
                // float randomPitch = Random.Range(-spreadAngle / 2f, spreadAngle / 2f); // Pitch（上下方向）
                // Vector3 randomOffset = Random.insideUnitSphere * spreadAngle;

                // 拡散角度を反映した方向ベクトルを計算
                // Vector3 spreadDirection = (rayDirection + randomOffset).normalized;

                // 弾に速度を設定
                // rb.linearVelocity = spreadDirection * bulletSpeed;

                // 中心方向（前方）
                Vector3 forward = rayOrigin.forward;

                // 前方に垂直なベクトルを作る
                Vector3 right = rayOrigin.right;
                Vector3 up = rayOrigin.up;

                // 円の中のランダム点を作る（半径 = spreadAngle）
                Vector2 circle = Random.insideUnitCircle * spreadAngle;

                // 方向ベクトルを組み立て（正面 + 右・上方向の微調整）
                Vector3 spreadDir = (forward + right * circle.x + up * circle.y).normalized;

                // 弾に速度を与える
                rb.linearVelocity = spreadDir * bulletSpeed;
#endif
            }

            // 一定時間後に弾を消す
            Destroy(bullet, bulletLifeTime); // 一定時間後に玉を自動で消す
        }
    }

    void Reload() // --- リロード用コード ---
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

    void SetLineAlpha(float alpha, Color color)
    {
        if (lineRenderer == null) return;

        // --- LineRenderer を常に描画更新 ---
        Vector3 origin = rayOriginObject.position;
        Vector3 direction = rayOriginObject.forward;
        
        Debug.DrawRay(origin, direction * rayDistance, Color.red); // Rayを可視化

        // 始点と終点を更新（Ray の見た目）
        lineRenderer.enabled = true;
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, origin + direction * rayDistance);

        // --- 色を反映 ---
        if (lineRenderer.material != null) {
            color.a = Mathf.Clamp01(alpha); // 透明度設定
            lineRenderer.material.color = color;
        }
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
