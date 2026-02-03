using UnityEngine;

public static class ChestSaveManager
{
    // 保存用のキーを作成するメソッド (例: "Chest_Stage1_Index2")
    private static string GetKey(int stageID, int chestID)
    {
        return $"Chest_Stage{stageID}_Index{chestID}";
    }

    // 宝箱の状態を保存する (1 = 開封済み, 0 = 未開封)
    public static void SaveChestState(int stageID, int chestID)
    {
        PlayerPrefs.SetInt(GetKey(stageID, chestID), 1);
        PlayerPrefs.Save(); // ディスクに書き込み
        Debug.Log($"セーブ完了: {GetKey(stageID, chestID)}");
    }

    // 宝箱が開封済みかどうかを確認する
    public static bool IsChestOpened(int stageID, int chestID)
    {
        // キーが存在し、かつ値が1であれば true を返す
        return PlayerPrefs.GetInt(GetKey(stageID, chestID), 0) == 1;
    }

    // デバッグ用：全てのセーブデータを消去したい場合に使用
    public static void ClearAllData()
    {
        PlayerPrefs.DeleteAll();
    }
}