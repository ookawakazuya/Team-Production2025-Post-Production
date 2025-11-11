using UnityEngine;

/// <summary>
/// プレイヤー検知専用のTrigger領域。
/// 自分の親にあるEnemyControllerへイベントを伝えるためのスクリプト。
/// </summary>
public class DetectionArea : MonoBehaviour
{
    // 親（EnemyController）の参照
    private EnemyController enemyController;

    /// <summary>
    /// 開始時に親のEnemyControllerを取得する。
    /// </summary>
    void Start()
    {
        enemyController = GetComponentInParent<EnemyController>();
    }

    /// <summary>
    /// プレイヤーが検知範囲に入ったとき、親に通知する。
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        enemyController?.OnChildTriggerEnter(other);
    }

    /// <summary>
    /// プレイヤーが検知範囲から出たとき、親に通知する。
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        enemyController?.OnChildTriggerExit(other);
    }
}
