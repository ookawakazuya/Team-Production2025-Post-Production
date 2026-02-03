using UnityEngine;

public class GameExitManager : MonoBehaviour
{
    /// <summary>
    /// ゲームを完全に終了し、すべてのセーブデータをリセットする
    /// ボタンのOnClickイベントから呼び出してください
    /// </summary>
    public void ExitGameWithFullReset()
    {
        // 1. すべての PlayerPrefs データを削除（宝箱情報、音量、その他すべての設定）
        PlayerPrefs.DeleteAll();

        // 2. 削除をディスクに即時反映
        PlayerPrefs.Save();

        Debug.Log("すべてのセーブデータをリセットしました。ゲームを終了します。");

        // 3. アプリケーションを終了させる
        // エディタ上でのテスト中と、ビルド後の実機両方で動作するように記述します
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Unityエディタでの再生停止
#else
        Application.Quit(); // ビルドしたゲーム（VR実機など）の終了
#endif
    }
}