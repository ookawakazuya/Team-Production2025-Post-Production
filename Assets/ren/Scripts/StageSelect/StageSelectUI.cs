using UnityEngine;

public class StageSelectUI : MonoBehaviour
{
    private void Start()
    {
        // ステージセレクトに来た時点で表示更新
        RefreshAllButtons();
    }

    public void RefreshAllButtons()
    {
        StageSelectButton[] buttons = FindObjectsOfType<StageSelectButton>();

        foreach (var button in buttons)
        {
            button.Refresh();
        }
    }

    // タイトルに戻るボタン用
    public void OnBackToTitle()
    {
        GameManager.Instance.ResetAllTreasure();
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }
}
