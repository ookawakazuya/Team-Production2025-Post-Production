using UnityEngine;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    public int maxLife = 3;
    public int currentLife;

    [Header("”í’e–³“GŠÔ")]
    public float invincibleTime = 1f;   // š 1•b

    [Header("ƒoƒŠƒAƒGƒtƒFƒNƒg")]
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
        // –³“G’† or Šù‚É€–S’†‚Í–³‹
        if (isInvincible)
        {
            return;
        }

        if (playerDeath.IsDead)
        {
            return;
        }

        currentLife -= damage;
        currentLife = Mathf.Max(0, currentLife);;

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
