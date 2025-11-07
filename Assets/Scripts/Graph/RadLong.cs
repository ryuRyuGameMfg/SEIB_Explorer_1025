using ChartAndGraph;
using System;
using UnityEngine;

public class RadLong : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart rad_long;

    [Header("Options")]
    [Tooltip("NetLine を Down-Up の真のネットで描く（既定: false = 互換の -Up 線）")]
    [SerializeField] private bool plotTrueNetAsDownMinusUp = false;

    // 定数
    private const int MonthsPerYear = 12;
    private const float YTickSnap = 10f; // 縦軸丸め刻み

    /// <summary>
    /// 対象カテゴリをクリアし、スクロールと軸レンジ/ラベルを初期化する。
    /// </summary>
    private void ResetChart()
    {
        if (rad_long == null || rad_long.DataSource == null) return;

        var ds = rad_long.DataSource;
        foreach (var cat in new[] { "RadLongDown", "NetLine", "RadLongUp" })
        {
            if (ds.HasCategory(cat))
                ds.ClearCategory(cat);
        }

        rad_long.HorizontalValueToStringMap.Clear();
        rad_long.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// データから縦軸の最大絶対値を求め、上下対称に設定。
    /// 3カテゴリを一括投入し、最後に UpdateValues()。
    /// </summary>
    public void Setup()
    {
        if (rad_long == null || rad_long.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- 縦軸の最大絶対値を求める（下向きと「下+上」のどちらが大きいか） ---
        float radLongMax = 0f;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                float down = Director.Instance.rad_long_down_m[yr][mon];
                float up = Director.Instance.rad_long_up_m[yr][mon];

                // 上下合計の負側（- (up + down)）まで表示域に入るため、|down| と |down+up| を比較
                float cand1 = Mathf.Abs(down);
                float cand2 = Mathf.Abs(down + up);
                if (cand1 > radLongMax) radLongMax = cand1;
                if (cand2 > radLongMax) radLongMax = cand2;
            }
        }

        // 丸め（10単位）
        radLongMax = Mathf.Ceil(radLongMax / YTickSnap) * YTickSnap;

        rad_long.DataSource.VerticalViewSize = 2f * radLongMax; // 上下対称の全高
        rad_long.DataSource.VerticalViewOrigin = -radLongMax;     // 下端 = -Max（上端が +Max）

        // --- ポイントを一括投入 ---
        var ds = rad_long.DataSource;
        ds.StartBatch();

        ds.ClearCategory("RadLongDown");
        ds.ClearCategory("NetLine");
        ds.ClearCategory("RadLongUp");

        int i = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int mon = 0; mon < MonthsPerYear; mon++)
            {
                float down = Director.Instance.rad_long_down_m[yr][mon];
                float up = Director.Instance.rad_long_up_m[yr][mon];

                // 下向きは正で表示
                ds.AddPointToCategory("RadLongDown", i, down);

                // NetLine：既定は互換の「-Up」。オプションで「Down-Up」の真正ネット。
                float net = plotTrueNetAsDownMinusUp ? (down - up) : (-up);
                ds.AddPointToCategory("NetLine", i, net);

                // Up 側の面は負方向へ（下+上の合算を負にして積み上げの底を作る既存仕様）
                ds.AddPointToCategory("RadLongUp", i, -(up + down));

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
        // startMonthIndex は (yearView-1)*12。可視領域以外も空文字で作ってOK（シンプルで安全）
        if (rad_long == null) return;
        var map = rad_long.HorizontalValueToStringMap;
        map.Clear();

        int total = (Director.SimYearMax + Director.SimTimeRangeMax) * MonthsPerYear;
        for (int idx = 0; idx < total; idx++)
        {
            string label = "";
            int m = idx % MonthsPerYear;
            // 2,4,6,8,10,12月
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
        if (rad_long == null) return;
        var map = rad_long.HorizontalValueToStringMap;
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
    /// 横軸スクロール/表示幅とラベルを外部状態から更新
    /// </summary>
    public void UpdateValues()
    {
        if (rad_long == null || rad_long.DataSource == null) return;

        int yearView = Director.SimYear;
        rad_long.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        int yearViewRange = Director.SimTimeRange;
        rad_long.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // ※必要なら -1 に戻す

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
