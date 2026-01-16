using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSelectTrigger : MonoBehaviour
{
    [Header("ステージセレクトシーン名")]
    [SerializeField] private string stageSelectSceneName = "StageSelect";

    private void OnTriggerEnter(Collider other)
    {
        // Playerタグを持つオブジェクトが触れたときのみ反応
        if (other.CompareTag("Player"))
        {
            LoadStageSelect();
        }
    }

    private void LoadStageSelect()
    {
        if (Application.CanStreamedLevelBeLoaded(stageSelectSceneName))
        {
            SceneManager.LoadScene(stageSelectSceneName);
        }
        else
        {
            Debug.LogError($"シーン '{stageSelectSceneName}' が Build Settings に存在しません");
        }
    }
}
