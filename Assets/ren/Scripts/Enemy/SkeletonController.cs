using UnityEngine;

public class SkeletonController : MonoBehaviour, IDamageable
{
    [Header("当たり判定")]
    public Collider headCollider;
    public Collider bodyCollider;

    public void ApplyDamage(int damage)
    {
        // スケルトン用ダメージ処理
    }
}
