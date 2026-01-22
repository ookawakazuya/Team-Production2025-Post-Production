using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.AI;

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

    [Header("AI Distance")]
    [SerializeField] protected float chaseDistance = 15f;
    [SerializeField] protected float attackDistance = 5f;
    [SerializeField] protected float stopDistance = 2f; // Åö ãﬂÇ√Ç´Ç∑Ç¨ñhé~

    protected Transform player;
    protected NavMeshAgent agent;
    protected Animator animator;

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

        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
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

    // =====================
    // HP UI ï\é¶êßå‰
    // =====================
    protected void UpdateHPUIVisibility()
    {
        if (!hpUIRoot || !player) return;

        float dist = Vector3.Distance(transform.position, player.position);

        bool shouldShow =
            !isDead &&
            currentHP < maxHP &&
            dist <= hpVisibleDistance;

        if (hpUIRoot.activeSelf != shouldShow)
            hpUIRoot.SetActive(shouldShow);
    }

    // =====================
    // ã§í  Chase à⁄ìÆÅiÇﬂÇËçûÇ›ñhé~Åj
    // =====================
    protected void UpdateChaseMovement(float dist)
    {
        if (!agent) return;

        if (dist <= stopDistance)
        {
            agent.isStopped = true;
            return;
        }

        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    // =====================
    // É_ÉÅÅ[ÉWéÛït
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
    // ìGÇ≤Ç∆Ç…é¿ëï
    // =====================
    protected abstract float CalculateDamage(float baseDamage, Collider hitPart);
    protected abstract void Die();
    protected abstract void OnRespawn();

    // =====================
    // ÉäÉXÉ|Å[Éì
    // =====================
    public virtual void Respawn()
    {
        StopAllCoroutines();

        isDead = false;
        currentHP = maxHP;

        if (agent)
            agent.Warp(startPosition);
        else
            transform.position = startPosition;

        transform.rotation = startRotation;

        gameObject.SetActive(true);
        hpUIRoot?.SetActive(false);

        OnRespawn();
    }

    protected void DropAmmo(GameObject ammoPrefab)
    {
        if (!ammoPrefab) return;

        Vector3 dropPos = transform.position + Vector3.up * 1.0f;

        // â∫Ç…RayÇîÚÇŒÇµÇƒè∞ÇíTÇ∑
        if (Physics.Raycast(dropPos, Vector3.down, out RaycastHit hit, 5f))
        {
            dropPos = hit.point + Vector3.up * 0.1f;
        }

        GameObject ammo = Instantiate(ammoPrefab, dropPos, Quaternion.identity);
        ammo.tag = "Amo";
    }
}

