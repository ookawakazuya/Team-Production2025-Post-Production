using UnityEngine;

public class AmmoTriggerRelay : MonoBehaviour
{
    [SerializeField] private Shotgun shotgun;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Shotgun スクリプトを探して保持
        // shotgun = GetComponentInParent<Shotgun>();
        if (shotgun == null)
        {
            Debug.LogWarning("[AmmoTriggerRelay] Shotgun が親に見つかりません。");
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 何かに接触したら Shotgun に通知
        if (shotgun != null)
        {
            shotgun.OnParentTriggerEnter(other);
            if (other.CompareTag("Ammo")) { Debug.Log("落ちた弾に接触"); }
        }
    }
}
