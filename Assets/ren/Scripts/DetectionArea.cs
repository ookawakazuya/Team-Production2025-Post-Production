// DetectionArea.cs
using UnityEngine;

/// <summary>
/// DetectionArea は Enemy の子オブジェクトに付けるトリガー（例：SphereCollider isTrigger）
/// プレイヤーが入った/出たを EnemyController に通知する。
/// </summary>
[RequireComponent(typeof(Collider))]
public class DetectionArea : MonoBehaviour
{
    EnemyController enemyController;

    void Start()
    {
        enemyController = GetComponentInParent<EnemyController>();
        if (enemyController == null)
            Debug.LogWarning("DetectionArea: 親に EnemyController が見つかりません。");
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // プレイヤーを検知。Transform を渡すことで Enemy 側が直接参照できる
            enemyController?.OnPlayerDetected(other.transform);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            enemyController?.OnPlayerLost(other.transform);
        }
    }
}
