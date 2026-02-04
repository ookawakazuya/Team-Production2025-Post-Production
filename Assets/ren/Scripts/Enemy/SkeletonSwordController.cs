using System.Collections;
using UnityEngine;

/// <summary>
/// Skeleton の剣制御（親の Animator に付ける）
/// ・AnimationEvent で剣を振り上げ／振り下ろし
/// ・振り下ろし後に初期角度にリセット
/// </summary>
public class SkeletonSwordController : MonoBehaviour
{
    [Header("剣の Transform（子オブジェクト）")]
    [SerializeField] private Transform sword;

    [Header("剣を振る角度")]
    [SerializeField] private Vector3 swingUpRotation = new Vector3(-60f, 0f, 0f);
    [SerializeField] private Vector3 swingDownRotation = Vector3.zero;

    [Header("剣を振るスピード")]
    [SerializeField] private float swingSpeed = 10f;

    private Quaternion targetRotation;
    private Quaternion initialRotation; // 初期角度を保持

    private void Awake()
    {
        if (sword == null)
        {
            Debug.LogError("剣の Transform が設定されていません！");
            enabled = false;
            return;
        }

        // 初期角度を保存
        initialRotation = sword.localRotation;
        targetRotation = initialRotation;
    }

    private void Update()
    {
        // 剣だけターゲット角度に向かって滑らかに回転
        sword.localRotation = Quaternion.Lerp(sword.localRotation, targetRotation, Time.deltaTime * swingSpeed);
    }

    // ==========================
    // AnimationEvent 用
    // ==========================

    /// <summary>
    /// 剣を振り上げる
    /// </summary>
    public void SwingUp()
    {
        targetRotation = Quaternion.Euler(swingUpRotation);
    }

    /// <summary>
    /// 剣を振り下ろす
    /// </summary>
    public void SwingDown()
    {
        targetRotation = Quaternion.Euler(swingDownRotation);

        // 振り下ろし完了後に初期角度に戻す（次フレーム以降）
        StartCoroutine(ResetAfterSwing());
    }

    IEnumerator ResetAfterSwing()
    {
        // 剣がターゲット角度に到達するまで待つ
        yield return new WaitForSeconds(0.2f); // スイングアニメーションの長さに合わせる

        targetRotation = initialRotation;
    }
}
