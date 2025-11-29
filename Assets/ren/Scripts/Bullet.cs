using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage = 20;      // 基本ダメージ
    public float destroyTime = 3f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // EnemyController を持っているオブジェクトか?
        EnemyController enemy = collision.collider.GetComponentInParent<EnemyController>();

        if (enemy != null)
        {
            int finalDamage = damage;

            // どのコライダーに当たったか判定
            if (collision.collider == enemy.headCollider)
            {
                finalDamage = damage * 2; // ヘッドショット
                Debug.Log("HeadShot! Damage: " + finalDamage);
            }
            else if (collision.collider == enemy.bodyCollider)
            {
                finalDamage = damage; // ボディ
                Debug.Log("BodyShot! Damage: " + finalDamage);
            }

            enemy.ApplyDamage(finalDamage);
        }

        Destroy(gameObject);
    }
}
