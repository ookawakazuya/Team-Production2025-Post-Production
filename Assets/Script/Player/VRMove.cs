using UnityEngine;
using UnityEngine.InputSystem;

public class VRMove : MonoBehaviour
{
    [Header("プレイヤーの設定")]
    [SerializeField] CharacterController controller;
    [SerializeField] float playerMoveSpeed = 5f;
    [SerializeField] Transform headTransform;

    [Header("地面以外の移動補正")]
    [SerializeField] float airMoveSpeedRate = 0.2f;


    [SerializeField] VRController vrController;
    // 新しい Input Action Asset
    VRHookActions vrActions;

    void Awake()
    {
        vrActions = new VRHookActions();
        vrActions.VR.Enable();
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
        if (controller != null && (vrController.IsRetracting || vrController.IsClinging))
            return; // ワイヤー移動中・張り付き中は入力無効
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 leftStickInput = vrActions.VR.Move.ReadValue<Vector2>();

        if (leftStickInput == Vector2.zero)
        {
            return;
        }
        Debug.Log("移動中");

        if (vrController.IsRetracting)
            return;

        float speed = playerMoveSpeed;
        if (!controller.isGrounded)
        {
            speed *= airMoveSpeedRate;
        }

        Vector3 forward = headTransform.forward;
        Vector3 right = headTransform.right;
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = (forward * leftStickInput.y + right * leftStickInput.x).normalized;
        controller.Move(moveDirection * speed * Time.deltaTime);
    }
}