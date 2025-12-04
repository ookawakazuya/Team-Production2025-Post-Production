using UnityEngine;
using UnityEngine.XR;

public class TriggerHaptics : MonoBehaviour
{
    [Range(0f, 1f)] public float amplitude = 0.7f;  // 振動の強さ
    [Range(0f, 1f)] public float duration = 0.2f;   // 振動の長さ

    void Update()
    {
        // 左右のコントローラーを取得
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

        // トリガー（IndexTrigger）の値を取得
        if (leftHand.TryGetFeatureValue(CommonUsages.trigger, out float leftTriggerValue) && leftTriggerValue > 0.8f)
        {
            StartCoroutine(VibrateController(leftHand));
        }

        if (rightHand.TryGetFeatureValue(CommonUsages.trigger, out float rightTriggerValue) && rightTriggerValue > 0.8f)
        {
            StartCoroutine(VibrateController(rightHand));
        }
    }

    private System.Collections.IEnumerator VibrateController(InputDevice device)
    {
        if (device.isValid)
        {
            if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
            {
                device.SendHapticImpulse(0, amplitude, duration);
            }
        }

        yield return new WaitForSeconds(duration);

        if (device.isValid)
        {
            device.StopHaptics();
        }
    }
}
