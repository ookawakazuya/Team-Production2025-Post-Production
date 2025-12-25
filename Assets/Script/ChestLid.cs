using UnityEngine;

public class ChestLid : MonoBehaviour
{
    HingeJoint joint;
    float initialX; //操作時のコントローラの高さ

     void Start()
    {
        joint = GetComponent<HingeJoint>();
    }

    //コントローラの上下移動量による蓋の回転
   public void UpdateRotation(float deltaY)
    {
        //感度調整
        float sensitivity = 150f;

        JointSpring spring = joint.spring;

        spring.targetPosition = Mathf.Clamp(spring.targetPosition + (deltaY * sensitivity), joint.limits.min, joint.limits.max);
        
        joint.spring = spring;
        joint.useSpring = true;
    }
}
