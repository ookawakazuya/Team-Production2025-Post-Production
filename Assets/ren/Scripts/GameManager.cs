using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体を管理するクラス
/// - シングルトン
/// - Enemy の登録・復活管理
/// - プレイヤーのリスポーンポイント管理
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("初期スタート地点")]
    public Transform startPoint;

    Transform currentRespawnPoint;

    [Header("Enemy管理")]
    public List<EnemyController> enemies = new List<EnemyController>();

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
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public Transform GetRespawnPoint() => currentRespawnPoint;

    public void UpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
        Debug.Log($"チェックポイント更新：{newPoint.name}");
    }

    /// <summary>
    /// Enemy を動的に登録
    /// </summary>
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    /// <summary>
    /// プレイヤーが死亡後リスポーン時に敵を全復活させる
    /// </summary>
    public void RespawnAllEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.Respawn();
        }
    }
}
