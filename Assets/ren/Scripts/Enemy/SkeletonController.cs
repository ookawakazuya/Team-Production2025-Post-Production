using UnityEngine;
using UnityEngine.VFX;

public class SkeletonController : EnemyBase
{
    [Header("Hit Colliders")]
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Collider headCollider;

    [Header("Damage")]
    [SerializeField] private float headShotMultiplier = 2f;

    [Header("Death")]
    [SerializeField] private float deathAnimTime = 2f;
    [SerializeField] private float deathVfxTime = 0.5f;
    [SerializeField] private VisualEffect deathVFX;

    [Header("Drop")]
    [SerializeField] private GameObject ammoPrefab;

    // ★ 追加：剣オブジェクト
    [Header("Weapon")]
    [SerializeField] private GameObject swordObject;

    // =====================
    // Damage Calculation
    // =====================
    protected override float CalculateDamage(float baseDamage, Collider hitPart)
    {
        animator.SetBool("isDamage", true);
        Invoke(nameof(ResetDamageAnim), 0.3f);

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
        animator.SetBool("isDeath", true);

        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        bodyCollider.enabled = false;
        headCollider.enabled = false;

        // ★ 剣を非表示
        if (swordObject)
            swordObject.SetActive(false);

        animator.SetTrigger("Death");

        yield return new WaitForSeconds(deathAnimTime);

        if (deathVFX)
        {
            deathVFX.transform.SetParent(null);
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        yield return new WaitForSeconds(deathVfxTime);

        DropAmmo(ammoPrefab);

        gameObject.SetActive(false);
    }

    // =====================
    // Respawn
    // =====================
    protected override void OnRespawn()
    {
        // Animator完全初期化
        animator.Rebind();
        animator.Update(0f);

        // ★ Death系を完全リセット
        animator.ResetTrigger("Death");

        animator.SetBool("isDamage", false);
        animator.SetBool("isAttack", false);
        animator.SetBool("isChase", false);
        animator.SetBool("isDeath", false);

        // ★ ここが最重要：Idleに強制遷移
        animator.Play("Idle", 0, 0f);

        bodyCollider.enabled = true;
        headCollider.enabled = true;

        // 剣を再表示
        if (swordObject)
            swordObject.SetActive(true);

        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(false);
            deathVFX.transform.SetParent(transform);
            deathVFX.transform.localPosition = Vector3.zero;
            deathVFX.transform.localRotation = Quaternion.identity;
        }
    }


    // =====================
    // Animation Event 用
    // =====================
    public void PlayWalkSE()
    {
        SoundManager.Instance.PlaySE("SE_Enemy_02");
    }

    public void PlayAttackSE()
    {
        SoundManager.Instance.PlaySE("SE_Enemy_04");
    }

    public void PlayHitSE()
    {
        SoundManager.Instance.PlaySE("SE_Enemy_06");
    }
}
