using UnityEngine;
using UnityEngine.UI;

public class TreasureIconUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    [SerializeField] private Color collectedColor = Color.white;
    [SerializeField] private Color notCollectedColor = Color.gray;

    public void SetCollected(bool collected)
    {
        iconImage.color = collected ? collectedColor : notCollectedColor;
    }
}
