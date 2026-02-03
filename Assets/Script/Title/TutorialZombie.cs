using UnityEngine;
using UnityEngine.VFX;
using System.Collections;
/// <summary>
/// チュートリアル用の的あてスクリプト
/// </summary>
public class TutorialZombie : EnemyBase
{
    [Header("Hit Colliders（部位別当たり判定）")]
    [SerializeField] private Collider bodyCollider; // 体の判定
    [SerializeField] private Collider headCollider; // 頭の判定

    [Header("Death Settings（死亡演出）")]
    [SerializeField] private float deathAnimTime = 2f; // 倒れるアニメーションの時間
    [SerializeField] private float deathVfxTime = 0.5f; // エフェクトが出てから消えるまでの時間
    [SerializeField] private float autoRespawnDelay = 5f; // 死亡から復活までの待機時間
    [SerializeField] private VisualEffect deathVFX;   // 死亡時の消滅エフェクト

    [Header("Drop Items")]
    [SerializeField] private GameObject ammoPrefab;  // 倒した時に落とす弾薬

    // =====================================================
    // ダメージ計算（EnemyBaseからの抽象メソッド実装）
    // =====================================================
    protected override float CalculateDamage(float baseDamage, Collider hitPart)
    {
        // 被弾アニメーションを再生
        animator.SetBool("isDamage", true);

        // 0.3秒後に被弾フラグを戻す（多重再生防止）
        Invoke(nameof(ResetDamageAnim), 0.3f);

        // チュートリアル：もし頭（headCollider）に当たったらダメージを2倍にする
        if (hitPart == headCollider)
        {
            Debug.Log("ヘッドショット！ダメージ2倍");
            return baseDamage * 2f;
        }

        return baseDamage;
    }

    private void ResetDamageAnim()
    {
        if (!isDead)
            animator.SetBool("isDamage", false);
    }

    // =====================================================
    // 死亡処理（EnemyBaseからの抽象メソッド実装）
    // =====================================================
    protected override void Die()
    {
        // すでに死亡処理中なら何もしない
        if (isDead) return;

        isDead = true;

        // 死亡演出から復活までの一連の流れ（シーケンス）を開始
        StartCoroutine(DeathAndRespawnSequence());
    }

    /// <summary>
    /// 死亡アニメーション → エフェクト発生 → 非表示 → 5秒待機 → 復活
    /// </summary>
    private IEnumerator DeathAndRespawnSequence()
    {
        // 1. ナビメッシュエージェントを止める
        if (agent && agent.enabled && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // 2. 当たり判定を消す（死体に弾が当たらないようにする）
        if (bodyCollider) bodyCollider.enabled = false;
        if (headCollider) headCollider.enabled = false;

        // 3. 死亡アニメーション再生
        animator.SetTrigger("Death");

        // 4. アニメーションが終わるまで待機
        yield return new WaitForSeconds(deathAnimTime);

        // 5. 消滅エフェクト（VFX）の再生
        if (deathVFX)
        {
            deathVFX.transform.SetParent(null); // 本体が消えてもエフェクトが残るように親子関係を解除
            deathVFX.gameObject.SetActive(true);
            deathVFX.Reinit();
            deathVFX.Play();
        }

        // 6. エフェクト演出分だけ少し待つ
        yield return new WaitForSeconds(deathVfxTime);

        // 7. アイテムドロップ
        DropAmmo(ammoPrefab);

        // 8. いったん非表示にする
        gameObject.SetActive(false);

        //ここで指定時間待機してから復活
        Debug.Log($"{autoRespawnDelay}秒後に復活します...");
        yield return new WaitForSeconds(autoRespawnDelay);

        // 9. EnemyBaseで定義されている復活処理を呼び出す
        Respawn();
    }

    // =====================================================
    // 復活時の初期化（EnemyBaseからの抽象メソッド実装）
    // =====================================================
    protected override void OnRespawn()
    {
        // アニメーションの状態をリセット
        animator.Rebind();
        animator.Update(0f);

        // 当たり判定を復活させる
        if (bodyCollider) bodyCollider.enabled = true;
        if (headCollider) headCollider.enabled = true;

        // VFXを自分の子要素に戻して再利用できるようにする
        if (deathVFX)
        {
            deathVFX.gameObject.SetActive(false);
            deathVFX.transform.SetParent(transform);
            deathVFX.transform.localPosition = Vector3.zero;
            deathVFX.transform.localRotation = Quaternion.identity;
        }

        Debug.Log("ゾンビが再配置されました。");
    }

    // =====================================================
    // Animation Event 用（アニメーションの特定のタイミングで音を鳴らす）
    // =====================================================
    public void PlayRoarSE() => SoundManager.Instance?.PlaySE("SE_Enemy_01");
    public void PlayHitSE() => SoundManager.Instance?.PlaySE("SE_Enemy_05");
    public void PlayWalkSE() => SoundManager.Instance?.PlaySE("SE_Enemy_07");
}
