using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : EnemyBase
{
    public enum State
    {
        Idle,
        Chase,
        Attack,
        Death
    }

    [Header("Attack")]
    [SerializeField] float attackCooldown = 1.5f;

    [Header("Hit Colliders")]
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("Death")]
    [SerializeField] float deathAnimTime = 2f;
    [SerializeField] VisualEffect deathVFX;

    [Header("Drop")]
    [SerializeField] GameObject ammoPrefab;

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
                animator.SetBool("isChase", false);
                animator.SetBool("isAttack", false);

                if (dist <= chaseDistance)
                {
                    currentState = State.Chase;
                    animator.SetBool("isChase", true);
                }
                break;

            case State.Chase:
                animator.SetBool("isChase", true);
                UpdateChaseMovement(dist);

                if (dist <= attackDistance)
                    TryAttack();
                else if (dist > chaseDistance)
                {
                    currentState = State.Idle;
                    animator.SetBool("isChase", false);
                }
                break;

            case State.Attack:
                FaceTarget();
                break;
        }
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        currentState = State.Attack;

        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;

        animator.SetBool("isAttack", true);

        lastAttackTime = Time.time;
        CancelInvoke(nameof(ExitAttack));
        Invoke(nameof(ExitAttack), 1.0f); // UŒ‚ƒAƒjƒ’·
    }

    void ExitAttack()
    {
        if (isDead) return;

        animator.SetBool("isAttack", false);

        if (agent.enabled && agent.isOnNavMesh)
            agent.isStopped = false;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackDistance)
        {
            TryAttack(); // ˜A‘±UŒ‚
            return;
        }

        if (dist <= chaseDistance)
        {
            currentState = State.Chase;
            animator.SetBool("isChase", true);
        }
        else
        {
            currentState = State.Idle;
            animator.SetBool("isChase", false);
        }
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
        Invoke(nameof(ResetDamage), 0.3f);
        return baseDamage;
    }

    void ResetDamage()
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
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        animator.SetBool("isDeath", true);
        yield return null;
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

        DropAmmo(ammoPrefab);
        gameObject.SetActive(false);
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
