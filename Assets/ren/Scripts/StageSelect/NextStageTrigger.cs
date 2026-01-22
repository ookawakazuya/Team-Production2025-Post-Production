using UnityEngine;
using UnityEngine.SceneManagement;

public class NextStageTrigger : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] GameObject resultCanvas;

    private void Start()
    {
        //ゲーム開始時リザルト画面を隠す。
        if(resultCanvas != null)
        {
            resultCanvas.SetActive(false);
        }
    }

    public void ShowClearResult()
    {
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);
        }
    }

    public void LoadNextStage()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = currentIndex + 1;

        if (nextIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextIndex);
        }
        else
        {
            Debug.Log("次のシーンが存在しません。");
        }
    }
}
