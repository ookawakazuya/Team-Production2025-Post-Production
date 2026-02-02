using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.XR;
using System.Collections;
using System.Linq;

/// <summary>
/// Line 用にモードの選択
/// </summary>
public enum AimMode
{
    Beginner,   // 常に線を表示（オレンジ → 敵ヒット時に赤）
    Normal,     // 現在の仕様
    Expert      // 線を非表示
}

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] private Transform playerHead;
    [SerializeField] private Transform leftHandInteractor;

    [Header("Ray 設定")]
    [SerializeField] private Transform rayOriginObject;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private AimMode aimMode = AimMode.Normal;
    [SerializeField] private float rayDistance = 50f;

    [Header("弾の設定")]
    [SerializeField] private Transform bulletrObject;
    [SerializeField] private GameObject bulletPrefab; // 弾のプレハブ
    [SerializeField] private int maxReserve = 1000;  // 最大ストック弾数
    [SerializeField] private float autoAmmoDelay = 5f; // ストック生成間隔（秒）
    [SerializeField] private bool infiniteAmmo = false; // デバッグ用トグル

    [Header("UI設定")]
    [SerializeField] private Slider ammoSlider;
    [SerializeField] private Image loadedImage;
    [SerializeField] private Image[] digitImages;    // 各桁のImage

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

    private float triggerValue = 0f;
    private float spreadAngle = 0.2f; // 拡散角度
    private float reloadThresholdY = 0.9f; // リロードを検出する高さ（Y位置）

    private bool isShooting = false;
    private bool isReloading = false;
    private bool hasHit = false; // Rayが当たったか
    private bool shouldDraw = false; // LateUpdateで描画すべきかs
    private bool hideCoroutineRunning = false;
    private bool isLeftHand = true; //左右の判断

    private Sprite[] numberSprites;  // 自動読み込み用

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

        // --- 数字画像を自動読み込み ---
        numberSprites = Resources.LoadAll<Sprite>("AmmoNumberFont").OrderBy(s => s.name).ToArray();  // ファイル名順に並べる（number_0, number_1...)

        // --- 数字Imageを一旦非表示 ---
        foreach (var img in digitImages)
        {
            if (img != null) { img.enabled = false; }
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // ゲージ初期化
        ammoSlider.value = 0f;
        ammoSlider.maxValue = 1f;
        ammoSlider.gameObject.SetActive(false);

        // --- 自動生成を監視開始 ---
        // --- ストック初期化 ---
        reserveAmmo = 5;
        UpdateLoadedDisplay();
        UpdateReserveText();

        // --- シーン内から取得 ---
        hapticC = FindFirstObjectByType<HapticController>();
        if (hapticC == null) { Debug.Log("HapticController がシーンに存在しません！"); }
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
        triggerValue = 0f;
        leftHandDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.trigger, out triggerValue);

        // --- 照準処理 ---
        rayOrigin = leftHandInteractor.transform;
        rayDirection = rayOrigin.forward;
        bulletDirection = bulletrObject.transform;

        // --- LineRenderer のモードに対応した Update() の呼び出し ---
        switch (aimMode)
        {
            case AimMode.Beginner:
                UpdateBeginnerMode();
                break;
            case AimMode.Normal:
                UpdateNormalMode();
                break;
            case AimMode.Expert:
                UpdateExpertMode();
                break;
        }

        // スライダーの常時表示
        ammoSlider.gameObject.SetActive(true);

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

        if (infiniteAmmo) { currentAmmo = 1; }

        // --- Y座標が一定より低くなったらリロード ---
        if (!infiniteAmmo && triggerValue < 0.1f)
        {
            // --- 相対位置で判定 ---
            float relativeY = rayOrigin.position.y - playerHead.position.y;
            // Debug.Log("relativeY：" + relativeY);

            // --- 無限モード時 ---
            if (infiniteAmmo && relativeY < reloadThresholdY)
            {
                Reload();
                // Debug.Log("リロード開始（無限モード）");
            }
            // --- 通常モード時 ---
            else if (!infiniteAmmo && reserveAmmo > 0 && relativeY < reloadThresholdY)
            {
                Reload();
                // Debug.Log("リロード開始");
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
    /// 初心者モード
    /// </summary>
    void UpdateBeginnerMode()
    {
        // --- 🔸ストックが0なら描画しない ---
        if (reserveAmmo <= 0)
        {
            hasHit = false;
            shouldDraw = false;
            return; // この関数をここで終了！
        }

        RaycastHit hit;

        if (Physics.Raycast(rayOrigin.position, rayDirection, out hit, rayDistance))
        {
            hitPoint = hit.point;
            hasHit = true;
            shouldDraw = true;

            if (hit.collider.CompareTag("Enemy")) { lineRenderer.startColor = lineRenderer.endColor = new Color(1f, 0.5f, 0f); } // オレンジ
            else { lineRenderer.startColor = lineRenderer.endColor = Color.red; }
        }
        else
        {
            hasHit = false;
            shouldDraw = true; // 常に描画
            lineRenderer.startColor = lineRenderer.endColor = Color.red;
        }
    }

    /// <summary>
    /// 中級者モード
    /// </summary>
    void UpdateNormalMode()
    {
        RaycastHit hit;

        if (triggerValue > 0f)
        {
            // --- Raycast 判定 ---
            if (Physics.Raycast(rayOrigin.position, rayDirection, out hit, rayDistance))
            {
                hitPoint = hit.point;
                hasHit = true;
                shouldDraw = true;

                lineRenderer.startColor = lineRenderer.endColor = Color.red;

                // hit.collider で当たったオブジェクトの Collider にアクセスできる
                // Debug.Log("Ray hit: " + hit.collider.name);

                // 当たったオブジェクトのタグも確認できる
                // Debug.Log("Hit tag: " + hit.collider.tag);
            }
            else { hasHit = false; }
        }
        else if (triggerValue < 0.1f)
        {
            shouldDraw = false;
        }
    }

    /// <summary>
    /// 上級者モード
    /// </summary>
    void UpdateExpertMode()
    {
        hasHit = false;
        shouldDraw = false;
        Debug.DrawRay(rayOrigin.position, rayDirection * rayDistance, Color.red);
    }

    /// <summary>
    /// 弾数チェック共通化
    /// </summary>
    bool CanShoot()
    {
        if (currentAmmo <= 0)
        {
            // Debug.Log("弾がありません！リロードしてください。");
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
        if (SoundManager.Instance == null) {
            // Debug.LogError("SoundManager.Instance が null です！");
        }
        else {
            // Debug.Log("SoundManager 再生チェック OK");
        }

        // --- 発砲音 ---
        SoundManager.Instance?.PlaySE("SE_Gun_01");

        UpdateLoadedDisplay();

        // if (hapticC != null) { hapticC.VibrateLingeringSound(isLeftHand); }
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

        UpdateLoadedDisplay();
    }

    /// <summary>
    /// リロード用コード
    /// </summary>
    void Reload()
    {
        if (currentAmmo > 0)
        {
            // Debug.Log("すでに装填済みです。");
            return;
        }

        // --- 無限モード ---
        if (infiniteAmmo)
        {
            currentAmmo = 1;
            // Debug.Log("【DEBUG】無限ストック：リロードしました！");
            SoundManager.Instance?.PlaySE("SE_Gun_02");
            return;
        }

        // --- 通常モード ---
        if (reserveAmmo > 0)
        {
            currentAmmo = 1;
            reserveAmmo--;
            // Debug.Log("リロード完了！残りストック：" + reserveAmmo);
            SoundManager.Instance?.PlaySE("SE_Gun_02");
        }
        else
        {
            // Debug.Log("ストックがありません！");
        }

        if (hapticC != null) { hapticC.VibrateReload(isLeftHand); }

        isReloading = true;

        // --- 短い待機で再リロード防止 ---
        Invoke(nameof(ResetReload), 0.5f);

        UpdateLoadedDisplay();
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
        float timer = 0f;

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
                timer += Time.deltaTime;
                ammoSlider.value = timer / autoAmmoDelay; // 進行に応じてゲージ更新

                if (timer >= autoAmmoDelay)
                {
                    timer = 0f;
                    reserveAmmo++;
                    UpdateReserveText();
                    // Debug.Log("ストックを補充！ 現在：" + reserveAmmo);

                    // 満タン→リセット
                    ammoSlider.value = 0f;
                }
            }
            else
            {
                // --- 弾が十分ならゲージ非表示＆リセット ---
                ammoSlider.value = 0f;
                timer = 0f;
            }

            // --- 一定時間待機（例：5秒ごと）---
            yield return null;
        }
    }

    /// <summary>
    /// 装填済みをImageで表示
    /// </summary>
    void UpdateLoadedDisplay()
    {
        if (loadedImage == null || numberSprites == null) return;

        int digit;

        // --- 🔸無限モード中は常に「1」を表示 ---
        if (infiniteAmmo)
        {
            digit = 1;
        }
        else
        {
            digit = Mathf.Clamp(currentAmmo, 0, 1);
        }

        // --- スプライト設定 ---
        loadedImage.sprite = numberSprites[digit];
        loadedImage.enabled = true;

        // --- 色変更 ---
        if (infiniteAmmo)
        {
            // 無限モード → 緑固定
            loadedImage.color = new Color(0.263f, 1f, 0f, 1f); // ライムグリーン
        }
        else if (digit == 0)
        {
            // 弾切れ → オレンジ赤
            loadedImage.color = new Color(1f, 0.345f, 0f, 1f);
        }
        else
        {
            // 通常時 → 緑
            loadedImage.color = new Color(0.263f, 1f, 0f, 1f);
        }
    }

    /// <summary>
    /// ストックをImageで表示
    /// </summary>
    void UpdateReserveText()
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        string numStr = reserveAmmo.ToString();

        // --- 数字Imageを一旦非表示 ---
        foreach (var img in digitImages)
        {
            if (img != null) { img.enabled = false; }
        }

        if (infiniteAmmo) { return; }

        // --- 右詰めで数字セット ---
        for (int i = 0; i < numStr.Length && i < digitImages.Length; i++)
        {
            int digit = numStr[numStr.Length - 1 - i] - '0';
            int index = digitImages.Length - 1 - i;

            if (digitImages[index] != null)
            {
                digitImages[index].sprite = numberSprites[digit];
                digitImages[index].enabled = true;

                // --- 🔸色変更処理をここに追加 ---
                if (reserveAmmo == 0) {
                    digitImages[index].color = new Color(1f, 0.345f, 0f, 1f); // 弾切れ → 赤
                }
                else {
                    digitImages[index].color = Color.white; // 通常 → 白
                }
            }
        }
    }

    /// <summary>
    /// Enemyを倒したら弾の追加
    /// </summary>
    public void OnParentTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ammo"))
        {
            reserveAmmo++;
            UpdateReserveText();
            Destroy(other.gameObject);
        }
    }

    /// <summary>
    /// リスポーン時にも自動生成コルーチンを再開
    /// </summary>
    void OnEnable()
    {
        StartCoroutine(AutoAmmoReserve());
    }
}
