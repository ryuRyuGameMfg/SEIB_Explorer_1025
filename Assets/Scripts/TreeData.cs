using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// このクラスは木のプロパティを保持します。

[System.Serializable]
public class TreeData
{
    // 年
    public int Year { get; set; }

    // 幹のX座標
    public float BoleX { get; set; }

    // 幹のY座標
    public float BoleY { get; set; }

    // 樹冠のX座標
    public float CrownX { get; set; }

    // 樹冠のY座標
    public float CrownY { get; set; }

    // 幹の高さ
    public float BoleH { get; set; }

    // 樹冠の高さ
    public float CrownH { get; set; }

    // 幹の直径
    public float BoleD { get; set; }

    // 樹冠の直径
    public float CrownD { get; set; }

    // 植生機能型 (Plant Functional Type)
    public int PFT { get; set; }

    // 高さ
    public float Height { get; set; }

    // パラメータなしのコンストラクタ
    public TreeData()
    {
    }

    // コピーコンストラクタ
    public TreeData(TreeData other)
    {
        Year = other.Year;
        BoleX = other.BoleX;
        BoleY = other.BoleY;
        CrownX = other.CrownX;
        CrownY = other.CrownY;
        BoleH = other.BoleH;
        CrownH = other.CrownH;
        BoleD = other.BoleD;
        CrownD = other.CrownD;
        PFT = other.PFT;
        Height = other.Height;
    }

    // クローンメソッド
    public TreeData Clone()
    {
        return new TreeData(this);
    }
}
