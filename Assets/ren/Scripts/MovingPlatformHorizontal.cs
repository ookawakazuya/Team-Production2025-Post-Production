using UnityEngine;

/// <summary>
/// 左右に動く床（X軸）
/// 設置した position を基準に ±moveRange で往復
/// </summary>
public class MovingPlatformHorizontal : MonoBehaviour
{
    [SerializeField] private float moveRange = 3f; // 左右の移動幅
    [SerializeField] private float moveSpeed = 2f; // 移動速度

    private Vector3 startPosition;

    void Start()
    {
        // 設置時の位置を保存
        startPosition = transform.position;
    }

    void Update()
    {
        float offset = Mathf.Sin(Time.time * moveSpeed) * moveRange;

        transform.position = new Vector3(
            startPosition.x + offset,
            startPosition.y,
            startPosition.z
        );
    }
}
