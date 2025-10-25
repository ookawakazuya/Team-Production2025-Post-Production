using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        // Oキーで初期リスポーン地点に戻る
        if (Input.GetKeyDown(KeyCode.O))
        {
            RespawnToInitial();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy") || other.CompareTag("DeathZone"))
        {
            RespawnToCheckpoint();
        }
    }

    private void RespawnToCheckpoint()
    {
        Transform respawnPoint = GameManager.Instance.GetRespawnPoint();
        MoveTo(respawnPoint);
        Debug.Log("リスポーン地点へ復活！");
    }

    private void RespawnToInitial()
    {
        Transform initialPoint = GameManager.Instance.GetInitialRespawnPoint();
        MoveTo(initialPoint);
        Debug.Log("初期リスポーン地点へ戻りました！");
    }

    private void MoveTo(Transform point)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.position = point.position;
        transform.rotation = point.rotation;
    }
}
