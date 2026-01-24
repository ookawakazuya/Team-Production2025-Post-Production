using UnityEngine;

/// <summary>
/// シーン開始時にメニュー状態を強制リセットし、プレイヤー操作を有効化するスクリプト
/// </summary>
public class MenuStateInitializer : MonoBehaviour
{
    void Awake()
    {
        //  時間の停止を解除（シーンをまたいで 0 になっている場合があるため）
        Time.timeScale = 1f;

        // 2VRController を探し、操作制限フラグをリセットする
        VRController controller = Object.FindFirstObjectByType<VRController>();
        if (controller != null)
        {
            // メニュー用フラグを false (通常モード) に戻す
            // VRController.cs にある SwitchToGameMode() を利用
            controller.SwitchToGameMode();
        }

        //  VRMenuManager を探し、メニューUIを非表示にする
        VRMenuManager menuManager = Object.FindFirstObjectByType<VRMenuManager>();
        if (menuManager != null)
        {
            // 内部の isMenuOpen フラグも false に同期させる
            // 前回の回答で作成した ForceCloseMenuForLoading と同様の処理をここでも行う
            menuManager.ForceCloseMenuForLoading();
        }

        Debug.Log("[MenuStateInitializer] シーン開始時の操作リセットが完了しました。");
    }
}