using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    [Header("死亡時に生成するエフェクト（任意）")]
    public GameObject deathEffect;

    private bool isDead = false;

    /// <summary>
    /// 衝突時に呼ばれる
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("FallZone"))
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

        // 一時的に非表示にする
        gameObject.SetActive(false);

        // 2秒後にリスポーン
        Invoke(nameof(Respawn), 2f);
    }

    void Respawn()
    {
        Transform respawnPoint = GameManager.Instance.GetRespawnPoint();
        transform.position = respawnPoint.position;

        Debug.Log($"プレイヤーがリスポーン ({respawnPoint.name})");

        gameObject.SetActive(true);
        isDead = false;
    }
}
