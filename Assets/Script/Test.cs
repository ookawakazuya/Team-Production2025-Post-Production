using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;

public class Test : MonoBehaviour
{
    InputDevice leftHand;
    InputDevice rightHand;

    void Start()
    {
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode (XRNode.RightHand);
    }
    void Update()
    {
        if(leftHand.TryGetFeatureValue(CommonUsages.primaryButton,out bool xButton))
        {
            if (xButton) Debug.Log("Xボタンの入力");

        }
        if(leftHand.TryGetFeatureValue(CommonUsages.secondaryButton, out bool yButton))
        {
            if(yButton) Debug.Log("Yボタンの入力");
        }
        if(leftHand.TryGetFeatureValue(CommonUsages.triggerButton,out bool triggerValue))
        {
            Debug.Log($"左トリガーの押し込み:{ triggerValue}");
        }
        if(leftHand.TryGetFeatureValue(CommonUsages.gripButton,out bool gripButon))
        {
            Debug.Log($"左グリップの押し込み:{gripButon}");
        }

        if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 joystick))
        {
            Debug.Log($"スティックの入力: {joystick}");
        }
    }
}
