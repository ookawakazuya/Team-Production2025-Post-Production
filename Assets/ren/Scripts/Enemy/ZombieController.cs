using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Zombie 固有の挙動
/// ・ヘッド倍率なし
/// ・死亡演出（アニメ＋VFX＋ドロップ）
/// ※ Skeleton との差分のみ実装
/// </summary>
public class ZombieController : EnemyBase
{
    // =====================
    // Hit Colliders
    // =====================
    [Header("Hit Colliders")]
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Collider headCollider;

    // =====================
    // Death
    // =====================
    [Header("Death")]
    [SerializeField] private float deathAnimTime = 2f;
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
        // 被弾アニメ（倍率なし）
        animator.SetBool("isDamage", true);
        Invoke(nameof(ResetDamageAnim), 0.3f);

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

        // 死亡アニメ
        animator.SetBool("isDeath", true);
        yield return null;
        animator.SetBool("isDeath", false);

        // 当たり判定無効
        bodyCollider.enabled = false;
        headCollider.enabled = false;

        // アニメ終了待ち
        yield return new WaitForSeconds(deathAnimTime);

        // VFX
        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        // ドロップ
        DropAmmo(ammoPrefab);

        gameObject.SetActive(false);
    }

    // =====================
    // Respawn
    // =====================
    protected override void OnRespawn()
    {
        animator.Rebind();
        animator.Update(0f);

        bodyCollider.enabled = true;
        headCollider.enabled = true;

        if (deathVFX)
            deathVFX.gameObject.SetActive(false);
    }
}
