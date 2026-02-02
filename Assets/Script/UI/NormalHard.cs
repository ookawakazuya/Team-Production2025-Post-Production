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
    }
    public void OnDownButton()
    {
        stagelevel = StageLevel.Hard;
    }
}
