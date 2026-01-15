using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : MonoBehaviour
{
    public enum State { Idle, Chase, Return }

    [Header("基本設定")]
    public float maxHP = 100f;
    public float moveSpeed = 3f;
    public float chaseDistance = 10f;
    public float returnDistance = 15f;

    [Header("出現・消失距離")]
    public float spawnDistance = 15f;
    public float hideDistance = 16f;

    [Header("攻撃設定")]
    public float attackDistance = 1.5f;
    public float attackCooldown = 1f;
    public float attackAnimTime = 0.6f;

    // ★ 追加：攻撃判定
    [Header("攻撃判定")]
    public Collider attackCollider;

    [Header("モデル / コライダー")]
    public Renderer[] modelRenderers;
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("Animator")]
    [SerializeField] Animator animator;

    [Header("VFX")]
    public VisualEffect spawnVFX;
    public VisualEffect deathVFX;

    [Header("HP UI")]
    public GameObject hpUIRoot;
    public Image hpGreen;
    public Image hpRed;
    public Image hpFrame;

    Transform player;
    NavMeshAgent agent;
    Shotgun shotgun;

    float currentHP;
    State currentState = State.Idle;
    bool isVisible = false;
    bool isAttacking = false;
    float lastAttackTime = -999f;

    public bool IsDead { get; private set; } = false;

    Vector3 startPosition;
    Quaternion startRotation;

    Coroutine greenRoutine;
    Coroutine redRoutine;

    // =============================
    // 初期化
    // =============================
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        animator = GetComponentInChildren<Animator>();

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (modelRenderers.Length == 0)
            modelRenderers = GetComponentsInChildren<Renderer>(true);

        // ★ 追加：攻撃判定は最初OFF
        if (attackCollider)
            attackCollider.enabled = false;
    }

    void Start()
    {
        currentHP = maxHP;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        shotgun = FindFirstObjectByType<Shotgun>();

        SetVisible(false, true);
        hpUIRoot.SetActive(false);
    }

    // =============================
    // Update
    // =============================
    void Update()
    {
        if (!player)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (!player) return;
        }

        float dist = Vector3.Distance(player.position, transform.position);

        if (!IsDead)
        {
            if (!isVisible && dist < spawnDistance) ShowEnemy();
            else if (isVisible && dist > hideDistance) HideEnemy();
        }

        if (!isVisible || IsDead) return;

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                animator.SetBool("isChase", false);

                if (dist <= chaseDistance)
                    currentState = State.Chase;
                break;

            case State.Chase:
                if (isAttacking) return;

                agent.isStopped = false;
                animator.SetBool("isChase", true);
                SafeSetDestination(player.position);

                if (dist <= attackDistance)
                    TryAttack(dist);
                else if (dist > returnDistance)
                    currentState = State.Return;
                break;

            case State.Return:
                agent.isStopped = false;
                animator.SetBool("isChase", true);
                SafeSetDestination(startPosition);

                if (Vector3.Distance(transform.position, startPosition) < 1f)
                {
                    agent.isStopped = true;
                    currentState = State.Idle;
                }
                break;
        }
    }

    // =============================
    // 攻撃処理
    // =============================
    void TryAttack(float dist)
    {
        if (isAttacking) return;
        if (Time.time < lastAttackTime + attackCooldown) return;

        lastAttackTime = Time.time;
        isAttacking = true;

        agent.isStopped = true;
        animator.SetBool("isAttack", true);

        StartCoroutine(EndAttack(dist));
    }

    IEnumerator EndAttack(float distAtAttack)
    {
        yield return new WaitForSeconds(attackAnimTime);

        animator.SetBool("isAttack", false);
        isAttacking = false;

        if (distAtAttack <= chaseDistance)
            currentState = State.Chase;
        else
            currentState = State.Idle;
    }

    // ★ 追加：AnimationEvent 用
    public void EnableAttackCollider()
    {
        if (attackCollider)
            attackCollider.enabled = true;
    }

    public void DisableAttackCollider()
    {
        if (attackCollider)
            attackCollider.enabled = false;
    }

    // =============================
    // ダメージ処理
    // =============================
    public void ApplyDamage(float damage)
    {
        if (IsDead) return;

        currentHP = Mathf.Max(0, currentHP - damage);
        float target = currentHP / maxHP;

        if (greenRoutine != null) StopCoroutine(greenRoutine);
        greenRoutine = StartCoroutine(AnimateHP(hpGreen, target, 0f));

        if (redRoutine != null) StopCoroutine(redRoutine);
        redRoutine = StartCoroutine(AnimateHP(hpRed, target, 0.5f));

        if (currentHP <= 0f)
            Die();
    }

    IEnumerator AnimateHP(Image img, float target, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

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

    // =============================
    // 出現・消失
    // =============================
    void ShowEnemy()
    {
        if (IsDead) return;
        SetVisible(true);
        hpUIRoot.SetActive(true);
        PlaySpawnVFX();
    }

    void HideEnemy()
    {
        if (IsDead) return;
        SetVisible(false);
        hpUIRoot.SetActive(false);
    }

    void SetVisible(bool visible, bool immediate = false)
    {
        isVisible = visible;

        foreach (var r in modelRenderers)
            if (r) r.enabled = visible;

        if (bodyCollider) bodyCollider.enabled = visible;
        if (headCollider) headCollider.enabled = visible;

        agent.isStopped = !visible;

        if (immediate)
        {
            agent.Warp(transform.position);
            agent.ResetPath();
        }
    }

    // =============================
    // 死亡
    // =============================
    void Die()
    {
        if (IsDead) return;
        IsDead = true;

        animator.SetBool("isAttack", false);
        animator.SetBool("isChase", false);

        SetVisible(false, true);
        hpUIRoot.SetActive(false);
        PlayDeathVFX();
        shotgun?.plusAmmo();
    }

    // =============================
    // NavMesh安全呼び出し
    // =============================
    void SafeSetDestination(Vector3 dest)
    {
#if UNITY_2022_1_OR_NEWER
        if (!agent.isOnNavMesh) return;
#endif
        agent.SetDestination(dest);
    }

    // =============================
    // VFX
    // =============================
    void PlaySpawnVFX()
    {
        if (!spawnVFX) return;
        spawnVFX.gameObject.SetActive(true);
        spawnVFX.Reinit();
        spawnVFX.Play();
        StartCoroutine(DisableAfter(spawnVFX.gameObject, 2f));
    }

    void PlayDeathVFX()
    {
        if (!deathVFX) return;
        deathVFX.gameObject.SetActive(true);
        deathVFX.Reinit();
        deathVFX.Play();
        StartCoroutine(DisableAfter(deathVFX.gameObject, 2f));
    }

    IEnumerator DisableAfter(GameObject go, float sec)
    {
        yield return new WaitForSeconds(sec);
        go.SetActive(false);
    }
}
