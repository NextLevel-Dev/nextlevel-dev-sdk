using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    public static ObjectPooler Instance;

    public Pool[] ObjectPrefabs;

    private GameObject _containerObject;
    private List<GameObject>[] _pooledObjects;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _containerObject = new GameObject("ObjectPool");
        _pooledObjects = new List<GameObject>[ObjectPrefabs.Length];

        for (int i = 0; i < ObjectPrefabs.Length; i++)
        {
            _pooledObjects[i] = new List<GameObject>();

            for (int j = 0; j < ObjectPrefabs[i].Buffer; j++)
            {
                var prefab = ObjectPrefabs[i].Prefab;
                var obj = Instantiate(prefab);
                obj.name = prefab.name;
                obj.SetActive(false);

                PoolObject(obj);
            }
        }
    }

    public GameObject GetPooledObject(string objectName)
    {
        for (int i = 0; i < ObjectPrefabs.Length; i++)
        {
            var prefab = ObjectPrefabs[i].Prefab;
            if (prefab.name == objectName)
            {
                for (int j = 0; j < _pooledObjects[i].Count; j++)
                {
                    if (!_pooledObjects[i][j].activeInHierarchy)
                        return _pooledObjects[i][j];
                }

                var go = Instantiate(prefab);
                go.name = prefab.name;
                PoolObject(go);

                return go;
            }
        }

        return null;
    }

    private void PoolObject(GameObject obj)
    {
        for (int i = 0; i < ObjectPrefabs.Length; i++)
        {
            if (ObjectPrefabs[i].Prefab.name == obj.name)
            {
                obj.transform.SetParent(_containerObject.transform);
                _pooledObjects[i].Add(obj);
            }
        }
    }

    public GameObject Instantiate(string objectName)
    {
        var go = GetPooledObject(objectName);

        if (go == null)
        {
            Debug.LogError($"Object '{objectName}' is not assigned in ObjectPooler");
            return null;
        }

        go.SetActive(true);
        return go;
    }

    public void Instantiate(string objectName, Vector3 position)
    {
        var go = GetPooledObject(objectName);

        if (go == null)
        {
            Debug.LogError($"Object '{objectName}' is not assigned in ObjectPooler");
            return;
        }

        go.transform.position = position;
        go.SetActive(true);
    }

    public void Instantiate(string objectName, Vector3 position, out GameObject go)
    {
        go = GetPooledObject(objectName);

        if (go == null)
        {
            Debug.LogError($"Object '{objectName}' is not assigned in ObjectPooler");
            return;
        }

        go.transform.position = position;
        go.SetActive(true);
    }

    public void Instantiate(string objectName, Vector3 position, Vector3 rotation)
    {
        var go = GetPooledObject(objectName);

        if (go == null)
        {
            Debug.LogError($"Object '{objectName}' is not assigned in ObjectPooler");
            return;
        }

        go.transform.position = position;
        go.transform.eulerAngles = rotation;
        go.SetActive(true);
    }

    public void Instantiate(string objectName, Vector3 position, Vector3 rotation, out GameObject go)
    {
        go = GetPooledObject(objectName);

        if (go == null)
        {
            Debug.LogError($"Object '{objectName}' is not assigned in ObjectPooler");
            return;
        }

        go.transform.position = position;
        go.transform.eulerAngles = rotation;
        go.SetActive(true);
    }

    private void CountPooledObjects()
    {
        var objects = new Dictionary<string, int>();
        foreach (var gameObjects in _pooledObjects)
        {
            foreach (var go in gameObjects)
            {
                if (objects.ContainsKey(go.name))
                    objects[go.name]++;
                else
                    objects.Add(go.name, 1);
            }
        }

        foreach (var go in objects)
            Debug.Log(go.Key + " " + go.Value);
    }
}

[Serializable]
public struct Pool
{
    public GameObject Prefab;
    public int Buffer;
}