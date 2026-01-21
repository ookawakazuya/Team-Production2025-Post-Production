using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// XR Ray Interactorを利用して、レイが当たっているUIを
/// トリガーボタンでクリックするための独立スクリプト。
/// </summary>
[RequireComponent(typeof(XRRayInteractor))]
public class VRSimpleUIClicker : MonoBehaviour
{
    [Header("入力設定")]
    [Tooltip("トリガーボタンの入力アクション (例: XRI RightHand Interaction/Activate)")]
    [SerializeField] private InputActionProperty clickAction;

    private XRRayInteractor _rayInteractor;

    void Awake()
    {
        // 自身のオブジェクトから XRRayInteractor を取得
        _rayInteractor = GetComponent<XRRayInteractor>();
    }

    void OnEnable()
    {
        // インスペクターで指定されたアクションを有効化する
        if (clickAction.action != null)
        {
            clickAction.action.Enable();
        }
    }

    void Update()
    {
        // トリガーが「たった今押された」瞬間かどうかを判定
        if (clickAction.action != null && clickAction.action.WasPressedThisFrame())
        {
            PerformClick();
        }
    }

    /// <summary>
    /// レイの先にあるUIに対してクリック処理を実行する
    /// </summary>
    private void PerformClick()
    {
        if (_rayInteractor == null) return;

        // XRRayInteractor から現在UIに当たっているか情報を取得
        if (_rayInteractor.TryGetCurrentUIRaycastResult(out RaycastResult result))
        {
            // ヒットしたオブジェクト、またはその親階層に Button があるか確認
            Button targetButton = result.gameObject.GetComponentInParent<Button>();

            // ボタンが存在し、かつクリック可能な状態（Interactable）であれば実行
            if (targetButton != null && targetButton.interactable)
            {
                // Unity標準の onClick イベントを呼び出す
                targetButton.onClick.Invoke();

                // デバッグ用（動作確認後に削除してOK）
                Debug.Log($"[VRUIClicker] {targetButton.gameObject.name} をクリックしました");
            }
        }
    }
}