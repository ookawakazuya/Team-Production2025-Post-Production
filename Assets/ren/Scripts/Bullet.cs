using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("弾の基本設定")]
    [SerializeField] private float bulletSpeed = 50.0f;

    public int damage = 20;
    public float destroyTime = 3f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // 前方に発射
        rb.linearVelocity = transform.forward * bulletSpeed;

        Destroy(gameObject, destroyTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Hit: " + collision.collider.name);

        // ダメージを受けられるか？
        IDamageable damageable =
            collision.collider.GetComponentInParent<IDamageable>();

        if (damageable != null)
        {
            int finalDamage = damage;

            // ヘッド or ボディ判定
            var hitRoot = collision.collider.GetComponentInParent<MonoBehaviour>();

            if (hitRoot is ZombieController zombie)
            {
                if (collision.collider == zombie.headCollider)
                    finalDamage *= 2;
            }
            else if (hitRoot is SkeletonController skeleton)
            {
                if (collision.collider == skeleton.headCollider)
                    finalDamage *= 2;
            }

            damageable.ApplyDamage(finalDamage);
        }

        Destroy(gameObject);
    }
}
