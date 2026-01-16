using UnityEngine;

/// <summary>
/// プレイヤーの移動、重力、フックなどの移動を管理するクラス
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VRMovementHandler : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [SerializeField] private CharacterController characterController;

    [Header("移動パラメータ")]
    [SerializeField] private float acceleration = 20f;         // 引き寄せ時の加速度
    [SerializeField] private float maxMoveSpeed = 30f;         // 最大移動速度
    [SerializeField] private float stopDistance = 1f;          // フック地点への到達判定距離

    [Header("重力 / 落下設定")]
    [SerializeField] private float gravity = -9.81f;           // 重力値
    [SerializeField] private float maxFallSpeed = -50f;        // 最大落下速度
    [SerializeField] private float minLandingSpeed = -1.0f;    // 着地音を鳴らす閾値

    [Header("張り付き(Cling)設定")]
    [SerializeField] private float clingDuration = 5f;         // 張り付き可能時間
    [SerializeField] private string wallTag = "Wall";          // 張り付き対象のタグ

    // 内部状態変数
    private float currentSpeed = 0f;
    private float fallSpeed = 0f;
    private float clingTimer = 0f;
    private bool wasGrounded = true;

    // プロパティ: 外部から状態を読み取れるようにする
    public bool IsRetracting { get; private set; }
    public bool IsClinging { get; private set; }
    public Vector3 GrapplePoint { get; private set; }

    private void Awake()
    {
        // CharacterControllerが未設定なら自動取得
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
    }

    /// <summary>
    /// 外部（VRControllerなど）から移動状態を更新するために毎フレーム呼び出す
    /// </summary>
    public void Tick(bool isHookActive)
    {
        if (IsClinging)
        {
            UpdateClingState();
        }
        else if (IsRetracting)
        {
            UpdateRetractMovement();
        }
        else
        {
            ApplyGravity();
        }
    }

    #region 公開メソッド (外部から命令を送る)

    /// <summary>
    /// 巻き取り（引き寄せ）移動を開始する
    /// </summary>
    public void StartRetracting(Vector3 targetPoint)
    {
        IsRetracting = true;
        IsClinging = false;
        GrapplePoint = targetPoint;
        currentSpeed = 0f;

        Debug.Log("[Movement] 巻き取り移動を開始しました。");
    }

    /// <summary>
    /// すべての特殊移動状態をリセットし、自由落下/接地状態に戻す
    /// </summary>
    public void ResetMovement()
    {
        IsRetracting = false;
        IsClinging = false;
        currentSpeed = 0f;
    }

    #endregion

    #region 内部移動ロジック

    /// <summary>
    /// フック地点へ向かって加速移動する
    /// </summary>
    private void UpdateRetractMovement()
    {
        Vector3 direction = GrapplePoint - transform.position;
        float distance = direction.magnitude;

        // 到達していない場合
        if (distance > stopDistance)
        {
            // 加速度計算
            currentSpeed += acceleration * Time.deltaTime;
            currentSpeed = Mathf.Min(currentSpeed, maxMoveSpeed);

            Vector3 moveStep = direction.normalized * currentSpeed * Time.deltaTime;
            characterController.Move(moveStep);
        }
        else
        {
            // 到達時の判定
            CheckForClingOpportunity();
        }
    }

    /// <summary>
    /// 到達地点が壁（Wallタグ）かどうかを確認し、張り付きを開始するか判断する
    /// </summary>
    private void CheckForClingOpportunity()
    {
        // 周囲のコライダをチェック
        Collider[] hitColliders = Physics.OverlapSphere(GrapplePoint, 0.2f);
        bool foundWall = false;

        foreach (var col in hitColliders)
        {
            if (col.CompareTag(wallTag))
            {
                foundWall = true;
                break;
            }
        }

        if (foundWall)
        {
            StartCling();
        }
        else
        {
            ResetMovement();
        }
    }

    /// <summary>
    /// 壁への張り付き状態を開始する
    /// </summary>
    private void StartCling()
    {
        IsRetracting = false;
        IsClinging = true;
        clingTimer = clingDuration;
        fallSpeed = 0f;

        // 張り付き開始SEなどの通知（必要に応じてイベントを発火）
        SoundManager.Instance.PlaySE("SE_Harituki");
        Debug.Log("[Movement] 壁に張り付きました。");
    }

    /// <summary>
    /// 張り付き中の時間経過を管理
    /// </summary>
    private void UpdateClingState()
    {
        clingTimer -= Time.deltaTime;
        if (clingTimer <= 0)
        {
            ResetMovement(); // 時間切れで落下
            Debug.Log("[Movement] 張り付き限界時間を超えたため落下します。");
        }
    }

    /// <summary>
    /// 通常の重力計算を適用する
    /// </summary>
    private void ApplyGravity()
    {
        bool isGrounded = characterController.isGrounded;

        if (isGrounded)
        {
            // 着地した瞬間の判定
            if (!wasGrounded && fallSpeed < minLandingSpeed)
            {
                SoundManager.Instance.PlaySE("SE_Player_01");
                fallSpeed = 0f;
            }
        }
        else
        {
            // 空中での加速
            fallSpeed += gravity * Time.deltaTime;
            fallSpeed = Mathf.Max(fallSpeed, maxFallSpeed);
            characterController.Move(new Vector3(0, fallSpeed, 0) * Time.deltaTime);
        }

        wasGrounded = isGrounded;
    }
    #endregion
}
