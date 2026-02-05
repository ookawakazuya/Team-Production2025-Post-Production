using System.Collections;
using UnityEngine;

public class PlayerLife : MonoBehaviour
{

    [Header("暗転設定")]
    public float fadeOutTime = 1f;   // 暗転にかかる時間
    public float darkWaitTime = 2f;  // 真っ暗のまま待つ時間

    private bool isDead = false;
    public bool IsDead => isDead;

    public delegate void PlayerDeathHandler();
    public static event PlayerDeathHandler OnPlayerDied;

    private PlayerHealth playerHealth;
    private Collider playerCollider;
    private MonoBehaviour playerController;

    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerCollider = GetComponent<Collider>();
        playerController = GetComponent<VRController>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isDead) return;

        if (other.CompareTag("FallZone") ||
            other.CompareTag("Magma") ||
            other.CompareTag("Acid") ||
            other.CompareTag("Needle"))
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        SoundManager.Instance.PlaySE("SE_Dead_01");

        if (playerController) playerController.enabled = false;
        if (playerCollider) playerCollider.enabled = false;

        OnPlayerDied?.Invoke();

        // ★ すぐ暗転 → 2秒待ってリスポーン
        FadeController.Instance.FadeOut(fadeOutTime, () =>
        {
            StartCoroutine(RespawnAfterDark());
        });
    }

    IEnumerator RespawnAfterDark()
    {
        yield return new WaitForSeconds(darkWaitTime);
        Respawn();
    }

    void Respawn()
    {
        Transform respawnPoint = GameManager.Instance.GetRespawnPoint();

        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        playerHealth?.ResetLife();

        gameObject.SetActive(true);

        FadeController.Instance.FadeIn(1f, () =>
        {
            if (playerCollider) playerCollider.enabled = true;
            if (playerController) playerController.enabled = true;

            isDead = false;

            GameManager.Instance.OnPlayerDead();
        });
    }
}
