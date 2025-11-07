using ChartAndGraph;
using System;
using UnityEngine;

public class AirTemperature : MonoBehaviour
{
    [Header("Chart")]
    [Tooltip("Chart And Graph の GraphChart 参照")]
    public GraphChart AirTemp;

    // 定数
    private const int DaysPerYear = 365;
    private const double YTickSnap = 5.0; // 縦軸丸め刻み

    /// <summary>
    /// カテゴリ/スクロール/軸ビュー/ラベルを初期化する（安全ガード付き）
    /// </summary>
    private void ResetChart()
    {
        if (AirTemp == null || AirTemp.DataSource == null) return;

        var ds = AirTemp.DataSource;
        if (ds.HasCategory("AirTemp"))
            ds.ClearCategory("AirTemp");

        AirTemp.HorizontalValueToStringMap.Clear();
        AirTemp.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// データ読み込み・縦軸スケール決定・ポイント一括投入・初期ラベル/表示反映
    /// </summary>
    public void Setup()
    {
        if (AirTemp == null || AirTemp.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- 縦軸範囲の算出 ---
        double yMax = 10.0;   // 初期値（上側）
        double yMin = -10.0;  // 初期値（下側）

        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int doy = 0; doy < DaysPerYear; doy++)
            {
                double v = Director.Instance.tmp_air[yr][doy];
                if (v > yMax) yMax = v;
                if (v < yMin) yMin = v;
            }
        }

        // 丸め（5度刻み）
        yMax = Math.Ceiling(yMax / YTickSnap) * YTickSnap;
        yMin = Math.Floor(yMin / YTickSnap) * YTickSnap;

        AirTemp.DataSource.VerticalViewSize = yMax - yMin;
        AirTemp.DataSource.VerticalViewOrigin = yMin;

        // --- ラベルは UpdateValues() で作るため一旦クリアだけ ---
        AirTemp.HorizontalValueToStringMap.Clear();

        // --- ポイントを一括投入 ---
        var ds = AirTemp.DataSource;
        ds.StartBatch();
        ds.ClearCategory("AirTemp");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int doy = 0; doy < DaysPerYear; doy++)
            {
                ds.AddPointToCategory("AirTemp", i, Director.Instance.tmp_air[yr][doy]);
                i++;
            }
        }
        ds.EndBatch(); // ここで一括反映

        // --- 初期の表示/ラベルを反映 ---
        UpdateValues();
    }

    /// <summary>
    /// 横軸ラベル（1年表示: 月ラベル）
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (AirTemp == null) return;
        var map = AirTemp.HorizontalValueToStringMap;
        map.Clear();

        // Day-of-year の代表日で月ラベル（必要なら後で厳密な月日換算に変更可能）
        // だいたい 2/15, 4/14, 6/15, 8/15, 10/15, 12/15 あたりを指す既存ロジックを踏襲
        for (int idx = 0; idx <= Director.SimYearMax * DaysPerYear; idx++)
        {
            string label = "";
            int d = idx % DaysPerYear;
            if (d == 45) label = "Feb";
            if (d == 104) label = "Apr";
            if (d == 166) label = "Jun";
            if (d == 227) label = "Aug";
            if (d == 288) label = "Oct";
            if (d == 349) label = "Dec";
            map[idx] = label;
        }
    }

    /// <summary>
    /// 横軸ラベル（複数年表示: 各年の先頭日に年表記）
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (AirTemp == null) return;
        var map = AirTemp.HorizontalValueToStringMap;
        map.Clear();

        int counter = 0;
        for (int yr = 1; yr <= Director.SimYearMax + Director.SimTimeRangeMax; yr++)
        {
            for (int doy = 1; doy <= DaysPerYear; doy++)
            {
                map[counter] = (doy == 1) ? yr.ToString() : "";
                counter++;
            }
        }
    }

    /// <summary>
    /// 外部状態（表示開始年・表示年数）から横軸表示を更新し、ラベルを作成
    /// </summary>
    public void UpdateValues()
    {
        if (AirTemp == null || AirTemp.DataSource == null) return;

        // スクロール開始位置（年→日オフセット）
        int yearView = Director.SimYear;
        AirTemp.HorizontalScrolling = (yearView - 1) * DaysPerYear;

        // 表示幅（年数→日数）
        int yearViewRange = Director.SimTimeRange;
        AirTemp.DataSource.HorizontalViewSize = yearViewRange * DaysPerYear;

        // ラベル
        if (yearViewRange == 1)
        {
            BuildMonthLabelsForOneYear();
        }
        else
        {
            BuildYearLabelsForMultiYears();
        }
    }
}
