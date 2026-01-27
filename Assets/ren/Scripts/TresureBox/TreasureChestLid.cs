using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TreasureChestLid : MonoBehaviour
{
    [Header("Stage")]
    [SerializeField] private StageID currentStage;

    [Header("Stage Clear")]
    public bool isStageClear = false;

    [Header("Angle Check")]
    [SerializeField] private float openAngle = 250f;   // -110°
    [SerializeField] private float angleTolerance = 2f;

    [SerializeField] float targetOpenAngle = -100f;

    HingeJoint joint;

    bool hasTriggered = false;

    [Header("設定")]
    [SerializeField] GameObject resultCanvas;

    private void Start()
    {
        joint = GetComponent<HingeJoint>();

        //ゲーム開始時リザルト画面を隠す。
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
        }
    }


    void Update()
    {
        if (hasTriggered || joint == null) return;
        // HingeJoint.angle は物理的な回転角をダイレクトに返します（-180～180）
        float currentAngle = joint.angle;

        Debug.Log($"{gameObject.name} の現在の角度: {transform.localEulerAngles.x} / Joint角度: {GetComponent<HingeJoint>().angle}");

        if (currentAngle <= (targetOpenAngle))
        {
            OnChestOpened();
        }
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

        int nextIndex = (int)currentStage + 1;

        //  最終ステージ
        if (nextIndex >= System.Enum.GetValues(typeof(StageID)).Length)
        {
            Debug.Log("最終ステージクリア！");
            StartCoroutine(DelayCoroutine());
        }

        
    }

   public void LoadNextStage()
    {
        int nextIndex = (int)currentStage + 1;

        StageID nextStage = (StageID)nextIndex;
        SceneManager.LoadScene(nextStage.ToString());
    }

    private IEnumerator DelayCoroutine()
    {
        yield return new WaitForSeconds(3);
        // 例：エンディングシーン
        SceneManager.LoadScene("TitleScene");
    }
}
