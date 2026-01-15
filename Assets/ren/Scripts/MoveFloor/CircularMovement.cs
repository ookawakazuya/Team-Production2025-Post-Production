using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CircularMovement : MonoBehaviour
{
    [Header("円運動パラメータ")]
    public float radius = 3f;     // 円の半径
    public float speed = 1f;      // 角速度（ラジアン/秒）

    private Rigidbody rb;
    private float angle;          // 現在の角度
    private Vector3 centerPos;    // 円の中心座標

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; // 必要に応じて無効化
        rb.constraints = RigidbodyConstraints.FreezePositionY; // 高さを固定する場合

        // 現在位置を中心からの相対位置として扱う
        centerPos = transform.position - transform.right * radius;

        // 現在の角度を初期化
        angle = Mathf.Atan2(transform.position.z - centerPos.z, transform.position.x - centerPos.x);
    }

    void FixedUpdate()
    {
        // 時間経過で角度を進める
        angle += speed * Time.fixedDeltaTime;

        // 新しい位置を計算
        float x = centerPos.x + Mathf.Cos(angle) * radius;
        float z = centerPos.z + Mathf.Sin(angle) * radius;
        Vector3 newPos = new Vector3(x, transform.position.y, z);

        // Rigidbodyで移動（補間的にスムーズ）
        rb.MovePosition(newPos);

        // 進行方向を向かせたい場合：
        Vector3 dir = (newPos - transform.position).normalized;
        if (dir.sqrMagnitude > 0)
            rb.MoveRotation(Quaternion.LookRotation(dir));
    }
}
