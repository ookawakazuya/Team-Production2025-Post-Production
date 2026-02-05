using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using System.Collections;


/// <summary>
/// 敵の共通基底クラス
/// ・AI（Idle / Chase / Attack）
/// ・HP管理 / HP UI
/// ・攻撃処理（AnimationEvent）
/// ・リスポーン
/// ※ 挙動は従来から変更なし
/// </summary>
public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    // =====================
    // State
    // =====================
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Death
    }
    public System.Action OnDead;

    protected EnemyState currentState = EnemyState.Idle;

    // =====================
    // HP
    // =====================
    [Header("HP")]
    [SerializeField] protected float maxHP = 100f;
    protected float currentHP;
    protected bool isDead;

    // =====================
    // HP UI
    // =====================
    [Header("HP UI")]
    [SerializeField] protected GameObject hpUIRoot;
    [SerializeField] protected Image hpGreen;
    [SerializeField] protected Image hpRed;

    [Header("HP UI Distance")]
    [SerializeField] protected float hpVisibleDistance = 10f;

    Coroutine greenRoutine;
    Coroutine redRoutine;

    // =====================
    // AI Distance
    // =====================
    [Header("AI Distance")]
    [SerializeField] protected float chaseDistance = 15f;
    [SerializeField] protected float attackDistance = 5f;
    [SerializeField] protected float stopDistance = 2f;

    // =====================
    // Attack
    // =====================
    [Header("Attack")]
    [SerializeField] protected int attackDamage = 1;
    [SerializeField] protected float attackCooldown = 1.5f;
    [SerializeField] protected float attackAnimTime = 1.0f;

    [Header("Attack Hit Range（敵ごと調整）")]
    [SerializeField] protected float attackHitRadius = 1.5f;
    [SerializeField] protected Vector3 attackHitOffset = Vector3.forward;

    protected float lastAttackTime;

    // =====================
    // Components
    // =====================
    protected Transform player;
    protected NavMeshAgent agent;
    protected Animator animator;

    // =====================
    // Respawn
    // =====================
    protected Vector3 startPosition;
    protected Quaternion startRotation;

    // =====================
    // Unity Lifecycle
    // =====================
    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.stoppingDistance = stopDistance; // ★追加

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    protected virtual void Start()
    {
        currentHP = maxHP;

        GameManager.Instance?.RegisterEnemy(this);
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    protected virtual void Update()
    {
        if (isDead || player == null) return;

        UpdateHPUIVisibility();
        UpdateAI();
    }

    // =====================
    // AI
    // =====================
    protected virtual void UpdateAI()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackDistance)
        {
            currentState = EnemyState.Attack;
            FaceTarget();
            TryAttack();
            return;
        }

        if (distance <= chaseDistance)
        {
            currentState = EnemyState.Chase;
            UpdateChase(distance);
            return;
        }

        EnterIdle();
    }

    protected void UpdateChase(float distance)
    {
        animator.SetBool("isChase", true);
        animator.SetBool("isAttack", false);

        if (!agent || !agent.isOnNavMesh) return;

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    protected void EnterIdle()
    {
        currentState = EnemyState.Idle;

        animator.SetBool("isChase", false);
        animator.SetBool("isAttack", false);

        if (agent && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    protected void FaceTarget()
    {
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
    }

    // =====================
    // Attack
    // =====================
    protected void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;

        if (agent && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; // ★重要
        }

        animator.SetBool("isAttack", true);

        CancelInvoke(nameof(EndAttack)); // ★多重Invoke防止
        Invoke(nameof(EndAttack), attackAnimTime);
    }

    protected void EndAttack()
    {
        if (isDead) return;

        animator.SetBool("isAttack", false);

        if (agent && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    /// <summary>
    /// AnimationEvent から呼ばれる実ダメージ処理
    /// </summary>
    public void DealDamageToPlayer()
    {
        if (!player) return;

        Vector3 center =
            transform.position +
            transform.forward * attackHitOffset.z +
            Vector3.up * attackHitOffset.y;

        Collider[] hits = Physics.OverlapSphere(center, attackHitRadius);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Player")) continue;

            PlayerHealth health = hit.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(attackDamage);

            break;
        }
    }

    // =====================
    // HP UI
    // =====================
    protected void UpdateHPUIVisibility()
    {
        if (!hpUIRoot || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        bool visible =
            !isDead &&
            currentHP < maxHP &&
            dist <= hpVisibleDistance;

        if (hpUIRoot.activeSelf != visible)
            hpUIRoot.SetActive(visible);
    }

    protected void UpdateHPUI()
    {
        float ratio = currentHP / maxHP;

        if (greenRoutine != null) StopCoroutine(greenRoutine);
        if (redRoutine != null) StopCoroutine(redRoutine);

        greenRoutine = StartCoroutine(AnimateHP(hpGreen, ratio, 0f));
        redRoutine = StartCoroutine(AnimateHP(hpRed, ratio, 0.5f));
    }

    IEnumerator AnimateHP(Image img, float target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float start = img.fillAmount;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            img.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }

        img.fillAmount = target;
    }

    // =====================
    // Damage
    // =====================
    public void ApplyDamage(int damage)
    {
        ApplyDamage((float)damage, null);
    }

    public void ApplyDamage(float baseDamage, Collider hitPart)
    {
        if (isDead) return;

        float finalDamage = CalculateDamage(baseDamage, hitPart);
        currentHP = Mathf.Max(0, currentHP - finalDamage);

        UpdateHPUI();

        if (currentHP <= 0)
        {
            Die();

            // ★ 追加：Spawner通知用（これだけ）
            OnDead?.Invoke();
        }
        else
            animator.SetBool("isDamage", true);
    }

    // =====================
    // Abstract
    // =====================
    protected abstract float CalculateDamage(float baseDamage, Collider hitPart);
    protected abstract void Die();
    protected abstract void OnRespawn();

    // =====================
    // Respawn
    // =====================
    public virtual void Respawn()
    {
        StopAllCoroutines();

        isDead = false;
        currentHP = maxHP;

        if (agent)
            agent.Warp(startPosition);
        else
            transform.position = startPosition;

        transform.rotation = startRotation;

        hpUIRoot?.SetActive(false);
        gameObject.SetActive(true);

        OnRespawn();
    }

    // =====================
    // Drop（共通）
    // =====================
    protected void DropAmmo(GameObject ammoPrefab)
    {
        if (!ammoPrefab) return;

        Vector3 dropPos = transform.position + Vector3.up * 1.0f;

        if (Physics.Raycast(dropPos, Vector3.down, out RaycastHit hit, 5f))
            dropPos = hit.point + Vector3.up * 0.1f;

        GameObject ammo = Instantiate(ammoPrefab, dropPos, Quaternion.identity);
        ammo.tag = "Ammo";

        // ★ GameManager に登録
        GameManager.Instance?.RegisterAmmo(ammo);
    }
}
