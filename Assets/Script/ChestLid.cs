using UnityEngine;

public class ChestLid : MonoBehaviour
{
    HingeJoint joint;
    float initialX; //操作時のコントローラの高さ

    public bool isBeingInteracted = false;

    [SerializeField] float Min = 0f;

     void Start()
    {
        joint = GetComponent<HingeJoint>();
    }

    private void Update()
    {
        if (!isBeingInteracted && joint != null) 
        {
            JointSpring spring = joint.spring;

            //最小値に向かって戻る。
            if (spring.targetPosition != Min)
            {
                spring.targetPosition = Min;
                joint.spring = spring;
                joint.useSpring = true;
            }

        }
    }

    //コントローラの上下移動量による蓋の回転
    public void UpdateRotation(float deltaY)
    {
        //感度調整
        float sensitivity = 150f;

        JointSpring spring = joint.spring;

        float invertedDeltaY = deltaY * -1f;

        // 反転させた移動量を使って目標角度を計算
        float newTarget = spring.targetPosition + (invertedDeltaY * sensitivity);

        spring.targetPosition = Mathf.Clamp(newTarget, joint.limits.min, joint.limits.max);

        joint.spring = spring;
        joint.useSpring = true;
    }
}
