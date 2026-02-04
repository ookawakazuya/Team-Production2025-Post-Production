using UnityEngine;
using UnityEngine.UI;

public class ChestUIElement : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int stageID;   // ステージ番号 (0〜3)
    [SerializeField] private int chestID;   // 宝箱番号 (0〜3)
    [SerializeField] private Image iconImage; // 色を変える対象のUIイメージ

    [Header("カラー設定")]
    [SerializeField] private Color lockedColor = Color.black;
    [SerializeField] private Color unlockedColor = Color.white;

    private void Start()
    {
        RefreshVisual();

        if(chestID != 3)
        {
            // イベントの購読（宝箱が開いた通知を受け取れるようにする）
            ChestEventManager.OnChestOpened += HandleChestOpened;
        }
        ChestEventManager.OnDataReset += RefreshVisual;
    }

    private void OnEnable()
    {
        // オブジェクトが有効になるたびに最新の状態を確認（念のための処理）
        RefreshVisual();
    }

    private void OnDestroy()
    {
        if (chestID != 3)
        {
            // メモリリーク防止のため、破棄時に購読を解除
            ChestEventManager.OnChestOpened -= HandleChestOpened;
        }
        ChestEventManager.OnChestOpened -= HandleChestOpened;
    }

    // セーブデータを参照して現在の色を決定する
    private void RefreshVisual()
    {
        if (iconImage == null) return;

        // SaveManagerから取得済みかどうかを確認
        bool isOpened = ChestSaveManager.IsChestOpened(stageID, chestID);


        if(chestID == 3)
        {
            iconImage.gameObject.SetActive(isOpened);
        }
        else
        {
            iconImage.gameObject.SetActive(true);
            // 取得済みなら白、未取得なら黒を設定
            iconImage.color = isOpened ? unlockedColor : lockedColor;
        }

    }


    // 宝箱が開かれた時に実行されるメソッド
    private void HandleChestOpened(int openedStageID, int openedChestID)
    {
        // 自分自身の担当するステージIDと宝箱IDが一致するか確認
        if (this.stageID == openedStageID && this.chestID == openedChestID)
        {
            UnlockVisual();
        }
    }

    private void UnlockVisual()
    {
        if (iconImage != null)
        {
            iconImage.color = unlockedColor;
            Debug.Log($"UI更新: ステージ{stageID} の 宝箱{chestID} が白くなりました。");
        }
    }
}
