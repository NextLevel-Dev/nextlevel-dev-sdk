using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.Networking;

public class BannerProxy : MonoBehaviour
{
    [Serializable]
    public class BannerData
    {
        public string name;
        public string iconUrl;
        public string packageId;
    }

    private class RequestForm
    {
        public string platform;
        public string gameId;
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (Config.GameId == null || Config.OpenStoreLink == null)
        {
            Debug.LogError("BannerConfig not set!");
            return;
        }

        if (!string.IsNullOrEmpty(Url))
        {
            CacheBanner();
        }
    }

    public void CacheBanner()
    {
#if UNITY_ANDROID || UNITY_IOS
        StartCoroutine(GetBanner());
#endif
    }

    private IEnumerator GetBanner()
    {
        var form = new RequestForm
        {
            gameId = Config.GameId,
            platform = Platform
        };

        string json = JsonUtility.ToJson(form);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        using (var req = new UnityWebRequest(Url, "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

            if (req.result is UnityWebRequest.Result.ConnectionError or UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(req.error);
                Banner.SetActive(IsCached);
                yield break;
            }

            _banner = JsonUtility.FromJson<BannerData>(req.downloadHandler.text);
        }
        
        StartCoroutine(SetBanner());
    }

    private IEnumerator SetBanner()
    {
        using (var req = UnityWebRequestTexture.GetTexture(_banner.iconUrl))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.ConnectionError && req.result != UnityWebRequest.Result.ProtocolError)
            {
                var icon = DownloadHandlerTexture.GetContent(req);

                BannerUpdated?.Invoke(icon, _banner.name);
            }
            else
            {
                Debug.LogError(req.error);
                yield break;
            }
        }

        Banner.SetActive(true);
    }

    public void Open()
    {
        Config.OpenStoreLink(_banner.packageId);
        Analytics.CustomEvent(TagBanner, new Dictionary<string, object>
        {
            { "game", _banner.name }
        });
    }
}
