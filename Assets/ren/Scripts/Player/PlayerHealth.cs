using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxLife = 3;
    public int currentLife;

    [Header("被弾無敵時間")]
    public float invincibleTime = 1f;   // ★ 1秒

    [Header("バリアエフェクト")]
    [SerializeField] private ParticleSystem barrierParticle;

    private bool isInvincible = false;

    PlayerDeath playerDeath;

    void Awake()
    {
        currentLife = maxLife;
        playerDeath = GetComponent<PlayerDeath>();

        if (barrierParticle)
            barrierParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    public void TakeDamage(int damage)
    {
        // 無敵中 or 既に死亡中は無視
        if (isInvincible || playerDeath.IsDead) return;

        currentLife -= damage;
        currentLife = Mathf.Max(0, currentLife);

        Debug.Log($"プレイヤー被ダメージ 残りライフ: {currentLife}");

        if (currentLife <= 0)
        {
            playerDeath.Die();
        }
        else
        {
            StartCoroutine(InvincibleCoroutine());
        }
    }

    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;

        if (barrierParticle)
        {
            barrierParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            barrierParticle.Play();
        }

        yield return new WaitForSeconds(invincibleTime);

        isInvincible = false;

        if (barrierParticle)
        {
            barrierParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    public void ResetLife()
    {
        currentLife = maxLife;
        isInvincible = false;

        if (barrierParticle)
            barrierParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
