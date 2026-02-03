using UnityEngine;
using UnityEngine.UI;

public class ChestUIElement : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private int stageID;   // ステージ番号 (0〜3)
    [SerializeField] private int chestID;   // 宝箱番号 (0〜2)
    [SerializeField] private Image iconImage; // 色を変える対象のUIイメージ

    [Header("カラー設定")]
    [SerializeField] private Color lockedColor = Color.black;
    [SerializeField] private Color unlockedColor = Color.white;

    private void Start()
    {
        // 初期状態は黒に設定
        if (iconImage != null)
        {
            iconImage.color = lockedColor;
        }

        // イベントの購読（宝箱が開いた通知を受け取れるようにする）
        ChestEventManager.OnChestOpened += HandleChestOpened;
    }

    private void OnDestroy()
    {
        // メモリリーク防止のため、破棄時に購読を解除
        ChestEventManager.OnChestOpened -= HandleChestOpened;
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
