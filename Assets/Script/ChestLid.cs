using UnityEngine;

public class ChestLid : MonoBehaviour
{
    HingeJoint joint;
    public bool isBeingInteracted = false;

    [Header("角度設定")]
    [SerializeField] float Min = 0f;            //閉じている時の角度
    [SerializeField] float stayOpen = -110f;    //これより開くと開いたままにする角度

    [SerializeField] Transform rayAnchorPoint;  //レイが吸着するポイント
    
    public Transform RayAnchorpoint => rayAnchorPoint;

     void Start()
    {
        joint = GetComponent<HingeJoint>();
    }

    private void Update()
    {
        //操作中でない、かつJointが存在する場合の自動処理
        if (!isBeingInteracted && joint != null)
        {
            JointSpring spring = joint.spring;

            //もし現在のtargetPositionが壊れていたら、現在の角度でリセット
            if (!float.IsFinite(spring.targetPosition))
            {
                spring.targetPosition = joint.angle;
            }

            //蓋の状態による自動戻り処理
            if (joint.angle > stayOpen) 
            {
                //まだ完全に開ききっていないなら、閉じる方向に戻す
                if(Mathf.Abs(spring.targetPosition - Min) > 0.1f)
                {
                    spring.targetPosition = Min;
                    joint.spring = spring;
                    joint.useSpring = true;
                }
            }
            else
            {
                //角度上限が超えている場合、その場でとどまるようにする。
                if(Mathf.Abs(spring.targetPosition - joint.angle) > 1.0f)
                {
                    spring.targetPosition = joint.angle;
                    joint.spring = spring;
                }
            }
        }
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
        }
    }


    public void StopInteracting()
    {
        isBeingInteracted = false;
    }
}
