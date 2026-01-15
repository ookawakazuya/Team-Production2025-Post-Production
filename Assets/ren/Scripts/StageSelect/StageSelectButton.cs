using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageSelectButton : MonoBehaviour
{
    [Header("ステージ設定")]
    public StageID stageID;
    public string stageSceneName;

    [Header("宝箱UI")]
    public TreasureIconUI[] treasureIcons; // 3つ

    private void Start()
    {
        Refresh();
    }

    public void Refresh()
    {
        bool[] state = GameManager.Instance.GetTreasureState(stageID);

        for (int i = 0; i < treasureIcons.Length; i++)
        {
            treasureIcons[i].SetCollected(state[i]);
        }
    }

    // ボタンOnClickに登録
    public void OnClickStage()
    {
        SceneManager.LoadScene(stageSceneName);
    }
}
