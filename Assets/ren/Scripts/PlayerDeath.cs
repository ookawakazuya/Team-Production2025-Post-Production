using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("死亡時に生成するエフェクト（任意）")]
    public GameObject deathEffect;

    bool isDead = false;

    // 敵に通知用イベント
    public delegate void PlayerDeathHandler();
    public static event PlayerDeathHandler OnPlayerDied;

    void OnCollisionEnter(Collision collision)
    {

    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Enemy") || other.gameObject.CompareTag("FallZone"))
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("プレイヤー死亡");

        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // 敵に死亡通知
        OnPlayerDied?.Invoke();

        // フェードアウト→非表示→リスポーン
        FadeController.Instance.FadeOut(1f, () =>
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), 2f);
        });
    }

    void Respawn()
    {
        Transform respawnPoint = GameManager.Instance.GetRespawnPoint();
        transform.position = respawnPoint.position;
        Debug.Log($"プレイヤーがリスポーン ({respawnPoint.name})");

        gameObject.SetActive(true);

        // フェードイン
        FadeController.Instance.FadeIn(1f, () =>
        {
            isDead = false;
        });
    }
}
