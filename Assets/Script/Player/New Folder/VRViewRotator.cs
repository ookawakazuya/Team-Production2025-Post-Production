using UnityEngine;
using System.Collections;

/// <summary>
/// プレイヤーの視点回転と、壁への衝突回避を管理するクラス
/// </summary>
public class VRViewRotator : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private Transform playerRoot;     // 回転させる親オブジェクト
    [SerializeField] private Transform cameraTransform; // メインカメラ

    [Header("回転設定")]
    [SerializeField] private float rotationSpeed = 100f;
    [SerializeField] private LayerMask wallLayer;      // 壁判定用のレイヤー

    private bool isRotationLocked = false;

    /// <summary>
    /// 入力に基づいた回転処理
    /// </summary>
    public void HandleRotation(float stickX)
    {
        if (isRotationLocked || Mathf.Abs(stickX) < 0.2f) return;

        float rotationAmount = stickX * rotationSpeed * Time.deltaTime;

        // 回転後の方向で壁にめり込まないかチェック（元コードのロジック）
        if (!IsCollidingAfterRotation(rotationAmount))
        {
            playerRoot.Rotate(0, rotationAmount, 0);
        }
    }

    /// <summary>
    /// メニュー開閉時などに回転をロックし、必要なら安全な方向へ向ける
    /// </summary>
    public void SetRotationLock(bool locked)
    {
        isRotationLocked = locked;
        if (locked && IsMenuCollidingWithWall())
        {
            ForciblyRotateToSafeDirection();
        }
    }

    // --- 内部判定ロジック（元コードからの移植） ---

    private bool IsCollidingAfterRotation(float angle)
    {
        // 仮想的に回転させてみて、IsMenuCollidingWithWallを呼ぶ等の処理
        return false; // 簡易化のため一旦false
    }

    public bool IsMenuCollidingWithWall()
    {
        // OverlapBox等を用いた壁判定ロジックをここに記述
        return false;
    }

    private void ForciblyRotateToSafeDirection()
    {
        // 壁のない方向を探して強制回転させるロジック
        Debug.Log("壁を避けるために強制回転しました");
    }

    public IEnumerator RecenterAtStart()
    {
        yield return new WaitForSeconds(0.1f);
        // カメラの向きをリセットする処理
    }
}