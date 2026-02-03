using UnityEngine;
using System.Collections;

/// <summary>
/// チュートリアル用：宝箱の蓋の挙動を制御するスクリプト
/// 完全に開くと5秒待機してから自動で閉じ、再び開けられるようになります。
/// </summary>
public class ChestLid_Tutorial : MonoBehaviour
{
    private HingeJoint joint;
    private Rigidbody rb;

    [Header("状態フラグ")]
    public bool isBeingInteracted = false; // 操作中かどうか
    private bool isWaitingToClose = false; // 閉じ待ち状態（5秒タイマー中）か

    [Header("角度設定")]
    [SerializeField] float minAngle = 0f;        // 閉じている時の角度
    [SerializeField] float stayOpenAngle = -120f; // これより開くと「全開」とみなす角度

    [Header("インターフェース設定")]
    [SerializeField] Transform rayAnchorPoint;
    public float interactionRadius = 5.0f;

    [Header("エフェクト・音")]
    [SerializeField] GameObject chestParticle;

    private bool hasPlayedOpenSE = false;

    void Start()
    {
        // コンポーネントの取得
        joint = GetComponent<HingeJoint>();
        rb = GetComponent<Rigidbody>();

        // 初期状態はバネ（Spring）を有効にして閉じる方向に力を働かせる
        ResetJointSpring();
    }

    private void Update()
    {
        // 自動で閉じるのを待機している間、または操作中はUpdateでの自動戻り処理をスキップ
        if (isWaitingToClose || isBeingInteracted) return;

        if (joint != null)
        {
            // 蓋が一定以上開いたら「全開固定処理」へ
            if (joint.angle <= stayOpenAngle)
            {
                StartCoroutine(AutoCloseSequence());
                return;
            }

            // 少し開いているが全開ではない場合、自動で閉じる方向にバネを向ける
            UpdateAutoClosingSpring();
        }

        HandleEffectsAndSound();
    }

    /// <summary>
    /// 全開になった後の「待機 → 閉じる」一連の流れ
    /// </summary>
    private IEnumerator AutoCloseSequence()
    {
        isWaitingToClose = true;
        isBeingInteracted = false;

        // 見た目を全開角度でピタッと止める
        transform.localRotation = Quaternion.Euler(stayOpenAngle, 0, 0);

        // 物理挙動を一時停止（固定）
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SoundManager.Instance?.PlaySE("Chest_Middle");
        Debug.Log("宝箱が全開になりました。5秒後に閉じます。");

        // --- チュートリアル：5秒待機 ---
        yield return new WaitForSeconds(5.0f);

        // 物理挙動を再開
        if (rb != null) rb.isKinematic = false;

        // バネの目標地点を「閉じ」に設定して勢いよく閉める
        ResetJointSpring();

        Debug.Log("宝箱が自動で閉じました。再び開けることが可能です。");

        // 状態をリセット
        isWaitingToClose = false;
        hasPlayedOpenSE = false;
    }

    /// <summary>
    /// バネを「閉じ」の状態にリセットする
    /// </summary>
    private void ResetJointSpring()
    {
        if (joint == null) return;
        JointSpring spring = joint.spring;
        spring.targetPosition = minAngle;
        joint.spring = spring;
        joint.useSpring = true;
    }

    private void UpdateAutoClosingSpring()
    {
        // 少しでも開いていたら、常に最小角度（閉じ）に向かってバネを動かす
        if (joint.angle < minAngle - 1f)
        {
            JointSpring spring = joint.spring;
            if (Mathf.Abs(spring.targetPosition - minAngle) > 0.1f)
            {
                spring.targetPosition = minAngle;
                joint.spring = spring;
                joint.useSpring = true;
            }
        }
    }

    private void HandleEffectsAndSound()
    {
        if (joint == null) return;

        bool isOpen = joint.angle < minAngle - 1f;

        // パーティクルの表示制御
        if (chestParticle != null && chestParticle.activeSelf != isOpen)
        {
            chestParticle.SetActive(isOpen);
        }

        // 開き始めた瞬間の音
        if (isOpen && !hasPlayedOpenSE)
        {
            SoundManager.Instance?.PlaySE("Chest_Open");
            hasPlayedOpenSE = true;
        }
    }

    /// <summary>
    /// コントローラー等の入力で蓋を回転させる（外部から呼び出し）
    /// </summary>
    public void UpdateRotation(float deltaY)
    {
        // 自動閉じ待機中は何もしない
        if (isWaitingToClose || !float.IsFinite(deltaY)) return;

        isBeingInteracted = true;
        float sensitivity = 450f;
        JointSpring spring = joint.spring;

        if (!float.IsFinite(spring.targetPosition)) spring.targetPosition = joint.angle;

        float newTarget = spring.targetPosition + (deltaY * -1f * sensitivity);
        float minL = joint.limits.min;
        float maxL = joint.limits.max;

        spring.targetPosition = Mathf.Clamp(newTarget, Mathf.Min(minL, maxL), Mathf.Max(minL, maxL));
        joint.spring = spring;
        joint.useSpring = true;
    }

    public void StopInteracting()
    {
        if (!isWaitingToClose)
            isBeingInteracted = false;
    }
}