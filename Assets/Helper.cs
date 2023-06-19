using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public static class Helper{

	public static bool IsPrime(int number)
    {
        if (number <= 1) return false;
        if (number == 2) return true;
        if (number % 2 == 0) return false;

        var boundary = (int)Math.Floor(Math.Sqrt(number));

        for (int i = 3; i <= boundary; i += 2)
            if (number % i == 0)
                return false;

        return true;
    }

    public static List<string> MergeColors(List<string>[] colors)
    {
        List<string> finalColors = new List<string>();

        for (int i = 0; i < colors.Length; i++)
        {
            int[] newColor = new int[] { 0, 0, 0 };
            for (int color = 0; color < colors[i].Count; color++)
            {
                newColor[0] = (newColor[0] + Int32.Parse(colors[i][color][0].ToString())) % 2;
                newColor[1] = (newColor[1] + Int32.Parse(colors[i][color][1].ToString())) % 2;
                newColor[2] = (newColor[2] + Int32.Parse(colors[i][color][2].ToString())) % 2;
            }

            finalColors.Add(newColor.Join(""));
        }
        return finalColors;
    }

    public static List<string> ShiftListClockwise(List<string> list, int shifts)
    {
        string nItem;

        for (int i = 0; i < shifts; i++)
        {
            nItem = list[list.Count - 1];     // get the last item of the list
            list.RemoveAt(list.Count - 1);  // remove this item from the end  ...
            list.Insert(0, nItem);         // ... and insert this (last) item in front of the list
        }
        return list;
    }
}
