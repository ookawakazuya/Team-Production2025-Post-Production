using UnityEngine;

public class MagmaMove : MonoBehaviour
{
    [Header("Y移動範囲")]
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 0f;

    [Header("動く速さ（大きいほど速い）")]
    [SerializeField] private float moveSpeed = 1f;

    [SerializeField] private float cycleTime = 5f;
    private float startTime;
    private Vector3 startPos;

    void Start()
    {
        startTime = Time.time;
        startPos = transform.position;
    }

    void Update()
    {
        float t = (Mathf.Sin(Time.time * (Mathf.PI * 2f / cycleTime)) + 1f) * 0.5f;

        // Y座標を補間
        float y = Mathf.Lerp(minY, maxY, t);

        transform.position = new Vector3(
            startPos.x,
            y,
            startPos.z
        );
    }
}
