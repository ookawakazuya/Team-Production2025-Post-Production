using UnityEngine;

public class ChestLid : MonoBehaviour
{
    HingeJoint joint;
    float initialX; //操作時のコントローラの高さ

    public bool isBeingInteracted = false;

    [SerializeField] float Min = 0f;
    [SerializeField] float stayOpen = -110f;

    [SerializeField] Transform rayAnchorPoint;  //レイが吸着するポイント
    [SerializeField] float openSpeed = 100f;
    [SerializeField] float minAngle = 0f;
    [SerializeField] float maxAngle = -110f;

    private float currentAngle = 0f;
    
    public Transform RayAnchorpoint => rayAnchorPoint;

     void Start()
    {
        joint = GetComponent<HingeJoint>();
    }

    private void Update()
    {
        if (!isBeingInteracted && joint != null) 
        {
            JointSpring spring = joint.spring;

            if (joint.angle > stayOpen)
            {

                //最小値に向かって戻る。
                if (spring.targetPosition != Min)
                {
                    spring.targetPosition = Min;
                    joint.spring = spring;
                    joint.useSpring = true;
                }
            }
            else
            {
                spring.targetPosition = joint.angle;
                joint.spring = spring;
            }

        }
    }

    //コントローラの上下移動量による蓋の回転
    public void UpdateRotation(float deltaY)
    {
        /*
        isBeingInteracted = true;
        //感度調整
        float sensitivity = 150f;

        JointSpring spring = joint.spring;

        float invertedDeltaY = deltaY * -1f;

        // 反転させた移動量を使って目標角度を計算
        float newTarget = spring.targetPosition + (invertedDeltaY * sensitivity);

        spring.targetPosition = Mathf.Clamp(newTarget, joint.limits.min, joint.limits.max);

        joint.spring = spring;
        joint.useSpring = true;*/

        // 異常値ガード
        if (float.IsNaN(deltaY) || float.IsInfinity(deltaY))
        {
            Debug.LogWarning("ChestLid: deltaY が異常値のため処理を中断");
            return;
        }


        isBeingInteracted = true;
        float sensitivity = 100;
        JointSpring spring = joint.spring;

        float invertedDeltaY = deltaY * -1f;
        float newTarget = spring.targetPosition + (invertedDeltaY * sensitivity);

        // ログを追加して、数値が変化しているか確認
        // Debug.Log($"DeltaY: {deltaY} | NewTarget: {newTarget}");

        spring.targetPosition = Mathf.Clamp(newTarget, joint.limits.min, joint.limits.max);
        joint.spring = spring;
        joint.useSpring = true;
    }


    public void StopInteracting()
    {
        isBeingInteracted = false;
    }
}
