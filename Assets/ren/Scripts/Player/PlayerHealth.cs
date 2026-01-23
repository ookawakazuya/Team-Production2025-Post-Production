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
        Debug.Log($"[PlayerHealth] TakeDamageŒÄ‚Î‚ê‚½ damage={damage}");

        // –³“G’† or Šù‚É€–S’†‚Í–³‹
        if (isInvincible)
        {
            Debug.Log("[PlayerHealth] –³“G’†‚È‚Ì‚Å–³‹");
            return;
        }

        if (playerDeath.IsDead)
        {
            Debug.Log("[PlayerHealth] ‚·‚Å‚É€–S’†‚È‚Ì‚Å–³‹");
            return;
        }

        currentLife -= damage;
        currentLife = Mathf.Max(0, currentLife);

        Debug.Log($"[PlayerHealth] ”íƒ_ƒ[ƒWI c‚èHP: {currentLife}");

        if (currentLife <= 0)
        {
            Debug.Log("[PlayerHealth] HP0 ¨ €–Sˆ—‚Ö");
            playerDeath.Die();
        }
        else
        {
            Debug.Log("[PlayerHealth] –³“GŠÔŠJn");
            StartCoroutine(InvincibleCoroutine());
        }
    }


    IEnumerator InvincibleCoroutine()
    {
        isInvincible = true;

        Debug.Log("[PlayerHealth] –³“GON");

        if (barrierParticle)
        {
            barrierParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            barrierParticle.Play();
        }

        yield return new WaitForSeconds(invincibleTime);

        isInvincible = false;

        Debug.Log("[PlayerHealth] –³“GOFF");

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
