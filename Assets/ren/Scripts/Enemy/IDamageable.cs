using UnityEngine;

/// <summary>
/// ダメージを受けられるオブジェクトのインターフェイス
/// </summary>
public interface IDamageable
{
    /// <summary>
    /// 整数でダメージを与える
    /// </summary>
    void ApplyDamage(int damage);

}
