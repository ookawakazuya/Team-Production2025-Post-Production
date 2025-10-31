using UnityEngine;

/// <summary>
/// チェックポイントの機能を管理するクラス。
/// プレイヤーが触れた際に、GameManager にリスポーン地点を更新させる。
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("このチェックポイントの名前（任意）")]
    public string checkpointName = "Checkpoint";

    private void OnTriggerEnter(Collider other)
    {
        // プレイヤーが触れたら
        if (other.CompareTag("Player"))
        {
            // GameManager にリスポーン地点を更新させる
            GameManager.Instance.UpdateRespawnPoint(transform);

            Debug.Log($"🏁 チェックポイント到達：{checkpointName}");
        }
    }
}
