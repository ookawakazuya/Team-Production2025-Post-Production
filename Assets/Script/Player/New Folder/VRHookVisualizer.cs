using UnityEngine;

/// <summary>
/// フックの見た目（ライン、モデル、エフェクト）の制御に特化したクラス
/// </summary>
public class VRHookVisualizer : MonoBehaviour
{
    [Header("LineRenderer設定")]
    [SerializeField] private LineRenderer commonLine;          // メインの紐
    [SerializeField] private Material aimMaterial;             // 狙っている時の色
    [SerializeField] private Material hookMaterial;            // 刺さっている時の色

    [Header("3Dモデル設定")]
    [SerializeField] private Transform hookObject;             // フックの先端モデル
    [SerializeField] private GameObject normalHookModel;       // 通常（手元）のモデル
    [SerializeField] private GameObject flyingHookModel;       // 射出中・刺さっている時のモデル
    [SerializeField] private float hookScaleOrigin = 0.5f;     // 通常時のサイズ
    [SerializeField] private float hookScaleActive = 5.0f;     // 動作時のサイズ

    [Header("ワイヤー実体オブジェクト")]
    [SerializeField] private GameObject rayVisualObject;       // 太さのあるワイヤーモデル

    [Header("エフェクト")]
    [SerializeField] private ParticleSystem hookHitParticle;   // ヒット時の火花など

    private void Awake()
    {
        // 初期状態のセットアップ
        if (commonLine != null)
        {
            commonLine.positionCount = 2;
            commonLine.enabled = true;
        }
        SetHookModelStatus(isIdle: true);
    }

    /// <summary>
    /// 毎フレームの描画更新
    /// </summary>
    /// <param name="rayOrigin">発射地点</param>
    /// <param name="isHookActive">フックが刺さっているか（または移動中か）</param>
    /// <param name="targetPoint">ターゲット地点（ヒット点または最大射程点）</param>
    public void UpdateVisuals(Transform rayOrigin, bool isHookActive, Vector3 targetPoint)
    {
        if (commonLine == null) return;

        // LineRendererの座標更新（0:先端, 1:手元）
        commonLine.SetPosition(0, targetPoint);
        commonLine.SetPosition(1, rayOrigin.position);

        if (isHookActive)
        {
            // --- 命中・移動中の見た目 ---
            commonLine.material = hookMaterial;

            // ワイヤー実体(筒状モデルなど)の伸縮処理
            if (rayVisualObject != null)
            {
                rayVisualObject.SetActive(true);
                UpdateWireObject(rayOrigin.position, targetPoint);
            }

            // フックモデルをヒット地点へ
            if (hookObject != null)
            {
                hookObject.position = targetPoint;
                hookObject.localScale = Vector3.one * hookScaleActive;
                // 向きを手元側に向ける（必要に応じて調整）
                hookObject.LookAt(rayOrigin);
                hookObject.Rotate(-90f, 0f, 0f);
            }
        }
        else
        {
            // --- 照準（待機）中の見た目 ---
            commonLine.material = aimMaterial;
            if (rayVisualObject != null) rayVisualObject.SetActive(false);

            // フックモデルを手元へ
            if (hookObject != null)
            {
                hookObject.position = rayOrigin.position;
                hookObject.forward = rayOrigin.forward;
                hookObject.localScale = Vector3.one * hookScaleOrigin;
                hookObject.Rotate(90f, 0f, 0f);
            }
        }
    }

    /// <summary>
    /// ワイヤー実体オブジェクトの長さと向きをヒット点に合わせる
    /// </summary>
    private void UpdateWireObject(Vector3 start, Vector3 end)
    {
        Vector3 midPoint = (start + end) / 2f;
        rayVisualObject.transform.position = midPoint;
        rayVisualObject.transform.LookAt(end);

        // Zスケールを距離に合わせて伸ばす
        float distance = Vector3.Distance(start, end);
        Vector3 scale = rayVisualObject.transform.localScale;
        scale.z = distance;
        rayVisualObject.transform.localScale = scale;
    }

    /// <summary>
    /// モデルの表示/非表示を切り替える
    /// </summary>
    public void SetHookModelStatus(bool isIdle)
    {
        if (normalHookModel != null) normalHookModel.SetActive(isIdle);
        if (flyingHookModel != null) flyingHookModel.SetActive(!isIdle);
    }

    /// <summary>
    /// ヒットエフェクトの再生
    /// </summary>
    public void PlayHitEffect(Vector3 position, Vector3 normal)
    {
        if (hookHitParticle == null) return;
        hookHitParticle.transform.position = position;
        hookHitParticle.transform.rotation = Quaternion.LookRotation(normal);
        hookHitParticle.Play();
    }

    public void StopHitEffect()
    {
        if (hookHitParticle != null) hookHitParticle.Stop();
    }
}