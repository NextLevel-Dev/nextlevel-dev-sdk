using System.Globalization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public static class NextLevelUtilities
{
    public const string NumberCommaFormat = "N0";

    public static void AddEventTrigger(EventTrigger eventTrigger, UnityAction action, EventTriggerType triggerType)
    {
        var trigger = new EventTrigger.TriggerEvent();
        trigger.AddListener((eventData) => action());

        var entry = new EventTrigger.Entry() { callback = trigger, eventID = triggerType };
        eventTrigger.triggers.Add(entry);
    }

    public static void RemoveAllEventTrigger(EventTrigger eventTrigger)
    {
        for (int i = 0; i < eventTrigger.triggers.Count; i++)
        {
            var entry = eventTrigger.triggers[i];
            var trigger = entry.callback;
            trigger.RemoveAllListeners();
            eventTrigger.triggers.Remove(entry);
        }
    }

    public static string ColorToHex(Color32 color)
    {
        return color.r.ToString("X2") + color.g.ToString("X2") + color.b.ToString("X2");
    }

    public static Color HexToColor(string hex)
    {
        byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        return new Color32(r, g, b, 255);
    }

    public static float ReduceDecimals(float value, float factor = 1000)
    {
        return Mathf.Round(value * factor) / factor;
    }

    public static float ScaleNumber(float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) * (toMax - toMin) / (fromMax - fromMin) + toMin;
    }

    public static void SplitULong(ulong value, out uint lowBits, out uint highBits)
    {
        lowBits = (uint)(value & uint.MaxValue);
        highBits = (uint)(value >> 32);
    }

    public static void MergeUInt(uint lowBits, uint highBits, out ulong value)
    {
        value = ((ulong)highBits << 32) | lowBits;
    }
}