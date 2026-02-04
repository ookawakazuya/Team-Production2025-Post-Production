using UnityEngine;
using UnityEngine.VFX;
using System.Collections;

public class TutorialZombie : EnemyBase
{
    [Header("Hit Colliders")]
    [SerializeField] private Collider bodyCollider;
    [SerializeField] private Collider headCollider;

    [Header("Death Settings")]
    [SerializeField] private float deathAnimTime = 2f;
    [SerializeField] private float deathVfxTime = 0.5f;
    [SerializeField] private float autoRespawnDelay = 5f; // 死亡から復活までの待機時間
    [SerializeField] private VisualEffect deathVFX;

    [Header("Drop Items")]
    [SerializeField] private GameObject ammoPrefab;

    // ダメージ計算
    protected override float CalculateDamage(float baseDamage, Collider hitPart)
    {
        animator.SetBool("isDamage", true);
        Invoke(nameof(ResetDamageAnim), 0.3f);

        if (hitPart == headCollider) return baseDamage * 2f;
        return baseDamage;
    }

    private void ResetDamageAnim()
    {
        if (!isDead) animator.SetBool("isDamage", false);
    }

    // 死亡時の入り口
    protected override void Die()
    {
        if (isDead) return;
        isDead = true;
        StartCoroutine(DeathAndRespawnSequence());
    }

    private IEnumerator DeathAndRespawnSequence()
    {
        // 1. ナビメッシュを止める
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 2. 当たり判定を無効化
        if (bodyCollider) bodyCollider.enabled = false;
        if (headCollider) headCollider.enabled = false;

        // 3. 死亡アニメーション
        animator.SetTrigger("Death");
        yield return new WaitForSeconds(deathAnimTime);

        // 4. VFX再生
        if (deathVFX)
        {
            deathVFX.transform.SetParent(null);
            deathVFX.gameObject.SetActive(true);
            deathVFX.Play();
        }
        yield return new WaitForSeconds(deathVfxTime);

        // 5. アイテムドロップ
        DropAmmo(ammoPrefab);

        // 6. ★重要：SetActive(false)をせず、見た目（Renderer）だけを消す
        // オブジェクトを消すとコルーチンが止まってしまうため
        SetAppearance(false);

        // 7. 指定時間待機
        Debug.Log($"{autoRespawnDelay}秒後に復活します...");
        yield return new WaitForSeconds(autoRespawnDelay);

        // 8. EnemyBaseのRespawnを呼ぶ
        // ※EnemyBase.Respawn()内で座標リセットやHP回復、SetActive(true)が行われます
        Respawn();
    }

    // 見た目を一括で切り替える補助関数
    private void SetAppearance(bool visible)
    {
        // 自身と子供のRendererをすべて切り替える
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = visible;

        // HPバー等のUIも連動させる
        if (hpUIRoot) hpUIRoot.SetActive(visible);
    }

    protected override void OnRespawn()
    {
        // 見た目を元に戻す
        SetAppearance(true);

        // アニメーションを初期状態へ
        animator.Rebind();
        animator.Update(0f);

        // 当たり判定を戻す
        if (bodyCollider) bodyCollider.enabled = true;
        if (headCollider) headCollider.enabled = true;

        // VFXを回収して再利用可能に
        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(false);
            deathVFX.transform.SetParent(transform);
            deathVFX.transform.localPosition = Vector3.zero;
        }

        Debug.Log("ゾンビがリスポーンしました");
    }

    // Animation Event用
    public void PlayRoarSE() => SoundManager.Instance?.PlaySE("SE_Enemy_01");
    public void PlayWalkSE() => SoundManager.Instance?.PlaySE("SE_Enemy_07");
}