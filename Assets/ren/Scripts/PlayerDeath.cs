using UnityEngine;

/// <summary>
/// プレイヤーの死亡処理とリスポーン処理を管理する。
/// 敵や奈落に接触した際に死亡し、
/// GameManager が保持する現在のリスポーン地点から復帰する。
/// </summary>
public class PlayerDeath : MonoBehaviour
{
    [Header("死亡時に生成するエフェクト（任意）")]
    public GameObject deathEffect;

    private bool isDead = false;

    private void OnTriggerEnter(Collider other)
    {
        // 敵または奈落に触れたら死亡
        if (other.CompareTag("Enemy") || other.CompareTag("FallZone"))
        {
            Die();
        }
    }

    /// <summary>
    /// 死亡時の処理。
    /// エフェクトを出し、一定時間後にリスポーン。
    /// </summary>
    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log("💀 プレイヤー死亡");

        // 死亡エフェクト生成
        if (deathEffect != null)
            Instantiate(deathEffect, transform.position, Quaternion.identity);

        // プレイヤーを一時的に無効化
        gameObject.SetActive(false);

        // 2秒後にリスポーン
        Invoke(nameof(Respawn), 2f);
    }

    /// <summary>
    /// リスポーン処理。
    /// GameManager から現在のリスポーン地点を取得して移動。
    /// </summary>
    void Respawn()
    {
        Transform respawnPoint = GameManager.Instance.GetRespawnPoint();
        transform.position = respawnPoint.position;

        Debug.Log($"🔄 プレイヤーがリスポーン ({respawnPoint.name})");

        // 再びプレイヤーを有効化
        gameObject.SetActive(true);
        isDead = false;
    }
}
