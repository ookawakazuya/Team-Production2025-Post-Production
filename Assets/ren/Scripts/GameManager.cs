using UnityEngine;

/// <summary>
/// ゲーム全体の管理を行うクラス。
/// 主に「スタート地点」と「現在のリスポーン地点（チェックポイント）」を保持する。
/// チェックポイント到達時にリスポーン地点を更新し、
/// プレイヤー死亡時に呼ばれることでリスポーン位置を提供する。
/// </summary>
public class GameManager : MonoBehaviour
{
    // シングルトンインスタンス（どこからでもアクセス可能）
    public static GameManager Instance;

    [Header("初期スタート地点（空オブジェクトを指定）")]
    public Transform startPoint;

    // 現在のリスポーン地点
    private Transform currentRespawnPoint;

    private void Awake()
    {
        // シングルトン化処理
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // スタート地点を初期リスポーンに設定（プレイヤーの位置は動かさない）
        currentRespawnPoint = startPoint;
        Debug.Log($"🎮 ゲーム開始：スタート地点を設定 ({startPoint.name})");
    }

    /// <summary>
    /// 現在のリスポーン地点を返す（死亡時に使用）
    /// </summary>
    public Transform GetRespawnPoint()
    {
        return currentRespawnPoint;
    }

    /// <summary>
    /// チェックポイント到達時に呼び出される。
    /// リスポーン地点を新しいチェックポイントに更新する。
    /// </summary>
    public void UpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
        Debug.Log($"✅ チェックポイント更新：{newPoint.name}");
    }
}

