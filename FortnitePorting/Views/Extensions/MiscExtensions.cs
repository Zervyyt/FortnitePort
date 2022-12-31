﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using SkiaSharp;

namespace FortnitePorting.Views.Extensions;

public static class MiscExtensions
{
    private static readonly Random RandomGen = new();

    public static bool MoveToEnd<T>(this List<T> list, Func<T, bool> predicate)
    {
        var found = list.FirstOrDefault(predicate);
        if (found is null) return false;

        var removed = list.Remove(found);
        if (!removed) return false;
        list.Add(found);

        return true;
    }

    public static byte[] ToBytes(this Stream str)
    {
        var bytes = new BinaryReader(str).ReadBytes((int) str.Length);
        return bytes;
    }

    public static string TitleCase(this string text)
    {
        var textInfo = CultureInfo.CurrentCulture.TextInfo;
        return textInfo.ToTitleCase(text);
    }

    public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    public static string CommaJoin<T>(this IEnumerable<T> enumerable)
    {
        var list = enumerable.ToList();
        return list.Count > 1 ? string.Join(", ", list.Take(list.Count - 1)) + ", and " + list.Last() : list.First().ToString();
    }

    public static T Random<T>(this IEnumerable<T> enumerable)
    {
        var list = enumerable.ToList();
        var index = RandomGen.Next(0, list.Count);
        return list[index];
    }
    
    public static string AsString(this byte[] bytes)
    {
        return Encoding.UTF8.GetString(bytes);
    }
}