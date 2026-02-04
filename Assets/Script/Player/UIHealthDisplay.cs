using UnityEngine;
using UnityEngine.UI;

public class UIHealthDisplay : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private PlayerHealth playerHealth; // プレイヤーのHPスクリプト

    [Header("ライフ用イメージ配列")]
    [Tooltip("左から順(Index 0, 1, 2)に配置してください")]
    [SerializeField] private Image[] heartImages;

    [Header("スプライト設定")]
    [SerializeField] private Sprite fullHeartSprite;  // 生きている時の画像
    [SerializeField] private Sprite emptyHeartSprite; // ダメージを受けた時の画像

    // 内部で保持する前回のモード（モード切替検知用）
    private NormalHard.StageLevel lastStageLevel;

    void Start()
    {
        // PlayerHealthが未設定ならタグから自動取得
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerHealth = player.GetComponent<PlayerHealth>();
        }

        // 開始時のモードを記録
        if (NormalHard.stagelevel != null)
        {
            lastStageLevel = NormalHard.stagelevel;
        }

        // 初回のUI表示更新
        RefreshUI();
    }

    void Update()
    {
        // 1. 難易度がボタン操作などで切り替わったかチェック
        if (lastStageLevel != NormalHard.stagelevel)
        {
            lastStageLevel = NormalHard.stagelevel;
            RefreshUI(); // モードに合わせて表示個数を変更
        }

        // 2. 常に最新のライフ残量をスプライトに反映
        UpdateHeartSprites();
    }

    /// <summary>
    /// モード（Normal/Hard）に合わせて、Imageオブジェクト自体の有効/無効を切り替える
    /// </summary>
    private void RefreshUI()
    {
        if (heartImages == null || heartImages.Length == 0) return;

        // ハードモードかどうかを判定
        bool isHard = (NormalHard.stagelevel == NormalHard.StageLevel.Hard);

        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] == null) continue;

            if (isHard)
            {
                // ハードモード：1番目(Index 0)以外はGameObjectを非表示にする
                heartImages[i].gameObject.SetActive(i == 0);
            }
            else
            {
                // ノーマルモード：3つともGameObjectを表示状態にする
                heartImages[i].gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// PlayerHealthのcurrentLifeを見て、スプライトを「あり/なし」に差し替える
    /// </summary>
    private void UpdateHeartSprites()
    {
        if (playerHealth == null) return;

        // PlayerHealthから現在のライフを取得
        int currentHp = playerHealth.currentLife;

        for (int i = 0; i < heartImages.Length; i++)
        {
            // 非表示中のオブジェクトはスキップ
            if (heartImages[i] == null || !heartImages[i].gameObject.activeSelf) continue;

            // インデックス(0〜2)が現在のHP(1〜3)未満なら「生存スプライト」
            if (i < currentHp)
            {
                if (fullHeartSprite != null)
                {
                    heartImages[i].sprite = fullHeartSprite;
                }
            }
            else
            {
                // HP以上のインデックスは「ダメージ後スプライト」を表示し続ける
                if (emptyHeartSprite != null)
                {
                    heartImages[i].sprite = emptyHeartSprite;
                }
            }
        }
    }
}