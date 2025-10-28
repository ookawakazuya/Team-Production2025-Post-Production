using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class Shotgun : MonoBehaviour
{
    [Header("XR 設定")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor rightHandInteractor;

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

    private int pelletCount = 8;     // ショットガンの散弾数
    private float spreadAngle = 0.2f; // 拡散角度
    private GameObject gunInstance;
    private InputAction triggerAction;
    private bool isShooting = false;
    private GameObject currentBullet;

/*  [SerializeField] Transform target;
    [SerializeField] Image image;
    [SerializeField] float rayDistance = 10f;

    [Header("Bullet Settings")]
    [SerializeField] GameObject bulletPrefab;
    [SerializeField] float bulletSpeed = 50.0f;
    [SerializeField] float bulletLifeTime = 0.2f;
    public Transform firePoint; // 発射位置（空のGameObjectなど）
    private GameObject currentBullet;
    private bool Bullet = false;
    private int pelletCount = 8;       // 発射する弾数
    private float spreadAngle = 0.2f;   // 拡散角度（度単位）
*/

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // --- 銃の装備 ---
        if (rightHandInteractor == null)
        {
            Debug.LogError("RightHandInteractor が設定されていません。");
            return;
        }

        if (gunPrefab != null)
        {
            gunInstance = Instantiate(gunPrefab);
            gunInstance.transform.SetParent(rightHandInteractor.transform);
            gunInstance.transform.localPosition = gunOffset;
            gunInstance.transform.localRotation = Quaternion.identity;
        }

        // --- トリガー入力設定 ---
        triggerAction = new InputAction("Fire", binding: "<XRController>{RightHand}/trigger");
        triggerAction.Enable();
    }

    // Update is called once per frame
    void Update()
    {
        // --- 照準処理 ---
        Vector3 rayOrigin = transform.position; // 自分の位置
        Vector3 rayDirection = transform.forward; // ターゲットへの方向（正規化）

        Debug.DrawRay(rayOrigin, rayDirection * rayDistance, Color.red); // Rayを可視化

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out hit, rayDistance))
        {
            Debug.Log("Ray hit:");
            crosshairImage.color = Color.green;

            if (currentBullet == null && !isShooting) { Shoot(); isShooting = true; }
        }
        else { crosshairImage.color = Color.red; }
    }

    void Shoot() // ショットガンをモデルに弾を飛ばす
    {
        for (int i = 0; i < pelletCount; i++)
        {
            // 玉を生成（向きもfirePointの向きに合わせる）
            GameObject bullet = Instantiate(bulletPrefab, transform.position, transform.rotation);
            // Rigidbodyが付いていることを確認して速度を設定
            Rigidbody rb = bullet.GetComponent<Rigidbody>();

            if (rb != null) {
                // ランダムな角度で拡散
                // float randomYaw = Random.Range(-spreadAngle / 2f, spreadAngle / 2f); // Yaw（左右方向）
                // float randomPitch = Random.Range(-spreadAngle / 2f, spreadAngle / 2f); // Pitch（上下方向）
                Vector3 randomOffset = Random.insideUnitSphere * spreadAngle;

                // 拡散角度を反映した方向ベクトルを計算
                Vector3 spreadDirection = (transform.forward + randomOffset).normalized;

                // 弾に速度を設定
                rb.linearVelocity = transform.forward * bulletSpeed;
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
