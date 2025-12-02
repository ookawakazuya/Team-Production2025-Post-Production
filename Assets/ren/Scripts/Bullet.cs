using UnityEngine;

public class Bullet : MonoBehaviour
{
    [Header("弾の基本設定")]
    [SerializeField] private float bulletSpeed = 50.0f; // 飛ぶ速さ

    public int damage = 20; // 基本ダメージ
    public float destroyTime = 3f;　// 弾の寿命

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    // Update is called once per frame
    void Update()
    {
        // --- 前方に進む ---
        float moveDistance = bulletSpeed * Time.deltaTime;
        transform.position += transform.forward * moveDistance;
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
        else
        {
            Debug.Log("Hit something else: " + collision.collider.name);
        }

        Destroy(gameObject);
    }
}
