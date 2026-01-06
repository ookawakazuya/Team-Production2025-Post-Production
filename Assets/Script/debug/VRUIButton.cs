using UnityEngine;
using UnityEngine.UI;

public class VRUIButton : MonoBehaviour
{
    [SerializeField] Button closebutton;
    [SerializeField] GameObject menuPanel;
    void Start()
    {
    }


    public void OnClickButton()
    {
        Debug.Log("VR Button Clicked!");
    }
}
