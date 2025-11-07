using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: CarbonFlux
/// 月次の炭素フラックスを積み上げ表示（正: GPP、負: RespA→RespH→Fire）。
/// 表示開始年・表示年数に応じて横スクロール/ラベルを更新し、表示レンジからY軸を自動設定する。
/// </summary>
public class CarbonFlux : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart c_flux;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 0.5; // 縦軸丸め刻み

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。存在するカテゴリのみクリア。
    /// </summary>
    private void ResetChart()
    {
        if (c_flux == null || c_flux.DataSource == null) return;

        var ds = c_flux.DataSource;
        foreach (var cat in new[] { "GPP", "Hline", "RespA", "RespH", "Fire" })
        {
            if (ds.HasCategory(cat)) ds.ClearCategory(cat);
        }

        c_flux.HorizontalValueToStringMap.Clear();
        c_flux.HorizontalScrolling = 0;
        ds.HorizontalViewOrigin = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 全期間の月次データを一括投入（正: GPP、負: RespA/RespH/Fire）。
    /// 初期の軸/ラベルは UpdateValues() で確定させる。
    /// </summary>
    public void Setup()
    {
        if (c_flux == null || c_flux.DataSource == null || Director.Instance == null) return;

        ResetChart();

        var ds = c_flux.DataSource;
        ds.StartBatch();

        ds.ClearCategory("GPP");
        ds.ClearCategory("Hline");
        ds.ClearCategory("RespA");
        ds.ClearCategory("RespH");
        ds.ClearCategory("Fire");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double gpp = Director.Instance.fluxC_gpp[yr][mo];
                double ra = Director.Instance.fluxC_atr[yr][mo];
                double rh = Director.Instance.fluxC_htr[yr][mo];
                double fir = Director.Instance.fluxC_fir[yr][mo];

                ds.AddPointToCategory("GPP", i, gpp);
                ds.AddPointToCategory("Hline", i, 0.000001);       // 0線の描画安定用
                ds.AddPointToCategory("RespA", i, -ra);
                ds.AddPointToCategory("RespH", i, -(ra + rh));
                ds.AddPointToCategory("Fire", i, -(ra + rh + fir));
                i++;
            }
        }
        ds.EndBatch();

        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化（可視範囲外は空文字）。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (c_flux == null) return;
        var map = c_flux.HorizontalValueToStringMap;
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
    /// 複数年表示: 各年の1月位置に年号（可視範囲外は空文字）。
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (c_flux == null) return;
        var map = c_flux.HorizontalValueToStringMap;
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
    /// 外部状態（開始年・表示年数）から横軸スクロール/表示幅を更新し、
    /// 表示レンジ内のデータからY軸（0.5刻み、正負対称）を決定。ラベルも再構築。
    /// </summary>
    public void UpdateValues()
    {
        if (c_flux == null || c_flux.DataSource == null) return;

        int yearView = Director.SimYear;
        c_flux.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        c_flux.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // 必要なら -1 に

        // ---- Y軸: 表示レンジ内の正/負最大を計算 ----
        double posMax = 0.5; // GPP 側
        double negMax = 0.5; // RespA+RespH+Fire 側（絶対値）

        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yrRef = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double gpp = Director.Instance.fluxC_gpp[yrRef][mo];
                double ra = Director.Instance.fluxC_atr[yrRef][mo];
                double rh = Director.Instance.fluxC_htr[yrRef][mo];
                double fir = Director.Instance.fluxC_fir[yrRef][mo];

                double negSum = ra + rh + fir;

                if (gpp > posMax) posMax = gpp;
                if (negSum > negMax) negMax = negSum;
            }
        }

        posMax = Math.Ceiling(posMax / YTickSnap) * YTickSnap;
        negMax = Math.Ceiling(negMax / YTickSnap) * YTickSnap;

        c_flux.DataSource.VerticalViewSize = posMax + negMax;
        c_flux.DataSource.VerticalViewOrigin = -negMax;

        // ---- ラベル ----
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }
}
