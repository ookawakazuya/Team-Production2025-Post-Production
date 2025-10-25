using UnityEngine;
using UnityEngine.UI;

public class VRButtonTest : MonoBehaviour
{
    [SerializeField] Button testButton;

    void Start()
    {
        if (testButton != null)
            testButton.onClick.AddListener(OnButtonClicked);
    }

    void OnButtonClicked()
    {
        Debug.Log("VRƒ{ƒ^ƒ“‚ª‰Ÿ‚³‚ê‚Ü‚µ‚½I");
    }
}