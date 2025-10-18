using UnityEngine;

public class ResPawnController : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Enemy"))
        {
            Die();
        }
    }

    void Die()
    {
        // ƒŠƒXƒ|[ƒ“ˆ—ˆË—Š
        GameManager.Instance.RespawnPlayer(gameObject);
    }
}
