using UnityEngine;

/// <summary>
/// 宝箱（ChestLid）とのインタラクションを制御するクラス。
/// VRControllerから入力を受け取り、ChestLidの物理回転を操作します。
/// </summary>
public class ChestInteractor : MonoBehaviour
{
    [Header("設定")]
    [SerializeField] private float chestRayDistance = 3.0f; // 宝箱に届く距離
    [SerializeField] private string chestTag = "Chest";     // 判定用タグ

    private ChestLid currentChestLid;    // 現在操作中の蓋
    private float lastHandY;             // 前フレームの手の高さ

    /// <summary>
    /// 宝箱操作のメインロジック。
    /// VRControllerのUpdateから呼ばれ、操作中なら true を返します。
    /// </summary>
    public bool HandleChestInteraction(Transform rayOrigin, bool isTriggerPressed)
    {
        // 1. トリガーが押された瞬間に宝箱を探す
        if (isTriggerPressed && currentChestLid == null)
        {
            CheckForChest(rayOrigin);
        }

        // 2. 操作継続中の処理
        if (currentChestLid != null)
        {
            if (isTriggerPressed)
            {
                // 前フレームからの高さの差分(deltaY)を計算して渡す
                float currentHandY = rayOrigin.position.y;
                float deltaY = currentHandY - lastHandY;

                // ChestLid側のメソッド名「UpdateRotation」に合わせて呼び出し
                currentChestLid.UpdateRotation(deltaY);

                // 現在の値を保存
                lastHandY = currentHandY;
                return true; // 操作中フラグを返す
            }
            else
            {
                // 3. トリガーを離したら操作終了
                currentChestLid.StopInteracting();
                currentChestLid = null;
            }
        }

        return false;
    }

    /// <summary>
    /// 前方に宝箱（ChestLid）があるかレイで判定する
    /// </summary>
    private void CheckForChest(Transform rayOrigin)
    {
        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, chestRayDistance))
        {
            // タグ判定、または直接コンポーネント取得を試みる
            if (hit.collider.CompareTag(chestTag) || hit.collider.GetComponent<ChestLid>())
            {
                ChestLid lid = hit.collider.GetComponentInParent<ChestLid>();
                if (lid != null)
                {
                    currentChestLid = lid;
                    lastHandY = rayOrigin.position.y; // 開始時の高さを記録
                    Debug.Log("宝箱の操作を開始しました");
                }
            }
        }
    }
}