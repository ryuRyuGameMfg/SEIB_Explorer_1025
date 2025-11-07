using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: WindVelocity
/// 月平均風速の時系列を描画し、開始年・表示年数に応じて
/// 横スクロールとラベル（1年: 月名 / 複数年: 年号）を切り替える。
/// </summary>
public class WindVelocity : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart WIND;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 1.0; // 縦軸丸め刻み（m/s）

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。対象カテゴリがある場合のみクリアする。
    /// </summary>
    private void ResetChart()
    {
        if (WIND == null || WIND.DataSource == null) return;

        var ds = WIND.DataSource;
        if (ds.HasCategory("WIND"))
            ds.ClearCategory("WIND");

        WIND.HorizontalValueToStringMap.Clear();
        WIND.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// データから縦軸上限を求めて丸め設定し、全期間の月次ポイントを一括投入。
    /// 仕上げに UpdateValues() で表示幅/ラベルを確定する。
    /// </summary>
    public void Setup()
    {
        if (WIND == null || WIND.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- 縦軸スケール算出（上端は最大値の1刻み上にスナップ、下端は0固定）---
        double yMax = 1.0; // 低すぎるレンジ回避のための初期値
        for (int yr = 0; yr < Director.SimYearMax; yr++)
            for (int mon = 0; mon < MonthsPerYear; mon++)
                yMax = Math.Max(yMax, Director.Instance.wind_m[yr][mon]);

        yMax = Math.Ceiling(yMax / YTickSnap) * YTickSnap;

        WIND.DataSource.VerticalViewOrigin = 0.0;
        WIND.DataSource.VerticalViewSize = yMax;

        // --- ポイントの一括投入 ---
        var ds = WIND.DataSource;
        ds.StartBatch();
        ds.ClearCategory("WIND");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                ds.AddPointToCategory("WIND", i, Director.Instance.wind_m[yr][mon]);
                i++;
            }
        }
        ds.EndBatch();

        // 初期の表示/ラベル反映
        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化（可視範囲外は空文字）。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (WIND == null) return;
        var map = WIND.HorizontalValueToStringMap;
        map.Clear();

        int total = (Director.SimYearMax + Director.SimTimeRangeMax) * MonthsPerYear;
        for (int idx = 0; idx < total; idx++)
        {
            string label = "";
            int m = idx % MonthsPerYear;
            if (m == 1) label = "Feb";
            if (m == 3) label = "Apr";
            if (m == 5) label = "Jun";
            if (m == 7) label = "Aug";
            if (m == 9) label = "Oct";
            if (m == 11) label = "Dec";
            map[idx] = label;
        }
    }

    /// <summary>
    /// 複数年表示: 各年の1月位置に年号（可視範囲外は空文字）。
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (WIND == null) return;
        var map = WIND.HorizontalValueToStringMap;
        map.Clear();

        int counter = 0;
        for (int yr = 1; yr <= Director.SimYearMax + Director.SimTimeRangeMax; yr++)
        {
            for (int mon = 1; mon <= MonthsPerYear; mon++)
            {
                map[counter] = (mon == 1) ? yr.ToString() : "";
                counter++;
            }
        }
    }

    /// <summary>
    /// 外部状態（開始年・表示年数）から横軸スクロール/表示幅を更新し、ラベルを再構築。
    /// </summary>
    public void UpdateValues()
    {
        if (WIND == null || WIND.DataSource == null) return;

        // 横軸スクロールと表示幅（※ズレる場合のみ -1 に調整）
        int yearView = Director.SimYear;
        WIND.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        WIND.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear;

        // ラベル
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }
}
