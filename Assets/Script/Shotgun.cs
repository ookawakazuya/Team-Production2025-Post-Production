using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Collections;

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] Transform leftHandInteractor;

    // [Header("照準用 UI")]
    // [SerializeField] private Image crosshairImage;

    [Header("Ray 設定")]
    [SerializeField] private Transform rayOriginObject;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float rayDistance = 50f;

    [Header("弾の設定")]
    [SerializeField] private Transform bulletrObject;
    [SerializeField] private GameObject bulletPrefab; // 弾のプレハブ
    [SerializeField] private int maxReserve = 1000;  // 最大ストック弾数
    [SerializeField] private float autoAmmoDelay = 5f; // ストック生成間隔（秒）

    [Header("銃モデルの設定")]
    [SerializeField] private GameObject gunPrefab;
    [SerializeField] private Vector3 gunOffset = new Vector3(0f, 0f, 0.1f); // コントローラーに対する位置補正

    private UnityEngine.XR.InputDevice leftHandDevice;
    private HapticController hapticC;
    private GameObject gunInstance;
    private Text reserveText;
    private Transform rayOrigin;  // コントローラーの位置情報
    private Transform bulletDirection;
    private Vector3 rayDirection;  // ターゲットへの方向（正規化）
    private Vector3 hitPoint; // Rayの終点を一時保存

    private int pelletCount = 1; // ショットガンの散弾数
    private int currentAmmo = 1;  // 現在装填されている弾
    private int reserveAmmo; // ストック弾

    private float spreadAngle = 0.2f; // 拡散角度
    private float reloadThresholdY = -2.9f; // リロードを検出する高さ（Y位置）

    private bool isShooting = false;
    private bool isReloading = false;
    private bool hasHit = false; // Rayが当たったか
    private bool shouldDraw = false; // LateUpdateで描画すべきかs
    private bool hideCoroutineRunning = false;

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
            lineRenderer.startWidth = 0.02f;
            lineRenderer.endWidth = 0.01f;

            // --- 最初は非表示 ---
            lineRenderer.enabled = false;

            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
        }

        // --- ストック初期化 ---
        reserveAmmo = 0;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // --- 自動生成を監視開始 ---
        StartCoroutine(AutoAmmoReserve());

        // --- シーン内から取得 ---
        hapticC = FindObjectOfType<HapticController>();
        if (hapticC == null) { Debug.Log("HapticController がシーンに存在しません！"); }

        // --- 名前で探す ---
        GameObject textObj = GameObject.Find("ReserveText");
        if (textObj != null)
        {
            reserveText = textObj.GetComponent<Text>();
            UpdateReserveText();
        }
        else
        {
            Debug.LogWarning("ReserveText が見つかりません。名前を確認してください。");
        }
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

        // --- 照準処理 ---
        rayOrigin = leftHandInteractor.transform;
        rayDirection = rayOrigin.forward;
        bulletDirection = bulletrObject.transform;

        // --- LineRenderer を常に描画更新 ---
        RaycastHit hit;

        if (triggerValue > 0.3f)
        {
            // --- Raycast 判定 ---
            if (Physics.Raycast(rayOrigin.position, rayDirection, out hit, rayDistance))
            {
                hitPoint = hit.point;
                hasHit = true;
                shouldDraw = true;

                // hit.collider で当たったオブジェクトの Collider にアクセスできる
                // Debug.Log("Ray hit: " + hit.collider.name);

                // 当たったオブジェクトのタグも確認できる
                // Debug.Log("Hit tag: " + hit.collider.tag);

                Debug.DrawRay(rayOrigin.position, rayDirection * rayDistance, Color.red);
            }
            else { hasHit = false; }
        }
        else if (triggerValue < 0.1f)
        {
            shouldDraw = false;
        }

        // --- トリガーが押されたら --- 
        if (!isShooting && triggerValue > 0.9f)
        {
            isShooting = true;
            ShootSingle();
            // Debug.Log("発射");
        }
        // --- トリガーが離されたら ---
        else if (isShooting && triggerValue < 0.1f) { isShooting = false; }

        // Debug.Log("postion.y" + rayOrigin.position.y);
        // Debug.Log($"Y座標: {rayOrigin.position.y}, 閾値: {reloadThresholdY}");

        // --- Y座標が一定より低くなったらリロード ---
        if (reserveAmmo > 0 && triggerValue < 0.1f && rayOrigin.position.y < reloadThresholdY)
        {
            Reload();
        }
    }

    /// <summary>
    /// Lineをきれいに描画するための関数
    /// </summary>
    void LateUpdate()
    {
        if (shouldDraw && hasHit)
        {
            // --- 当たった先までLineRendererで描画 ---
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, rayOriginObject.position);
            lineRenderer.SetPosition(1, hitPoint);
        }
        else
        {
            lineRenderer.enabled = false;
        }
    }

    /// <summary>
    /// 弾数チェック共通化
    /// </summary>
    bool CanShoot()
    {
        if (currentAmmo <= 0)
        {
            Debug.Log("弾がありません！リロードしてください。");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 単発ショット用コード
    /// </summary>
    void ShootSingle()
    {
        // --- チェック（弾数確認）---
        if (!CanShoot()) return;

        // --- 1発減る ---
        currentAmmo--;

        // --- 弾プレハブを発射位置・向きで生成 ---
        Instantiate(bulletPrefab, bulletDirection.position, bulletDirection.rotation);

        // --- 振動を与える ---
        if (hapticC != null) { hapticC.VibrateWallHit(false); }
    }

#if false
    void Shoot() // --- 発砲用コード ---
    {
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
            }

            // 一定時間後に弾を消す
            Destroy(bullet, bulletLifeTime); // 一定時間後に玉を自動で消す
        }
    }
#endif

    /// <summary>
    /// リロード用コード
    /// </summary>
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

        isReloading = true;

        // --- 短い待機で再リロード防止 ---
        Invoke(nameof(ResetReload), 0.5f);

        UpdateReserveText();
    }

    void ResetReload()
    {
        isReloading = false;
    }

    /// <summary>
    /// ストック自動生成
    /// </summary>
    IEnumerator AutoAmmoReserve()
    {
        while (true)
        {
            // --- ストックが3以下なら生成開始 ---
            if (reserveAmmo < 3)
            {
                reserveAmmo++;
                UpdateReserveText();
                Debug.Log("ストックを補充！ 現在：" + reserveAmmo);
            }

            // --- 一定時間待機（例：5秒ごと）---
            yield return new WaitForSeconds(autoAmmoDelay);
        }
    }

    /// <summary>
    /// ストックをtextで表示
    /// </summary>
    void UpdateReserveText()
    {
        if (reserveText != null)
        {
            reserveText.text = $"×{reserveAmmo}";
        }
    }
}
