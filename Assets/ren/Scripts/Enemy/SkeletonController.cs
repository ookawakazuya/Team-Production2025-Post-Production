using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Skeleton 固有の挙動
/// ・ヘッドショット倍率
/// ・死亡演出（アニメ＋VFX＋ドロップ）
/// ※ AI / 移動 / 攻撃は EnemyBase 側
/// </summary>
public class SkeletonController : EnemyBase
{
    // =====================
    // Hit Colliders
    // =====================
    [Header("Hit Colliders")]
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Collider headCollider;

    // =====================
    // Damage
    // =====================
    [Header("Damage")]
    [SerializeField] private float headShotMultiplier = 2f;

    // =====================
    // Death
    // =====================
    [Header("Death")]
    [SerializeField] private float deathAnimTime = 2f;
    [SerializeField] private float deathVfxTime = 0.5f;
    [SerializeField] private VisualEffect deathVFX;

    // =====================
    // Drop
    // =====================
    [Header("Drop")]
    [SerializeField] private GameObject ammoPrefab;

    // =====================
    // Damage Calculation
    // =====================
    protected override float CalculateDamage(float baseDamage, Collider hitPart)
    {
        // 被弾アニメ（共通挙動・変更なし）
        animator.SetBool("isDamage", true);
        Invoke(nameof(ResetDamageAnim), 0.3f);

        // ヘッドショットのみ倍率
        if (hitPart == headCollider)
            return baseDamage * headShotMultiplier;

        return baseDamage;
    }

    private void ResetDamageAnim()
    {
        if (!isDead)
            animator.SetBool("isDamage", false);
    }

    // =====================
    // Death
    // =====================
    protected override void Die()
    {
        if (isDead) return;

        isDead = true;
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        // NavMesh 停止
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 死亡アニメ（ワンフレームトリガー）
        animator.SetBool("isDeath", true);
        yield return null;
        animator.SetBool("isDeath", false);

        // 当たり判定無効化
        bodyCollider.enabled = false;
        headCollider.enabled = false;

        // アニメ終了待ち
        yield return new WaitForSeconds(deathAnimTime);

        // VFX 再生
        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        // VFX 再生時間待ち
        yield return new WaitForSeconds(deathVfxTime);

        // ドロップ
        DropAmmo(ammoPrefab);

        // 非表示（EnemyManager 管理想定）
        gameObject.SetActive(false);
    }

    // =====================
    // Respawn
    // =====================
    protected override void OnRespawn()
    {
        // Animator 初期化
        animator.Rebind();
        animator.Update(0f);

        // 当たり判定復活
        bodyCollider.enabled = true;
        headCollider.enabled = true;

        // VFX 非表示
        if (deathVFX)
            deathVFX.gameObject.SetActive(false);
    }
}
