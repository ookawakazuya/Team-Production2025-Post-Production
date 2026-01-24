using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxLife = 3;
    public int currentLife;

    [Header("被弾無敵時間")]
    public float invincibleTime = 1f;

    [Header("バリアエフェクト")]
    [SerializeField] private GameObject barrierObject; // ★ GameObjectで持つ
    [SerializeField] private ParticleSystem barrierParticle;

    private bool isInvincible = false;

    PlayerLife playerDeath;
    VignettController vignette;

    void Awake()
    {
        vignette = FindObjectOfType<VignettController>();
        currentLife = maxLife;
        playerDeath = GetComponent<PlayerLife>();

        // ★ 最初は必ずOFF
        if (barrierObject)
            barrierObject.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        if (playerDeath.IsDead) return;

        // 先にHP計算
        int nextLife = currentLife - damage;

        // 死亡するならそのまま死亡（バリア出さない）
        if (nextLife <= 0)
        {
            currentLife = 0;
            playerDeath.Die();
            return;
        }

        // ★ ここに来た＝まだ生きてる

        currentLife = nextLife;

        // バリアON
        ShowBarrier();

        // Vignette
        vignette?.PlayDamageVignette();

        // 無敵開始
        StopAllCoroutines();
        StartCoroutine(InvincibleCoroutine());
    }


    void ShowBarrier()
    {
        if (!barrierObject) return;

        barrierObject.SetActive(true);

        if (barrierParticle)
        {
            barrierParticle.Stop();
            barrierParticle.Clear();
            barrierParticle.Play();
        }
    }

    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;

        yield return new WaitForSeconds(invincibleTime);

        isInvincible = false;

        // ★ OFFに戻す
        if (barrierObject)
            barrierObject.SetActive(false);
    }

    public void ResetLife()
    {
        currentLife = maxLife;
        isInvincible = false;

        if (barrierObject)
            barrierObject.SetActive(false);
    }
}
