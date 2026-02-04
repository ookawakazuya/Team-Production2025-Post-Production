using System;

public static class ChestEventManager
{
    // 宝箱が開いたことを知らせるイベント (引数：ステージID, 宝箱ID)
    public static Action<int, int> OnChestOpened;
    //データがリセットされたことを知らせるイベント
    public static Action OnDataReset;

    // 宝箱からこのメソッドを呼ぶことで、UI側へ通知が飛ぶ
    public static void TriggerChestOpen(int stageID, int chestID)
    {
        OnChestOpened?.Invoke(stageID, chestID);
    }

    //リセットタイミング
    public static void TriggerDataReset()
    {
        OnDataReset?.Invoke();
    }
}