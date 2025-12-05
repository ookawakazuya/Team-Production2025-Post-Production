using UnityEngine;
using UnityEngine.XR;

public class HapticController : MonoBehaviour
{
    InputDevice leftHand;
    InputDevice rightHand;

    float retractCooldown = 0f;

    void Start()
    {
        InitDevices();
    }


    public void Update()
    {
        if (retractCooldown > 0f)
            retractCooldown -= Time.deltaTime;
    }

    void InitDevices()
    {
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode (XRNode.RightHand);
    }

    /// <summary>
    /// U“®‚³‚¹‚éƒfƒoƒCƒX‚Ì“o˜^
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


    // •Ç‚É“–‚½‚Á‚½iãj
    /// <summary>
    /// 
    /// </summary>
    /// <param name="isRightHand">‚Ç‚¿‚ç‚ÌƒRƒ“ƒgƒ[ƒ‰[‚È‚Ì‚©</param>
    /// <param name="= amplitude">U“®‚Ì‹­‚³(0.0f`1.0f)</param>
    /// <param name="duration">U“®ŠÔ(•b)</param>
    public void VibrateWallHit(bool isRightHand)
    {
        float amplitude = 0.2f;
        float duration = 0.05f;

        if (isRightHand)
            VibrateRight(amplitude, duration);
        else
            VibrateLeft(amplitude, duration);
    }

    // •Ç‚É“’B‚µ‚½i‹­j
    public void VibrateArrivedWall(bool isRightHand)
    {
        float amplitude = 1.0f;
        float duration = 0.15f;

        if (isRightHand)
            VibrateRight(amplitude, duration);
        else
            VibrateLeft(amplitude, duration);
    }

    /// <summary>
    /// ƒƒCƒ„[ˆÚ“®’†‚ÌU“®
    /// </summary>
    /// <param name="isRightHand"></param>
    public void VibrateRetracting(bool isRightHand)
    {
        if (retractCooldown > 0f) return;

        float amplitude = 0.1f;
        float duration = 0.02f;

        if (isRightHand)
            VibrateRight(amplitude, duration);
        else
            VibrateLeft(amplitude, duration);

        retractCooldown = 0.05f;
    }

    /// <summary>
    /// e‚Ì”½“®
    /// </summary>
    /// <param name="isLeftHand"></param>
    ///[SerializeField] HapticController haptic;
    ///[SerializeField] bool isLeftHand = true;   //¶‰E‚Ì”»’f
    ///if (haptic != null)
    ///haptic.VibrateFiring(isLeftHand);
    public void VibrateFiring(bool isLeftHand)
    {
        float amplitude = 1.0f;
        float duration = 0.15f;

        if (isLeftHand)
            VibrateLeft(amplitude, duration);
        else
            VibrateRight(amplitude, duration);
    }

    /// <summary>
    /// —]‰C
    /// </summary>
    /// <param name="isLeftHand"></param>
    ///if (haptic != null)
    ///haptic.VibrateLingeringSound(isLeftHand);
    public void VibrateLingeringSound(bool isLeftHand)
    {
        float amplitude = 0.3f;
        float duration = 4.0f;

        if (isLeftHand)
            VibrateLeft(amplitude, duration);
        else
            VibrateRight(amplitude, duration);
    }

    /// <summary>
    /// —]‰C
    /// </summary>
    /// <param name="isLeftHand"></param>
    ///if (haptic != null)
    ///haptic.VibrateReload(isLeftHand);
    public void VibrateReload(bool isLeftHand)
    {
        float amplitude = 0.1f;
        float duration = 0.5f;

        if (isLeftHand)
            VibrateLeft(amplitude, duration);
        else
            VibrateRight(amplitude, duration);
    }
}
