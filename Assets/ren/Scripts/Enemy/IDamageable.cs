using UnityEngine;

/// <summary>
/// ダメージを受けられるオブジェクト用インターフェース
/// </summary>
public interface IDamageable
{
    void ApplyDamage(int damage);
}
