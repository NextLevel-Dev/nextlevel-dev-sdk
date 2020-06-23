using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioUtility : AutoMonoBehaviour<AudioUtility>
{
    private AudioSource _music;
    private const float MusicVolume = 1f;

    private AudioClip _lastPlayedClip;
    private AudioClip _lastPlayedMusicClip;

    private const string AudioObjectName = "AudioPool";

    void Awake()
    {
        _music = GetComponent<AudioSource>();
        _music.loop = true;
    }

    public void PlayClip(AudioClip clip, bool loop = false)
    {
        PlayClip(clip, Camera.main.transform.position, loop);
    }

    public void PlayClip(AudioClip clip, Vector3 position, bool loop = false)
    {
        PlayClip(clip, position, clip.length, loop);
    }

    public void PlayClip(AudioClip clip, float duration, bool loop = false)
    {
        PlayClip(clip, Camera.main.transform.position, duration, loop);
    }

    public void PlayClip(AudioClip clip, Vector3 position, float duration, bool loop = false)
    {
        ObjectPooler.Instance.Instantiate(AudioObjectName, position, out var go);
        var source = go.GetComponent<AudioSource>();

        source.clip = clip;
        source.loop = loop;
        source.Play();

        if (!loop)
        {
            UnityRuntime.Invoke(() => { go?.SetActive(false); }, duration);
        }
    }

    public void PlayClipDelayed(AudioClip clip, float delay)
    {
        UnityRuntime.Invoke(() => PlayClip(clip), delay);
    }

    public void PlayRandomClip(IList<AudioClip> clips)
    {
        ObjectPooler.Instance.Instantiate(AudioObjectName, Camera.main.transform.position, out var go);
        var source = go.GetComponent<AudioSource>();

        var clip = clips.RandomObject();
        while (clip == _lastPlayedClip)
            clip = clips.RandomObject();

        _lastPlayedClip = clip;

        source.clip = clip;
        source.loop = false;
        source.Play();

        UnityRuntime.Invoke(() => { go?.SetActive(false); }, clip.length);
    }

    public void PlayRandomClip(IList<AudioClip> clips, float duration)
    {
        ObjectPooler.Instance.Instantiate(AudioObjectName, Camera.main.transform.position, out var go);
        var source = go.GetComponent<AudioSource>();

        var clip = clips.RandomObject();
        while (clip == _lastPlayedClip)
            clip = clips.RandomObject();

        _lastPlayedClip = clip;

        source.clip = clip;
        source.loop = false;
        source.Play();

        UnityRuntime.Invoke(() => { go?.SetActive(false); }, duration);
    }

    public void ChangeMusic(AudioClip clip, AudioSource source = null)
    {
        StartCoroutine(source == null ? CrossFade(clip) : CrossFade(clip, source));
    }

    public void ChangeMusicRandom(IList<AudioClip> clips, AudioSource source = null)
    {
        var clip = clips.RandomObject();
        while (clip == _lastPlayedMusicClip)
            clip = clips.RandomObject();

        _lastPlayedMusicClip = clip;
        StartCoroutine(source == null ? CrossFade(clip) : CrossFade(clip, source));
    }

    public void PlaySequential(IList<AudioClip> clips, AudioSource source)
    {
        StartCoroutine(Playlist(clips, source));
    }

    private static IEnumerator Playlist(IList<AudioClip> clips, AudioSource source)
    {
        AudioClip lastPlayedMusicClip = null;
        while (true)
        {
            if (!source.gameObject.activeInHierarchy)
                break;

            if (!source.isPlaying)
            {
                var clip = clips.RandomObject();
                while (clip == lastPlayedMusicClip)
                    clip = clips.RandomObject();

                lastPlayedMusicClip = clip;
                source.clip = clip;
                source.Play();
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator CrossFade(AudioClip to, AudioSource on = null)
    {
        var source = on ?? _music;
        var delay = 1f;
        if (source.clip != null)
        {
            while (delay > 0)
            {
                source.volume = delay * MusicVolume;
                delay -= Time.unscaledDeltaTime;
                yield return 0;
            }
        }

        source.clip = to;

        if (to == null)
        {
            source.Stop();
            yield break;
        }

        delay = 0;

        if (!source.isPlaying)
            source.Play();

        while (delay < 1)
        {
            source.volume = delay * MusicVolume;
            delay += Time.unscaledDeltaTime;
            yield return 0;
        }

        source.volume = MusicVolume;
    }

    public static void Mute()
    {
        AudioListener.volume = 0f;
    }

    public static void UnMute()
    {
        AudioListener.volume = 1f;
    }
}