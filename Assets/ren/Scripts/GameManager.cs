using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体管理クラス。
/// EnemyController の見た目表示距離を管理し、プレイヤーから遠い敵は見た目を消す。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("初期スタート地点")]
    public Transform startPoint;

    private Transform currentRespawnPoint;

    [Header("Enemy管理")]
    public List<EnemyController> enemies = new List<EnemyController>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        currentRespawnPoint = startPoint;

        if (enemies.Count == 0)
            enemies.AddRange(FindObjectsOfType<EnemyController>());
    }

    private void Update()
    {
        UpdateEnemiesVisibility();
    }

    private void UpdateEnemiesVisibility()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        foreach (var enemy in enemies)
        {
            if (enemy == null) continue;

            float distance = Vector3.Distance(enemy.transform.position, player.position);
            bool shouldShow = distance <= enemy.visibleDistance;

            Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                rend.enabled = shouldShow;
            }
        }
    }

    public Transform GetRespawnPoint() => currentRespawnPoint;

    public void UpdateRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
        Debug.Log($"チェックポイント更新：{newPoint.name}");
    }
}
