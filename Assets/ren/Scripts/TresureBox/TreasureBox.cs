using UnityEngine;

public class TreasureBox : MonoBehaviour
{
    public StageID stageID;
    [Range(0, 2)] public int treasureIndex;

    private bool isCollected = false;

    private void Start()
    {
        // すでに取得済みなら最初から非表示
        if (GameManager.Instance.GetTreasureState(stageID)[treasureIndex])
        {
            isCollected = true;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 宝箱のふたが一定角度まで開いたときに呼ばれる
    /// </summary>
    public void OnTreasureOpened()
    {
        if (isCollected) return;

        if (GameManager.Instance.GetTreasureState(stageID)[treasureIndex])
            return;

        isCollected = true;

        GameManager.Instance.CollectTreasure(stageID, treasureIndex);

        // 必要ならここでSE / VFX
        Debug.Log($"宝箱取得 : {stageID} / {treasureIndex}");

        // 宝箱を消す（表示だけ消したい場合は Renderer OFF でもOK）
        gameObject.SetActive(false);
    }
}
