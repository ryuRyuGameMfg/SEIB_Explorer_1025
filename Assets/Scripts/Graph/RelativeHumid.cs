using ChartAndGraph;
using System;
using UnityEngine;

public class RelativeHumid : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart RH;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 5.0; // 縦軸丸め刻み(%)

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）
    /// </summary>
    private void ResetChart()
    {
        if (RH == null || RH.DataSource == null) return;

        var ds = RH.DataSource;
        if (ds.HasCategory("RH"))
            ds.ClearCategory("RH");

        RH.HorizontalValueToStringMap.Clear();
        RH.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// データから縦軸範囲(最小～最大)を求めて5刻み丸めで設定し、
    /// RHカテゴリへ月次ポイントを一括投入。最後に UpdateValues()。
    /// </summary>
    public void Setup()
    {
        if (RH == null || RH.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- 縦軸範囲の算出 ---
        double yMax = 0.0;    // 上側初期値
        double yMin = 100.0;  // 下側初期値

        for (int yr = 0; yr < Director.SimYearMax; yr++)
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                double v = Director.Instance.rh_m[yr][mon];
                if (v > yMax) yMax = v;
                if (v < yMin) yMin = v;
            }

        // 丸め（5%刻み）＆最小レンジ確保
        yMax = Math.Ceiling(yMax / YTickSnap) * YTickSnap;
        yMin = Math.Floor(yMin / YTickSnap) * YTickSnap;

        double viewSize = Math.Max(yMax - yMin, 5.0); // 最低レンジ5%
        RH.DataSource.VerticalViewSize = viewSize;
        RH.DataSource.VerticalViewOrigin = yMin;

        // --- ラベルは UpdateValues() で作るため一度クリア ---
        RH.HorizontalValueToStringMap.Clear();

        // --- ポイントを一括投入 ---
        var ds = RH.DataSource;
        ds.StartBatch();
        ds.ClearCategory("RH");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                ds.AddPointToCategory("RH", i, Director.Instance.rh_m[yr][mon]);
                i++;
            }

        ds.EndBatch();

        // 初期の表示/ラベル反映
        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (RH == null) return;
        var map = RH.HorizontalValueToStringMap;
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
    /// 複数年表示: 各年の1月位置に年号
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (RH == null) return;
        var map = RH.HorizontalValueToStringMap;
        map.Clear();

        int counter = 0;
        for (int yr = 1; yr <= Director.SimYearMax + Director.SimTimeRangeMax; yr++)
            for (int mon = 1; mon <= MonthsPerYear; mon++)
            {
                map[counter] = (mon == 1) ? yr.ToString() : "";
                counter++;
            }
    }

    /// <summary>
    /// 外部状態(開始年・表示年数)から横軸スクロール/表示幅を更新し、ラベルを再構築
    /// </summary>
    public void UpdateValues()
    {
        if (RH == null || RH.DataSource == null) return;

        // スクロール位置（年→月オフセット）
        int yearView = Director.SimYear;
        RH.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        // 表示幅（年数→月数）
        int yearViewRange = Director.SimTimeRange;
        RH.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // ※必要なら -1 に調整

        // ラベル
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }
}
