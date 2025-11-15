// EnemyController.cs
using UnityEngine;
using UnityEngine.AI;
using System.Collections;

/// <summary>
/// EnemyController (NavMesh 版)
/// - ステート: Idle / Chase / Return
/// - DetectionArea からの通知で Chase に入る
/// - プレイヤーを見失ったら Return（初期位置へ復帰）
/// - NavMeshAgent による経路探索・障害物回避を利用
/// - 床の端で落ちないように raycast による床チェックを行い、危険なら停止する
/// - GameManager によるアクティブ/非アクティブ管理を受ける (SetEnabled)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Return }

    [Header("基本設定")]
    [Tooltip("移動速度（NavMeshAgent の speed とも連動）")]
    [SerializeField] float moveSpeed = 3f;
    [Tooltip("回転の速さ（プレイヤー方向へ向くための補間）")]
    [SerializeField] float rotationSpeed = 8f;

    [Header("検知設定")]
    [Tooltip("DetectionArea に頼るが、追跡中にプレイヤーが一定距離離れたら追跡解除")]
    [SerializeField] float losePlayerDistance = 15f;

    [Header("床チェック")]
    [Tooltip("床判定に使うレイヤーマスク（Floor レイヤー等を割り当ててください）")]
    [SerializeField] LayerMask floorLayer;
    [Tooltip("床判定用 Ray のオフセット高さ")]
    [SerializeField] float groundCheckHeight = 0.1f;
    [Tooltip("前方に向かって床があるかチェックする距離（NavMesh の次の角に相当する位置ではなく安全マージン）")]
    [SerializeField] float forwardGroundCheckDistance = 0.8f;

    [Header("可視/有効設定")]
    [Tooltip("GameManager の基準距離よりさらに細かく見た目を制御したい場合に利用")]
    [SerializeField] float visibleDistance = 20f;

    [Header("戻り動作")]
    [Tooltip("開始地点にほぼ戻ったとみなす距離")]
    [SerializeField] float returnStopThreshold = 0.25f;

    // 内部参照
    NavMeshAgent agent;
    Transform player;
    Vector3 startPosition;
    State currentState = State.Idle;
    bool isEnabled = true; // GameManager による有効/無効フラグ

    // 内部タイマー：プレイヤーを見失ってからすぐ追跡をやめるのではなく、少しの猶予を設けたい場合
    [Header("追跡猶予")]
    [Tooltip("プレイヤーを検知してから追跡を続ける猶予時間（見失ってもこの時間は追跡を維持）")]
    [SerializeField] float chaseGraceTime = 0.6f;
    float lastSeenPlayerTime = -999f;

    Renderer[] renderers;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false; // 自分で向きを補間する
        agent.speed = moveSpeed;

        renderers = GetComponentsInChildren<Renderer>();
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
            // 非アクティブ状態なら Agent を停止してレンダリングも OFF にしておく
            if (!agent.isStopped) agent.isStopped = true;
            return;
        }

        // 状態ごとの処理
        switch (currentState)
        {
            case State.Idle:
                // 何もしない（NavAgent 停止）
                if (!agent.isStopped) agent.isStopped = true;
                break;

            case State.Chase:
                if (player == null)
                {
                    TransitionToReturn();
                    break;
                }

                // 追跡先設定
                agent.isStopped = false;
                agent.SetDestination(player.position);

                // 向きをプレイヤー方向に向ける
                FaceTarget(player.position);

                // 床チェック：進行方向に床がないなら止める
                if (!IsForwardGroundSafe())
                {
                    agent.isStopped = true;
                }
                else
                {
                    agent.isStopped = false;
                }

                // プレイヤー見失い判定（距離ベース）
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer > losePlayerDistance && Time.time - lastSeenPlayerTime > chaseGraceTime)
                {
                    TransitionToReturn();
                }
                break;

            case State.Return:
                // 帰還先を設定
                agent.isStopped = false;
                agent.SetDestination(startPosition);

                // 向きを帰還方向に向ける
                FaceTarget(startPosition);

                // 床チェック：進行方向に床がないなら止める
                if (!IsForwardGroundSafe())
                {
                    agent.isStopped = true;
                }
                else
                {
                    agent.isStopped = false;
                }

                // 十分近ければ Idle に戻す
                if (Vector3.Distance(transform.position, startPosition) <= returnStopThreshold)
                {
                    TransitionToIdle();
                }
                break;
        }

        // レンダリングの ON/OFF（見た目の最適化） - GameManager と併用可
        UpdateVisibilityByDistanceToPlayer();
    }

    #region Public API (DetectionArea / GameManager 用)
    /// <summary>
    /// DetectionArea からプレイヤー検知通知。Transform が null なら無視。
    /// </summary>
    public void OnPlayerDetected(Transform playerTransform)
    {
        if (playerTransform == null) return;
        player = playerTransform;
        lastSeenPlayerTime = Time.time;
        TransitionToChase();
    }

    /// <summary>
    /// DetectionArea からプレイヤー離脱通知
    /// （直接 Return に遷移せず, graceTime を使って少し猶予を与える）
    /// </summary>
    public void OnPlayerLost(Transform playerTransform)
    {
        if (playerTransform == null) return;
        lastSeenPlayerTime = Time.time;
        // すぐに Return しない。Update 内で距離と経過時間を見て決定する。
    }

    /// <summary>
    /// GameManager から呼ばれる有効/無効設定。
    /// 無効時は NavAgent を停止し、レンダラーも消す。
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        isEnabled = enabled;

        // NavAgent 停止/再開の制御
        if (!isEnabled)
        {
            agent.isStopped = true;
        }
        else
        {
            // 有効化時は state に応じて agent の挙動を許可
            if (currentState == State.Idle) agent.isStopped = true;
            else agent.isStopped = false;
        }

        // レンダラ ON/OFF（有効化に合わせる）
        foreach (var r in renderers)
            r.enabled = isEnabled;
    }
    #endregion

    #region State Transitions
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
        // 直ちに目的地更新
        if (player != null) agent.SetDestination(player.position);
    }

    void TransitionToReturn()
    {
        currentState = State.Return;
        agent.isStopped = false;
        agent.SetDestination(startPosition);
    }
    #endregion

    #region Utilities
    /// <summary>
    /// 前方方向（Agent の velocity / 進行方向）に床があるかチェックする。
    /// NavMesh を使っている場合でも、平台の端で落ちないよう物理レイヤーでチェックする。
    /// </summary>
    bool IsForwardGroundSafe()
    {
        // 進行方向を推定する（目的地方向または forward）
        Vector3 forwardDir;
        if (agent.hasPath && agent.desiredVelocity.sqrMagnitude > 0.01f)
            forwardDir = agent.desiredVelocity.normalized;
        else
            forwardDir = transform.forward;

        // 始点は少し上に上げてから前方にオフセット
        Vector3 origin = transform.position + Vector3.up * groundCheckHeight;
        Vector3 forwardPoint = origin + forwardDir * forwardGroundCheckDistance;

        // 下に向けてレイを飛ばす
        if (Physics.Raycast(forwardPoint, Vector3.down, out RaycastHit hit, 2f, floorLayer))
        {
            // 床が見つかれば安全
            return true;
        }
        // 床が無ければ危険（端）
        return false;
    }

    /// <summary>
    /// 自分の向きを targetPos に対して Slerp / Smooth に回す
    /// </summary>
    void FaceTarget(Vector3 targetPos)
    {
        Vector3 dir = targetPos - transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;
        Quaternion targetRot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }

    /// <summary>
    /// レンダリングの ON/OFF をプレイヤー距離で簡易制御（GameManager と協調可）
    /// </summary>
    void UpdateVisibilityByDistanceToPlayer()
    {
        if (player == null) return;
        float dist = Vector3.Distance(transform.position, player.position);
        bool shouldShow = dist <= visibleDistance && isEnabled;
        foreach (var r in renderers)
            r.enabled = shouldShow;
    }
    #endregion

    #region Debug / Gizmos
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visibleDistance);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, losePlayerDistance);
    }
    #endregion
}
