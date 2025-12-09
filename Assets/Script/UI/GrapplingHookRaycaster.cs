using UnityEngine;

/// <summary>
/// フック移動に必要なレイキャスト処理、最大距離の制限、およびAimレイの描画を担当します。
/// </summary>
public class GrapplingHookRaycaster : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] Transform rayOrigin;       // レイの発射位置（コントローラーの子）
    [SerializeField] LineRenderer commonLine;   // Aim/Hook共通のLineRenderer
    [SerializeField] Material aimMaterial;       // Aim用マテリアル
    [SerializeField] Material hookMaterial;      // Hook用マテリアル

    [Header("フック / レイ設定")]
    [SerializeField] float maxWireLength = 50f; // レイの最大長
    [SerializeField] string[] hookInvalidTags;   // フックを無効にするタグのリスト

    // 外部アクセス用のプロパティ
    public Vector3 AimHitPoint => aimHitPoint;
    public bool HasAimHitPoint => hasAimHitPoint;
    public float MaxWireLength => maxWireLength;

    private Vector3 aimHitPoint;
    private bool hasAimHitPoint;

    private void Start()
    {
        // LineRendererが設定されていることを確認
        if (commonLine != null)
        {
            commonLine.positionCount = 2;
        }
    }

    private void Update()
    {
        if (commonLine == null || rayOrigin == null) return;

        // UpdateAimRayFixedのロジックをここに移植
        UpdateAimRayFixed();
    }

    /// <summary>
    /// フックが有効な場所を継続的にRaycastし、Aim Rayを描画します。
    /// </summary>
    void UpdateAimRayFixed()
    {
        // maxWireLengthが0以下の場合、Aim Rayの描画を停止する（0で描画される問題の解決）
        if (maxWireLength <= 0.0f)
        {
            commonLine.enabled = false;
            hasAimHitPoint = false;
            aimHitPoint = Vector3.zero;
            return;
        }
        commonLine.enabled = true;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);

        // Raycastによるヒットチェック (maxWireLengthを使用)
        if (Physics.Raycast(ray, out RaycastHit hit, maxWireLength))
        {
            // 無効タグチェック（VRController.csから移植）
            if (IsTagInvalidForHook(hit.collider.tag))
            {
                // ヒットしたが無効な場合は、Raycastがヒットしなかった場合として扱う
                DrawFixedLengthAim();
                return;
            }

            // ヒットした場合: ヒット位置で終点を設定
            hasAimHitPoint = true;
            aimHitPoint = hit.point;

            commonLine.material = hookMaterial; // ヒット時はフックマテリアルを使用
            commonLine.SetPosition(0, aimHitPoint);
            commonLine.SetPosition(1, rayOrigin.position);
            return;
        }

        // ヒットしなかった場合
        DrawFixedLengthAim();
    }

    /// <summary>
    /// Raycastがヒットしなかったとき、maxWireLengthの長さでAim Rayを描画する。
    /// </summary>
    void DrawFixedLengthAim()
    {
        hasAimHitPoint = false;
        aimHitPoint = Vector3.zero;

        // 終点を maxWireLength の位置に設定する (長さの修正箇所)
        Vector3 endPoint = rayOrigin.position + rayOrigin.forward * maxWireLength;

        commonLine.material = aimMaterial;
        commonLine.SetPosition(0, endPoint);
        commonLine.SetPosition(1, rayOrigin.position);
    }

    // VRController.cs から移植したタグチェック関数
    bool IsTagInvalidForHook(string tag)
    {
        if (hookInvalidTags == null) return false;

        foreach (string invalidTag in hookInvalidTags)
        {
            if (tag.Equals(invalidTag, System.StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }
}