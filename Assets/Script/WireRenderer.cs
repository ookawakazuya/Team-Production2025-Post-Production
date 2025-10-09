using UnityEngine;

public class WireRenderer : MonoBehaviour
{

    [Header("ワイヤー設定")]
    public Transform startPoint;        //ワイヤーの開始点
    public Transform endPoint;          //ワイヤーの終着点
    [Range(2,50)]
    public int segmentCount = 20;       //線の数
    [Range(0f,2f)]
    public float sagAmount = 0.5f;      //たわみ量

    [Header("動的な揺れ")]
    public float waveAmplitudo = 0.1f;  //
    public float waveFrequency = 5.0f;  //

    LineRenderer line;
    Vector3[] points;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        points = new Vector3[segmentCount];
    }

    void Update()
    {
        if(startPoint == null|| endPoint == null)
        {
            line.enabled = false;
            return;
        }
        line.enabled = true;

        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;

        for(int i = 0;i < segmentCount; i++)
        {
            float t = (float)i / (segmentCount - 1);
            Vector3 pos = Vector3.Lerp(start, end, t);

            //放物線
            float sag = Mathf.Sin(Mathf.PI * t) * sagAmount;
            pos += Vector3.down * sag;

            //揺れ
            if(waveAmplitudo > 0)
            {
                pos += Vector3.up * Mathf.Sin(Time.time * waveFrequency + t * Mathf.PI) * waveAmplitudo;
            }
            points[i] = pos;
        }

        line.positionCount = segmentCount;
        line.SetPositions(points);
    }

    public void SetPoints(Transform start, Transform end)
    {
        startPoint = start;
        endPoint = end;
        line.enabled = true;
    }

    public void Clear()
    {
        startPoint = null;
        endPoint = null;
        line.enabled = false;
    }
}
