using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleToselect : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] string SceleName;

    public void OnClickButton()
    {
        SceneManager.LoadScene(SceleName);
        Debug.Log("VR Button Clicked!");
    }
}
