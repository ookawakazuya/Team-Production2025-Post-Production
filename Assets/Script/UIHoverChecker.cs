using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class UIHoverChecker : MonoBehaviour
{
    //鎌田担当

    public XRRayInteractor rayInteractor;

    void OnEnable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.AddListener(OnHoverEnter);
            rayInteractor.hoverExited.AddListener(OnHoverExit);
            rayInteractor.selectEntered.AddListener(OnSelectEnter);
            rayInteractor.selectExited.AddListener(OnSelectExit);
        }
    }

    void OnDisable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.hoverEntered.RemoveListener(OnHoverEnter);
            rayInteractor.hoverExited.RemoveListener(OnHoverExit);
            rayInteractor.selectEntered.RemoveListener(OnSelectEnter);
            rayInteractor.selectExited.RemoveListener(OnSelectExit);
        }
    }

    void OnHoverEnter(HoverEnterEventArgs args)
    {
        Debug.Log("UI に触れた: " + args.interactableObject);
    }

    void OnHoverExit(HoverExitEventArgs args)
    {
        Debug.Log("UI から離れた: " + args.interactableObject);
    }

    void OnSelectEnter(SelectEnterEventArgs args)
    {
        Debug.Log("UI を選択した（クリックした）: " + args.interactableObject);
    }

    void OnSelectExit(SelectExitEventArgs args)
    {
        Debug.Log("UI の選択を解除した: " + args.interactableObject);
    }
}
