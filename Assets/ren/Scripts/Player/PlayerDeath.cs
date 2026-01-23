using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("死亡時に生成するエフェクト（任意）")]
    public GameObject deathEffect;

    private bool isDead = false;

    public bool IsDead => isDead;

    // 拡張用イベント（将来UIやSE用）
    public delegate void PlayerDeathHandler();
    public static event PlayerDeathHandler OnPlayerDied;

    private void OnTriggerEnter(Collider other)
    {


        if (isDead) return;

        if (other.CompareTag("FallZone"))
        {
            Die();
        }
        if (other.CompareTag("Magma")){
            Die();
        }

        if (other.CompareTag("Enemy") ||
            other.CompareTag("Sword"))
        {
            var health = GetComponent<PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(1);
            }
        }
    }


    public void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("プレイヤー死亡");

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // 🔔 拡張用イベント
        OnPlayerDied?.Invoke();

        // 🔥 ここが最重要：必ず Enemy を復活させる
        GameManager.Instance.OnPlayerDead();

        // フェードアウト → 非表示 → リスポーン
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

        // ★ 物理リセット（どちらか使ってる方）
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        GetComponent<PlayerHealth>()?.ResetLife();

        gameObject.SetActive(true);

        FadeController.Instance.FadeIn(1f, () =>
        {
            isDead = false;
        });
    }
}
