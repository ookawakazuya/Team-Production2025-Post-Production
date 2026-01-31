using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{
    private Shotgun shotgun;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // 子オブジェクトの Shotgun スクリプトを探して保持
        shotgun = GetComponentInChildren<Shotgun>();
        if (shotgun == null)
        {
            Debug.LogWarning("[AmmoTriggerRelay] Shotgun が子に見つかりません。");
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
