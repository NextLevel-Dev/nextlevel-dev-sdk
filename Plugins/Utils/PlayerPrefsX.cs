﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class PlayerPrefsX
{
    private static int _endianDiff1;
    private static int _endianDiff2;
    private static int _idx;
    private static byte[] _byteBlock;

    enum ArrayType { Float, Int32, Bool, String, Vector2, Vector3, Quaternion, Color }

    public static void SetBool(string name, bool value)
    {
        PlayerPrefs.SetInt(name, value ? 1 : 0);
    }

    public static bool GetBool(string name)
    {
        return PlayerPrefs.GetInt(name) == 1;
    }

    public static bool GetBool(string name, bool defaultValue)
    {
        return (1 == PlayerPrefs.GetInt(name, defaultValue ? 1 : 0));
    }

    public static long GetLong(string key, long defaultValue)
    {
        SplitLong(defaultValue, out var lowBits, out var highBits);
        lowBits = PlayerPrefs.GetInt(key + "_lowBits", lowBits);
        highBits = PlayerPrefs.GetInt(key + "_highBits", highBits);

        // unsigned, to prevent loss of sign bit.
        ulong ret = (uint)highBits;
        ret = (ret << 32);
        return (long)(ret | (ulong)(uint)lowBits);
    }

    public static long GetLong(string key)
    {
        int lowBits = PlayerPrefs.GetInt(key + "_lowBits");
        int highBits = PlayerPrefs.GetInt(key + "_highBits");

        // unsigned, to prevent loss of sign bit.
        ulong ret = (uint)highBits;
        ret = (ret << 32);
        return (long)(ret | (ulong)(uint)lowBits);
    }

    private static void SplitLong(long input, out int lowBits, out int highBits)
    {
        // unsigned everything, to prevent loss of sign bit.
        lowBits = (int)(uint)(ulong)input;
        highBits = (int)(uint)(input >> 32);
    }

    public static void SetLong(string key, long value)
    {
        SplitLong(value, out var lowBits, out var highBits);
        PlayerPrefs.SetInt(key + "_lowBits", lowBits);
        PlayerPrefs.SetInt(key + "_highBits", highBits);
    }

    public static bool SetVector2(string key, Vector2 vector)
    {
        return SetFloatArray(key, new[] { vector.x, vector.y });
    }

    static Vector2 GetVector2(string key)
    {
        var floatArray = GetFloatArray(key);
        return floatArray.Length < 2 ? Vector2.zero : new Vector2(floatArray[0], floatArray[1]);
    }

    public static Vector2 GetVector2(string key, Vector2 defaultValue)
    {
        return PlayerPrefs.HasKey(key) ? GetVector2(key) : defaultValue;
    }

    public static bool SetVector3(string key, Vector3 vector)
    {
        return SetFloatArray(key, new[] { vector.x, vector.y, vector.z });
    }

    public static Vector3 GetVector3(string key)
    {
        var floatArray = GetFloatArray(key);
        return floatArray.Length < 3 ? Vector3.zero : new Vector3(floatArray[0], floatArray[1], floatArray[2]);
    }

    public static Vector3 GetVector3(string key, Vector3 defaultValue)
    {
        return PlayerPrefs.HasKey(key) ? GetVector3(key) : defaultValue;
    }

    public static bool SetQuaternion(string key, Quaternion vector)
    {
        return SetFloatArray(key, new[] { vector.x, vector.y, vector.z, vector.w });
    }

    public static Quaternion GetQuaternion(string key)
    {
        var floatArray = GetFloatArray(key);
        return floatArray.Length < 4 ? Quaternion.identity : new Quaternion(floatArray[0], floatArray[1], floatArray[2], floatArray[3]);
    }

    public static Quaternion GetQuaternion(string key, Quaternion defaultValue)
    {
        return PlayerPrefs.HasKey(key) ? GetQuaternion(key) : defaultValue;
    }

    public static bool SetColor(string key, Color color)
    {
        return SetFloatArray(key, new[] { color.r, color.g, color.b, color.a });
    }

    public static Color GetColor(string key)
    {
        var floatArray = GetFloatArray(key);
        return floatArray.Length < 4 ? new Color(0.0f, 0.0f, 0.0f, 0.0f) : new Color(floatArray[0], floatArray[1], floatArray[2], floatArray[3]);
    }

    public static Color GetColor(string key, Color defaultValue)
    {
        return PlayerPrefs.HasKey(key) ? GetColor(key) : defaultValue;
    }

    public static bool SetBoolArray(string key, bool[] boolArray)
    {
        // Make a byte array that's a multiple of 8 in length, plus 5 bytes to store the number of entries as an int32 (+ identifier)
        // We have to store the number of entries, since the boolArray length might not be a multiple of 8, so there could be some padded zeroes
        var bytes = new byte[(boolArray.Length + 7) / 8 + 5];
        bytes[0] = Convert.ToByte(ArrayType.Bool);   // Identifier
        var bits = new BitArray(boolArray);
        bits.CopyTo(bytes, 5);
        Initialize();
        ConvertInt32ToBytes(boolArray.Length, bytes); // The number of entries in the boolArray goes in the first 4 bytes

        return SaveBytes(key, bytes);
    }

    public static bool[] GetBoolArray(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return new bool[0];
        var bytes = Convert.FromBase64String(PlayerPrefs.GetString(key));
        if (bytes.Length < 5)
        {
            Debug.LogError("Corrupt preference file for " + key);
            return new bool[0];
        }
        if ((ArrayType)bytes[0] != ArrayType.Bool)
        {
            Debug.LogError(key + " is not a boolean array");
            return new bool[0];
        }
        Initialize();

        // Make a new bytes array that doesn't include the number of entries + identifier (first 5 bytes) and turn that into a BitArray
        var bytes2 = new byte[bytes.Length - 5];
        Array.Copy(bytes, 5, bytes2, 0, bytes2.Length);
        var bits = new BitArray(bytes2) { Length = ConvertBytesToInt32(bytes) };
        // Get the number of entries from the first 4 bytes after the identifier and resize the BitArray to that length, then convert it to a boolean array
        var boolArray = new bool[bits.Count];
        bits.CopyTo(boolArray, 0);

        return boolArray;
    }

    public static bool[] GetBoolArray(string key, bool defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetBoolArray(key);
        }
        var boolArray = new bool[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            boolArray[i] = defaultValue;
        }
        return boolArray;
    }

    public static bool SetStringArray(string key, string[] stringArray)
    {
        var bytes = new byte[stringArray.Length + 1];
        bytes[0] = Convert.ToByte(ArrayType.String); // Identifier
        Initialize();

        // Store the length of each string that's in stringArray, so we can extract the correct strings in GetStringArray
        for (var i = 0; i < stringArray.Length; i++)
        {
            if (stringArray[i] == null)
            {
                Debug.LogError("Can't save null entries in the string array when setting " + key);
                return false;
            }
            if (stringArray[i].Length > 255)
            {
                Debug.LogError("Strings cannot be longer than 255 characters when setting " + key);
                return false;
            }
            bytes[_idx++] = (byte)stringArray[i].Length;
        }

        try
        {
            PlayerPrefs.SetString(key, Convert.ToBase64String(bytes) + "|" + string.Join("", stringArray));
        }
        catch
        {
            return false;
        }
        return true;
    }

    public static string[] GetStringArray(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return new string[0];
        var completeString = PlayerPrefs.GetString(key);
        var separatorIndex = completeString.IndexOf("|"[0]);
        if (separatorIndex < 4)
        {
            Debug.LogError("Corrupt preference file for " + key);
            return new string[0];
        }
        var bytes = Convert.FromBase64String(completeString.Substring(0, separatorIndex));
        if ((ArrayType)bytes[0] != ArrayType.String)
        {
            Debug.LogError(key + " is not a string array");
            return new string[0];
        }
        Initialize();

        var numberOfEntries = bytes.Length - 1;
        var stringArray = new string[numberOfEntries];
        var stringIndex = separatorIndex + 1;
        for (var i = 0; i < numberOfEntries; i++)
        {
            int stringLength = bytes[_idx++];
            if (stringIndex + stringLength > completeString.Length)
            {
                Debug.LogError("Corrupt preference file for " + key);
                return new string[0];
            }
            stringArray[i] = completeString.Substring(stringIndex, stringLength);
            stringIndex += stringLength;
        }

        return stringArray;
    }

    public static string[] GetStringArray(string key, string defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetStringArray(key);
        }
        var stringArray = new string[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            stringArray[i] = defaultValue;
        }
        return stringArray;
    }

    public static bool SetIntArray(string key, int[] intArray)
    {
        return SetValue(key, intArray, ArrayType.Int32, 1, ConvertFromInt);
    }

    public static bool SetFloatArray(string key, float[] floatArray)
    {
        return SetValue(key, floatArray, ArrayType.Float, 1, ConvertFromFloat);
    }

    public static bool SetVector2Array(string key, Vector2[] vector2Array)
    {
        return SetValue(key, vector2Array, ArrayType.Vector2, 2, ConvertFromVector2);
    }

    public static bool SetVector3Array(string key, Vector3[] vector3Array)
    {
        return SetValue(key, vector3Array, ArrayType.Vector3, 3, ConvertFromVector3);
    }

    public static bool SetQuaternionArray(string key, Quaternion[] quaternionArray)
    {
        return SetValue(key, quaternionArray, ArrayType.Quaternion, 4, ConvertFromQuaternion);
    }

    public static bool SetColorArray(string key, Color[] colorArray)
    {
        return SetValue(key, colorArray, ArrayType.Color, 4, ConvertFromColor);
    }

    private static bool SetValue<T>(string key, T array, ArrayType arrayType, int vectorNumber, Action<T, byte[], int> convert) where T : IList
    {
        var bytes = new byte[(4 * array.Count) * vectorNumber + 1];
        bytes[0] = Convert.ToByte(arrayType);    // Identifier
        Initialize();

        for (var i = 0; i < array.Count; i++)
        {
            convert(array, bytes, i);
        }
        return SaveBytes(key, bytes);
    }

    private static void ConvertFromInt(int[] array, byte[] bytes, int i)
    {
        ConvertInt32ToBytes(array[i], bytes);
    }

    private static void ConvertFromFloat(float[] array, byte[] bytes, int i)
    {
        ConvertFloatToBytes(array[i], bytes);
    }

    private static void ConvertFromVector2(Vector2[] array, byte[] bytes, int i)
    {
        ConvertFloatToBytes(array[i].x, bytes);
        ConvertFloatToBytes(array[i].y, bytes);
    }

    private static void ConvertFromVector3(Vector3[] array, byte[] bytes, int i)
    {
        ConvertFloatToBytes(array[i].x, bytes);
        ConvertFloatToBytes(array[i].y, bytes);
        ConvertFloatToBytes(array[i].z, bytes);
    }

    private static void ConvertFromQuaternion(Quaternion[] array, byte[] bytes, int i)
    {
        ConvertFloatToBytes(array[i].x, bytes);
        ConvertFloatToBytes(array[i].y, bytes);
        ConvertFloatToBytes(array[i].z, bytes);
        ConvertFloatToBytes(array[i].w, bytes);
    }

    private static void ConvertFromColor(Color[] array, byte[] bytes, int i)
    {
        ConvertFloatToBytes(array[i].r, bytes);
        ConvertFloatToBytes(array[i].g, bytes);
        ConvertFloatToBytes(array[i].b, bytes);
        ConvertFloatToBytes(array[i].a, bytes);
    }

    public static int[] GetIntArray(string key)
    {
        var intList = new List<int>();
        GetValue(key, intList, ArrayType.Int32, 1, ConvertToInt);
        return intList.ToArray();
    }

    public static int[] GetIntArray(string key, int defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetIntArray(key);
        }
        var intArray = new int[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            intArray[i] = defaultValue;
        }
        return intArray;
    }

    public static float[] GetFloatArray(string key)
    {
        var floatList = new List<float>();
        GetValue(key, floatList, ArrayType.Float, 1, ConvertToFloat);
        return floatList.ToArray();
    }

    public static float[] GetFloatArray(string key, float defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetFloatArray(key);
        }
        var floatArray = new float[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            floatArray[i] = defaultValue;
        }
        return floatArray;
    }

    public static Vector2[] GetVector2Array(string key)
    {
        var vector2List = new List<Vector2>();
        GetValue(key, vector2List, ArrayType.Vector2, 2, ConvertToVector2);
        return vector2List.ToArray();
    }

    public static Vector2[] GetVector2Array(string key, Vector2 defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetVector2Array(key);
        }
        var vector2Array = new Vector2[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            vector2Array[i] = defaultValue;
        }
        return vector2Array;
    }

    public static Vector3[] GetVector3Array(string key)
    {
        var vector3List = new List<Vector3>();
        GetValue(key, vector3List, ArrayType.Vector3, 3, ConvertToVector3);
        return vector3List.ToArray();
    }

    public static Vector3[] GetVector3Array(string key, Vector3 defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))

        {
            return GetVector3Array(key);
        }
        var vector3Array = new Vector3[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            vector3Array[i] = defaultValue;
        }
        return vector3Array;
    }

    public static Quaternion[] GetQuaternionArray(string key)
    {
        var quaternionList = new List<Quaternion>();
        GetValue(key, quaternionList, ArrayType.Quaternion, 4, ConvertToQuaternion);
        return quaternionList.ToArray();
    }

    public static Quaternion[] GetQuaternionArray(string key, Quaternion defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetQuaternionArray(key);
        }
        var quaternionArray = new Quaternion[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            quaternionArray[i] = defaultValue;
        }
        return quaternionArray;
    }

    public static Color[] GetColorArray(string key)
    {
        var colorList = new List<Color>();
        GetValue(key, colorList, ArrayType.Color, 4, ConvertToColor);
        return colorList.ToArray();
    }

    public static Color[] GetColorArray(string key, Color defaultValue, int defaultSize)
    {
        if (PlayerPrefs.HasKey(key))
        {
            return GetColorArray(key);
        }
        var colorArray = new Color[defaultSize];
        for (int i = 0; i < defaultSize; i++)
        {
            colorArray[i] = defaultValue;
        }
        return colorArray;
    }

    private static void GetValue<T>(string key, T list, ArrayType arrayType, int vectorNumber, Action<T, byte[]> convert) where T : IList
    {
        if (PlayerPrefs.HasKey(key))
        {
            var bytes = Convert.FromBase64String(PlayerPrefs.GetString(key));
            if ((bytes.Length - 1) % (vectorNumber * 4) != 0)
            {
                Debug.LogError("Corrupt preference file for " + key);
                return;
            }
            if ((ArrayType)bytes[0] != arrayType)
            {
                Debug.LogError(key + " is not a " + arrayType.ToString() + " array");
                return;
            }
            Initialize();

            var end = (bytes.Length - 1) / (vectorNumber * 4);
            for (var i = 0; i < end; i++)
            {
                convert(list, bytes);
            }
        }
    }

    private static void ConvertToInt(List<int> list, byte[] bytes)
    {
        list.Add(ConvertBytesToInt32(bytes));
    }

    private static void ConvertToFloat(List<float> list, byte[] bytes)
    {
        list.Add(ConvertBytesToFloat(bytes));
    }

    private static void ConvertToVector2(List<Vector2> list, byte[] bytes)
    {
        list.Add(new Vector2(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    private static void ConvertToVector3(List<Vector3> list, byte[] bytes)
    {
        list.Add(new Vector3(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    private static void ConvertToQuaternion(List<Quaternion> list, byte[] bytes)
    {
        list.Add(new Quaternion(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    private static void ConvertToColor(List<Color> list, byte[] bytes)
    {
        list.Add(new Color(ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes), ConvertBytesToFloat(bytes)));
    }

    public static void ShowArrayType(string key)
    {
        var bytes = Convert.FromBase64String(PlayerPrefs.GetString(key));
        if (bytes.Length > 0)
        {
            var arrayType = (ArrayType)bytes[0];
            Debug.Log(key + " is a " + arrayType.ToString() + " array");
        }
    }

    private static void Initialize()
    {
        if (BitConverter.IsLittleEndian)
        {
            _endianDiff1 = 0;
            _endianDiff2 = 0;
        }
        else
        {
            _endianDiff1 = 3;
            _endianDiff2 = 1;
        }
        if (_byteBlock == null)
        {
            _byteBlock = new byte[4];
        }
        _idx = 1;
    }

    private static bool SaveBytes(string key, byte[] bytes)
    {
        try
        {
            PlayerPrefs.SetString(key, Convert.ToBase64String(bytes));
        }
        catch
        {
            return false;
        }
        return true;
    }

    private static void ConvertFloatToBytes(float f, byte[] bytes)
    {
        _byteBlock = BitConverter.GetBytes(f);
        ConvertTo4Bytes(bytes);
    }

    private static float ConvertBytesToFloat(byte[] bytes)
    {
        ConvertFrom4Bytes(bytes);
        return BitConverter.ToSingle(_byteBlock, 0);
    }

    private static void ConvertInt32ToBytes(int i, byte[] bytes)
    {
        _byteBlock = BitConverter.GetBytes(i);
        ConvertTo4Bytes(bytes);
    }

    private static int ConvertBytesToInt32(byte[] bytes)
    {
        ConvertFrom4Bytes(bytes);
        return BitConverter.ToInt32(_byteBlock, 0);
    }

    private static void ConvertTo4Bytes(byte[] bytes)
    {
        bytes[_idx] = _byteBlock[_endianDiff1];
        bytes[_idx + 1] = _byteBlock[1 + _endianDiff2];
        bytes[_idx + 2] = _byteBlock[2 - _endianDiff2];
        bytes[_idx + 3] = _byteBlock[3 - _endianDiff1];
        _idx += 4;
    }

    private static void ConvertFrom4Bytes(byte[] bytes)
    {
        _byteBlock[_endianDiff1] = bytes[_idx];
        _byteBlock[1 + _endianDiff2] = bytes[_idx + 1];
        _byteBlock[2 - _endianDiff2] = bytes[_idx + 2];
        _byteBlock[3 - _endianDiff1] = bytes[_idx + 3];
        _idx += 4;
    }

    public static T GetObject<T>(string key, T defaultValue) where T : new()
    {
        var t = typeof(T);
        var fieldInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        var newObj = new T();

        foreach (var fieldInfo in fieldInfos)
        {
            var fieldName = fieldInfo.Name;
            var fieldKey = $"{t}-{fieldInfo.MemberType}-{fieldName}";

            if (fieldInfo.FieldType == typeof(int))
            {
                var val = PlayerPrefs.GetInt(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(float))
            {
                var val = PlayerPrefs.GetFloat(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(string))
            {
                var val = PlayerPrefs.GetString(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(long))
            {
                var val = GetLong(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(Vector2))
            {
                var val = GetVector2(fieldKey, Vector2.zero);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(Quaternion))
            {
                var val = GetQuaternion(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(Color))
            {
                var val = GetColor(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            // array.
            else if (fieldInfo.FieldType == typeof(int[]))
            {
                var val = GetIntArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(float[]))
            {
                var val = GetFloatArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(string[]))
            {
                var val = GetStringArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(Quaternion[]))
            {
                var val = GetQuaternionArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(Color[]))
            {
                var val = GetColorArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            // list.
            else if (fieldInfo.FieldType == typeof(List<int>))
            {
                var val = GetIntArray(fieldKey);
                fieldInfo.SetValue(newObj, val.OfType<int>().ToList());
            }
            else if (fieldInfo.FieldType == typeof(List<float>))
            {
                var val = GetFloatArray(fieldKey);
                fieldInfo.SetValue(newObj, val.OfType<float>().ToList());
            }
            else if (fieldInfo.FieldType == typeof(List<string>))
            {
                var val = GetStringArray(fieldKey);
                fieldInfo.SetValue(newObj, val.OfType<string>().ToList());
            }
            else if (fieldInfo.FieldType == typeof(List<Quaternion>))
            {
                var val = GetQuaternionArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            else if (fieldInfo.FieldType == typeof(List<Color>))
            {
                var val = GetColorArray(fieldKey);
                fieldInfo.SetValue(newObj, val);
            }
            // other.
            else
            {
                Debug.LogError("Unknown type.");
            }
        }

        if (newObj != null)
            return newObj;

        return defaultValue;
    }

    public static bool SetObject(string name, object value)
    {
        var t = value.GetType();
        var fieldInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);

        try
        {
            foreach (var fieldInfo in fieldInfos)
            {
                var fieldName = fieldInfo.Name;
                var fieldKey = $"{name}-{t}-{fieldInfo.MemberType}-{fieldName}";

                if (fieldInfo.FieldType == typeof(int))
                {
                    var val = (int)fieldInfo.GetValue(value);
                    PlayerPrefs.SetInt(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(float))
                {
                    var val = (float)fieldInfo.GetValue(value);
                    PlayerPrefs.SetFloat(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(string))
                {
                    var val = (string)fieldInfo.GetValue(value);
                    PlayerPrefs.SetString(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(long))
                {
                    var val = (long)fieldInfo.GetValue(value);
                    SetLong(fieldKey, val);
                }
                // Unity val.
                else if (fieldInfo.FieldType == typeof(Vector2))
                {
                    var val = (Vector2)fieldInfo.GetValue(value);
                    SetVector2(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(Vector3))
                {
                    var val = (Vector3)fieldInfo.GetValue(value);
                    SetVector3(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(Quaternion))
                {
                    var val = (Quaternion)fieldInfo.GetValue(value);
                    SetQuaternion(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(Color))
                {
                    var val = (Color)fieldInfo.GetValue(value);
                    SetColor(fieldKey, val);
                }
                // array.
                else if (fieldInfo.FieldType == typeof(int[]))
                {
                    var val = (int[])fieldInfo.GetValue(value);
                    SetIntArray(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(float[]))
                {
                    var val = (float[])fieldInfo.GetValue(value);
                    SetFloatArray(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(string[]))
                {
                    var val = (string[])fieldInfo.GetValue(value);
                    SetStringArray(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(Quaternion[]))
                {
                    var val = (Quaternion[])fieldInfo.GetValue(value);
                    SetQuaternionArray(fieldKey, val);
                }
                else if (fieldInfo.FieldType == typeof(Color[]))
                {
                    var val = (Color[])fieldInfo.GetValue(value);
                    SetColorArray(fieldKey, val);
                }
                // list.
                else if (fieldInfo.FieldType == typeof(List<int>))
                {
                    var val = (List<int>)fieldInfo.GetValue(value);
                    SetIntArray(fieldKey, val.ToArray());
                }
                else if (fieldInfo.FieldType == typeof(List<float>))
                {
                    var val = (List<float>)fieldInfo.GetValue(value);
                    SetFloatArray(fieldKey, val.ToArray());
                }
                else if (fieldInfo.FieldType == typeof(List<string>))
                {
                    var val = (List<string>)fieldInfo.GetValue(value);
                    SetStringArray(fieldKey, val.ToArray());
                }
                else if (fieldInfo.FieldType == typeof(List<Quaternion>))
                {
                    var val = (Quaternion[])fieldInfo.GetValue(value);
                    SetQuaternionArray(fieldKey, val.ToArray());
                }
                else if (fieldInfo.FieldType == typeof(List<Color>))
                {
                    var val = (Color[])fieldInfo.GetValue(value);
                    SetColorArray(fieldKey, val.ToArray());
                }
                else
                {
                    Debug.LogError("Unknown type.");
                }
            }
        }
        catch
        {
            return false;
        }
        return true;
    }
}