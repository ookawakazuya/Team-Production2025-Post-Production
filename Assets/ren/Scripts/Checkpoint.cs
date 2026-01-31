using System.Collections;
using UnityEngine;

/// <summary>
/// チェックポイントの機能を管理するクラス。
/// プレイヤーが触れた際に、GameManager にリスポーン地点を更新させる。
/// </summary>
[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("このチェックポイントの名前（任意）")]
    public string checkpointName = "Checkpoint";

    [Header("崩れるアニメーター")]
    [SerializeField] private Animator animator;

    [Header("光エフェクト")]
    [SerializeField] private ParticleSystem glowParticle;

    [Header("フレアエフェクト")]
    [SerializeField] private ParticleSystem flaresParticle;

    private bool isBreak = false;

    // ★ 元の色とEmissionを保存
    private Color glowStartColor;
    private Color flaresStartColor;
    private float glowEmissionRate;
    private float flaresEmissionRate;

    private void Awake()
    {
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
    }

    void OnTriggerEnter(Collider other)
    {
        // プレイヤーが触れたら
        if (other.CompareTag("Player") && !isBreak)
        {
            isBreak = true;

            // GameManager にリスポーン地点を更新させる
            GameManager.Instance.UpdateRespawnPoint(transform);

            Debug.Log($"チェックポイント到達：{checkpointName}");

            // アニメーション再生
            if (animator)
            {
                animator.SetBool("isBreak", true);
            }

            if (glowParticle)
                StartCoroutine(DelayFadeOut(glowParticle));

            if (flaresParticle)
                StartCoroutine(DelayFadeOut(flaresParticle));

        }
    }

    private IEnumerator DelayFadeOut(ParticleSystem ps)
    {
        // ★ アニメーションを見せるために1秒待つ
        yield return new WaitForSeconds(1f);

        // その後フェードアウト開始
        yield return StartCoroutine(FadeOutParticle(ps));
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

                // ★ 元のアルファを基準にフェード
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

    // ★ ステージリセット用
    public void ResetCheckpoint()
    {
        isBreak = false;

        if (animator)
        {
            animator.SetBool("isBreak", false);
        }

        if (glowParticle)
        {
            var main = glowParticle.main;
            var emission = glowParticle.emission;

            main.startColor = glowStartColor;           // 元の色に戻す
            emission.rateOverTime = glowEmissionRate;  // 元のEmissionに戻す
            glowParticle.Clear();
            glowParticle.Play();
        }

        if (flaresParticle)
        {
            var main = flaresParticle.main;
            var emission = flaresParticle.emission;

            main.startColor = flaresStartColor;           // 元の色に戻す
            emission.rateOverTime = flaresEmissionRate;  // 元のEmissionに戻す
            flaresParticle.Clear();
            flaresParticle.Play();
        }
    }
}
