using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class XRSliderDragHandler : MonoBehaviour
{
    private Slider slider;
    private bool isGrabbed = false;
    private float startSliderValue;
    private Vector3 startPos;
    private XRBaseInteractor interactor;

    [Header("感度(0.1〜2.0) 推奨:1.0")]
    [SerializeField] private float sensitivity = 1.0f;

    void Awake()
    {
        slider = GetComponent<Slider>();
    }

    public void OnGrab(XRBaseInteractor grabInteractor)
    {
        interactor = grabInteractor;
        startPos = interactor.transform.position;
        startSliderValue = slider.value;
        isGrabbed = true;
    }

    public void OnRelease(XRBaseInteractor grabInteractor)
    {
        isGrabbed = false;
        interactor = null;
    }

    void Update()
    {
        if (!isGrabbed || interactor == null) return;

        Vector3 delta = interactor.transform.position - startPos;

        // 横方向(X軸)の移動をスライダーに反映
        float deltaValue = delta.x * sensitivity;

        slider.value = Mathf.Clamp01(startSliderValue + deltaValue);
    }
}
