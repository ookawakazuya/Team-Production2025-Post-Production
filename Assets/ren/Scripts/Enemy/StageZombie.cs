using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class StageZombie : MonoBehaviour
{
    [Header("HP")]
    [SerializeField] private float maxHP = 100f;
    private float currentHP;

    [Header("Hit Colliders")]
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Collider headCollider;

    [Header("HP UI")]
    [SerializeField] private GameObject hpUIRoot;
    [SerializeField] private Image hpGreen;
    [SerializeField] private Image hpRed;

    private Coroutine greenRoutine;
    private Coroutine redRoutine;
    private bool isDead = false;

    private Animator animator;

    public bool IsDead => isDead;

    private void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        currentHP = maxHP;

        if (hpUIRoot)
            hpUIRoot.SetActive(false);
    }

    public void ApplyDamage(float damage, Collider hitPart = null)
    {
        if (isDead) return;

        float finalDamage = damage;

        // ヘッドショット2倍
        if (hitPart == headCollider)
            finalDamage *= 2f;

        currentHP = Mathf.Max(0f, currentHP - finalDamage);

        UpdateHPUI();

        if (currentHP <= 0f)
        {
            Die();
        }
        else
        {
            animator?.SetBool("isDamage", true);
            Invoke(nameof(ResetDamageAnim), 0.3f);
        }
    }

    private void ResetDamageAnim()
    {
        if (!isDead)
            animator?.SetBool("isDamage", false);
    }

    private void UpdateHPUI()
    {
        if (!hpUIRoot) return;

        hpUIRoot.SetActive(currentHP < maxHP);

        float ratio = currentHP / maxHP;

        if (greenRoutine != null) StopCoroutine(greenRoutine);
        if (redRoutine != null) StopCoroutine(redRoutine);

        greenRoutine = StartCoroutine(AnimateHP(hpGreen, ratio, 0f));
        redRoutine = StartCoroutine(AnimateHP(hpRed, ratio, 0.5f));
    }

    private IEnumerator AnimateHP(Image img, float target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float start = img.fillAmount;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 2f;
            img.fillAmount = Mathf.Lerp(start, target, t);
            yield return null;
        }

        img.fillAmount = target;
    }

    private void Die()
    {
        if (isDead) return;

        isDead = true;

        animator?.SetBool("isDeath", true);
        if (bodyCollider) bodyCollider.enabled = false;
        if (headCollider) headCollider.enabled = false;

        if (hpUIRoot) hpUIRoot.SetActive(false);

        // デストロイせず非アクティブ化
        gameObject.SetActive(false);
    }

    public void ResetZombie()
    {
        isDead = false;
        currentHP = maxHP;

        // コライダー復活
        if (bodyCollider) bodyCollider.enabled = true;
        if (headCollider) headCollider.enabled = true;

        // アニメーション初期化
        animator?.Rebind();
        animator?.Update(0f);

        // HPバーのCoroutineを停止
        if (greenRoutine != null) StopCoroutine(greenRoutine);
        if (redRoutine != null) StopCoroutine(redRoutine);

        // HPバーをフルに設定
        if (hpGreen) hpGreen.fillAmount = 1f;
        if (hpRed) hpRed.fillAmount = 1f;

        hpUIRoot?.SetActive(false);

        // ゾンビを表示
        gameObject.SetActive(true);
    }
}
