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

    public static BannerProxy Instance;

    public GameObject Banner;
    public RawImage IconUgui;
    public Text NameUgui;
    public UITexture IconNgui;
    public UILabel NameNgui;

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

    public static BannerConfig Config;

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
                var texture = DownloadHandlerTexture.GetContent(webRequest);

                if (IconNgui != null)
                    IconNgui.mainTexture = texture;
                if (IconUgui != null)
                    IconUgui.texture = texture;
            }
            else
            {
                Debug.LogError(webRequest.error);
                yield break;
            }
        }

        if (NameNgui != null)
            NameNgui.text = _banner.Name.ToUpper();
        if (NameUgui != null)
            NameUgui.text = _banner.Name.ToUpper();

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