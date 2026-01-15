using UnityEngine;

/// <summary>
/// 水平方向（XZ平面）で八の字（∞）に動く床
/// 設置した position を中心に移動する
/// </summary>
public class MovingPlatformFigure8 : MonoBehaviour
{
    [Header("移動設定")]
    [SerializeField] private float rangeX = 3f;     // 横方向の大きさ
    [SerializeField] private float rangeZ = 3f;     // 奥行き方向の大きさ
    [SerializeField] private float speed = 1.5f;    // 移動速度

    private Vector3 startPosition;

    void Start()
    {
        // 設置時の位置を基準にする
        startPosition = transform.position;
    }

    void Update()
    {
        float t = Time.time * speed;

        // 八の字（∞）カーブ
        float x = Mathf.Sin(t) * rangeX;
        float z = Mathf.Sin(t * 2f) * rangeZ * 0.5f;

        transform.position = new Vector3(
            startPosition.x + x,
            startPosition.y,
            startPosition.z + z
        );
    }
}
