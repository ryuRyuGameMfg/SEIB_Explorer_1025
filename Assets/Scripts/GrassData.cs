using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GrassData
{
    public int Year { get; private set; }
    public float[,] LAI_floor { get; private set; }

    public GrassData(int year, float[,] lai_Floor)
    {
        Year = year;
        LAI_floor = lai_Floor;
    }

    public float[,] GetLAI_floor_byYear(int year)
    {
        if (Year == year)
        {
            return LAI_floor;
        }
        else
        {
            throw new ArgumentException("指定されたYearは存在しません。");
        }
    }
}

