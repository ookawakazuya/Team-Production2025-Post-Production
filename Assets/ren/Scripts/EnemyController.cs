using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;

public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Return }

    [Header("基本設定")]
    public float maxHP = 100f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 8f;
    public float chaseDistance = 10f;
    public float returnDistance = 15f;

    [Header("視認距離スポーン管理")]
    public float spawnDistance = 15f;
    public float hideDistance = 16f;

    [Header("VFX")]
    public VisualEffect spawnVFX;
    public VisualEffect deathVFX;

    Transform player;
    NavMeshAgent agent;

    // HP管理
    float currentHP;
    float displayedHP = 1f;

    // 敵の初期位置
    Vector3 startPosition;
    Quaternion startRotation;

    // 状態
    State currentState = State.Idle;

    // 非表示管理
    bool isVisible = false;

    // 死亡状態
    public bool IsDead { get; private set; } = false;

    // コライダー（★ public に変更）
    public Collider bodyCollider;
    public Collider headCollider;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        bodyCollider = GetComponent<Collider>();
        headCollider = transform.Find("HeadCollider")?.GetComponent<Collider>();

        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    void Start()
    {
        currentHP = maxHP;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        GameManager.Instance?.RegisterEnemy(this);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (!IsDead)
        {
            if (!isVisible && dist < spawnDistance)
                ShowEnemy();

            if (isVisible && dist > hideDistance)
                HideEnemy();
        }

        if (!isVisible) return;
        if (IsDead) return;

        switch (currentState)
        {
            case State.Idle:
                if (dist <= chaseDistance)
                    currentState = State.Chase;
                break;

            case State.Chase:
                agent.isStopped = false;
                agent.SetDestination(player.position);

                if (dist > returnDistance)
                    currentState = State.Return;
                break;

            case State.Return:
                agent.isStopped = false;
                agent.SetDestination(startPosition);

                if (Vector3.Distance(transform.position, startPosition) < 1f)
                {
                    currentState = State.Idle;
                    agent.isStopped = true;
                }
                break;
        }
    }

    // =========================================================
    // ■ HP処理
    // =========================================================
    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHP -= damage;
        displayedHP = currentHP / maxHP;

        if (currentHP <= 0f)
            Die();
    }

    // ★ Bullet.cs 用の互換関数
    public void ApplyDamage(float damage)
    {
        TakeDamage(damage);
    }

    // =========================================================
    // ■ 死亡処理
    // =========================================================
    void Die()
    {
        IsDead = true;
        isVisible = false;

        agent.isStopped = true;
        agent.enabled = false;

        bodyCollider.enabled = false;
        if (headCollider != null) headCollider.enabled = false;

        if (deathVFX) Instantiate(deathVFX, transform.position, Quaternion.identity);

        Shotgun shotgun = FindFirstObjectByType<Shotgun>(); //ここにショットガンの玉追加書いたよ！！！！！！！！
        if (shotgun != null)
            shotgun.plusAmmo();

        gameObject.SetActive(false);
    }

    // =========================================================
    // ■ プレイヤー距離で出現
    // =========================================================
    void ShowEnemy()
    {
        isVisible = true;
        gameObject.SetActive(true);

        agent.enabled = true;
        agent.isStopped = false;

        if (spawnVFX)
            Instantiate(spawnVFX, transform.position, Quaternion.identity);
    }

    // =========================================================
    // ■ プレイヤー距離で非表示
    // =========================================================
    void HideEnemy()
    {
        isVisible = false;

        agent.isStopped = true;
        agent.enabled = false;

        if (deathVFX)
            Instantiate(deathVFX, transform.position, Quaternion.identity);

        gameObject.SetActive(false);
    }

    // =========================================================
    // ■ GameManager から呼ばれる「完全復活」
    // =========================================================
    public void Respawn()
    {
        IsDead = false;
        isVisible = false;

        currentHP = maxHP;
        displayedHP = 1f;

        transform.position = startPosition;
        transform.rotation = startRotation;

        agent.enabled = true;
        agent.Warp(startPosition);
        agent.isStopped = true;
        agent.ResetPath();

        bodyCollider.enabled = true;
        if (headCollider != null) headCollider.enabled = true;

        currentState = State.Idle;

        gameObject.SetActive(false);
    }
}
