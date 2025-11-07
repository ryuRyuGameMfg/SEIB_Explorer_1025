using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: CarbonReloc
/// 月次の炭素再配置フラックス（lit, som）を折れ線/面で表示する。
/// 表示開始年・表示年数に応じて横スクロール/ラベルを更新し、Y軸は表示レンジから自動設定。
/// </summary>
public class CarbonReloc : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart c_reloc;

    private const int MonthsPerYear = 12;
    private const double YTickSnap = 0.5; // 縦軸丸め刻み

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベル初期化（安全ガード付き）
    /// </summary>
    private void ResetChart()
    {
        if (c_reloc == null || c_reloc.DataSource == null) return;

        var ds = c_reloc.DataSource;
        foreach (var cat in new[] { "fluxClit", "fluxCsom" })
        {
            if (ds.HasCategory(cat)) ds.ClearCategory(cat);
        }

        c_reloc.HorizontalValueToStringMap.Clear();
        c_reloc.HorizontalScrolling = 0;
        ds.HorizontalViewOrigin = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 全期間データを一括投入。軸/ラベルは UpdateValues() で確定。
    /// </summary>
    public void Setup()
    {
        if (c_reloc == null || c_reloc.DataSource == null || Director.Instance == null) return;

        ResetChart();

        var ds = c_reloc.DataSource;
        ds.StartBatch();

        ds.ClearCategory("fluxClit");
        ds.ClearCategory("fluxCsom");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                ds.AddPointToCategory("fluxClit", i, Director.Instance.fluxC_lit[yr][mo]);
                ds.AddPointToCategory("fluxCsom", i, Director.Instance.fluxC_som[yr][mo]);
                i++;
            }
        }
        ds.EndBatch();

        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (c_reloc == null) return;
        var map = c_reloc.HorizontalValueToStringMap;
        map.Clear();

        int total = (Director.SimYearMax + Director.SimTimeRangeMax) * MonthsPerYear;
        for (int idx = 0; idx < total; idx++)
        {
            int m = idx % MonthsPerYear;
            map[idx] = (m == 1) ? "Feb" :
                       (m == 3) ? "Apr" :
                       (m == 5) ? "Jun" :
                       (m == 7) ? "Aug" :
                       (m == 9) ? "Oct" :
                       (m == 11) ? "Dec" : "";
        }
    }

    /// <summary>
    /// 複数年表示: 各年の1月位置に年号
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (c_reloc == null) return;
        var map = c_reloc.HorizontalValueToStringMap;
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
    /// スライダー状態から横軸スクロール/表示幅を反映し、
    /// 表示レンジ内の最大からY軸（0.5刻み、下限0）を決定。ラベルも再構築。
    /// </summary>
    public void UpdateValues()
    {
        if (c_reloc == null || c_reloc.DataSource == null) return;

        // 横軸スクロール・表示幅
        int yearView = Director.SimYear;
        c_reloc.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        c_reloc.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // ずれる場合は -1 に

        // ---- Y軸: 表示レンジ内最大値 ----
        double yMax = 0.5;
        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yrRef = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                yMax = Math.Max(yMax, Director.Instance.fluxC_lit[yrRef][mo]);
                yMax = Math.Max(yMax, Director.Instance.fluxC_som[yrRef][mo]);
            }
        }
        yMax = Math.Ceiling(yMax / YTickSnap) * YTickSnap;

        c_reloc.DataSource.VerticalViewOrigin = 0.0;
        c_reloc.DataSource.VerticalViewSize = yMax;

        // ---- ラベル ----
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }
}
