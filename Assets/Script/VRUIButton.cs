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
    void CloseMenu()
    {
        if(menuPanel != null)
        {
            menuPanel.SetActive(false);
        }
    }
}
