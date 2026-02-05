using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    public float bulletSpeed = 50f;
    public int damage = 20;
    public float destroyTime = 3f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = transform.forward * bulletSpeed;

        Destroy(gameObject, destroyTime);
    }

    void OnCollisionEnter(Collision collision)
    {
        // EnemyBase �Ή�
        var enemyBase = collision.collider.GetComponentInParent<EnemyBase>();
        if (enemyBase != null)
        {
            enemyBase.ApplyDamage(damage, collision.collider);
        }

        // StageZombieSimpleNoDrop �Ή�
        var stageZombie = collision.collider.GetComponentInParent<StageZombie>();
        if (stageZombie != null)
        {
            stageZombie.ApplyDamage(damage, collision.collider);
        }

        // �e��j��
        Destroy(gameObject);
    }
}
