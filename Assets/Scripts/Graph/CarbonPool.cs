using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: CarbonPool
/// 木本/草本/リター/SOM1/SOM2 の炭素プールを月次で積み上げ表示する。
/// 表示開始年・表示年数に応じて横スクロール/ラベルを更新し、Y軸は合計量から自動設定。
/// </summary>
public class CarbonPool : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart c_pool;

    private const int MonthsPerYear = 12;
    private const double YTickSnap = 10.0; // 縦軸丸め刻み（10単位）

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// </summary>
    private void ResetChart()
    {
        if (c_pool == null || c_pool.DataSource == null) return;

        var ds = c_pool.DataSource;
        foreach (var cat in new[] { "Tree", "Grass", "Litter", "SOM1", "SOM2" })
        {
            if (ds.HasCategory(cat)) ds.ClearCategory(cat);
        }

        c_pool.HorizontalValueToStringMap.Clear();
        c_pool.HorizontalScrolling = 0;
        ds.HorizontalViewOrigin = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 全期間の月次データを一括投入（上から Tree→Grass→Litter→SOM1→SOM2 の順に積む）。
    /// 初期の軸/ラベルは UpdateValues() で確定させる。
    /// </summary>
    public void Setup()
    {
        if (c_pool == null || c_pool.DataSource == null || Director.Instance == null) return;

        ResetChart();

        var ds = c_pool.DataSource;
        ds.StartBatch();

        ds.ClearCategory("Tree");
        ds.ClearCategory("Grass");
        ds.ClearCategory("Litter");
        ds.ClearCategory("SOM1");
        ds.ClearCategory("SOM2");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double woody = Director.Instance.poolC_Woody[yr][mo];
                double grass = Director.Instance.poolC_Grass[yr][mo];
                double litter = Director.Instance.poolC_Litter[yr][mo];
                double som1 = Director.Instance.poolC_SOM1[yr][mo];
                double som2 = Director.Instance.poolC_SOM2[yr][mo];

                // 上から順に合計を持たせて塗りつぶしを作る
                ds.AddPointToCategory("Tree", i, som2 + som1 + litter + grass + woody);
                ds.AddPointToCategory("Grass", i, som2 + som1 + litter + grass);
                ds.AddPointToCategory("Litter", i, som2 + som1 + litter);
                ds.AddPointToCategory("SOM1", i, som2 + som1);
                ds.AddPointToCategory("SOM2", i, som2);
                i++;
            }
        }
        ds.EndBatch();

        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (c_pool == null) return;
        var map = c_pool.HorizontalValueToStringMap;
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
    /// 複数年表示: 各年の1月位置に年号。
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (c_pool == null) return;
        var map = c_pool.HorizontalValueToStringMap;
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
    /// 表示レンジの合計最大からY軸（10刻み）を決定。ラベルも再構築。
    /// </summary>
    public void UpdateValues()
    {
        if (c_pool == null || c_pool.DataSource == null) return;

        int yearView = Director.SimYear;
        c_pool.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        c_pool.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // 必要なら -1 に調整

        // ---- 表示レンジ内の合計最大値を求める ----
        double yMax = 1.0;
        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yrRef = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double woody = Director.Instance.poolC_Woody[yrRef][mo];
                double grass = Director.Instance.poolC_Grass[yrRef][mo];
                double litter = Director.Instance.poolC_Litter[yrRef][mo];
                double som1 = Director.Instance.poolC_SOM1[yrRef][mo];
                double som2 = Director.Instance.poolC_SOM2[yrRef][mo];
                double sum = woody + grass + litter + som1 + som2;

                if (sum > yMax) yMax = sum;
            }
        }

        // 10刻みにスナップ
        yMax = Math.Ceiling(yMax / YTickSnap) * YTickSnap;
        c_pool.DataSource.VerticalViewOrigin = 0.0;
        c_pool.DataSource.VerticalViewSize = yMax;

        // ---- ラベル ----
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }
}
