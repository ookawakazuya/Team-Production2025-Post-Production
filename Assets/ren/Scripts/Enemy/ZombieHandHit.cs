using UnityEngine;

public class ZombieHandHit : MonoBehaviour
{
    ZombieController zombie;

    void Awake()
    {
        zombie = GetComponentInParent<ZombieController>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!zombie) return;
        if (!zombie.CanDealDamage) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health)
        {
            health.TakeDamage(1);
            zombie.MarkAttackHit();
        }
    }
}
