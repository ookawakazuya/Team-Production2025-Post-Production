using UnityEngine;
using UnityEngine.InputSystem; // 新Input Systemを使うために必要

/// <summary>
/// 新Input System対応のプレイヤーコントローラー。
/// Rigidbodyを使って移動し、Tキーで(100,100,100)にテレポートする。
/// Enemyの動作検証用にシンプル構成。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class PlayerController_NewInput : MonoBehaviour
{
    [Header("移動設定")]
    [Tooltip("プレイヤーの移動速度")]
    public float moveSpeed = 5f;

    // 入力情報を保持するベクトル
    private Vector2 moveInput;

    // Rigidbody参照
    private Rigidbody rb;

    /// <summary>
    /// 初期化処理。Rigidbodyを取得。
    /// </summary>
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true; // 転倒防止
    }

    /// <summary>
    /// 入力検出処理。
    /// 新Input Systemを使用して移動入力とテレポートを検知。
    /// </summary>
    void Update()
    {
        // WASDまたは矢印キーから移動入力を取得
        moveInput = Vector2.zero;
        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                moveInput.y += 1;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                moveInput.y -= 1;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                moveInput.x -= 1;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                moveInput.x += 1;

            // Tキーが押されたらテレポート
            if (Keyboard.current.tKey.wasPressedThisFrame)
            {
                TeleportToTarget();
            }
        }
    }

    /// <summary>
    /// Rigidbodyを使った移動処理。
    /// </summary>
    void FixedUpdate()
    {
        MovePlayer();
    }

    /// <summary>
    /// 移動処理：WASD入力に応じて物理的に移動。
    /// </summary>
    private void MovePlayer()
    {
        Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;

        if (moveDir.sqrMagnitude > 0.01f)
        {
            // 移動方向を向く
            Quaternion targetRot = Quaternion.LookRotation(moveDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.fixedDeltaTime * 10f);

            // Rigidbodyを使って前進
            rb.MovePosition(transform.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
    }

    /// <summary>
    /// Tキーで(100,100,100)にテレポートする処理。
    /// </summary>
    private void TeleportToTarget()
    {
        transform.position = new Vector3(0f, 1.0f, -70.0f);
        rb.linearVelocity = Vector3.zero; // 慣性リセット
    }
}
