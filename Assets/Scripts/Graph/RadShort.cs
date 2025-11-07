using ChartAndGraph;
using System;
using UnityEngine;

public class RadShort : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart rad_short;

    // 定数
    private const int MonthsPerYear = 12;
    private const double YTickSnap = 10.0; // 縦軸丸め刻み

    /// <summary>
    /// 対象カテゴリをクリアし、スクロールと軸レンジ/ラベルを初期化する。
    /// セットアップ前のお掃除。nullガード付き。
    /// </summary>
    private void ResetChart()
    {
        if (rad_short == null || rad_short.DataSource == null) return;

        var ds = rad_short.DataSource;
        foreach (var cat in new[] { "RadShortDirect", "RadShortDiffuse", "Hline", "RadShortUp" })
        {
            if (ds.HasCategory(cat))
                ds.ClearCategory(cat);
        }

        rad_short.HorizontalValueToStringMap.Clear();
        rad_short.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// データから縦軸範囲を決め、3カテゴリ(直達+散乱, 散乱, 上向き)と基準線を一括投入。
    /// 最後に UpdateValues() で表示範囲/ラベルを確定。
    /// </summary>
    public void Setup()
    {
        if (rad_short == null || rad_short.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- 縦軸範囲の算出 ---
        // 上端は (直達+散乱) の最大、下端は (上向き) の最大を負方向へ使う。
        double maxDown = 2.0;   // 初期上端候補
        double maxUpAbs = 1.0;  // 初期下端候補（絶対値）

        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                double down = Director.Instance.rad_short_direct_m[yr][mon]
                            + Director.Instance.rad_short_diffuse_m[yr][mon];
                double up = Director.Instance.rad_short_up_m[yr][mon]; // 正の値と仮定

                if (down > maxDown) maxDown = down;
                if (up > maxUpAbs) maxUpAbs = up;
            }
        }

        // 丸め（10単位）
        double yTop = Math.Ceiling(maxDown / YTickSnap) * YTickSnap; // 上端 > 0
        double yBottom = Math.Ceiling(maxUpAbs / YTickSnap) * YTickSnap; // 下端(絶対値) > 0

        // 上端=+yTop, 下端=-yBottom になるように設定
        rad_short.DataSource.VerticalViewSize = yTop + yBottom;
        rad_short.DataSource.VerticalViewOrigin = -yBottom;

        // --- ポイントを一括投入 ---
        var ds = rad_short.DataSource;
        ds.StartBatch();

        ds.ClearCategory("RadShortDirect");
        ds.ClearCategory("RadShortDiffuse");
        ds.ClearCategory("Hline");
        ds.ClearCategory("RadShortUp");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                double direct = Director.Instance.rad_short_direct_m[yr][mon];
                double diffuse = Director.Instance.rad_short_diffuse_m[yr][mon];
                double up = Director.Instance.rad_short_up_m[yr][mon];

                // 上向きは負方向へ（積み上げの下側を作る）
                ds.AddPointToCategory("RadShortDirect", i, direct + diffuse);
                ds.AddPointToCategory("RadShortDiffuse", i, diffuse);
                ds.AddPointToCategory("Hline", i, 0.000001);      // 0線の表示を安定させるための微小値
                ds.AddPointToCategory("RadShortUp", i, -up);

                i++;
            }
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
        if (rad_short == null) return;
        var map = rad_short.HorizontalValueToStringMap;
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
        if (rad_short == null) return;
        var map = rad_short.HorizontalValueToStringMap;
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
    /// 外部状態（表示開始年・表示年数）に基づき、横軸スクロールと表示幅を更新。
    /// 1年表示は月ラベル、複数年は年ラベルを再構築する。
    /// </summary>
    public void UpdateValues()
    {
        if (rad_short == null || rad_short.DataSource == null) return;

        int yearView = Director.SimYear;
        rad_short.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        rad_short.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // ※必要なら -1 に調整

        if (yearViewRange == 1)
        {
            BuildMonthLabelsForOneYear();
        }
        else
        {
            BuildYearLabelsForMultiYears();
        }
    }
}
