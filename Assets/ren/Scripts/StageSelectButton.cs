using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectButton : MonoBehaviour
{
    [SerializeField] private string sceneName;  // ボタンごとにロードしたいシーン名を設定

    public void LoadStage()
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("シーン名が設定されていません: " + gameObject.name);
        }
    }
}
