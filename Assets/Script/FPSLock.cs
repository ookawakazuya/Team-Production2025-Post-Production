using UnityEngine;

public class FPSLock : MonoBehaviour
{
    public int FrameRate = 60;
    void Start()
    {
        Application.targetFrameRate = FrameRate;   //FPSの固定
        QualitySettings.vSyncCount = 0;     //Vsyncを切りモニターのリフレッシュレートを無視する。
    }

}
