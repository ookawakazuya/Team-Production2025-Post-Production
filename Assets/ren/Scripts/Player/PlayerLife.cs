using UnityEngine;

public class PlayerLife : MonoBehaviour
{
    [Header("死亡時に生成するエフェクト（任意）")]
    public GameObject deathEffect;

    private bool isDead = false;
    public bool IsDead => isDead;

    // 拡張用イベント（将来UIやSE用）
    public delegate void PlayerDeathHandler();
    public static event PlayerDeathHandler OnPlayerDied;

    // キャッシュ
    private PlayerHealth playerHealth;
    private Collider playerCollider;
    private MonoBehaviour playerController; // 移動・操作用

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerCollider = GetComponent<Collider>();

        // ★ 自分のプロジェクトの操作スクリプト名に合わせて
        playerController = GetComponent<VRController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("FallZone") || other.CompareTag("Magma"))
        {
            Die();
            return;
        }

        if (other.CompareTag("Enemy") || other.CompareTag("Sword"))
        {
            playerHealth?.TakeDamage(1);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;


        SoundManager.Instance.PlaySE("SE_Dead_01");
        // 操作・当たり判定を止める
        if (playerController) playerController.enabled = false;
        if (playerCollider) playerCollider.enabled = false;

        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // 拡張イベント
        OnPlayerDied?.Invoke();

        // Enemy / Drop など全体リセット
        GameManager.Instance.OnPlayerDead();

        // フェードアウト → リスポーン
        FadeController.Instance.FadeOut(1f, () =>
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), 0.5f);
        });
    }

    void Respawn()
    {
        Transform respawnPoint = GameManager.Instance.GetRespawnPoint();

        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        // 物理リセット
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerHealth?.ResetLife();

        gameObject.SetActive(true);

        FadeController.Instance.FadeIn(1f, () =>
        {
            if (playerCollider) playerCollider.enabled = true;
            if (playerController) playerController.enabled = true;

            isDead = false;

            // ★ ここでEnemy復活
            GameManager.Instance.OnPlayerDead();
        });
    }
}
