// GameManager.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体管理クラス
/// - シングルトン
/// - Enemy の登録管理（Start時に自動登録）
/// - プレイヤーからの距離に応じて Enemy のアクティブ/非アクティブを切り替える（見た目や処理負荷軽減）
///   -> 実際のレンダリング ON/OFF は EnemyController 側で行う（EnemyController.SetEnabled）
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("初期スタート地点")]
    [Tooltip("プレイヤーのリスポーン起点")]
    public Transform startPoint;

    Transform currentRespawnPoint;

    [Header("Enemy管理")]
    [Tooltip("シーン内の Enemy を自動登録（空の場合）")]
    public List<EnemyController> enemies = new List<EnemyController>();

    [Header("最適化設定")]
    [Tooltip("この距離より離れている敵は非アクティブ扱いにする")]
    public float enemyActiveDistance = 60f;

    Transform player;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        currentRespawnPoint = startPoint;

        if (enemies.Count == 0)
            enemies.AddRange(FindObjectsOfType<EnemyController>());

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    void Update()
    {
        if (player == null) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        UpdateEnemiesActiveState();
    }

    /// <summary>
    /// 全 Enemy に対してプレイヤー距離に基づき有効/無効を指示する。
    /// - 無効時は EnemyController.SetEnabled(false) を呼ぶ（内部でレンダリングOFFや NavAgent の停止などを実施）
    /// </summary>
    void UpdateEnemiesActiveState()
    {
        float sqrThreshold = enemyActiveDistance * enemyActiveDistance;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            var e = enemies[i];
            if (e == null)
            {
                enemies.RemoveAt(i);
                continue;
            }

            float sqrDist = (e.transform.position - player.position).sqrMagnitude;
            bool shouldBeActive = sqrDist <= sqrThreshold;
            e.SetEnabled(shouldBeActive);
        }
    }

    public Transform GetRespawnPoint() => currentRespawnPoint;

    public void UpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
        Debug.Log($"チェックポイント更新：{newPoint.name}");
    }

    /// <summary>
    /// 動的に Enemy を登録したい場合に呼ぶ（Spawner などから）
    /// </summary>
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void UnregisterEnemy(EnemyController enemy)
    {
        if (enemies.Contains(enemy))
            enemies.Remove(enemy);
    }
}
