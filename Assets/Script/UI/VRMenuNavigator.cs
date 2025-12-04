using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VRMenuNavigator : MonoBehaviour
{
    [Header("パネル一覧（メニュー階層ごとに登録）")]
    [SerializeField] List<GameObject> menuPanels;  // 各パネルを登録
    [SerializeField] int startPanelIndex = 0;       // 初期表示するパネル番号（通常は0）

    private int currentPanelIndex = -1;

    void Start()
    {
        // 初期パネルを表示
        ShowPanel(startPanelIndex);
    }

    /// <summary>
    /// 指定したインデックスのパネルを表示し、それ以外を非表示にする
    /// </summary>
    public void ShowPanel(int index)
    {
        if (index < 0 || index >= menuPanels.Count)
        {
            Debug.LogWarning("パネル番号が範囲外です: " + index);
            return;
        }

        // すべてのパネルを非表示にする
        foreach (var panel in menuPanels)
            panel.SetActive(false);

        // 指定パネルを有効化
        menuPanels[index].SetActive(true);
        currentPanelIndex = index;
    }

    /// <summary>
    /// 「戻る」ボタンなどで前のパネルに戻る処理
    /// （シンプルな1階層戻り動作）
    /// </summary>
    public void GoBackToMain()
    {
        ShowPanel(startPanelIndex);
    }
}
