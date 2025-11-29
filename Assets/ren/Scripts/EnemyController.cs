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

    [Header("検知距離")]
    [Tooltip("プレイヤーがこの距離以内で追跡開始")]
    [SerializeField] float chaseDistance = 15f;
    [Tooltip("プレイヤーがこの距離以上で追跡解除")]
    [SerializeField] float returnDistance = 16f;

    [Header("戻り動作")]
    [SerializeField] float returnStopThreshold = 0.25f;

    [Header("HP設定")]
    public int maxHP = 100;
    public int currentHP;

    [Header("当たり判定コライダー")]
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("UI")]
    public Transform hpBarRoot;
    public Image hpFillImage;
    private float displayedHP = 1f;

    NavMeshAgent agent;
    Transform player;
    Vector3 startPosition;
    State currentState = State.Idle;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.speed = moveSpeed;

        currentHP = maxHP;
    }

    void Start()
    {
        startPosition = transform.position;
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void OnEnable()
    {
        PlayerDeath.OnPlayerDied += OnPlayerDied;
    }

    void OnDisable()
    {
        PlayerDeath.OnPlayerDied -= OnPlayerDied;
    }

    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                if (dist <= chaseDistance)
                    TransitionToChase();
                break;

            case State.Chase:
                if (dist >= returnDistance)
                    TransitionToReturn();
                else
                    HandleChase();
                break;

            case State.Return:
                if (dist <= chaseDistance)
                    TransitionToChase();
                else
                    HandleReturn();
                break;
        }

        UpdateHPBarRotation();
        UpdateHPFillSmooth();
    }

    //========================
    // 追跡処理
    //========================
    void HandleChase()
    {
        agent.isStopped = false;
        agent.SetDestination(player.position);
        FaceTarget(player.position);
    }

    //========================
    // 元の位置へ戻る処理
    //========================
    void HandleReturn()
    {
        agent.isStopped = false;
        agent.SetDestination(startPosition);
        FaceTarget(startPosition);

        if (Vector3.Distance(transform.position, startPosition) <= returnStopThreshold)
            TransitionToIdle();
    }

    //========================
    // ダメージ処理
    //========================
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

    //========================
    // プレイヤー死亡通知
    //========================
    void OnPlayerDied()
    {
        TransitionToReturn();
    }

    //========================
    // 状態遷移
    //========================
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
        if (player != null)
            agent.SetDestination(player.position);
    }

    void TransitionToReturn()
    {
        currentState = State.Return;
        agent.isStopped = false;
        agent.SetDestination(startPosition);
    }

    //========================
    // 補助処理
    //========================
    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }

    void UpdateHPBarRotation()
    {
        if (hpBarRoot == null || player == null) return;

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
}
