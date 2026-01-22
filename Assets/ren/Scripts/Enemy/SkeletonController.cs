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
        Damage,
        Death
    }

    [Header("Movement")]
    [SerializeField] private float chaseDistance = 15f;
    [SerializeField] private float attackDistance = 5f;

    [Header("Attack")]
    [SerializeField] private float attackCooldown = 1.5f;

    [Header("Hit Colliders")]
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("Damage")]
    [SerializeField] private float headShotMultiplier = 2f;

    [Header("VFX")]
    [SerializeField] private VisualEffect deathVFX;

    NavMeshAgent agent;
    Animator animator;

    State currentState = State.Idle;
    float lastAttackTime;

    protected override void Awake()
    {
        base.Awake();
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    protected override void Start()
    {
        base.Start();
        // EnemyBase の player を使用
    }

    void Update()
    {
        if (isDead || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                if (dist <= chaseDistance)
                    EnterChase();
                break;

            case State.Chase:
                agent.SetDestination(player.position);

                if (dist <= attackDistance)
                    TryAttack();
                break;

            case State.Attack:
                FaceTarget();
                break;
        }
    }

    // =====================
    void EnterChase()
    {
        currentState = State.Chase;
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        currentState = State.Attack;
        agent.isStopped = true;

        animator.SetBool("isAttack", true);

        lastAttackTime = Time.time;
        Invoke(nameof(ExitAttack), 1.0f); // 攻撃アニメ長
    }

    void ExitAttack()
    {
        animator.SetBool("isAttack", false);
        agent.isStopped = false;
        currentState = State.Chase;
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

    // =====================
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

        StartCoroutine(DeathSequence());
    }

    IEnumerator DeathSequence()
    {
        agent.isStopped = true;

        animator.SetBool("isDeath", true);

        bodyCollider.enabled = false;
        headCollider.enabled = false;

        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        yield return new WaitForSeconds(1f);

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
