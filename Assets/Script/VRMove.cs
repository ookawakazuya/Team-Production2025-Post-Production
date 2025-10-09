using UnityEngine;
using UnityEngine.InputSystem;

public class VRMove : MonoBehaviour
{
    [Header("プレイヤーの設定")]
    [SerializeField] CharacterController controller;
    [SerializeField] float playerMoveSpeed = 5f;
    [SerializeField] Transform headTransform;


    [SerializeField] VRController vrController;
    // 新しい Input Action Asset
    private VRHookActions vrActions;

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
        if (vrController != null && vrController.IsWireMoving())
            return;
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector2 leftStickInput = vrActions.VR.Move.ReadValue<Vector2>();

        if (leftStickInput != Vector2.zero)
        {
            Debug.Log("移動中");
            Vector3 forward = headTransform.forward;
            Vector3 right = headTransform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * leftStickInput.y + right * leftStickInput.x).normalized;
            controller.Move(moveDirection * playerMoveSpeed * Time.deltaTime);
        }
    }
}