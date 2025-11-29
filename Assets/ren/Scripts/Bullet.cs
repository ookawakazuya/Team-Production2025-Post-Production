using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 20;      // 弾のダメージ量
    public float destroyTime = 3f;

    void Start()
    {
        Destroy(gameObject, destroyTime); // 一定時間後に自動削除
    }

    void OnCollisionEnter(Collision collision)
    {
        // EnemyController を持っているかチェック
        EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();

        if (enemy != null)
        {
            enemy.ApplyDamage(damage); // ダメージを与える
        }

        Destroy(gameObject); // 弾は当たったら消す
    }
}
