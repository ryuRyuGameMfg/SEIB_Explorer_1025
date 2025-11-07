using ChartAndGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class Biomass : MonoBehaviour
{
    [Header("Chart")]
    public GraphChart Chart;

    public int NumCategoriesMax = 15; // 表示できる最大カテゴリ数（PFT数の上限）
    public int NumCategories;         // 実際に表示するカテゴリ数（有効PFT数）

    private double[] xArr;   // グラフX軸（0..月数-1）
    private double[,] yArr;  // グラフY軸（[時系列, カテゴリ]）

    [Header("PFT Buttons")]
    public List<PFTButton> pftButtons = new List<PFTButton>();

    [Header("Disabled Color")]
    public Color disabledColor = Color.gray;

    class CategoryEntry
    {
        public List<double> mYValues = new List<double>();
        public List<DoubleVector2> mVectors = new List<DoubleVector2>();
        public LargeDataFeed mFeed = null;
        public bool mEnabled = true;
    }

    Dictionary<string, CategoryEntry> mData = new Dictionary<string, CategoryEntry>();
    List<double> mXValues = new List<double>();
    List<double> mAccumilated = new List<double>();


    /// <summary>
    /// カテゴリ/スクロール/軸レンジ/ラベルを初期化（安全ガード付き）。
    /// セットアップ前のお掃除。存在するカテゴリのみクリア。
    /// </summary>
    private void ResetChart()
    {
        if (Chart == null || Chart.DataSource == null) return;

        var ds = Chart.DataSource;
        foreach (var name in ds.CategoryNames.ToList())
        {
            if (ds.HasCategory(name))
                ds.ClearCategory(name);
        }

        Chart.HorizontalValueToStringMap.Clear();
        Chart.HorizontalScrolling = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;

        //全ボタンを消す
        foreach (var pft in pftButtons)
        {
            if (pft != null && pft.button != null)
                pft.button.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 時系列・カテゴリ配列を準備し、LargeDataFeedに初期データを投入。
    /// ラベル/レンジを整えて初期表示を完了する。
    /// </summary>
    public void Setup()
    {
        if (Chart == null || Chart.DataSource == null || Director.Instance == null) return;

        ResetChart();

        // --- データ配列を準備 ---
        int totalMonths = Director.SimYearMax * 12;
        xArr = Enumerable.Range(0, totalMonths).Select(i => (double)i).ToArray();
        yArr = new double[totalMonths, NumCategoriesMax];

        // --- 有効PFTの数を数え、ボタンとインデックスを整える ---
        NumCategories = 0;
        for (int p = 0; p < Director.PFT_no_Max; p++)
        {
            if (!Director.Instance.PFT_available[p]) continue;

            NumCategories++;
            int btnIndex = NumCategories - 1;
            if (btnIndex >= 0 && btnIndex < pftButtons.Count && pftButtons[btnIndex].button != null)
            {
                var btn = pftButtons[btnIndex].button;
                btn.gameObject.SetActive(true);
                var txt = btn.GetComponentInChildren<Text>(true);
                if (txt != null) txt.text = "PFT " + (p + 1);
            }
        }
        // 余剰ボタンは非表示
        for (int i = NumCategories; i < pftButtons.Count; i++)
        {
            if (pftButtons[i]?.button != null)
                pftButtons[i].button.gameObject.SetActive(false);
        }

        // --- yArr に PFT順で積み上げ元データを格納（末尾列が最前面） ---
        int col = NumCategoriesMax - 1;
        for (int p = 0; p < Director.PFT_no_Max; p++)
        {
            if (!Director.Instance.PFT_available[p]) continue;

            int t = 0;
            for (int yr = 0; yr < Director.SimYearMax; yr++)
            {
                for (int mo = 0; mo < 12; mo++)
                {
                    yArr[t, col] = Director.Instance.poolC_whole[yr][p][mo];
                    t++;
                }
            }
            col--;
            if (col < 0) break; // ← 不要なオーバーフローを防ぐ
        }

        // --- グラフ設定（横軸の初期ビュー） ---
        Chart.Scrollable = false;
        Chart.AutoScrollHorizontally = false;
        Chart.HorizontalScrolling = 0;
        Chart.DataSource.HorizontalViewOrigin = 1; // 仕様に合わせ既存値を踏襲
        Chart.DataSource.HorizontalViewSize = 12;

        // --- 初期積算＆フィード ---
        InitialData(xArr, yArr);

        // --- 初期ラベル（複数年でも壊れないよう毎回作り直す） ---
        BuildMonthLabelsForOneYear();

        // --- 初期の縦軸レンジ（全カテゴリ合計の最大） ---
        double yMax = 5.0;
        for (int t = 0; t < totalMonths; t++)
        {
            double sum = 0.0;
            for (int k = 0; k < NumCategories; k++)
            { 
                //Debug.Log("k: " + k);
                //Debug.Log("t: " + t);
                //Debug.Log("t: " + t);
                //Debug.Log("NumCategoriesMax: " + NumCategoriesMax);

                sum += yArr[t, NumCategoriesMax - 1 - k];
            }
            yMax = Math.Max(yMax, sum);
        }
        double scaleMax = Math.Ceiling(yMax);
        Chart.DataSource.VerticalViewSize = scaleMax;
        Chart.DataSource.VerticalViewOrigin = 0.0;

        RefreshButtonColors();

        UpdateValues();
    }

    /// <summary>
    /// 1年表示用の月ラベル（Feb/Apr/Jun/Aug/Oct/Dec）を作成。
    /// ※番兵キーに依存せず、毎回クリアして再構築。
    /// </summary>
    private void BuildMonthLabelsForOneYear()
    {
        if (Chart == null) return;
        var map = Chart.HorizontalValueToStringMap;
        map.Clear();

        int total = (Director.SimYearMax + Director.SimTimeRangeMax) * 12;
        for (int idx = 0; idx < total; idx++)
        {
            int m = idx % 12;
            map[idx] = (m == 1) ? "Feb" :
                       (m == 3) ? "Apr" :
                       (m == 5) ? "Jun" :
                       (m == 7) ? "Aug" :
                       (m == 9) ? "Oct" :
                       (m == 11) ? "Dec" : "";
        }
    }

    /// <summary>
    /// 複数年表示用ラベル（各年の1月位置に年号）を作成。
    /// </summary>
    private void BuildYearLabelsForMultiYears()
    {
        if (Chart == null) return;
        var map = Chart.HorizontalValueToStringMap;
        map.Clear();

        int counter = 0;
        for (int yr = 1; yr <= Director.SimYearMax + Director.SimTimeRangeMax; yr++)
        {
            for (int mon = 1; mon <= 12; mon++)
            {
                map[counter] = (mon == 1) ? yr.ToString() : "";
                counter++;
            }
        }
    }

    /// <summary>
    /// スクロール/表示幅を UI 状態から反映。ラベルは毎回クリアして1年/複数年を作り分ける。
    /// </summary>
    public void UpdateValues()
    {
        if (Chart == null || Chart.DataSource == null) return;

        int yearView = Director.SimYear;
        Chart.HorizontalScrolling = (yearView - 1) * 12;

        int yearViewRange = Director.SimTimeRange;
        Chart.DataSource.HorizontalViewSize = yearViewRange * 12;

        if (yearViewRange == 1)
            BuildMonthLabelsForOneYear();
        else
            BuildYearLabelsForMultiYears();
    }

    /// <summary>
    /// 表示対象ボタンが押下されたとき呼ばれる。カテゴリの表示ON/OFFを切替え、
    /// その状態での積み上げ最大値から縦軸レンジを再設定する。
    /// </summary>
    public void Toogle(string name)
    {
        var manager = GetComponent<Biomass>();
        if (manager != null) manager.ToggleCategoryEnabled(name);

        // 現在有効なカテゴリのみを積算し、最大値からYレンジ更新
        double yMax = 5.0;
        int totalMonths = Director.SimYearMax * 12;

        for (int t = 0; t < totalMonths; t++)
        {
            double sum = 0.0;
            int max = Math.Min(NumCategories, pftButtons.Count);
            for (int i = 0; i < max; i++)
            {
                var cat = pftButtons[i].categoryName;
                if (string.IsNullOrEmpty(cat)) continue;

                bool enabled = mData.ContainsKey(cat) ? mData[cat].mEnabled : true;
                if (!enabled) continue;

                int yIndex = NumCategoriesMax - 1 - i;
                if (yIndex < 0 || yIndex >= NumCategoriesMax) continue;

                sum += yArr[t, yIndex];
            }
            yMax = Math.Max(yMax, sum);
        }

        double scaleMax = Math.Ceiling(yMax);
        Chart.DataSource.VerticalViewSize = scaleMax;
        Chart.DataSource.VerticalViewOrigin = 0.0;

        RefreshButtonColors();
    }

    // __________ 以降は LargeDataFeed の管理（基本は触る必要なし） __________

    public DoubleVector2 GetPointValue(string category, int inGraphIndex)
    {
        if (!mData.ContainsKey(category))
            throw new ArgumentException("Category does not exist");

        var entry = mData[category];
        int index = entry.mFeed.GetIndex(inGraphIndex);
        return new DoubleVector2(mXValues[index], entry.mYValues[index]);
    }

    public void ToggleCategoryEnabled(string category)
    {
        VerifyCategories();
        if (!mData.ContainsKey(category))
            throw new ArgumentException("no such category");
        var entry = mData[category];
        entry.mEnabled = !entry.mEnabled;
        Chart.DataSource.SetCategoryEnabled(category, entry.mEnabled);
        ApplyData();
    }

    public void SetCategoryEnabled(string category, bool isEnabled)
    {
        VerifyCategories();
        if (!mData.ContainsKey(category))
            throw new ArgumentException("no such category");
        var entry = mData[category];
        entry.mEnabled = isEnabled;
        Chart.DataSource.SetCategoryEnabled(category, isEnabled);
        ApplyData();
    }

    void VerifyNewCategory(string name)
    {
        CategoryEntry data = null;
        if (mData.ContainsKey(name))
            data = mData[name];
        if (data == null)
        {
            data = new CategoryEntry
            {
                mYValues = new List<double>(mXValues.Count),
                mVectors = new List<DoubleVector2>(mXValues.Count),
                mFeed = gameObject.AddComponent<LargeDataFeed>()
            };
            data.mFeed.LoadExample = false;
            data.mFeed.AlternativeGraph = Chart;
            data.mFeed.Category = name;
            mData[name] = data;
        }
        data.mVectors.Clear();
        data.mYValues.Clear();
        for (int i = 0; i < mXValues.Count; i++)
        {
            data.mVectors.Add(new DoubleVector2(mXValues[i], 0.0));
            data.mYValues.Add(0.0);
        }
    }

    void VerifyRemoveCategory(string name)
    {
        if (!mData.ContainsKey(name)) return;
        var data = mData[name];
        if (data != null)
        {
            data.mVectors = null;
            if (data.mFeed != null) Destroy(data.mFeed);
        }
        mData.Remove(name);
    }

    void VerifyCategories()
    {
        var names = Chart.DataSource.CategoryNames.ToList(); // ← スナップショットを先に取る
        foreach (string name in names)
        {
            if (!mData.ContainsKey(name))
                VerifyNewCategory(name);
        }
        // mData は削除を伴うので ToList() で安全に列挙
        foreach (string name in mData.Keys.ToList())
        {
            if (!names.Contains(name))
                VerifyRemoveCategory(name);
        }
    }

    // X軸配列と各カテゴリベクタをクリア
    void ClearEntries()
    {
        mXValues.Clear();
        foreach (string name in Chart.DataSource.CategoryNames)
        {
            if (!mData.ContainsKey(name)) continue;
            var entry = mData[name];
            entry.mVectors.Clear();
            entry.mFeed.SetData(new List<DoubleVector2>());
        }
    }

    /// <summary>
    /// グラフの初期データをセット（各カテゴリの積み上げ値を LargeDataFeed に渡す）
    /// </summary>
    public void InitialData(double[] x, double[,] y)
    {
        VerifyCategories();
        ClearEntries();
        if (x.Length != y.GetLength(0))
            throw new ArgumentException("x and y size should match");

        mXValues.Clear();
        mXValues.AddRange(x);
        mAccumilated.Clear();
        mAccumilated.AddRange(Enumerable.Repeat(0.0, mXValues.Count));

        // ★カテゴリ名で列を探さず、「末尾列から順に」対応させる（元コードと同じ）
        int categoryIndex = Chart.DataSource.CategoryNames.Count() - 1;

        foreach (string name in Chart.DataSource.CategoryNames.Reverse())
        {
            var entry = mData[name];

            if (entry.mEnabled)
            {
                for (int i = 0; i < x.Length; i++)
                    mAccumilated[i] += y[i, categoryIndex];
            }

            entry.mYValues.Clear();
            entry.mVectors.Clear();
            for (int i = 0; i < mXValues.Count; i++)
            {
                entry.mYValues.Add(y[i, categoryIndex]);
                entry.mVectors.Add(new DoubleVector2(mXValues[i], mAccumilated[i]));
            }

            entry.mFeed.SetData(entry.mVectors);
            categoryIndex--; // ←ここが肝
        }
    }

    /// <summary>
    /// カテゴリON/OFFを反映して積み上げ系列を再構築し、LargeDataFeedへ反映。
    /// </summary>
    void ApplyData()
    {
        mAccumilated.Clear();
        mAccumilated.AddRange(Enumerable.Repeat(0.0, mXValues.Count));

        // 逆順で積む（後ろのカテゴリが前面）
        foreach (string name in Chart.DataSource.CategoryNames.Reverse())
        {
            var entry = mData[name];
            entry.mVectors.Clear();

            if (entry.mEnabled)
            {
                for (int i = 0; i < mXValues.Count; i++)
                    mAccumilated[i] += entry.mYValues[i];
            }

            for (int i = 0; i < mXValues.Count; i++)
                entry.mVectors.Add(new DoubleVector2(mXValues[i], mAccumilated[i]));

            entry.mFeed.SetData(entry.mVectors);
        }
    }

    // PFTボタンの色を、表示ON(各PFT色)/OFF(グレー)に更新する
    private void RefreshButtonColors()
    {
        foreach (var pft in pftButtons)
        {
            if (pft == null || pft.button == null) continue;

            bool isEnabled = true;
            if (!string.IsNullOrEmpty(pft.categoryName) && mData.ContainsKey(pft.categoryName))
                isEnabled = mData[pft.categoryName].mEnabled;

            var colors = pft.button.colors;
            var onCol = pft.enabledColor;

            colors.normalColor = isEnabled ? onCol : disabledColor;
            colors.highlightedColor = isEnabled ? onCol : disabledColor;
            colors.pressedColor = isEnabled ? onCol * 0.9f : disabledColor * 0.9f;
            colors.selectedColor = isEnabled ? onCol : disabledColor;
            colors.disabledColor = disabledColor;

            pft.button.colors = colors; // ColorBlock は構造体。代入で反映が必要
        }
    }

    void Update() { }


}
