using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Collections;
using static UnityEditorInternal.ReorderableList;

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] private Transform playerHead;
    [SerializeField] private Transform leftHandInteractor;

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
    [SerializeField] private bool infiniteAmmo = false; // デバッグ用トグル

    // [Header("銃モデルの設定")]
    // [SerializeField] private GameObject gunPrefab;
    // [SerializeField] private Vector3 gunOffset = new Vector3(0f, 0f, 0.1f); // コントローラーに対する位置補正

    private UnityEngine.XR.InputDevice leftHandDevice;
    private HapticController hapticC;
    private EnemyController enemyC;
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
    private float reloadThresholdY = 0.9f; // リロードを検出する高さ（Y位置）

    private bool isShooting = false;
    private bool isReloading = false;
    private bool hasHit = false; // Rayが当たったか
    private bool shouldDraw = false; // LateUpdateで描画すべきかs
    private bool hideCoroutineRunning = false;
    private bool isLeftHand = true; //左右の判断

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
        /* if (gunPrefab != null)
           {
               gunInstance = Instantiate(gunPrefab);
               gunInstance.transform.SetParent(leftHandInteractor.transform);
               gunInstance.transform.localPosition = gunOffset;
               gunInstance.transform.localRotation = Quaternion.identity;
           } */

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
        hapticC = FindFirstObjectByType<HapticController>();
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

        if (triggerValue > 0f)
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
            // ShootSpread();
            // Debug.Log("発射");
        }
        // --- トリガーが離されたら ---
        else if (isShooting && triggerValue < 0.1f) { isShooting = false; }

        // Debug.Log($"Y座標: {rayOrigin.position.y}, 閾値: {reloadThresholdY}");

        // --- Y座標が一定より低くなったらリロード ---
        if (triggerValue < 0.1f)
        {
            // --- 相対位置で判定 ---
            float relativeY = rayOrigin.position.y - playerHead.position.y;
            // Debug.Log("relativeY：" + relativeY);

            // --- 無限モード時 ---
            if (infiniteAmmo && relativeY < reloadThresholdY)
            {
                Debug.Log("リロード開始（無限モード）");
                Reload();
            }
            // --- 通常モード時 ---
            else if (!infiniteAmmo && reserveAmmo > 0 && relativeY < reloadThresholdY)
            {
                Debug.Log("リロード開始");
                Reload();
            }
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

        // --- 振動を与える ---
        if (hapticC != null) { hapticC.VibrateFiring(isLeftHand); }

        // --- 1発減る ---
        currentAmmo--;

        // --- 弾プレハブを発射位置・向きで生成 ---
        Instantiate(bulletPrefab, bulletDirection.position, bulletDirection.rotation);

        // デバッグ用
        if (SoundManager.Instance == null)
            Debug.LogError("SoundManager.Instance が null です！");
        else
            Debug.Log("SoundManager 再生チェック OK");

        // --- 発砲音 ---
        SoundManager.Instance?.PlaySE("SE_Gun_01");

        if (hapticC != null) { hapticC.VibrateLingeringSound(isLeftHand); }
    }

    /// <summary>
    /// 拡散ショット用コード
    /// </summary>
    void ShootSpread()
    {
        // --- チェック（弾数確認）---
        if (!CanShoot()) return;

        // --- 1発減る ---
        currentAmmo--;

        // --- 拡散発射 ---
        for (int i = 0; i < pelletCount; i++)
        {
            // 拡散方向を計算
            Vector3 forward = rayOrigin.forward;
            Vector3 right = rayOrigin.right;
            Vector3 up = rayOrigin.up;

            // 単位円内のランダム点（角度ベースで拡散）
            Vector2 circle = Random.insideUnitCircle * Mathf.Tan(spreadAngle * Mathf.Deg2Rad);
            Vector3 spreadDir = (forward + right * circle.x + up * circle.y).normalized;

            // 弾の向きを Quaternion で設定
            Quaternion spreadRot = Quaternion.LookRotation(spreadDir);

            // 弾を生成（向きを持たせる）
            Instantiate(bulletPrefab, rayOrigin.position, spreadRot);
        }

        // --- 発砲SE ---
        SoundManager.Instance?.PlaySE("SE_Gun_01");
    }

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

        // --- 無限モード ---
        if (infiniteAmmo)
        {
            currentAmmo = 1;
            Debug.Log("【DEBUG】無限ストック：リロードしました！");
            SoundManager.Instance?.PlaySE("SE_Gun_02");
            return;
        }

        // --- 通常モード ---
        if (reserveAmmo > 0)
        {
            currentAmmo = 1;
            reserveAmmo--;
            Debug.Log("リロード完了！残りストック：" + reserveAmmo);
            SoundManager.Instance?.PlaySE("SE_Gun_02");
        }
        else
        {
            Debug.Log("ストックがありません！");
        }

        if (hapticC != null) { hapticC.VibrateReload(isLeftHand); }

        isReloading = true;

        // --- 短い待機で再リロード防止 ---
        Invoke(nameof(ResetReload), 0.5f);

        UpdateReserveText();
    }

    /// <summary>
    /// リロード用の関数
    /// </summary>
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
            // --- 無限モードなら何もしない ---
            if (infiniteAmmo)
            {
                yield return null; // 次のフレームまで待機（ループ維持）
                continue;
            }

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
        if (reserveText == null) return;

        // --- テキスト内容の更新 ---
        if (infiniteAmmo) { reserveText.text = "×∞"; }  // 無限モード表示
        else { reserveText.text = $"×{reserveAmmo}"; }

        // --- 位置調整 ---
        Vector2 pos = reserveText.rectTransform.anchoredPosition;

        if (infiniteAmmo) { pos.x = -16; }
        else if (reserveAmmo >= 10) { pos.x = -5; }
        else { pos.x = -16; }

        reserveText.rectTransform.anchoredPosition = pos;
    }

    /// <summary>
    /// Enemyを倒したら弾の追加
    /// EnemyController.cs で呼ぶ
    /// </summary>
    public void plusAmmo()
    {
        reserveAmmo++;
    }
}
