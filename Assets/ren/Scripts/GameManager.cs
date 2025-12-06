using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体を管理するクラス
/// - 全シーン共通のシングルトン
/// - 敵の登録・復活管理
/// - プレイヤーのリスポーン地点管理
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("初期スタート地点")]
    [SerializeField] private Transform startPoint;

    /// <summary> 現在のリスポーン地点 </summary>
    private Transform currentRespawnPoint;

    /// <summary> 現在ロードされているシーンの全敵リスト </summary>
    private readonly List<EnemyController> enemies = new List<EnemyController>();

    /// <summary> プレイヤーの参照 </summary>
    private Transform player;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // シーン切り替えのたびに敵・プレイヤーを再検出
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        currentRespawnPoint = startPoint;
        FindPlayerAndEnemies();
    }

    private void Update()
    {
        // 途中生成にも対応（フェイルセーフ）
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    /// <summary> シーンロード時に呼ばれる </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindPlayerAndEnemies();
    }

    /// <summary> 現在のシーンにいるプレイヤーと敵を検出し登録し直す </summary>
    private void FindPlayerAndEnemies()
    {
        enemies.Clear();
        enemies.AddRange(FindObjectsOfType<EnemyController>());
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    /// <summary> リスポーン地点を取得 </summary>
    public Transform GetRespawnPoint() => currentRespawnPoint;

    /// <summary> チェックポイント通過でリスポーン地点を更新 </summary>
    public void UpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
        Debug.Log($"チェックポイント更新：{newPoint.name}");
    }

    /// <summary> 動的生成された敵を登録 </summary>
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    /// <summary> プレイヤー死亡 → リスポーン時に敵を全再生・再アクティブ化 </summary>
    public void RespawnAllEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.Respawn();
        }
    }
}
