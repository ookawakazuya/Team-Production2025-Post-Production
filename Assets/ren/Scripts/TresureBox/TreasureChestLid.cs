using UnityEngine;
using UnityEngine.SceneManagement;

public class TreasureChestLid : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private StageID currentStage;

    [Header("Stage Clear")]
    public bool isStageClear = false;

    [Header("Angle Check")]
    [SerializeField] private float openAngle = 250f;   // -110°
    [SerializeField] private float angleTolerance = 2f;

    bool hasTriggered = false;

    [Header("設定")]
    [SerializeField] GameObject resultCanvas;

    private void Start()
    {
        //ゲーム開始時リザルト画面を隠す。
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
        }
    }


    void Update()
    {
        if (hasTriggered) return;

        float xAngle = transform.localEulerAngles.x;

        if (IsAngleReached(xAngle))
        {
            OnChestOpened();
        }
    }

    bool IsAngleReached(float angle)
    {
        return angle >= openAngle - angleTolerance &&
               angle <= openAngle + angleTolerance;
    }

    void OnChestOpened()
    {
        hasTriggered = true;
        isStageClear = true;

        Debug.Log($"{currentStage} クリア！");

        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);
        }

       // LoadNextStage();
    }

   public void LoadNextStage()
    {
        int nextIndex = (int)currentStage + 1;

        // ★ 最終ステージ
        if (nextIndex >= System.Enum.GetValues(typeof(StageID)).Length)
        {
            Debug.Log("最終ステージクリア！");
            // 例：エンディングシーン
            SceneManager.LoadScene("Ending");
            return;
        }

        StageID nextStage = (StageID)nextIndex;
        SceneManager.LoadScene(nextStage.ToString());
    }
}
