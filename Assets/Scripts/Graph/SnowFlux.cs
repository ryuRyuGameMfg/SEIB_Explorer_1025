using ChartAndGraph;
using System;
using UnityEngine;

public class SnowFlux : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart s_flux;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 5.0; // 縦軸丸め刻み

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。存在する対象カテゴリをクリアする。
    /// </summary>
    private void ResetChart()
    {
        if (s_flux == null || s_flux.DataSource == null) return;

        var ds = s_flux.DataSource;
        foreach (var cat in new[] { "sn", "Hline", "sl", "tw" })
        {
            if (ds.HasCategory(cat))
                ds.ClearCategory(cat);
        }

        s_flux.HorizontalValueToStringMap.Clear();
        s_flux.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 月次データ（sn:正、sl/tw:負方向）を3カテゴリ＋基準線に一括投入。
    /// y軸は UpdateValues() で表示レンジに基づき確定させる。
    /// </summary>
    public void Setup()
    {
        if (s_flux == null || s_flux.DataSource == null || Director.Instance == null) return;

        ResetChart();

        var ds = s_flux.DataSource;
        ds.StartBatch();
        ds.ClearCategory("sn");
        ds.ClearCategory("Hline");
        ds.ClearCategory("sl");
        ds.ClearCategory("tw");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double a1 = Director.Instance.fluxW_sn[yr][mo];                  // 正方向に積む
                double a2 = Director.Instance.fluxW_sl[yr][mo];                  // 以降は負方向へ積む
                double a3 = Director.Instance.fluxW_tw[yr][mo];

                ds.AddPointToCategory("sn", i, a1);
                ds.AddPointToCategory("Hline", i, 0.00000001d);                 // 0線描画安定用の微小値
                ds.AddPointToCategory("sl", i, -a2);
                ds.AddPointToCategory("tw", i, -(a2 + a3));

                i++;
            }
        }
        ds.EndBatch();

        UpdateValues(); // y軸・ラベル・スクロール確定
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (s_flux == null) return;
        var map = s_flux.HorizontalValueToStringMap;
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
    /// 複数年表示: 各年の1月位置に年号。
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (s_flux == null) return;
        var map = s_flux.HorizontalValueToStringMap;
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
    /// 外部状態（開始年・表示年数）に基づき、横軸スクロール/表示幅とラベルを更新。
    /// あわせて現在表示レンジの y軸最大正値(sn) と 最大負値(|sl+tw|) を集計し、5刻みで丸めて設定。
    /// </summary>
    public void UpdateValues()
    {
        if (s_flux == null || s_flux.DataSource == null) return;

        // 横軸スクロールと表示幅
        int yearView = Director.SimYear;
        s_flux.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        s_flux.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // 必要なら -1 に調整

        // ラベル
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();

        // ---- y軸レンジ（表示範囲だけ）を再計算 ----
        double yMaxPos = 0.5; // sn の上方向
        double yMaxNeg = 0.5; // (sl+tw) の絶対値（下方向）

        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yr_refer = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double pos = Director.Instance.fluxW_sn[yr_refer][mo];
                double neg = Director.Instance.fluxW_sl[yr_refer][mo] + Director.Instance.fluxW_tw[yr_refer][mo];

                if (pos > yMaxPos) yMaxPos = pos;
                if (neg > yMaxNeg) yMaxNeg = neg;
            }
        }

        // 5刻みで丸め
        yMaxPos = Math.Ceiling(yMaxPos / YTickSnap) * YTickSnap;
        yMaxNeg = Math.Ceiling(yMaxNeg / YTickSnap) * YTickSnap;

        s_flux.DataSource.VerticalViewSize = yMaxPos + yMaxNeg; // 上下合計
        s_flux.DataSource.VerticalViewOrigin = -yMaxNeg;          // 下端（負側）
    }
}
