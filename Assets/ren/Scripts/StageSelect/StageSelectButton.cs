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
        // GameManagerが存在するかチェック
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません！シーンに配置されていますか？");
            return;
        }

        bool[] state = GameManager.Instance.GetTreasureState(stageID);

        // 配列がセットされているかチェック
        if (treasureIcons == null || treasureIcons.Length == 0)
        {
            Debug.LogError($"{gameObject.name} の TreasureIcons がインスペクターで設定されていません。");
            return;
        }

        for (int i = 0; i < treasureIcons.Length; i++)
        {
            // アイコンの各要素がセットされているかチェック
            if (treasureIcons[i] == null)
            {
                Debug.LogError($"{gameObject.name} の TreasureIcons の {i}番目が空っぽです。");
                continue;
            }
            treasureIcons[i].SetCollected(state[i]);
        }
    }

    // ボタンOnClickに登録
    public void OnClickStage()
    {
        SceneManager.LoadScene(stageSceneName);
    }
}
