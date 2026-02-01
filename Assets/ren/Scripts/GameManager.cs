using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

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

    // 宝箱が取得された時に実行されるイベント
    public static event Action OnTreasureCollected;

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

    // ★ EnemyController → EnemyBase
    private readonly List<EnemyBase> enemies = new();

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

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
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

        //チェックポイントをリセット
        ResetAllCheckpoints();
    }

    private void FindPlayerAndEnemies()
    {
        enemies.Clear();

        //EnemyBase を取得（Zombie / Skeleton 共通）
        enemies.AddRange(UnityEngine.Object.FindObjectsByType<EnemyBase>(FindObjectsInactive.Include,FindObjectsSortMode.None));

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
    public void RegisterEnemy(EnemyBase enemy)
    {
        if (enemy == null) return;

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

        ClearAllAmmo();
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
            PlayerPrefs.SetInt($"Stage_{stage}_{index}", 1);
            PlayerPrefs.Save();
            Debug.Log($"{stage} 宝箱 {index} 取得");
            OnTreasureCollected?.Invoke();
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

    // Ammo 管理
    // =========================
    private readonly List<GameObject> spawnedAmmo = new();

    public void RegisterAmmo(GameObject ammo)
    {
        if (!ammo) return;

        if (!spawnedAmmo.Contains(ammo))
            spawnedAmmo.Add(ammo);
    }

    public void ClearAllAmmo()
    {
        foreach (var ammo in spawnedAmmo)
        {
            if (ammo)
                Destroy(ammo);
        }

        spawnedAmmo.Clear();
    }

    private void ResetAllCheckpoints()
    {
        var checkpoints = UnityEngine.Object.FindObjectsByType<Checkpoint>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        foreach (var cp in checkpoints)
        {
            cp.ResetCheckpoint();
        }
    }
}


