using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    public enum State { Idle, Chase, Return }

    // -----------------------
    // Inspector
    // -----------------------
    [Header("基本設定")]
    public float maxHP = 100f;
    public float moveSpeed = 3f;
    public float rotationSpeed = 8f;
    public float chaseDistance = 10f;
    public float returnDistance = 15f;

    [Header("出現・消失距離")]
    public float spawnDistance = 15f;
    public float hideDistance = 16f;

    [Header("モデル / コライダー")]
    public Renderer[] modelRenderers;
    public Collider bodyCollider;
    public Collider headCollider;

    [Header("VFX (子オブジェクト)")]
    public VisualEffect spawnVFX;
    public VisualEffect deathVFX;

    [Header("HP UI")]
    public Image hpFillImage; // 緑の体力 Image

    // -----------------------
    // 内部キャッシュ
    // -----------------------
    Transform player;
    NavMeshAgent agent;
    Shotgun shotgun;

    float currentHP;
    State currentState = State.Idle;
    bool isVisible = false;
    public bool IsDead { get; private set; } = false;

    Vector3 startPosition;
    Quaternion startRotation;

    Coroutine spawnVFXCoroutine;
    Coroutine deathVFXCoroutine;

    // -----------------------
    // 初期設定
    // -----------------------
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;

        startPosition = transform.position;
        startRotation = transform.rotation;

        if (modelRenderers.Length == 0)
            modelRenderers = GetComponentsInChildren<Renderer>(true);
    }

    void Start()
    {
        currentHP = maxHP;
        

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        shotgun = FindFirstObjectByType<Shotgun>();

        GameManager.Instance?.RegisterEnemy(this);

        SetVisible(false, immediate: true);
    }

    // -----------------------
    // 更新処理
    // -----------------------
    void Update()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (player == null) return;
        }

        float dist = Vector3.Distance(player.position, transform.position);

        if (!IsDead)
        {
            if (!isVisible && dist < spawnDistance) ShowEnemy();
            else if (isVisible && dist > hideDistance) HideEnemy();
        }

        if (!isVisible || IsDead) return;

        switch (currentState)
        {
            case State.Idle:
                agent.isStopped = true;
                if (dist <= chaseDistance) currentState = State.Chase;
                break;

            case State.Chase:
                agent.isStopped = false;
                SafeSetDestination(player.position);
                if (dist > returnDistance) currentState = State.Return;
                break;

            case State.Return:
                agent.isStopped = false;
                SafeSetDestination(startPosition);
                if (Vector3.Distance(transform.position, startPosition) < 1f)
                {
                    agent.isStopped = true;
                    currentState = State.Idle;
                }
                break;
        }
    }

    // -----------------------
    // ダメージ処理
    // -----------------------
    public void ApplyDamage(float damage) => TakeDamage(damage);

    public void TakeDamage(float damage)
    {
        if (IsDead) return;

        currentHP -= damage;
        hpFillImage.fillAmount = currentHP / maxHP; // ← Sliderの代わり

        if (currentHP <= 0f) Die();
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;

        SetVisible(false, immediate: true);
        PlayDeathVFX();

        shotgun?.plusAmmo();
    }

    // -----------------------
    // 出現・消失
    // -----------------------
    void ShowEnemy()
    {
        if (IsDead) return;
        SetVisible(true);
        PlaySpawnVFX();
        hpFillImage.transform.parent.gameObject.SetActive(true);  // ← HPバー表示
    }

    void HideEnemy()
    {
        if (IsDead) return;
        SetVisible(false);
        hpFillImage.transform.parent.gameObject.SetActive(false); // ← HPバー非表示
        PlayDeathVFX();
    }

    void SetVisible(bool visible, bool immediate = false)
    {
        isVisible = visible;

        foreach (var r in modelRenderers)
            if (r != null) r.enabled = visible;

        if (bodyCollider) bodyCollider.enabled = visible;
        if (headCollider) headCollider.enabled = visible;

        if (agent)
        {
            agent.isStopped = !visible;
            if (immediate)
            {
                agent.Warp(transform.position);
                agent.ResetPath();
            }
        }
    }

    // -----------------------
    // Respawn（GameManagerから呼ばれる）
    // -----------------------
    public void Respawn()
    {
        IsDead = false;
        isVisible = false;

        currentHP = maxHP;
        hpFillImage.fillAmount = 1f; // ← HPリセット
        hpFillImage.transform.parent.gameObject.SetActive(false);

        transform.position = startPosition;
        transform.rotation = startRotation;

        agent.Warp(startPosition);
        agent.ResetPath();
        agent.isStopped = true;

        currentState = State.Idle;

        StopSpawnVFXImmediate();
        StopDeathVFXImmediate();

        SetVisible(false, immediate: true);
    }

    // -----------------------
    // NavMesh安全呼び出し
    // -----------------------
    void SafeSetDestination(Vector3 dest)
    {
        if (!agent) return;
#if UNITY_2022_1_OR_NEWER
        if (!agent.isOnNavMesh) return;
#endif
        try { agent.SetDestination(dest); }
        catch { }
    }

    // -----------------------
    // VFX管理
    // -----------------------
    void PlaySpawnVFX()
    {
        if (!spawnVFX) return;
        spawnVFX.gameObject.SetActive(true);
        spawnVFX.Reinit();
        spawnVFX.Play();
        if (spawnVFXCoroutine != null) StopCoroutine(spawnVFXCoroutine);
        spawnVFXCoroutine = StartCoroutine(DisableAfter(spawnVFX.gameObject, 2f));
    }

    void PlayDeathVFX()
    {
        if (!deathVFX) return;
        deathVFX.gameObject.SetActive(true);
        deathVFX.Reinit();
        deathVFX.Play();
        if (deathVFXCoroutine != null) StopCoroutine(deathVFXCoroutine);
        deathVFXCoroutine = StartCoroutine(DisableAfter(deathVFX.gameObject, 2f));
    }

    IEnumerator DisableAfter(GameObject go, float sec)
    {
        yield return new WaitForSeconds(sec);
        go.SetActive(false);
    }

    void StopSpawnVFXImmediate()
    {
        if (spawnVFXCoroutine != null) StopCoroutine(spawnVFXCoroutine);
        if (spawnVFX) spawnVFX.gameObject.SetActive(false);
    }

    void StopDeathVFXImmediate()
    {
        if (deathVFXCoroutine != null) StopCoroutine(deathVFXCoroutine);
        if (deathVFX) deathVFX.gameObject.SetActive(false);
    }
}
