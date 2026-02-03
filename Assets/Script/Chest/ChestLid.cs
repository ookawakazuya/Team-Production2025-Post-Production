using UnityEngine;

public class ChestLid : MonoBehaviour
{
    HingeJoint joint;
    public bool isBeingInteracted = false;
    private bool isLockedOpen = false; // 最大まで開いたら真にするフラグ


    [Header("角度設定")]
    [SerializeField] float Min = 0f;            //閉じている時の角度
    [SerializeField] float stayOpen = -120f;    //これより開くと開いたままにする角度

    [SerializeField] Transform rayAnchorPoint;  //レイが吸着するポイント
    [SerializeField] public float interactionRadius = 5.0f;

    [Header("エフェクト")]
    [SerializeField] GameObject chestParticle;

    [Header("識別ID")]
    [SerializeField] private int stageID; // 0~3を設定
    [SerializeField] private int chestID; // 0~2を設定

    bool hasPlayedOpenSE = false;
    bool hasPlayedMaxSE = false;


    public Transform RayAnchorpoint => rayAnchorPoint;

    void Start()
    {
        joint = GetComponent<HingeJoint>();

        if (ChestSaveManager.IsChestOpened(stageID, chestID))
        {
            ApplyAlreadyOpenedState();
        }
    }

    private void Update()
    {
        if (isLockedOpen) return;
        //操作中でない、かつJointが存在する場合の自動処理
        if (!isBeingInteracted && joint != null)
        {
            JointSpring spring = joint.spring;

            //もし現在のtargetPositionが壊れていたら、現在の角度でリセット
            if (!float.IsFinite(spring.targetPosition))
            {
                spring.targetPosition = joint.angle;
            }

            if (joint.angle <= stayOpen)
            {

                LockChestOpen();
                return;
            }

            //蓋の状態による自動戻り処理
            if (joint.angle > stayOpen+5f)
            {
                //まだ完全に開ききっていないなら、閉じる方向に戻す
                if (Mathf.Abs(spring.targetPosition - Min) > 0.1f)
                {
                    spring.targetPosition = Min;
                    joint.spring = spring;
                    joint.useSpring = true;
                }
            }
        }

        // パーティクル表示制御
        if (chestParticle != null && joint != null)
        {
            // 少しでも開いていたら表示
            bool isOpen = joint.angle < Min - 1f;

            if (chestParticle.activeSelf != isOpen)
            {
                chestParticle.SetActive(isOpen);
            }

            // 開け始めた瞬間のSE
            if (isOpen && !hasPlayedOpenSE)
            {
                SoundManager.Instance.PlaySE("Chest_Open");
                hasPlayedOpenSE = true;
            }

            // 閉じたらリセット
            if (!isOpen)
            {
                hasPlayedOpenSE = false;
                hasPlayedMaxSE = false;
            }
        }
    }

    // セーブデータがある場合に、演出抜きで即座に全開状態にするメソッド
    private void ApplyAlreadyOpenedState()
    {
        isLockedOpen = true;
        transform.localRotation = Quaternion.Euler(stayOpen, 0, 0);

        if (joint != null)
        {
            var spring = joint.spring;
            spring.targetPosition = stayOpen;
            joint.spring = spring;
            joint.useSpring = true;
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        // UI側にも通知を送る
        ChestEventManager.TriggerChestOpen(stageID, chestID);
    }

    private void LockChestOpen()
    {
        // ロック時の物理角度を強制的に stayOpen に補正して、見た目のズレを直す
        transform.localRotation = Quaternion.Euler(stayOpen, 0, 0); // 軸方向はモデルに合わせて調整してください

        isLockedOpen = true;
        isBeingInteracted = false;

        // HingeJointの動きを物理的に止める設定
        if (joint != null)
        {
            JointSpring spring = joint.spring;
            spring.targetPosition = stayOpen;
            joint.spring = spring;
            joint.useSpring = true;

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;        // 慣性を消す
            rb.angularVelocity = Vector3.zero; // 回転の慣性を消す
        }

        SoundManager.Instance.PlaySE("Chest_Middle");

        // セーブデータを保存
        ChestSaveManager.SaveChestState(stageID, chestID);

        // イベントを発火させてUI等に通知する
        ChestEventManager.TriggerChestOpen(stageID, chestID);

        Debug.Log($"宝箱が全開で固定されました。Stage:{stageID}, Index:{chestID}");
    }


    // コントローラの上下移動量（deltaY）を受け取って蓋を回転させる
    public void UpdateRotation(float deltaY)
    {
        // 入力値が正常（有限）であるかチェック
        if (!float.IsFinite(deltaY)) return;

        isBeingInteracted = true;

        // 腕の振りを1/2以下にするため、感度を高めに設定（調整可能）
        float sensitivity = 450f;
        JointSpring spring = joint.spring;

        // 現在の値が NaN なら現在の角度からリスタート
        if (!float.IsFinite(spring.targetPosition)) spring.targetPosition = joint.angle;

        // コントローラーを上に上げると蓋が開く（マイナス方向）ように計算
        float invertedDeltaY = deltaY * -1f;
        float newTarget = spring.targetPosition + (invertedDeltaY * sensitivity);

        // HingeJointのLimits（-120〜0など）の範囲内にクランプして、異常な値を防ぐ
        float minL = joint.limits.min;
        float maxL = joint.limits.max;

        // 万が一 Min/Max が逆転していてもエラーにならないよう安全策
        float finalTarget = Mathf.Clamp(newTarget, Mathf.Min(minL, maxL), Mathf.Max(minL, maxL));

        // 数値が正常な場合のみ、Jointを更新する
        if (float.IsFinite(finalTarget))
        {
            spring.targetPosition = finalTarget;
            joint.spring = spring;
            joint.useSpring = true;

            float angleBuffer = 5.0f; // 物理角の許容誤差（度）
            if (finalTarget <= stayOpen && joint.angle <= stayOpen + angleBuffer)
            {
                LockChestOpen();
            }
        }
    }


    public void StopInteracting()
    {
        if (!isLockedOpen)
        isBeingInteracted = false;
    }
}
