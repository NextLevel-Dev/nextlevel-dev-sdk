using System;
using UnityEngine;

public class AutoMonoBehaviour<T> : MonoBehaviour where T : class
{
    private const string RootGameObjectName = "AutoMonoBehaviours";
    private static T _instance;
    private static GameObject _gameObject;
    private static bool _isInstantiating;

    protected static bool IsRunning { get; private set; } = true;

    public static T Instance
    {
        get
        {
            if (_instance == null && IsRunning)
            {
                if (_isInstantiating)
                {
                    throw new Exception(string.Format("Recursive calls to Constuctor of AutoMonoBehaviour! Check your {0}:Awake() function for calls to {0}.Instance", typeof(T)));
                }
                _isInstantiating = true;
                _instance = GetInstance();
            }

            return _instance;
        }
    }

    static AutoMonoBehaviour()
    {
    }

    private static T GetInstance()
    {
        var gameObject = GameObject.Find(RootGameObjectName);
        if (gameObject == null)
        {
            gameObject = new GameObject(RootGameObjectName);
            DontDestroyOnLoad(gameObject);
        }

        string name = typeof(T).Name;
        _gameObject = GameObject.Find(RootGameObjectName + "/" + name);
        if (_gameObject == null)
        {
            _gameObject = new GameObject(name);
            _gameObject.transform.parent = gameObject.transform;
        }

        return _gameObject.AddComponent(typeof(T)) as T;
    }

    private void OnApplicationQuit()
    {
        IsRunning = false;
    }

    protected virtual void Start()
    {
        if (_instance == null)
            throw new Exception("The script " + typeof(T).Name + " is self instantiating and shouldn't be manually attached to any gameobject.");
    }
}