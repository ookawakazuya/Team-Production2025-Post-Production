using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class SkeletonController : EnemyBase
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Death
    }

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Hit Colliders")]
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("Damage")]
    [SerializeField] private float headShotMultiplier = 2f;

    [Header("Death")]
    [SerializeField] private float deathAnimTime = 2f;
    [SerializeField] private float deathVfxTime = 0.5f;
    [SerializeField] private VisualEffect deathVFX;

    [Header("Drop")]
    [SerializeField] private GameObject ammoPrefab;

    //NavMeshAgent agent;
    //Animator animator;

    State currentState = State.Idle;
    float lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (isDead || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                // Idleは常にIdleループ
                animator.SetBool("isChase", false);
                animator.SetBool("isAttack", false);

                if (dist <= chaseDistance)
                {
                    currentState = State.Chase;
                    animator.SetBool("isChase", true); // Chaseループ開始
                }
                break;

            case State.Chase:
                // Chaseは見つけている間ずっとループ
                animator.SetBool("isChase", true);

                UpdateChaseMovement(dist);

                if (dist <= attackDistance)
                {
                    TryAttack(); // Attackへ（1回再生）
                }
                else if (dist > chaseDistance)
                {
                    // 見失ったらIdleへ
                    currentState = State.Idle;
                    animator.SetBool("isChase", false);
                }
                break;

            case State.Attack:
                // Attack中は移動しない・Faceだけ
                FaceTarget();
                break;
        }
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        currentState = State.Attack;
        agent.isStopped = true;

        animator.SetBool("isAttack", true);

        lastAttackTime = Time.time;
        Invoke(nameof(ExitAttack), 1.0f);
    }

    void ExitAttack()
    {
        if (isDead) return;

        animator.SetBool("isAttack", false);
        agent.isStopped = false;
        currentState = State.Chase;
        animator.SetBool("isChase", true);
    }

    void FaceTarget()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude > 0.01f)
        {
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation =
                Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 10f);
        }
    }

    protected override float CalculateDamage(float baseDamage, Collider hitPart)
    {
        animator.SetBool("isDamage", true);
        Invoke(nameof(ResetDamageFlag), 0.3f);

        if (hitPart == headCollider)
            return baseDamage * headShotMultiplier;

        return baseDamage;
    }

    void ResetDamageFlag()
    {
        if (!isDead)
            animator.SetBool("isDamage", false);
    }

    protected override void Die()
    {
        if (isDead) return;

        isDead = true;
        currentState = State.Death;

        animator.SetBool("isAttack", false);
        animator.SetBool("isDamage", false);

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        // NavMeshAgentは「有効＆NavMesh上」の時だけ止める
        if (agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // boolを擬似Trigger化：1フレームだけtrueにしてすぐ戻す
        animator.SetBool("isDeath", true);
        yield return null; // 1フレーム待って遷移を確定させる
        animator.SetBool("isDeath", false);

        bodyCollider.enabled = false;
        headCollider.enabled = false;

        yield return new WaitForSeconds(deathAnimTime);

        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        yield return new WaitForSeconds(deathVfxTime);

        DropAmmo(ammoPrefab);

        gameObject.SetActive(false);
        hpUIRoot?.SetActive(false);
    }

    protected override void OnRespawn()
    {
        currentState = State.Idle;
        isDead = false;

        animator.Rebind();
        animator.Update(0f);

        animator.SetBool("isAttack", false);
        animator.SetBool("isDamage", false);
        animator.SetBool("isDeath", false);

        bodyCollider.enabled = true;
        headCollider.enabled = true;

        agent.isStopped = false;
        agent.ResetPath();

        if (deathVFX)
            deathVFX.gameObject.SetActive(false);
    }
}
