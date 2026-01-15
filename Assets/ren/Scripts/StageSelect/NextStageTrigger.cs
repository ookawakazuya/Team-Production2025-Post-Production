using UnityEngine;
using UnityEngine.SceneManagement;

public class NextStageTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Playerタグを持つオブジェクトが触れたときのみ反応
        if (other.CompareTag("Player"))
        {
            LoadNextStage();
        }
    }

    private void LoadNextStage()
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
