using ChartAndGraph;
using System;
using UnityEngine;

/// <summary>
/// クラス: SnowPool
/// 月次の積雪貯留量を折れ線/面で描画する。
/// 表示開始年・表示年数に応じて横軸スクロールとラベルを切り替え、
/// y軸は現在の表示レンジから5刻みで動的にスケーリングする。
/// </summary>
public class SnowPool : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart s_pool;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 5.0; // 縦軸丸め刻み

    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。対象カテゴリが存在する場合のみクリアする。
    /// </summary>
    private void ResetChart()
    {
        if (s_pool == null || s_pool.DataSource == null) return;

        var ds = s_pool.DataSource;
        if (ds.HasCategory("SnowPool"))
            ds.ClearCategory("SnowPool");

        s_pool.HorizontalValueToStringMap.Clear();
        s_pool.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// 全期間の月次データを投入。y軸の下端は0固定（非負想定）。
    /// ラベルや表示幅は UpdateValues() で現在のUI状態に合わせて確定させる。
    /// </summary>
    public void Setup()
    {
        if (s_pool == null || s_pool.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // y軸下端は0（積雪貯留量は非負と想定）
        s_pool.DataSource.VerticalViewOrigin = 0;

        // まず全期間の点を投入
        var ds = s_pool.DataSource;
        ds.StartBatch();
        ds.ClearCategory("SnowPool");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                ds.AddPointToCategory("SnowPool", i, Director.Instance.poolW_snow_m[yr][mo]);
                i++;
            }
        }
        ds.EndBatch();

        // 初期の表示/ラベル/スケールを確定
        UpdateValues();
    }

    /// <summary>
    /// 1年表示: Feb/Apr/Jun/Aug/Oct/Dec をラベル化（可視範囲外は空文字）。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (s_pool == null) return;
        var map = s_pool.HorizontalValueToStringMap;
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
        if (s_pool == null) return;
        var map = s_pool.HorizontalValueToStringMap;
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
    /// 外部状態（開始年・表示年数）から横軸スクロール/表示幅を反映し、
    /// 表示レンジ内の最大値から y軸上端を5刻みで丸めて設定する。
    /// </summary>
    public void UpdateValues()
    {
        if (s_pool == null || s_pool.DataSource == null) return;

        // 横軸スクロールと表示幅（必要なら -1 に調整可）
        int yearView = Director.SimYear;
        s_pool.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        s_pool.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear;

        // ラベル再構築
        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();

        // 表示範囲だけから yMax を集計 → 5刻みで丸め、最小レンジを確保
        double yMax = 1.0; // 最小初期値（小さすぎるレンジ回避）
        for (int yr = 0; yr < yearViewRange; yr++)
        {
            int yr_refer = Math.Min(yr + yearView - 1, Director.SimYearMax - 1);
            for (int mo = 0; mo < MonthsPerYear; mo++)
            {
                double v = Director.Instance.poolW_snow_m[yr_refer][mo];
                if (v > yMax) yMax = v;
            }
        }
        yMax = Math.Ceiling(yMax / YTickSnap) * YTickSnap;
        if (yMax < YTickSnap) yMax = YTickSnap; // 最低レンジ確保

        s_pool.DataSource.VerticalViewOrigin = 0.0;
        s_pool.DataSource.VerticalViewSize = yMax;
    }
}
