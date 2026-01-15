using UnityEngine;

public class TreasureBox : MonoBehaviour
{
    public StageID stageID;
    [Range(0, 2)] public int treasureIndex;

    private void Start()
    {
        // ‚·‚Å‚Éæ“¾Ï‚İ‚È‚çÅ‰‚©‚ç”ñ•\¦
        if (GameManager.Instance.GetTreasureState(stageID)[treasureIndex])
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // ‚·‚Å‚Éæ“¾Ï‚İ‚È‚ç‰½‚à‚µ‚È‚¢
        if (GameManager.Instance.GetTreasureState(stageID)[treasureIndex])
            return;

        GameManager.Instance.CollectTreasure(stageID, treasureIndex);

        gameObject.SetActive(false);
    }
}
