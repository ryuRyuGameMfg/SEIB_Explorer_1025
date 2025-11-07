using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: Albedo
/// 地表・植生・平均のアルベド（0–1）を日次で描画する。
/// 表示開始年・表示年数に応じて横スクロール/ラベルを切り替え、Y軸は表示範囲から動的に設定。
/// </summary>
public class Albedo : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart albedo;

    // 縦軸スナップと最低レンジ（必要ならInspector化してもOK）
    private const double YTickSnap = 0.05;
    private const double MinViewRange = 0.05;

    /// <summary>
    /// 実データの1年の日数を取得（albedo_mean[yr].Count）。取得不能なら365にフォールバック。
    /// </summary>
    private int GetDaysPerYear()
    {
        var inst = Director.Instance;
        if (inst?.albedo_mean != null && inst.albedo_mean.Count > 0 && inst.albedo_mean[0] != null)
            return inst.albedo_mean[0].Count; // List<List<float>> は Count
        return 365;
    }

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。存在するカテゴリのみクリア。
    /// </summary>
    private void ResetChart()
    {
        if (albedo == null || albedo.DataSource == null) return;

        var ds = albedo.DataSource;
        foreach (var cat in new[] { "AlbedoMean", "AlbedoVeg", "AlbedoSoil" })
        {
            if (ds.HasCategory(cat)) ds.ClearCategory(cat);
        }

        albedo.HorizontalValueToStringMap.Clear();
        albedo.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// データを全期間投入。Y軸/ラベルは UpdateValues() で表示レンジに合わせて確定。
    /// </summary>
    public void Setup()
    {
        if (albedo == null || albedo.DataSource == null || Director.Instance == null) return;

        ResetChart();

        int days = GetDaysPerYear();
        var ds = albedo.DataSource;
        ds.StartBatch();

        ds.ClearCategory("AlbedoMean");
        ds.ClearCategory("AlbedoVeg");
        ds.ClearCategory("AlbedoSoil");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int d = 0; d < days; d++)
            {
                ds.AddPointToCategory("AlbedoMean", i, Director.Instance.albedo_mean[yr][d]);
                ds.AddPointToCategory("AlbedoVeg", i, Director.Instance.albedo_veg[yr][d]);
                ds.AddPointToCategory("AlbedoSoil", i, Director.Instance.albedo_soil[yr][d]);
                i++;
            }
        }
        ds.EndBatch();

        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec にラベル（可視範囲外は空文字）。
    /// </summary>
    private void BuildMonthLabelsForOneYear(int days)
    {
        if (albedo == null) return;
        var map = albedo.HorizontalValueToStringMap;
        map.Clear();

        // 非うるう年の月開始DOY（0始まり）
        int[] monthStart = { 0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334 };
        int[] labelMonths = { 1, 3, 5, 7, 9, 11 }; // Feb,Apr,Jun,Aug,Oct,Dec

        int total = Director.SimYearMax * days;
        for (int idx = 0; idx < total; idx++) map[idx] = "";

        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            foreach (var m in labelMonths)
            {
                int anchor = monthStart[m] + 14;           // 月中頃
                if (anchor >= days) anchor = monthStart[m];
                int x = yr * days + anchor;
                if (x < total)
                    map[x] = (m == 1) ? "Feb" :
                             (m == 3) ? "Apr" :
                             (m == 5) ? "Jun" :
                             (m == 7) ? "Aug" :
                             (m == 9) ? "Oct" : "Dec";
            }
        }
    }

    /// <summary>
    /// 複数年表示: 各年の先頭日に年号（可視範囲外は空文字）。
    /// </summary>
    private void BuildYearLabelsForMultiYears(int days)
    {
        if (albedo == null) return;
        var map = albedo.HorizontalValueToStringMap;
        map.Clear();

        int total = (Director.SimYearMax + Director.SimTimeRangeMax) * days;
        for (int idx = 0; idx < total; idx++) map[idx] = "";

        int pos = 0;
        for (int yr = 1; yr <= Director.SimYearMax + Director.SimTimeRangeMax; yr++)
        {
            if (pos < total) map[pos] = yr.ToString();
            pos += days;
        }
    }

    /// <summary>
    /// 開始年・表示年数から横スクロール/表示幅を更新し、
    /// 表示レンジ内の min/max からY軸（0.05刻み＋最小0.05）を決定。ラベルも再構築。
    /// </summary>
    public void UpdateValues()
    {
        if (albedo == null || albedo.DataSource == null) return;

        int days = GetDaysPerYear();

        // 横軸スクロールと表示幅（※ズレる場合のみ -1 に調整可）
        int yearView = Director.SimYear;
        albedo.HorizontalScrolling = (yearView - 1) * days;

        int yearViewRange = Director.SimTimeRange;
        albedo.DataSource.HorizontalViewSize = yearViewRange * days;

        // ---- Y軸: 表示レンジ内の min/max から決定 ----
        double aMax = 0.0;
        double aMin = 1.0;

        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yrRef = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int d = 0; d < days; d++)
            {
                double v1 = Director.Instance.albedo_mean[yrRef][d];
                double v2 = Director.Instance.albedo_veg[yrRef][d];
                double v3 = Director.Instance.albedo_soil[yrRef][d];

                double vmax = Math.Max(v1, Math.Max(v2, v3));
                double vmin = Math.Min(v1, Math.Min(v2, v3));

                if (vmax > aMax) aMax = vmax;
                if (vmin < aMin) aMin = vmin;
            }
        }

        // 0.05刻みにスナップし、最低レンジを確保
        aMax = Math.Ceiling(aMax / YTickSnap) * YTickSnap;
        aMin = Math.Floor(aMin / YTickSnap) * YTickSnap;
        double viewSize = Math.Max(aMax - aMin, MinViewRange);

        albedo.DataSource.VerticalViewOrigin = aMin;
        albedo.DataSource.VerticalViewSize = viewSize;

        // ---- ラベル ----
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear(days);
        else
            BuildYearLabelsForMultiYears(days);
    }
}
