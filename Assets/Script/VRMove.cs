using UnityEngine;
using UnityEngine.InputSystem;

public class VRMove : MonoBehaviour
{
    [Header("プレイヤーの設定")]
    [SerializeField] CharacterController controller;
    [SerializeField] float playerMoveSpeed = 5f;
    [SerializeField] Transform headTransform;

    [Header("カメラ設定")]
    [SerializeField] Camera mainCamera; // VRカメラ
    [SerializeField] float cameraDistance = 3f;
    [SerializeField] float cameraHeight = 1.5f;

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
        HandleMovement();
        HandleCamera();
    }

    private void HandleMovement()
    {
        Vector2 leftStickInput = vrActions.VR.Move.ReadValue<Vector2>();

        if (leftStickInput != Vector2.zero)
        {
            Debug.Log("移動中");
            Vector3 forward = mainCamera.transform.forward;
            Vector3 right = mainCamera.transform.right;
            forward.y = 0;
            right.y = 0;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = (forward * leftStickInput.y + right * leftStickInput.x).normalized;
            controller.Move(moveDirection * playerMoveSpeed * Time.deltaTime);
        }
    }

    private void HandleCamera()
    {
        Vector3 targetPosition = transform.position - mainCamera.transform.forward * cameraDistance;
        targetPosition.y += cameraHeight;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, Time.deltaTime * 5f);
    }
}