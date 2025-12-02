using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("オーディオソース")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource seSource;

    [Header("音源リスト")]
    [SerializeField] private List<AudioClip> bgmClips;
    [SerializeField] private List<AudioClip> seClips;

    private Dictionary<string, AudioClip> bgmDict;
    private Dictionary<string, AudioClip> seDict;

    private void Awake()
    {
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

        // Clip名で検索できるようDictionary化
        bgmDict = new Dictionary<string, AudioClip>();
        foreach (var clip in bgmClips) bgmDict[clip.name] = clip;

        seDict = new Dictionary<string, AudioClip>();
        foreach (var clip in seClips) seDict[clip.name] = clip;
    }

    public void PlayBGM(string name)
    {
        if (!bgmDict.ContainsKey(name)) return;

        bgmSource.clip = bgmDict[name];
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void PlaySE(string name)
    {
        if (!seDict.ContainsKey(name)) return;

        seSource.PlayOneShot(seDict[name]);
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }
}
