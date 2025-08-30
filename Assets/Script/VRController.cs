using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Readers;
using System.Collections;

public class VRController : MonoBehaviour
{
    [Header("XR Controllers")]
    [SerializeField] GameObject rightController;
    [SerializeField] GameObject leftController;
    InputDevice leftHand;
    InputDevice rightHand;

    [Header("フック関連")]
    [SerializeField] Camera mainCamera;
    //[SerializeField] LineRenderer lineRenderer;
    [SerializeField] float maxWireLength = 15f;

    bool isGrappling = false;
    Vector3 grapplePoiint;

    private void Start()
    {
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }
    private void Update()
    {
        //if (rightController == null) return;
        //if (leftController == null) return;

        //if (rightController.activateAction.action.WasPressedThisFrame())
        //{
        //    ShootHook();
        //}
        //if (rightController.selectAction.action.WasPressedThisFrame())
        //{
        //    StartRetract();
        //}


        if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerValue))
        {
            ShootHook();
            Debug.Log($"左トリガーの押し込み:{triggerValue}");
        }
        if (rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButon))
        {
            StartRetract();
            Debug.Log($"左グリップの押し込み:{gripButon}");
        }

    }

    void ShootHook()
    {
        Debug.Log("フック射出");
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        if(Physics.Raycast(ray,out RaycastHit hit, maxWireLength))
        {
            grapplePoiint = hit.point;
            isGrappling = true;
            //lineRenderer.enabled = true;
            //lineRenderer.SetPosition(0,mainCamera.transform.position);
            //lineRenderer.SetPosition(1, grapplePoiint);
        }
    }
    void StartRetract()
    {
        if (isGrappling)
        {
            Debug.Log("巻き取り開始");
            // 移動処理などをここに書く
        }
    }
}