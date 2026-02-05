using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//using static UnityEditor.VersionControl.Asset;

public class StageSelectButton : MonoBehaviour
{
    [Header("ステージ設定")]
    public StageID stageID;
    public string stageSceneName;

    [Header("宝箱UI")]
    public TreasureIconUI[] treasureIcons; // 3つ

    private void OnEnable()
    {
        // GameManagerのイベントに自分のRefreshメソッドを登録する
        GameManager.OnTreasureCollected += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        // オブジェクトが消えるときは登録を解除する（メモリリーク防止）
        GameManager.OnTreasureCollected -= Refresh;
    }

    public void Refresh()
    {
        // GameManagerが存在するかチェック
        if (GameManager.Instance == null)
        {
           // Debug.LogError("GameManagerが見つかりません！シーンに配置されていますか？");
            return;
        }

        bool[] state = GameManager.Instance.GetTreasureState(stageID);

        //if (state == null || state.Length < 3)
        //{
        //    Debug.LogWarning($"[{gameObject.name}] ステージ {stageID} の宝箱データが正しく取得できませんでした。");
        //    return;
        //}

        //// 配列がセットされているかチェック
        //if (treasureIcons == null || treasureIcons.Length == 0)
        //{
        //    Debug.LogError($"{gameObject.name} の TreasureIcons がインスペクターで設定されていません。");
        //    return;
        //}

        //for (int i = 0; i < treasureIcons.Length; i++)
        //{
        //    // アイコンの各要素がセットされているかチェック
        //    if (treasureIcons[i] == null)
        //    {
        //        Debug.LogError($"{gameObject.name} の TreasureIcons の {i}番目が空っぽです。");
        //        continue;
        //    }
        //    if (treasureIcons[i] != null && i < state.Length)
        //    {
        //        treasureIcons[i].SetCollected(state[i]);
        //    }
        //}
        for (int i = 0; i < treasureIcons.Length; i++)
        {
            if (i < state.Length && treasureIcons[i] != null)
            {
                // 各アイコンに取得状況を伝える
                treasureIcons[i].SetCollected(state[i]);
            }
        }
    }

    // ボタンOnClickに登録
    public void OnClickStage()
    {
        SceneManager.LoadScene(stageSceneName);
    }
}
