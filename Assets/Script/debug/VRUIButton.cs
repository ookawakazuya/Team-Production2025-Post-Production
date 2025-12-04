using UnityEngine;
using UnityEngine.UI;

public class VRUIButton : MonoBehaviour
{
    [SerializeField] Button closebutton;
    [SerializeField] GameObject menuPanel;
    void Start()
    {
        if(closebutton != null)
        {
            closebutton.onClick.AddListener(CloseMenu);
        }
    }
   public void CloseMenu()
    {
        if(menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }


    public void OnClickButton()
    {
        Debug.Log("VR Button Clicked!");
    }
}
