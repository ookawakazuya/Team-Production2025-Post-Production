using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("このCrystalの名前")]
    public string crystalName = "Crystal";

    [Header("崩れるアニメーター")]
    [SerializeField] private Animator animator;

    [Header("光エフェクト")]
    [SerializeField] private ParticleSystem glowParticle;

    [Header("フレアエフェクト")]
    [SerializeField] private ParticleSystem flaresParticle;

    [Header("キラキラSE用")]
    [SerializeField] private float sparkleDistance = 5f; // プレイヤーとの距離でSE再生

    private bool isBreak = false;
    private bool isSparklePlaying = false;

    private Transform player;

    private Color glowStartColor;
    private Color flaresStartColor;
    private float glowEmissionRate;
    private float flaresEmissionRate;

    private void Awake()
    {
        // パーティクル初期値保存
        if (glowParticle)
        {
            glowStartColor = glowParticle.main.startColor.color;
            glowEmissionRate = glowParticle.emission.rateOverTime.constant;
        }

        if (flaresParticle)
        {
            flaresStartColor = flaresParticle.main.startColor.color;
            flaresEmissionRate = flaresParticle.emission.rateOverTime.constant;
        }

        // Collider は必ず Trigger にする
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    private void Update()
    {
        if (isBreak || !player) return;

        // プレイヤーとの距離でキラキラSE再生 / 停止
        float dist = Vector3.Distance(player.position, transform.position);
        if (dist <= sparkleDistance && !isSparklePlaying)
        {
            SoundManager.Instance.PlaySELoop("Onoma-Sparkle01-1");
            isSparklePlaying = true;
        }
        else if (dist > sparkleDistance && isSparklePlaying)
        {
            SoundManager.Instance.StopSELoop();
            isSparklePlaying = false;
        }
    }

    // ===========================
    // プレイヤーが触れたら崩れる
    // ===========================
    private void OnTriggerEnter(Collider other)
    {
        if (isBreak) return;

        if (other.CompareTag("Player"))
        {
            OnCrystalTouched();
        }
    }

    public void OnCrystalTouched()
    {
        if (isBreak) return;

        isBreak = true;

        // 崩れるSE
        SoundManager.Instance.PlaySE("StonesCrumble");

        // キラキラ停止
        if (isSparklePlaying)
        {
            SoundManager.Instance.StopSELoop();
            isSparklePlaying = false;
        }

        // GameManager にリスポーンポイント更新
        GameManager.Instance.UpdateRespawnPoint(transform);

        // アニメーション再生
        if (animator)
            animator.SetBool("isBreak", true);

        // パーティクルフェードアウト
        if (glowParticle)
            StartCoroutine(FadeOutParticle(glowParticle));
        if (flaresParticle)
            StartCoroutine(FadeOutParticle(flaresParticle));

        Debug.Log($"Crystal崩壊：{crystalName}");
    }

    private IEnumerator FadeOutParticle(ParticleSystem ps)
    {
        var emission = ps.emission;
        emission.rateOverTime = 0;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[ps.main.maxParticles];

        float time = 0f;
        float duration = 1f;

        while (time < duration)
        {
            int count = ps.GetParticles(particles);
            float t = time / duration;

            for (int i = 0; i < count; i++)
            {
                Color c = particles[i].startColor;
                float baseAlpha = c.a;
                c.a = Mathf.Lerp(baseAlpha, 0f, t);
                particles[i].startColor = c;
            }

            ps.SetParticles(particles, count);
            time += Time.deltaTime;
            yield return null;
        }

        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ps.gameObject.SetActive(false);
    }

    // ===========================
    // ステージリセット用
    // ===========================
    public void ResetCheckpoint()
    {
        isBreak = false;
        isSparklePlaying = false;

        if (animator)
            animator.SetBool("isBreak", false);

        if (glowParticle)
        {
            var main = glowParticle.main;
            var emission = glowParticle.emission;
            main.startColor = glowStartColor;
            emission.rateOverTime = glowEmissionRate;
            glowParticle.Clear();
            glowParticle.Play();
        }

        if (flaresParticle)
        {
            var main = flaresParticle.main;
            var emission = flaresParticle.emission;
            main.startColor = flaresStartColor;
            emission.rateOverTime = flaresEmissionRate;
            flaresParticle.Clear();
            flaresParticle.Play();
        }
    }
}
