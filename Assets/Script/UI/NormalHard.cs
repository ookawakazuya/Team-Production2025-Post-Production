using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NormalHard : MonoBehaviour
{
   public enum StageLevel
    {
        Normal,
        Hard,
    }

    public static StageLevel stagelevel;

    [SerializeField] GameObject NormalButton;
    [SerializeField] GameObject HardButton;

    void Start()
    {
        stagelevel = StageLevel.Normal;
    }

    // Update is called once per frame
    void Update()
    {
        switch (stagelevel)
        {
            case StageLevel.Normal:
                NormalButton.SetActive(true);
                HardButton.SetActive(false);
                break;
            case StageLevel.Hard:
                HardButton.SetActive(true);
                NormalButton.SetActive(false);
                break;
        }

        Debug.Log(stagelevel);
    }


    public void OnUpButton()
    {
        stagelevel = StageLevel.Normal;

        // ★ プレイヤーHPを3にする
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp)
            {
                hp.currentLife = hp.maxLife; // 3
            }
        }
    }

    public void OnDownButton()
    {
        stagelevel = StageLevel.Hard;

        // ★ プレイヤーHPを1にする
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player)
        {
            var hp = player.GetComponent<PlayerHealth>();
            if (hp)
            {
                hp.currentLife = hp.minLife; // 1
            }
        }
    }

}
