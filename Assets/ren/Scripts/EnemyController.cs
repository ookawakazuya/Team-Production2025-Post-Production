using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Return }

    [Header("基本設定")]
    [SerializeField] float moveSpeed = 3f;
    [SerializeField] float rotationSpeed = 8f;

    [Header("視認距離設定")]
    [SerializeField] float visibleDistance = 50f;  // 表示
    [SerializeField] float hideDistance = 51f;     // 非表示（わずかに広い）

    [Header("検知設定")]
    [SerializeField] float losePlayerDistance = 15f;

    [Header("床チェック")]
    [SerializeField] LayerMask floorLayer;
    [SerializeField] float groundCheckHeight = 0.1f;
    [SerializeField] float forwardGroundCheckDistance = 0.8f;

    [Header("戻り動作")]
    [SerializeField] float returnStopThreshold = 0.25f;

    [Header("追跡猶予")]
    [SerializeField] float chaseGraceTime = 0.6f;

    //================ HP設定 ====================
    [Header("HP 設定")]
    public int maxHP = 100;
    public int currentHP;

    [Header("当たり判定コライダー")]
    public Collider bodyCollider; // 全身
    public Collider headCollider; // 頭（2倍判定用）

    [Header("UI")]
    public Transform hpBarRoot;
    public Image hpFillImage;

    private float displayedHP = 1f; // HPバー補完用

    //============================================

    NavMeshAgent agent;
    Transform player;
    Vector3 startPosition;
    State currentState = State.Idle;
    float lastSeenPlayerTime = -999f;

    Renderer[] renderers;
    bool isEnabled = true;  // 外部ON/OFF用

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.speed = moveSpeed;

        renderers = GetComponentsInChildren<Renderer>();
        currentHP = maxHP;
    }

    void Start()
    {
        startPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (!isEnabled)
        {
            agent.isStopped = true;
            return;
        }

        // 状態処理
        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                break;
            case State.Chase:
                HandleChase();
                break;
            case State.Return:
                HandleReturn();
                break;
        }

        // 表示/非表示処理
        UpdateVisibilityByDistanceToPlayer();

        // HPバー回転＆更新
        UpdateHPBarRotation();
        UpdateHPFillSmooth();
    }

    //===============================
    // 追跡処理
    //===============================
    void HandleChase()
    {
        if (player == null)
        {
            TransitionToReturn();
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);

        FaceTarget(player.position);

        if (!IsForwardGroundSafe())
            agent.isStopped = true;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist > losePlayerDistance && Time.time - lastSeenPlayerTime > chaseGraceTime)
        {
            TransitionToReturn();
        }
    }

    //===============================
    // 戻り処理
    //===============================
    void HandleReturn()
    {
        agent.isStopped = false;
        agent.SetDestination(startPosition);

        FaceTarget(startPosition);

        if (!IsForwardGroundSafe())
            agent.isStopped = true;

        if (Vector3.Distance(transform.position, startPosition) <= returnStopThreshold)
        {
            TransitionToIdle();
        }
    }

    //===============================
    // Public API
    //===============================
    public void OnPlayerDetected(Transform playerTransform)
    {
        if (playerTransform == null) return;

        player = playerTransform;
        lastSeenPlayerTime = Time.time;

        TransitionToChase();
    }

    public void OnPlayerLost(Transform playerTransform)
    {
        if (playerTransform == null) return;

        lastSeenPlayerTime = Time.time;
    }

    // 敵の外部ON/OFF制御
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;

        agent.isStopped = !enabled;

        foreach (var r in renderers)
            r.enabled = enabled;

        if (hpBarRoot != null)
            hpBarRoot.gameObject.SetActive(enabled);
    }

    //===============================
    // ダメージ処理
    //===============================
    public void ApplyDamage(int damage)
    {
        currentHP -= damage;
        if (currentHP < 0) currentHP = 0;

        if (currentHP == 0)
            Die();
    }

    void Die()
    {
        Destroy(gameObject);
    }

    //===============================
    // 弾の当たり判定
    //===============================
    //void OnTriggerEnter(Collider other)
    //{
    //    if (!other.CompareTag("Bullet")) return;

    //    Bullet bullet = other.GetComponent<Bullet>();
    //    if (bullet == null) return;

    //    int damage = bullet.damage;

    //    // ■ 正確な頭判定
    //    if (other == headCollider)
    //    {
    //        damage *= 2;
    //    }

    //    ApplyDamage(damage);
    //}

    //===============================
    // 状態遷移
    //===============================
    void TransitionToIdle()
    {
        currentState = State.Idle;
        agent.ResetPath();
        agent.isStopped = true;
    }

    void TransitionToChase()
    {
        currentState = State.Chase;
        agent.isStopped = false;
        if (player != null) agent.SetDestination(player.position);
    }

    void TransitionToReturn()
    {
        currentState = State.Return;
        agent.isStopped = false;
        agent.SetDestination(startPosition);
    }

    //===============================
    // 補助
    //===============================
    bool IsForwardGroundSafe()
    {
        Vector3 forward = agent.desiredVelocity.sqrMagnitude > 0.01f ?
                          agent.desiredVelocity.normalized :
                          transform.forward;

        Vector3 origin = transform.position + Vector3.up * groundCheckHeight;
        Vector3 forwardPoint = origin + forward * forwardGroundCheckDistance;

        return Physics.Raycast(forwardPoint, Vector3.down, 2f, floorLayer);
    }

    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }

    //===============================
    // HPバー処理
    //===============================
    void UpdateHPBarRotation()
    {
        if (hpBarRoot == null || player == null) return;

        // 逆向き問題を完全に防ぐ LookRotation
        Vector3 dir = hpBarRoot.position - player.position;
        dir.y = 0;

        hpBarRoot.rotation = Quaternion.LookRotation(dir);
    }

    void UpdateHPFillSmooth()
    {
        if (hpFillImage == null) return;

        float target = (float)currentHP / maxHP;

        displayedHP = Mathf.Lerp(displayedHP, target, Time.deltaTime * 10f);

        hpFillImage.fillAmount = displayedHP;
    }

    //===============================
    // 表示 / 非表示
    //===============================
    void UpdateVisibilityByDistanceToPlayer()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool shouldShow = (dist <= visibleDistance);
        bool shouldHide = (dist >= hideDistance);

        if (shouldHide)
        {
            foreach (var r in renderers) r.enabled = false;
            if (hpBarRoot != null) hpBarRoot.gameObject.SetActive(false);
            agent.isStopped = true;  // ← NavMesh負荷軽減用
        }
        else if (shouldShow)
        {
            foreach (var r in renderers) r.enabled = true;
            if (hpBarRoot != null) hpBarRoot.gameObject.SetActive(true);
            agent.isStopped = false;
        }
    }
}
