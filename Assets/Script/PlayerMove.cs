using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : MonoBehaviour
{
    [Header("プレイヤーの設定")]
    [SerializeField] CharacterController controller;
    [SerializeField] float playerMoveSpeed = 5f;
    [Header("カメラ設定")]
    [SerializeField] Camera mainCamera;
    [SerializeField] float cameraDistance = 3f;
    [SerializeField] float cameraHeight  = 1.5f;
    void Update()
    {
        if (Gamepad.current == null) return;

        // プレイヤーの移動
        HandleMovement();

        // カメラの追従
        HandleCamera();
    }

    private void HandleMovement()
    {
        // 左スティックの入力値を取得
        Vector2 leftStickInput = Gamepad.current.leftStick.ReadValue();

        if (leftStickInput != Vector2.zero)
        {
            // カメラの向きを考慮した移動方向を計算
            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            // スティックの入力に応じて移動方向を決定
            Vector3 moveDirection = (forward * leftStickInput.y + right * leftStickInput.x).normalized;

            // キャラクターコントローラーで移動を実行
            controller.Move(moveDirection * playerMoveSpeed * Time.deltaTime);
        }
    }

    private void HandleCamera()
    {
        // カメラの位置をプレイヤーの後ろに設定
        Vector3 targetPosition = transform.position - mainCamera.transform.forward * cameraDistance;
        targetPosition.y += cameraHeight;

        // カメラをスムーズに追従させる
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * 5f);
    }
}
