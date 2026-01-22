using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public abstract class EnemyBase : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public float maxHP = 100f;

    [Header("HP UI")]
    public GameObject hpUIRoot;
    public Image hpGreen;
    public Image hpRed;

    [Header("HP UI Distance")]
    [SerializeField] protected float hpVisibleDistance = 10f;

    protected Transform player;

    protected float currentHP;
    protected bool isDead;

    protected Vector3 startPosition;
    protected Quaternion startRotation;

    Coroutine greenRoutine;
    Coroutine redRoutine;

    protected virtual void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    protected virtual void Start()
    {
        currentHP = maxHP;
        GameManager.Instance?.RegisterEnemy(this);

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    protected virtual void Update()
    {
        UpdateHPUIVisibility();
    }

    protected void UpdateHPUIVisibility()
    {
        if (!hpUIRoot || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        bool shouldShow =
            !isDead &&
            currentHP < maxHP &&        // ダメージを受けた敵だけ
            dist <= hpVisibleDistance;

        if (hpUIRoot.activeSelf != shouldShow)
            hpUIRoot.SetActive(shouldShow);
    }


    // =====================
    // ダメージ受付
    // =====================
    public void ApplyDamage(int damage)
    {
        ApplyDamage((float)damage, null);
    }

    public void ApplyDamage(float damage)
    {
        ApplyDamage(damage, null);
    }

    public void ApplyDamage(float baseDamage, Collider hitPart)
    {
        if (isDead) return;

        float finalDamage = CalculateDamage(baseDamage, hitPart);

        currentHP = Mathf.Max(0, currentHP - finalDamage);
        UpdateHPUI();

        if (currentHP <= 0)
            Die();
    }

    protected void UpdateHPUI()
    {
        float target = currentHP / maxHP;

        if (greenRoutine != null) StopCoroutine(greenRoutine);
        greenRoutine = StartCoroutine(AnimateHP(hpGreen, target, 0f));

        if (redRoutine != null) StopCoroutine(redRoutine);
        redRoutine = StartCoroutine(AnimateHP(hpRed, target, 0.5f));
    }

    IEnumerator AnimateHP(Image img, float target, float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

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

    // =====================
    // 敵ごとに実装
    // =====================
    protected abstract float CalculateDamage(float baseDamage, Collider hitPart);
    protected abstract void Die();
    protected abstract void OnRespawn();

    // =====================
    // リスポーン
    // =====================
    public virtual void Respawn()
    {
        StopAllCoroutines();

        isDead = false;
        currentHP = maxHP;

        transform.position = startPosition;
        transform.rotation = startRotation;

        gameObject.SetActive(true);
        hpUIRoot?.SetActive(true);

        OnRespawn();
    }
}
