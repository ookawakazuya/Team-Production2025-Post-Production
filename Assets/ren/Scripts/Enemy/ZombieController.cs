using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : EnemyBase
{
    public enum State
    {
        Idle,
        Chase,
        Attack
    }

    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Movement")]
    [SerializeField] private float chaseDistance = 15f;
    [SerializeField] private float attackDistance = 5f;

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

    [Header("VFX")]
    public VisualEffect deathVFX;

    NavMeshAgent agent;
    Animator animator;

    State currentState = State.Idle;

    float lastAttackTime;
    float attackTimer;

    bool hasHitThisAttack;

    // ★ HandHit 用
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

    protected override void Start()
    {
        base.Start();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
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
                    agent.SetDestination(player.position);
                break;

            case State.Attack:
                UpdateAttack();
                FaceTarget();
                break;
        }
    }

    // =====================
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

        // プレイヤーが離れたら攻撃中断
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > attackDistance)
        {
            ExitAttack();
        }
    }

    void ExitAttack()
    {
        currentState = State.Chase;
        agent.isStopped = false;
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
        if (hitPart == headCollider)
            return baseDamage * headShotMultiplier;

        return baseDamage;
    }

    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(DeathSequence());
    }

    System.Collections.IEnumerator DeathSequence()
    {
        agent.isStopped = true;

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
        attackTimer = 0f;
        hasHitThisAttack = false;

        agent.isStopped = false;
        agent.ResetPath();

        animator.Rebind();
        animator.Update(0f);

        bodyCollider.enabled = true;
        headCollider.enabled = true;

        if (deathVFX)
            deathVFX.gameObject.SetActive(false);
    }

    // ★ HandHit 用
    public void MarkAttackHit()
    {
        hasHitThisAttack = true;
    }
}
