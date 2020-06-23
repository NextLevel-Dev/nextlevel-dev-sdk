using System;
using System.Reflection;
using System.Threading;

public class Singleton<T> : IDisposable where T : Singleton<T>
{
    private static readonly object _lock = new object();
    private static volatile T _instance;

    public static T Instance
    {
        get
        {
            if (_instance != null)
                return _instance;

            var obj = _lock;
            Monitor.Enter(obj);
            try
            {
                if (_instance == null)
                {
                    var constructor = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[0], null);
                    if (constructor == null || constructor.IsAssembly)
                        throw new Exception($"A private or protected constructor is missing for '{typeof(T).Name}'.");

                    _instance = (T)constructor.Invoke(null);
                }
            }
            finally
            {
                Monitor.Exit(obj);
            }
            return _instance;
        }
    }

    static Singleton()
    {
    }

    public void Dispose()
    {
        OnDispose();
        _instance = null;
    }

    protected virtual void OnDispose()
    {
    }
}