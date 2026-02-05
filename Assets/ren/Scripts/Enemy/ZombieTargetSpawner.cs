using UnityEngine;
using System.Collections;

public class ZombieTargetSpawner : MonoBehaviour
{
    [Header("生成するゾンビPrefab")]
    [SerializeField] private GameObject zombiePrefab;

    [Header("再生成までの待ち時間")]
    [SerializeField] private float respawnDelay = 0.5f;

    private GameObject currentZombie;


    void Start()
    {
        SpawnZombie();
    }


    void SpawnZombie()
    {
        if (!zombiePrefab) return;

        // このオブジェクトの位置に生成
        currentZombie = Instantiate(
            zombiePrefab,
            transform.position,
            transform.rotation
        );

        // 死亡監視用
        EnemyBase enemy = currentZombie.GetComponent<EnemyBase>();

        if (enemy)
        {
            enemy.OnDead += OnZombieDead;
        }
    }


    void OnZombieDead()
    {
        StartCoroutine(RespawnCoroutine());
    }


    IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        SpawnZombie();
    }
}
