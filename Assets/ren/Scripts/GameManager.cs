using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("初期リスポーン地点")]
    [SerializeField] private Transform initialRespawnPoint;

    private Transform currentRespawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        currentRespawnPoint = initialRespawnPoint;
    }

    /// <summary>
    /// 現在のリスポーン地点を設定
    /// </summary>
    public void SetRespawnPoint(Transform point)
    {
        currentRespawnPoint = point;
        Debug.Log($"リスポーン地点更新: {point.name}");
    }

    /// <summary>
    /// 現在のリスポーン地点を取得
    /// </summary>
    public Transform GetRespawnPoint()
    {
        return currentRespawnPoint;
    }

    /// <summary>
    /// 初期リスポーン地点を取得
    /// </summary>
    public Transform GetInitialRespawnPoint()
    {
        return initialRespawnPoint;
    }
}
