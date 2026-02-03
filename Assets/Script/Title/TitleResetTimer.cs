using UnityEngine;

public class TitleResetTimer : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float resetThresholdSeconds = 60f;

    private float idleTimer = 0f;
    private bool isDataReset = false;

    void Update()
    {
        if (isDataReset) return;

        idleTimer += Time.deltaTime;

        if (idleTimer >= resetThresholdSeconds)
        {
            isDataReset = true;

            //宝箱専用のリセットメソッドを呼び出す
            ChestSaveManager.ResetOnlyChestData();
        }
    }
}