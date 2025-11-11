using UnityEngine;

/// <summary>
/// プレイヤーを検知し、追跡・離脱・初期位置への復帰を行う敵キャラクター制御クラス。
/// Floor端で止まるように制御しつつ、元の位置に戻れるように調整。
/// プレイヤーが一定範囲内に入ると追跡を開始。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class EnemyController : MonoBehaviour
{
    [Header("追跡設定")]
    public float moveSpeed = 3f;
    public float groundCheckDistance = 1.5f;

    [Header("範囲検知設定")]
    public float detectionRange = 10f;

    [Header("表示設定")]
    [Tooltip("この距離以内なら敵を表示")]
    public float visibleDistance = 20f;

    [Header("デバッグ")]
    public bool playerInRange = false;

    private Transform player;
    private Rigidbody rb;
    private Vector3 startPosition;
    private bool isChasing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPosition = transform.position;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) player = playerObj.transform;
        else Debug.LogWarning("Playerタグがついたオブジェクトが見つかりません。");
    }

    void Update()
    {
        if (player == null) return;

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // 追跡開始判定
        if (distanceToPlayer <= detectionRange)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                isChasing = true;
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                isChasing = false;
            }
        }

        // 床チェック
        if (!IsGrounded())
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (isChasing) ChasePlayer();
        else ReturnToStart();
    }

    private void ChasePlayer()
    {
        Vector3 dir = (player.position - transform.position).normalized;
        dir.y = 0;

        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
        }

        // 追跡時は床端で止まる
        if (CanMoveForward(dir))
            rb.MovePosition(transform.position + dir * moveSpeed * Time.deltaTime);
    }

    private void ReturnToStart()
    {
        Vector3 dir = (startPosition - transform.position);
        dir.y = 0;

        if (dir.magnitude > 0.1f)
        {
            dir.Normalize();
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);

            // 帰還時は少し床がなくても戻れる
            rb.MovePosition(transform.position + dir * moveSpeed * Time.deltaTime);
        }
    }

    /// <summary>
    /// プレイヤーや移動方向の床があるか確認（追跡用）
    /// Floorの端で止まる判定
    /// </summary>
    private bool CanMoveForward(Vector3 dir)
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        Vector3 forwardPoint = origin + dir.normalized * 0.5f; // 前方少し先
        if (Physics.Raycast(forwardPoint, Vector3.down, out RaycastHit hit, groundCheckDistance + 0.1f))
        {
            return hit.collider.CompareTag("Floor");
        }
        return false;
    }

    private bool IsGrounded()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, groundCheckDistance))
            return hit.collider.CompareTag("Floor");
        return false;
    }

    // 子オブジェクトのTriggerイベントは残しても良い
    public void OnChildTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) isChasing = true;
    }

    public void OnChildTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isChasing = false;
    }
}
