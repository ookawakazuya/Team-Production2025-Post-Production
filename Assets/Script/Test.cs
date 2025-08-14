using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    void Update()
    {
        if (Gamepad.current != null)
        {
            // L2�i���g���K�[�j��R2�i�E�g���K�[�j�̉������݁i0.0�`1.0�j
            float l2 = Gamepad.current.leftTrigger.ReadValue();
            float r2 = Gamepad.current.rightTrigger.ReadValue();

            // L1�i���o���p�[�j��R1�i�E�o���p�[�j�̉�����ԁitrue/false�j
            bool l1Pressed = Gamepad.current.leftShoulder.isPressed;
            bool r1Pressed = Gamepad.current.rightShoulder.isPressed;

            Debug.Log($"L1: {l1Pressed}  L2: {l2:F2}  |  R1: {r1Pressed}  R2: {r2:F2}");
        }
        else
        {
            Debug.Log("PS5�R���g���[���[���ڑ�����Ă��܂���");
        }
    }
}
