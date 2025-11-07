using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: WaterFlux
/// 月次の水フラックスを積み上げ表示（正: 有効降水 Pre=pre-sn、負: Ic/Tr/Ev/RO1/RO2 を順に積む）。
/// 表示開始年・表示年数に応じて横軸スクロール/ラベルを更新し、
/// 表示レンジのデータから動的にY軸スケールを決定する。
/// </summary>
public class WaterFlux : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart w_flux;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 10.0; // 縦軸丸め刻み

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）
    /// </summary>
    private void ResetChart()
    {
        if (w_flux == null || w_flux.DataSource == null) return;

        var ds = w_flux.DataSource;
        foreach (var cat in new[] { "Pre", "Hline", "Ic", "Tr", "Ev", "RO1", "RO2" })
        {
            if (ds.HasCategory(cat))
                ds.ClearCategory(cat);
        }

        w_flux.HorizontalValueToStringMap.Clear();
        w_flux.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 全期間の月次データを一括投入（正: Pre=pre-sn、負: Ic/Tr/Ev/RO1/RO2）。
    /// </summary>
    public void Setup()
    {
        if (w_flux == null || w_flux.DataSource == null || Director.Instance == null) return;

        ResetChart();

        var ds = w_flux.DataSource;
        ds.StartBatch();

        ds.ClearCategory("Pre");
        ds.ClearCategory("Hline");
        ds.ClearCategory("Ic");
        ds.ClearCategory("Tr");
        ds.ClearCategory("Ev");
        ds.ClearCategory("RO1");
        ds.ClearCategory("RO2");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double pre = Director.Instance.fluxW_pre[yr][mo];
                double sn = Director.Instance.fluxW_sn[yr][mo];  // snowfall 相当
                double ic = Director.Instance.fluxW_ic[yr][mo];
                double tr = Director.Instance.fluxW_tr[yr][mo];
                double ev = Director.Instance.fluxW_ev[yr][mo];
                double ro1 = Director.Instance.fluxW_ro1[yr][mo];
                double ro2 = Director.Instance.fluxW_ro2[yr][mo];

                double preEff = pre - sn; // 描画と同じ式を採用

                ds.AddPointToCategory("Pre", i, preEff);
                ds.AddPointToCategory("Hline", i, 0.000001); // 0線の描画安定用
                ds.AddPointToCategory("Ic", i, -ic);
                ds.AddPointToCategory("Tr", i, -(ic + tr));
                ds.AddPointToCategory("Ev", i, -(ic + tr + ev));
                ds.AddPointToCategory("RO1", i, -(ic + tr + ev + ro1));
                ds.AddPointToCategory("RO2", i, -(ic + tr + ev + ro1 + ro2));
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
        if (w_flux == null) return;
        var map = w_flux.HorizontalValueToStringMap;
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
        if (w_flux == null) return;
        var map = w_flux.HorizontalValueToStringMap;
        map.Clear();

        int counter = 0;
        for (int yr = 1; yr <= Director.SimYearMax + Director.SimTimeRangeMax; yr++)
        {
            for (int mo = 1; mo <= MonthsPerYear; mo++)
            {
                map[counter] = (mo == 1) ? yr.ToString() : "";
                counter++;
            }
        }
    }

    /// <summary>
    /// 外部状態に基づき横軸スクロール/表示幅を更新し、表示レンジからY軸を再計算。
    /// 正側: max(pre-sn)、負側: max(ic+tr+ev+ro1+ro2) を10刻みで丸めて対称設定。
    /// </summary>
    public void UpdateValues()
    {
        if (w_flux == null || w_flux.DataSource == null) return;

        int yearView = Director.SimYear;
        w_flux.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        w_flux.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // 必要なら -1 に調整

        // ラベル
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();

        // ---- Y軸スケール（表示レンジだけで集計）----
        double yMaxPos = 10.0; // 正側の最低初期値（過小レンジ回避）
        double yMaxNeg = 10.0; // 負側（合算）の最低初期値

        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yr_refer = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double pre = Director.Instance.fluxW_pre[yr_refer][mo];
                double sn = Director.Instance.fluxW_sn[yr_refer][mo];
                double ic = Director.Instance.fluxW_ic[yr_refer][mo];
                double tr = Director.Instance.fluxW_tr[yr_refer][mo];
                double ev = Director.Instance.fluxW_ev[yr_refer][mo];
                double ro1 = Director.Instance.fluxW_ro1[yr_refer][mo];
                double ro2 = Director.Instance.fluxW_ro2[yr_refer][mo];

                double preEff = pre - sn;              // 正側
                double negSum = ic + tr + ev + ro1 + ro2; // 負側（絶対値）

                if (preEff > yMaxPos) yMaxPos = preEff;
                if (negSum > yMaxNeg) yMaxNeg = negSum;
            }
        }

        // 丸め
        yMaxPos = Math.Ceiling(yMaxPos / YTickSnap) * YTickSnap;
        yMaxNeg = Math.Ceiling(yMaxNeg / YTickSnap) * YTickSnap;

        w_flux.DataSource.VerticalViewSize = yMaxPos + yMaxNeg;
        w_flux.DataSource.VerticalViewOrigin = -yMaxNeg;
    }
}
