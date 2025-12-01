using UnityEngine;
using UnityEngine.XR;

public class HapticController : MonoBehaviour
{
    InputDevice leftHand;
    InputDevice rightHand;

    void Start()
    {
        InitDevices();
    }

    void InitDevices()
    {
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode (XRNode.RightHand);
    }

    /// <summary>
    /// 振動させるデバイスの登録
    /// </summary>
    /// <param name="device"></param>
    /// <param name="node"></param>
    void TryInitDevice(ref InputDevice device,XRNode node)
    {
        if (!device.isValid)
        {
            device = InputDevices.GetDeviceAtXRNode(node);
        }
    }

    void SendHaptic(InputDevice device,float amplitude,float duration) 
    {
        if(!device.isValid) return;

        HapticCapabilities capabilities;
        if(device.TryGetHapticCapabilities(out capabilities)&capabilities.supportsImpulse) 
        {
            device.SendHapticImpulse(0, amplitude, duration);
        }
    }

    public void VibrateLeft(float amplitude, float duration)
    {
        TryInitDevice(ref leftHand, XRNode.LeftHand);
        SendHaptic(leftHand, amplitude, duration);
    }

    public void VibrateRight(float amplitude, float duration)
    {
        TryInitDevice(ref rightHand, XRNode.RightHand);
        SendHaptic(rightHand, amplitude, duration);
    }


    // 壁に当たった時（弱）
    /// <summary>
    /// 
    /// </summary>
    /// <param name="isRightHand">どちらのコントローラーなのか</param>
    /// <param name="= amplitude">振動の強さ(0.0f～1.0f)</param>
    /// <param name="duration">振動時間(秒)</param>
    public void VibrateWallHit(bool isRightHand)
    {
        float amplitude = 0.2f;
        float duration = 0.05f;

        if (isRightHand)
            VibrateRight(amplitude, duration);
        else
            VibrateLeft(amplitude, duration);
    }

    // 壁に到達した時（強）
    public void VibrateArrivedWall(bool isRightHand)
    {
        float amplitude = 1.0f;
        float duration = 0.15f;

        if (isRightHand)
            VibrateRight(amplitude, duration);
        else
            VibrateLeft(amplitude, duration);
    }
}
