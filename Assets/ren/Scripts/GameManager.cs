using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ゲーム全体を管理するクラス
/// ・全シーン共通シングルトン
/// ・敵管理（登録 / 全リスポーン）
/// ・プレイヤー参照管理
/// ・リスポーン地点管理
/// ・ステージごとの宝箱取得管理
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // =========================
    // リスポーン管理
    // =========================
    [Header("ステージ開始地点")]
    [SerializeField] private Transform startPoint;

    private Transform currentRespawnPoint;

    // =========================
    // プレイヤー / 敵管理
    // =========================
    private Transform player;
    private readonly List<EnemyController> enemies = new();

    // =========================
    // 宝箱管理
    // =========================
    private Dictionary<StageID, bool[]> stageTreasureData;

    // =========================
    // 初期化
    // =========================
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeTreasureData();
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        // フェイルセーフ（途中生成・破棄対策）
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // =========================
    // シーンロード時
    // =========================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // ステージ開始地点をシーンから再取得
        GameObject start = GameObject.FindGameObjectWithTag("StageStart");
        if (start != null)
        {
            startPoint = start.transform;
            currentRespawnPoint = startPoint;
        }
        else
        {
            Debug.LogWarning("StageStart が見つかりません");
        }

        FindPlayerAndEnemies();
    }

    private void FindPlayerAndEnemies()
    {
        enemies.Clear();
        enemies.AddRange(FindObjectsOfType<EnemyController>());

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    // =========================
    // リスポーン地点管理
    // =========================
    public Transform GetRespawnPoint()
    {
        return currentRespawnPoint;
    }

    public void UpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
        Debug.Log($"チェックポイント更新：{newPoint.name}");
    }

    // =========================
    // 敵管理
    // =========================
    public void RegisterEnemy(EnemyController enemy)
    {
        if (!enemies.Contains(enemy))
            enemies.Add(enemy);
    }

    public void RespawnAllEnemies()
    {
        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;
            enemy.Respawn();
        }
    }

    // =========================
    // プレイヤー死亡時処理
    // =========================
    public void OnPlayerDead()
    {
        Debug.Log("GameManager : プレイヤー死亡検知");

        // 敵を全リスポーン
        RespawnAllEnemies();

        // 将来拡張用
        // ・ギミック初期化
        // ・タイマーリセット
    }

    // =========================
    // 宝箱管理
    // =========================
    private void InitializeTreasureData()
    {
        stageTreasureData = new Dictionary<StageID, bool[]>();

        foreach (StageID stage in System.Enum.GetValues(typeof(StageID)))
        {
            stageTreasureData[stage] = new bool[3]; // 宝箱3つ
        }
    }

    public void CollectTreasure(StageID stage, int index)
    {
        if (!stageTreasureData[stage][index])
        {
            stageTreasureData[stage][index] = true;
            Debug.Log($"{stage} 宝箱 {index} 取得");
        }
    }

    public bool[] GetTreasureState(StageID stage)
    {
        return stageTreasureData[stage];
    }

    // =========================
    // タイトル戻り用
    // =========================
    public void ResetAllTreasure()
    {
        InitializeTreasureData();
        Debug.Log("宝箱取得数をリセットしました");
    }
}
