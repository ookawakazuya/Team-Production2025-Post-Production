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

    [Header("Attack Timing")]
    [SerializeField] private float attackCooldown = 1f;
    [SerializeField] private float damageStartTime = 0.3f;
    [SerializeField] private float damageEndTime = 0.6f;
    [SerializeField] private float attackTotalTime = 1.0f;

    [Header("Hit Colliders")]
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("Damage")]
    [SerializeField] private float headShotMultiplier = 2f;

    [Header("Death")]
    [SerializeField] private float deathAnimTime = 2f;   // ★ 変更可能
    [SerializeField] private float deathVfxTime = 0.5f;
    [SerializeField] private VisualEffect deathVFX;

    [Header("Drop")]
    [SerializeField] private GameObject ammoPrefab;

    //NavMeshAgent agent;
    //Animator animator;

    State currentState = State.Idle;

    float lastAttackTime;
    float attackTimer;
    bool hasHitThisAttack;

    public bool CanDealDamage =>
        currentState == State.Attack &&
        attackTimer >= damageStartTime &&
        attackTimer <= damageEndTime &&
        !hasHitThisAttack;

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
                animator.SetBool("Chase", false);
                if (dist <= chaseDistance)
                    currentState = State.Chase;
                break;

            case State.Chase:
                animator.SetBool("Chase", true);
                if (dist <= attackDistance)
                    TryEnterAttack();
                else
                    UpdateChaseMovement(dist);
                break;

            case State.Attack:
                UpdateAttack();
                FaceTarget();
                break;
        }
    }

    void TryEnterAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        currentState = State.Attack;
        agent.isStopped = true;

        animator.SetBool("Chase", false);
        animator.SetTrigger("Attack");

        attackTimer = 0f;
        hasHitThisAttack = false;
        lastAttackTime = Time.time;
    }

    void UpdateAttack()
    {
        attackTimer += Time.deltaTime;

        if (attackTimer >= attackTotalTime)
        {
            ExitAttack();
            return;
        }

        if (Vector3.Distance(transform.position, player.position) > attackDistance)
            ExitAttack();
    }

    void ExitAttack()
    {
        if (isDead) return;

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

    protected override float CalculateDamage(float baseDamage, Collider hitPart)
    {
        if (hitPart == headCollider)
            return baseDamage * headShotMultiplier;

        return baseDamage;
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
        agent.isStopped = true;
        bodyCollider.enabled = false;
        headCollider.enabled = false;

        animator.SetTrigger("Death");

        yield return new WaitForSeconds(deathAnimTime);

        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        yield return new WaitForSeconds(deathVfxTime);

        // 弾薬ドロップ
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

        bodyCollider.enabled = true;
        headCollider.enabled = true;

        agent.isStopped = false;
        agent.ResetPath();

        if (deathVFX)
            deathVFX.gameObject.SetActive(false);
    }

    public void MarkAttackHit()
    {
        hasHitThisAttack = true;
    }
}
