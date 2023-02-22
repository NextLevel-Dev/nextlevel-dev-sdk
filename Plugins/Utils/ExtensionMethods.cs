using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class ExtensionMethods
{
    public static void FitSpriteToScreen(this GameObject go, float fitToScreenWidth, float fitToScreenHeight)
    {
        var sr = go.GetComponent<SpriteRenderer>();

        if (sr == null)
            return;

        go.transform.localScale = new Vector3(1, 1, 1);

        var width = sr.sprite.bounds.size.x;
        var height = sr.sprite.bounds.size.y;

        var worldScreenHeight = (float)(Camera.main.orthographicSize * 2.0);
        var worldScreenWidth = worldScreenHeight / Screen.height * Screen.width;

        if (fitToScreenWidth != 0)
        {
            var sizeX = new Vector2(worldScreenWidth / width / fitToScreenWidth, go.transform.localScale.y);
            go.transform.localScale = sizeX;
        }

        if (fitToScreenHeight != 0)
        {
            var sizeY = new Vector2(go.transform.localScale.x, worldScreenHeight / height / fitToScreenHeight);
            go.transform.localScale = sizeY;
        }
    }

    public static void FitRotationScaleToScreen(this GameObject go, float offset = 0f)
    {
        var bottomLeft = Camera.main.ViewportToWorldPoint(new Vector3(0, 0, Camera.main.nearClipPlane));
        var topRight = Camera.main.ViewportToWorldPoint(new Vector3(1, 1, Camera.main.nearClipPlane));

        var diagonal = Vector3.Distance(bottomLeft, topRight) + offset;

        go.transform.localScale = new Vector3(diagonal, diagonal, 1f);
    }

    public static T GetOrAddComponent<T>(this Component child) where T : Component
    {
        return child.GetComponent<T>() != null ? child.GetComponent<T>() : child.gameObject.AddComponent<T>();
    }

    public static bool IsVisibleFrom(this Renderer renderer, Camera camera)
    {
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return GeometryUtility.TestPlanesAABB(planes, renderer.bounds);
    }

    public static void RandomizeList<T>(this List<T> list)
    {
        var size = list.Count;

        for (var i = 0; i < size; i++)
        {
            var indexToSwap = Random.Range(i, size);
            var oldValue = list[i];
            list[i] = list[indexToSwap];
            list[indexToSwap] = oldValue;
        }
    }

    public static T RandomObject<T>(this IList<T> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    public static T RandomObject<T>(this T[] list)
    {
        return list[Random.Range(0, list.Length)];
    }

    public static string ToString(this decimal value, bool compactFormat, bool checkInfinity)
    {
        if (checkInfinity && value == decimal.MaxValue)
            return "Infinity";
        return compactFormat ? value.ToString(NextLevelUtilities.NumberCommaFormat) : value.ToString();
    }

    public static void Invoke(this MonoBehaviour b, Action method, float delay)
    {
        b.Invoke(method.Method.Name, delay);
    }

    public static void CancelInvoke(this MonoBehaviour b, Action method)
    {
        b.CancelInvoke(method.Method.Name);
    }

    public static string ToSentence(this string input)
    {
        return new string(input.ToCharArray().SelectMany((c, i) => i > 0 && char.IsUpper(c) ? new[] { ' ', c } : new[] { c }).ToArray());
    }
}
