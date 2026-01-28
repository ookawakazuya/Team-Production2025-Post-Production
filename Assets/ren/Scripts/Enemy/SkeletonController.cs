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

        // 当たり判定OFF
        bodyCollider.enabled = false;
        headCollider.enabled = false;

        // ===== ① 死亡アニメ =====
        animator.SetTrigger("Death"); // Trigger推奨

        yield return new WaitForSeconds(deathAnimTime);

        // ===== ② 死亡VFX =====
        if (deathVFX)
        {
            deathVFX.transform.SetParent(null); // ★親から切り離す
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        yield return new WaitForSeconds(deathVfxTime);

        // ===== ③ ドロップ =====
        DropAmmo(ammoPrefab);

        // ===== ④ Enemy消滅 =====
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
