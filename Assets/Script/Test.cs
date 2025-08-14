using UnityEngine;
using UnityEngine.InputSystem;

public class Test : MonoBehaviour
{
    void Update()
    {
        if (Gamepad.current != null)
        {
            // L2（左トリガー）とR2（右トリガー）の押し込み（0.0～1.0）
            float l2 = Gamepad.current.leftTrigger.ReadValue();
            float r2 = Gamepad.current.rightTrigger.ReadValue();

            // L1（左バンパー）とR1（右バンパー）の押下状態（true/false）
            bool l1Pressed = Gamepad.current.leftShoulder.isPressed;
            bool r1Pressed = Gamepad.current.rightShoulder.isPressed;

            Debug.Log($"L1: {l1Pressed}  L2: {l2:F2}  |  R1: {r1Pressed}  R2: {r2:F2}");
        }
        else
        {
            Debug.Log("PS5コントローラーが接続されていません");
        }
    }
}
