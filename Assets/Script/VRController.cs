using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

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

    public VRHookActions HookMap;

     void Awake()
    {
        HookMap = new VRHookActions();

        HookMap.VR.HookShoot.started += ctx =>
        {
            isGrappling = true;
        };

        HookMap.VR.HookShoot.canceled +=ctx =>
        {
            isGrappling = false;
        };
        HookMap.VR.Retract.started += ctx =>
        {
            if (isGrappling)
            {
                Debug.Log("グリップ処理");
            }
        };
    }

    void OnEnable()
    {
        HookMap.Enable();
    }
    void OnDisable()
    {
        HookMap.Disable();
    }
    private void HookShoot_canceled(InputAction.CallbackContext obj)
    {
        throw new System.NotImplementedException();
    }

    private void Start()
    {
    }
    private void Update()
    {


        //if (rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerValue))
        //{
        //    ShootHook();
        //    Debug.Log($"左トリガーの押し込み:{triggerValue}");
        //}
        //if (rightHand.TryGetFeatureValue(CommonUsages.gripButton, out bool gripButon))
        //{
        //    StartRetract();
        //    Debug.Log($"左グリップの押し込み:{gripButon}");
        //}

    }


    public void OnRightTriggerButton(InputValue input)
    {
        if (input.isPressed)
        {
            ShootHook();
        }
    }

    public void OnRightGripButton()
    {

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