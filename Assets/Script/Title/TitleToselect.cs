using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TitleToselect : MonoBehaviour
{
    [SerializeField] Button button;
    [SerializeField] string SceleName;

    public void OnClickButton()
    {
        VRMenuManager menuManager = Object.FindFirstObjectByType<VRMenuManager>();

        if (menuManager != null && menuManager.IsMenuOpen)
        {
            // メニューが開いているなら、強制的に閉じる処理を実行
            // これにより Time.timeScale も 1f に戻ります
            menuManager.ForceCloseMenuForLoading();
        }
        else
        {
            // 万が一 Manager が見つからない場合でも、念のため時間を動かす
            Time.timeScale = 1f;
        }


        SceneManager.LoadScene(SceleName);
        Debug.Log("VR Button Clicked!");
    }
}
