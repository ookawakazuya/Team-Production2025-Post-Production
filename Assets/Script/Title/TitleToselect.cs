using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleToselect : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] char SceleName;

    public void OnClickButton()
    {
        SceneManager.LoadScene(SceleName);
        Debug.Log("VR Button Clicked!");
    }
}
