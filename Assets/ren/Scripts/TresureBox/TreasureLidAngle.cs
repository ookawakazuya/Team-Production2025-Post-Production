using UnityEngine;

public class TreasureLidAngle : MonoBehaviour
{
    [Header("æ“¾Šp“x”»’è")]
    [Tooltip("ŠJ‚¢‚½”»’è‚ğæ‚éÅ¬Šp“xi—á: -120j")]
    public float openAngleMin = -120f;

    [Tooltip("ŠJ‚¢‚½”»’è‚ğæ‚éÅ‘åŠp“xi—á: -110j")]
    public float openAngleMax = -110f;

    [Header("QÆ")]
    public TreasureBox treasureBox;

    private bool opened = false;

    void Update()
    {
        if (opened) return;

        // š X²‰ñ“]‚ğ -180 ` 180 ‚É³‹K‰»
        float angle = NormalizeAngle(transform.localEulerAngles.x);

        if (angle >= openAngleMin && angle <= openAngleMax)
        {
            opened = true;
            treasureBox.OnTreasureOpened();
        }
    }

    /// <summary>
    /// 0`360 ‚ÌŠp“x‚ğ -180`180 ‚É•ÏŠ·
    /// </summary>
    float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
