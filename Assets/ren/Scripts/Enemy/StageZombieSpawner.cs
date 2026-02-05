using UnityEngine;
using System.Collections;

public class StageZombieSpawner : MonoBehaviour
{
    [Header("Zombie Prefab")]
    [SerializeField] private StageZombie zombiePrefab;

    [Header("Respawn Delay")]
    [SerializeField] private float respawnDelay = 5f;

    private void Start()
    {
        // スポナー自身の位置で初期生成
        SpawnZombie(transform.position, transform.rotation);
    }

    private void SpawnZombie(Vector3 pos, Quaternion rot)
    {
        var zombie = Instantiate(zombiePrefab, pos, rot);
        StartCoroutine(MonitorZombie(zombie));
    }

    private IEnumerator MonitorZombie(StageZombie zombie)
    {
        // ゾンビが死ぬまで待つ
        while (!zombie.IsDead)
            yield return null;

        // respawnDelay 後に再生成
        yield return new WaitForSeconds(respawnDelay);

        zombie.ResetZombie();

        // 再び監視
        StartCoroutine(MonitorZombie(zombie));
    }
}
