using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: WaterPool
/// 土壌層(1/2/3)の水プール量と、基準線(W_fi, W_wilt)を月次で描画する。
/// 表示開始年・表示年数に応じて横軸スクロール/ラベルを更新する。
/// </summary>
public class WaterPool : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart w_pool;

    // 定数
    private const int MonthsPerYear = 12;

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。対象カテゴリが存在する場合のみクリアする。
    /// </summary>
    private void ResetChart()
    {
        if (w_pool == null || w_pool.DataSource == null) return;

        var ds = w_pool.DataSource;
        foreach (var cat in new[] { "poolW1", "poolW2", "poolW3", "Wfi", "Wwilt" })
        {
            if (ds.HasCategory(cat))
                ds.ClearCategory(cat);
        }

        w_pool.HorizontalValueToStringMap.Clear();
        w_pool.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 全期間の月次データを一括投入し、縦軸は W_wilt～W_fi を丸めて設定。
    /// ラベルや表示幅は UpdateValues() で現在のUI状態に合わせて確定させる。
    /// </summary>
    public void Setup()
    {
        if (w_pool == null || w_pool.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // 縦軸の表示範囲設定（丸め＆最小レンジ確保）
        double viewMin = Math.Floor(Director.W_wilt * 10.0) / 10.0;
        double viewMax = Math.Ceiling(Director.W_fi * 10.0) / 10.0;
        double viewSize = Math.Max(viewMax - viewMin, 0.1);
        w_pool.DataSource.VerticalViewOrigin = viewMin;
        w_pool.DataSource.VerticalViewSize = viewSize;

        // ポイントを一括投入
        var ds = w_pool.DataSource;
        ds.StartBatch();

        ds.ClearCategory("poolW1");
        ds.ClearCategory("poolW2");
        ds.ClearCategory("poolW3");
        ds.ClearCategory("Wfi");
        ds.ClearCategory("Wwilt");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                ds.AddPointToCategory("poolW1", i, Director.Instance.poolW_L1_m[yr][mo]);
                ds.AddPointToCategory("poolW2", i, Director.Instance.poolW_L2_m[yr][mo]);
                ds.AddPointToCategory("poolW3", i, Director.Instance.poolW_L3_m[yr][mo]);
                ds.AddPointToCategory("Wfi", i, Director.W_fi);
                ds.AddPointToCategory("Wwilt", i, Director.W_wilt);
                i++;
            }
        }
        ds.EndBatch();

        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化（可視範囲外も空文字でOK）。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (w_pool == null) return;
        var map = w_pool.HorizontalValueToStringMap;
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
        if (w_pool == null) return;
        var map = w_pool.HorizontalValueToStringMap;
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
    /// 外部状態（開始年・表示年数）から横軸スクロール/表示幅を反映し、ラベルを再構築。
    /// （縦軸は W_wilt～W_fi の固定範囲。必要ならここで再計算に切り替え可能）
    /// </summary>
    public void UpdateValues()
    {
        if (w_pool == null || w_pool.DataSource == null) return;

        // 横軸スクロールと表示幅（※ズレる場合のみ -1 に調整）
        int yearView = Director.SimYear;
        w_pool.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        w_pool.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear;

        // ラベル再構築
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }
}
