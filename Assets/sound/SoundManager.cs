using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム全体のBGM・SEを一括管理するサウンドマネージャー。
/// 
/// 【機能まとめ】
/// ● Singleton（どこからでも呼び出し可能）
/// ● BGM … ループ再生
/// ● SE … PlayOneShot（多重再生OK）
/// ● ループSE … 専用のAudioSourceで再生/停止可能
/// 
/// 【使い分け】
/// BGM          → PlayBGM()
/// 通常SE       → PlaySE()
/// ループSE     → PlaySELoop() / StopSELoop()
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("オーディオソース")]
    [Tooltip("BGM専用のAudioSource")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("通常SE専用のAudioSource（PlayOneShot用）")]
    [SerializeField] private AudioSource seSource;

    [Tooltip("ループSE専用のAudioSource（足音・風音など）")]
    [SerializeField] private AudioSource seLoopSource;

    [Header("音源リスト（Inspectorで設定）")]
    [Tooltip("使用したいBGMのAudioClipリスト")]
    [SerializeField] private List<AudioClip> bgmClips;

    [Tooltip("使用したいSEのAudioClipリスト")]
    [SerializeField] private List<AudioClip> seClips;

    // Clip 名 → Clip 実体（辞書化）
    private Dictionary<string, AudioClip> bgmDict;
    private Dictionary<string, AudioClip> seDict;

    private void Awake()
    {
        // ================================
        // Singleton（ゲームに一つだけ）
        // ================================
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // ================================
        // 辞書の初期化
        // ================================
        bgmDict = new Dictionary<string, AudioClip>();
        foreach (var clip in bgmClips)
            bgmDict[clip.name] = clip;

        seDict = new Dictionary<string, AudioClip>();
        foreach (var clip in seClips)
            seDict[clip.name] = clip;
    }

    // =========================================================
    // BGM 再生
    // =========================================================
    /// <summary>
    /// BGMを名前で再生（ループ）
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
    /// BGM停止
    /// </summary>
    public void StopBGM()
    {
        bgmSource.Stop();
    }

    // =========================================================
    // 通常SE 再生
    // =========================================================
    /// <summary>
    /// SEを名前で再生（PlayOneShot）/ 同時再生OK
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
    /// すべてのSE停止（通常は使わない）
    /// </summary>
    public void StopSE()
    {
        seSource.Stop();
    }

    // =========================================================
    // ループSE 再生
    // =========================================================
    /// <summary>
    /// 指定したSEをループ再生（足音・風音・エンジン音など）
    /// </summary>
    public void PlaySELoop(string name)
    {
        if (!seDict.ContainsKey(name))
        {
            Debug.LogWarning($"ループSE '{name}' が見つかりません");
            return;
        }

        seLoopSource.clip = seDict[name];
        seLoopSource.loop = true;
        seLoopSource.Play();
    }

    /// <summary>
    /// ループSEを停止
    /// </summary>
    public void StopSELoop()
    {
        seLoopSource.Stop();
    }
}
