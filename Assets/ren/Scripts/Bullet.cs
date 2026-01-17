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
        var enemy = collision.collider.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            enemy.ApplyDamage(damage, collision.collider);
        }

        Destroy(gameObject);
    }
}
