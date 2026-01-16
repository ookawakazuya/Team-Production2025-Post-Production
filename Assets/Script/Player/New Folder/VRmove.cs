using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// スティック入力によるプレイヤーの基本移動を制御するクラス。
/// フック移動中や壁への張り付き中は、このスクリプトによる移動を制限します。
/// </summary>
public class VRmove : MonoBehaviour
{
    [Header("コンポーネント参照")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform headTransform;
    // 分離後の移動管理クラスを参照
    [SerializeField] private VRMovementHandler movementHandler;

    [Header("移動パラメータ")]
    [SerializeField] private float playerMoveSpeed = 5f;
    [SerializeField] private float airMoveSpeedRate = 0.2f;

    // Input Action Asset
    private VRHookActions vrActions;

    void Awake()
    {
        vrActions = new VRHookActions();
        vrActions.VR.Enable();

        // 未設定の場合、自動で取得を試みる
        if (controller == null) controller = GetComponent<CharacterController>();
        if (movementHandler == null) movementHandler = GetComponent<VRMovementHandler>();
    }

    void OnDestroy()
    {
        if (vrActions != null)
        {
            vrActions.VR.Disable();
        }
    }

    void Update()
    {
        // --- 特殊移動中の入力制限 ---
        // VRMovementHandler側で「引き寄せ中」または「張り付き中」であれば、スティック移動を無効化する
        if (movementHandler != null)
        {
            if (movementHandler.IsRetracting || movementHandler.IsClinging)
            {
                return;
            }
        }

        HandleMovement();
    }

    /// <summary>
    /// スティック入力に基づいた移動処理
    /// </summary>
    private void HandleMovement()
    {
        // 左スティックの入力を取得
        Vector2 leftStickInput = vrActions.VR.Move.ReadValue<Vector2>();

        // 入力がない場合は処理しない
        if (leftStickInput == Vector2.zero) return;

        // 移動速度の決定（空中では制限をかける）
        float speed = playerMoveSpeed;
        if (!controller.isGrounded)
        {
            speed *= airMoveSpeedRate;
        }

        // 頭部（HMD）の向きを基準に移動方向を計算
        // Y成分を0にすることで、上下を向いていても水平に移動するようにする
        Vector3 forward = headTransform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = headTransform.right;
        right.y = 0;
        right.Normalize();

        // 入力ベクトルをワールド座標系の移動ベクトルに変換
        Vector3 moveDirection = (forward * leftStickInput.y + right * leftStickInput.x);

        // 斜め移動で速くならないよう正規化
        if (moveDirection.magnitude > 1f)
        {
            moveDirection.Normalize();
        }

        // キャラクターコントローラーを動かす
        controller.Move(moveDirection * speed * Time.deltaTime);
    }
}