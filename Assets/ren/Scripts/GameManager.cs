using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    private Transform currentRespawnPoint;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ÉQÅ[ÉÄä‘Ç≈ï€éùÇµÇΩÇ¢èÍçá
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetRespawnPoint(Transform newPoint)
    {
        currentRespawnPoint = newPoint;
    }

    public void RespawnPlayer(GameObject player)
    {
        if (currentRespawnPoint != null)
        {
            player.transform.position = currentRespawnPoint.position;
        }
        else
        {
            Debug.LogWarning("Respawn point is not set!");
        }
    }
}