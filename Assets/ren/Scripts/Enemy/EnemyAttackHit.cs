using UnityEngine;

public class EnemyAttackHit : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health)
        {
            health.TakeDamage(1);
        }
    }
}
