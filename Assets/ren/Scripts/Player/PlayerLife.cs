using UnityEngine;

public class PlayerLife : MonoBehaviour
{
    [Header("死亡時に生成するエフェクト（任意）")]
    public GameObject deathEffect;

    private bool isDead = false;
    public bool IsDead => isDead;

    public delegate void PlayerDeathHandler();
    public static event PlayerDeathHandler OnPlayerDied;

    private PlayerHealth playerHealth;
    private Collider playerCollider;
    private MonoBehaviour playerController;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerCollider = GetComponent<Collider>();
        playerController = GetComponent<VRController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        // ★ 即死トラップ判定
        if (other.CompareTag("FallZone")
         || other.CompareTag("Magma")
         || other.CompareTag("Acid")
         || other.CompareTag("Needle"))
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

        if (playerController) playerController.enabled = false;
        if (playerCollider) playerCollider.enabled = false;

        if (deathEffect)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        OnPlayerDied?.Invoke();

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

            GameManager.Instance.OnPlayerDead();
        });
    }
}
