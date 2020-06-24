using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;
using UnityEngine.UI;

public class BannerProxy : MonoBehaviour
{
    [Serializable]
    public class BannerData
    {
        public string name;
        public string icon_url;
        public string package_id;
    }

    public struct BannerConfig
    {
        public string GameId;
        public Action<string> OpenStoreLink;
    }

    public GameObject Banner;
    public string Url;

#if UNITY_ANDROID
    private const string Platform = "Android";
#elif UNITY_IOS
    private const string Platform = "iOS";
#else
    private const string Platform = "Unknown";
#endif

    private const string TagBanner = "Banner";

    private static bool IsCached => _banner != null;
    private static BannerData _banner;

    public delegate void BannerUpdate(Texture icon, string name);
    public static event BannerUpdate BannerUpdated;

    public static BannerConfig Config;
    public static BannerProxy Instance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (Config.GameId == null || Config.OpenStoreLink == null)
        {
            Debug.LogError("BannerConfig not set!");
            return;
        }

        CacheBanner();
    }

    public void CacheBanner()
    {
#if UNITY_ANDROID || UNITY_IOS
        StartCoroutine(GetBanner());
#endif
    }

    private IEnumerator GetBanner()
    {
        var form = new WWWForm();

        form.AddField("platform", Platform);
        form.AddField("gameId", Config.GameId);

        using (var www = UnityWebRequest.Post(Url, form))
        {
            yield return www.SendWebRequest();

            if (www.isNetworkError || www.isHttpError)
            {
                Debug.LogError(www.error);
                Banner.SetActive(IsCached);
                yield break;
            }

            _banner = JsonUtility.FromJson<BannerData>(www.downloadHandler.text);

            StartCoroutine(SetBanner());
        }
    }

    private IEnumerator SetBanner()
    {
        using (var webRequest = UnityWebRequestTexture.GetTexture(_banner.icon_url))
        {
            yield return webRequest.SendWebRequest();

            if (!webRequest.isNetworkError && !webRequest.isHttpError)
            {
                var icon = DownloadHandlerTexture.GetContent(webRequest);

                BannerUpdated?.Invoke(icon, _banner.name);
            }
            else
            {
                Debug.LogError(webRequest.error);
                yield break;
            }
        }
        
        Banner.SetActive(true);
    }

    public void Open()
    {
        Config.OpenStoreLink(_banner.package_id);
        Analytics.CustomEvent(TagBanner, new Dictionary<string, object>
        {
            { "game", _banner.name }
        });
    }
}