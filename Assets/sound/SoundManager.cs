using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体のBGM・SEを一括で管理するサウンドマネージャー。
/// ・Singletonパターンでどこからでも呼び出し可能
/// ・AudioClipをDictionary化して名前で再生
/// ・BGMはループ、SEはOneShotで再生
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("オーディオソース（BGM/SE再生専用）")]
    [SerializeField] private AudioSource bgmSource;  // BGM専用オーディオソース
    [SerializeField] private AudioSource seSource;   // SE専用オーディオソース

    [Header("音源リスト（Inspectorで設定）")]
    [SerializeField] private List<AudioClip> bgmClips;  // 登録したいBGM音源
    [SerializeField] private List<AudioClip> seClips;   // 登録したいSE音源

    // 名前で検索できるようにDictionary化
    private Dictionary<string, AudioClip> bgmDict;
    private Dictionary<string, AudioClip> seDict;

    private void Awake()
    {
        // Singleton（ゲーム中に一つだけ存在させる）
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // シーンを跨いでも破棄しない
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // BGMの辞書作成
        bgmDict = new Dictionary<string, AudioClip>();
        foreach (var clip in bgmClips)
        {
            bgmDict[clip.name] = clip;
        }

        // SEの辞書作成
        seDict = new Dictionary<string, AudioClip>();
        foreach (var clip in seClips)
        {
            seDict[clip.name] = clip;
        }
    }

    /// <summary>
    /// BGMを指定名で再生する。（ループ再生）
    /// </summary>
    public void PlayBGM(string name)
    {
        if (!bgmDict.ContainsKey(name))
        {
            Debug.LogWarning($"BGM '{name}' が見つかりません");
            return;
        }

        bgmSource.clip = bgmDict[name];
        bgmSource.loop = true;
        bgmSource.Play();
    }

    /// <summary>
    /// SEを指定名で再生する（同時再生可能）
    /// </summary>
    public void PlaySE(string name)
    {
        if (!seDict.ContainsKey(name))
        {
            Debug.LogWarning($"SE '{name}' が見つかりません");
            return;
        }

        seSource.PlayOneShot(seDict[name]);
    }

    /// <summary>
    /// BGMの停止
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }
}
