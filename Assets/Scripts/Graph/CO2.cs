using ChartAndGraph;
using System;
using UnityEngine;

public class AtomosphericCO2 : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart CO2;

    private const double YTickSnap = 50.0;  // 縦軸丸め刻み（ppm）

    /// <summary>
    /// グラフのカテゴリ/スクロール/軸レンジ/ラベル初期化。
    /// </summary>
    private void ResetChart()
    {
        if (CO2 == null || CO2.DataSource == null) return;

        var ds = CO2.DataSource;
        if (ds.HasCategory("CO2")) ds.ClearCategory("CO2");

        CO2.HorizontalValueToStringMap.Clear();
        CO2.HorizontalScrolling = 0;
        ds.HorizontalViewOrigin = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;
    }

    /// <summary>
    /// セットアップ：全期間のデータを投入し、横軸は初年→最終年まで固定表示。
    /// 縦軸はデータ範囲を 50ppm 刻みで見やすく丸める。
    /// </summary>
    public void Setup()
    {
        if (CO2 == null || CO2.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- 縦軸レンジ決定（ppm） ---
        double yMax = 400.0;
        double yMin = 250.0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            double v = Director.Instance.aCO2[yr];
            if (v > yMax) yMax = v;
            if (v < yMin) yMin = v;
        }
        double origin = Math.Floor(yMin / YTickSnap) * YTickSnap;  // 下限は切り下げ
        double top = Math.Ceiling(yMax / YTickSnap) * YTickSnap; // 上限は切り上げ
        double viewSize = Math.Max(YTickSnap, top - origin);          // 最低レンジ確保

        var ds = CO2.DataSource;
        ds.VerticalViewOrigin = origin;
        ds.VerticalViewSize = viewSize;

        // --- 横軸は全期間固定（初年→最終年） ---
        ds.HorizontalViewOrigin = 0;
        ds.HorizontalViewSize = Math.Max(1, Director.SimYearMax); // 全年数

        // --- ラベル（年数に応じて間引き） ---
        BuildYearLabelsForFullRange();

        // --- データ投入（全期間） ---
        ds.StartBatch();
        ds.ClearCategory("CO2");
        for (int yr = 0; yr < Director.SimYearMax; yr++)
            ds.AddPointToCategory("CO2", yr, Director.Instance.aCO2[yr]);
        ds.EndBatch();

        // 仕様上、以降は常に全期間表示なので UpdateValues は何もしない
        UpdateValues();
    }

    /// <summary>
    /// 全期間用の年ラベルを作成（短期は毎年、長期は10/20/50/100年刻み）。
    /// 先頭年は空文字、2年目（index=1）は「1」を必ず表示（従来踏襲）。
    /// </summary>
    private void BuildYearLabelsForFullRange()
    {
        if (CO2 == null) return;
        var map = CO2.HorizontalValueToStringMap;
        map.Clear();

        int totalYears = Math.Max(1, Director.SimYearMax);

        int step;
        if (totalYears <= 15) step = 1;
        else if (totalYears < 50) step = 10;
        else if (totalYears < 100) step = 20;
        else if (totalYears < 500) step = 50;
        else step = 100;

        for (int yr = 0; yr < totalYears; yr++)
        {
            // 0年目は空、以降は step ごとにラベル
            map[yr] = (yr > 0 && (step == 1 || yr % step == 0)) ? yr.ToString() : "";
        }
        if (totalYears > 1) map[1] = "1"; // 2年目は必ず "1"
    }

    /// <summary>
    /// 全期間固定表示のため処理なし（仕様どおり）。
    /// </summary>
    public void UpdateValues() { }
}
