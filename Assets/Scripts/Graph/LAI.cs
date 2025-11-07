using ChartAndGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class LAI : MonoBehaviour
{
    public GraphChart Chart;

    public int NumCategoriesMax = 15;   //最大の表示カテゴリー数、つまりPFT数（このスクリプトがアタッチされたオブジェクトのインスペクタが優先される）
    public int NumCategories;           //実際の表示カテゴリー数

    private const int MonthsPerYear = 12;

    private double[] xArr;   //グラフX軸に入る数字
    private double[,] yArr;  //グラフY軸に入る数字

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
    /// グラフのデータ/表示状態を初期化するユーティリティ。
    /// 既存カテゴリの内容・スクロール・軸レンジ・ラベルをすべてクリア。
    /// Setup() 前に毎回呼び出して、表示の取りこぼしを防止する。
    /// </summary>
    private void ResetChart()
    {
        var ds = Chart.DataSource;
        foreach (var cat in ds.CategoryNames.ToList())
        {
            ds.ClearCategory(cat);
        }

        Chart.HorizontalValueToStringMap.Clear();
        Chart.HorizontalScrolling = 0;
        ds.HorizontalViewOrigin = 0;
        ds.HorizontalViewSize = 0;
        ds.VerticalViewOrigin = 0;
        ds.VerticalViewSize = 0;

    }

    /// <summary>
    /// シミュレーション結果をグラフ用配列に詰め、初期描画を行うエントリポイント。
    /// PFT の出現数に合わせてボタンの表示/文言を調整し、Y軸スケールも自動決定。
    /// 初期化後に UpdateValues() を呼んで、横軸ラベル/表示幅を反映する。
    /// </summary>
    public void Setup()
    {
        ResetChart();

        // データを入れる配列を準備
        xArr = Enumerable.Range(0, Director.SimYearMax * MonthsPerYear).Select(num => (double)num).ToArray();
        yArr = new double[xArr.Length, NumCategoriesMax];

        // NumCategories: このシミュレーションで出現するPFTの種類を数える。ついでにボタン上のテキストも設定。
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
                if (txt != null)
                    txt.text = "PFT " + (p + 1);
            }

            Debug.Log($"PFT {p}: {Director.Instance.PFT_available[p]}");
        }

        // 余剰ボタンは非表示
        for (int i = NumCategories; i < pftButtons.Count; i++)
        {
            if (pftButtons[i] != null && pftButtons[i].button != null)
            {
                pftButtons[i].button.gameObject.SetActive(false);
            }
        }

        // グラフ設定
        Chart.Scrollable = false;
        Chart.AutoScrollHorizontally = false;   // 横軸の自動調整を防止
        Chart.HorizontalScrolling = 0;
        Chart.DataSource.HorizontalViewOrigin = 1;     // グラフ横軸の起点
        Chart.DataSource.HorizontalViewSize = MonthsPerYear;  // 表示する横軸の幅（1年）

        // Y座標の配列を格納（PFTを右から左へ積むために末尾から詰める）
        int count_PFTin = NumCategoriesMax - 1;
        for (int p = 0; p < Director.PFT_no_Max; p++)
        {
            if (Director.Instance.PFT_available[p] == true)
            {
                int count_time = 0;
                for (int yr = 0; yr < Director.SimYearMax; yr++)
                {
                    for (int mo = 0; mo < MonthsPerYear; mo++)
                    {
                        yArr[count_time, count_PFTin] = Director.Instance.lai_PFT_m[yr][p][mo];
                        count_time++;
                    }
                }
                count_PFTin--;
            }
            // ここで負になったら打ち切り（これ以上は格納不可）
            if (count_PFTin < 0) break;
        }

        // 初期データの設定（LargeDataFeedへ）
        InitialData(xArr, yArr);

        // y軸の表示最大値を設定する（表示中カテゴリの積み上げ最大）
        double yMax = 0.5; // 初期値
        for (int t = 0; t < Director.SimYearMax * MonthsPerYear; t++)
        {
            double sumup = 0.0;
            for (int p = 0; p < NumCategories; p++)
            {
                //int yIndex = NumCategoriesMax - 1 - p;
                //if (yIndex < 0 || yIndex >= NumCategoriesMax) continue;
                sumup += yArr[t, NumCategoriesMax - 1 - p];
            }
            yMax = Math.Max(yMax, sumup);
        }

        double scaleMax = Math.Ceiling(yMax / 0.1) * 0.1; // 0.1刻みで丸め上げ
        Chart.DataSource.VerticalViewSize = scaleMax;
        Chart.DataSource.VerticalViewOrigin = 0.0;

        RefreshButtonColors();
        UpdateValues();
    }

    /// <summary>
    /// PFT の有効/無効に応じて、各ボタンの UI カラーを更新する。
    /// 表示中は個別色、非表示は共通のグレーで一目で状態が分かる。
    /// </summary>
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

            pft.button.colors = colors;
        }
    }

    /// <summary>
    /// 時間スライダーに応じて横軸のスクロール量と表示幅を更新する。
    /// 表示期間が1年なら月ラベル、複数年なら年ラベルを毎回クリアして再構築。
    /// </summary>
    public void UpdateValues()
    {
        // スクロール位置
        int yearView = Director.SimYear;
        Chart.HorizontalScrolling = (yearView - 1) * MonthsPerYear;

        // 表示幅
        int yearViewRange = Director.SimTimeRange;
        Chart.DataSource.HorizontalViewSize = yearViewRange * MonthsPerYear; // ※環境で1ヶ月ズレる場合は -1 に

        // 横軸ラベルは毎回クリアして再構築
        var map = Chart.HorizontalValueToStringMap;
        map.Clear();

        if (yearViewRange == 1) // 1年間表示：Feb/Apr/Jun/Aug/Oct/Dec
        {
            double counter = 0.0;
            for (int yr = 0; yr < Director.SimYearMax + 1; yr++)
            {
                for (int mon = 0; mon < MonthsPerYear; mon++)
                {
                    string label = "";
                    if (mon == 1) label = "Feb";
                    else if (mon == 3) label = "Apr";
                    else if (mon == 5) label = "Jun";
                    else if (mon == 7) label = "Aug";
                    else if (mon == 9) label = "Oct";
                    else if (mon == 11) label = "Dec";
                    map[counter] = label;
                    counter += 1.0;
                }
            }
        }
        else // 複数年表示：各年の2番目(=Feb位置)を年表示に合わせていた仕様を踏襲（yr+1）
        {
            double counter = 0.0;
            for (int yr = 0; yr < Director.SimYearMax + Director.SimTimeRangeMax; yr++)
            {
                for (int mon = 0; mon < MonthsPerYear; mon++)
                {
                    map[counter] = (mon == 1) ? (yr + 1).ToString() : "";
                    counter += 1.0;
                }
            }
        }
    }

    /// <summary>
    /// 指定カテゴリ（PFT）の表示/非表示をトグルし、Y軸最大値を再計算する。
    /// 表示対象の積み上げ合計からスケールを丸めて設定し直す。ボタン色も更新。
    /// </summary>
    public void Toogle(string name)
    {
        var manager = GetComponent<LAI>();
        if (manager != null) { manager.ToggleCategoryEnabled(name); }

        // y軸の表示最大値を再計算
        double yMax = 0.5; //初期値
        for (int t = 0; t < Director.SimYearMax * MonthsPerYear; t++)
        {
            double sumup = 0.0;

            int max = Math.Min(NumCategories, pftButtons.Count);
            for (int i = 0; i < max; i++)
            {
                var cat = pftButtons[i].categoryName;
                if (string.IsNullOrEmpty(cat)) continue;

                bool enabled = mData.ContainsKey(cat) ? mData[cat].mEnabled : true;
                if (!enabled) continue;

                int yIndex = NumCategoriesMax - 1 - i;
                if (yIndex < 0 || yIndex >= NumCategoriesMax) continue;

                sumup += yArr[t, yIndex];
            }
            yMax = Math.Max(yMax, sumup);
        }

        double scaleMax = Math.Ceiling(yMax / 0.1) * 0.1;
        Chart.DataSource.VerticalViewSize = scaleMax;
        Chart.DataSource.VerticalViewOrigin = 0.0;

        RefreshButtonColors();
    }

    // ______________ これ以降のコードは機能の詳細は良く分からない不明 (触る必要無い？) ______________

    /// <summary>
    /// 指定カテゴリのグラフ上インデックスに対応する (x,y) 値を返す。
    /// 内部 LargeDataFeed のインデックス変換を用いて座標を取得。
    /// </summary>
    public DoubleVector2 GetPointValue(string category, int inGraphIndex)
    {
        if (mData.ContainsKey(category) == false)
            throw new ArgumentException("Category does not exist");

        var entry = mData[category];
        int index = entry.mFeed.GetIndex(inGraphIndex);
        double y = entry.mYValues[index];
        double x = mXValues[index];
        return new DoubleVector2(x, y);
    }

    /// <summary>
    /// 指定カテゴリの有効/無効を反転し、DataSource にも反映したうえで再適用。
    /// </summary>
    public void ToggleCategoryEnabled(string category)
    {
        VerifyCategories();
        if (mData.ContainsKey(category) == false)
            throw new ArgumentException("no such category");
        var entry = mData[category];
        entry.mEnabled = !entry.mEnabled;
        Chart.DataSource.SetCategoryEnabled(category, entry.mEnabled);
        ApplyData();
    }

    /// <summary>
    /// 指定カテゴリの有効/無効を明示的に設定し、DataSource にも反映したうえで再適用。
    /// </summary>
    public void SetCategoryEnabled(string category, bool isEnabled)
    {
        VerifyCategories();
        if (mData.ContainsKey(category) == false)
            throw new ArgumentException("no such category");
        var entry = mData[category];
        entry.mEnabled = isEnabled;
        Chart.DataSource.SetCategoryEnabled(category, isEnabled);
        ApplyData();
    }

    /// <summary>
    /// 指定名のカテゴリが未登録なら LargeDataFeed を生成して登録。
    /// 既存の X 値数に合わせて 0 初期化のベクタを用意。
    /// </summary>
    void VerifyNewCategory(string name)
    {
        CategoryEntry data = null;
        if (mData.ContainsKey(name))
            data = mData[name];
        if (data == null)
        {
            data = new CategoryEntry();
            data.mYValues = new List<double>(mXValues.Count);
            data.mVectors = new List<DoubleVector2>(mXValues.Count);
            data.mFeed = gameObject.AddComponent<LargeDataFeed>();
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

    /// <summary>
    /// 指定名のカテゴリを内部辞書から削除し、関連コンポーネントを破棄。
    /// </summary>
    void VerifyRemoveCategory(string name)
    {
        if (mData.ContainsKey(name) == false)
            return;
        var data = mData[name];
        if (data != null)
        {
            data.mVectors = null;
            UnityEngine.Object.Destroy(data.mFeed);
        }
        mData.Remove(name);
    }

    /// <summary>
    /// Graph の CategoryNames と内部辞書 mData を同期。
    /// 新規カテゴリは作成・消えたカテゴリは削除（Keys はスナップショットで安全に走査）。
    /// </summary>
    void VerifyCategories()
    {
        var names = Chart.DataSource.CategoryNames;
        foreach (string name in names)
        {
            if (mData.ContainsKey(name) == false)
            {
                VerifyNewCategory(name);
            }
        }
        // 走査中変更を避けるため Keys のスナップショットで回す
        foreach (string name in mData.Keys.ToList())
        {
            if (names.Contains(name) == false)
                VerifyRemoveCategory(name);
        }
    }

    /// <summary>
    /// 全カテゴリの既存データをクリアし、X 配列も初期化。
    /// LargeDataFeed 側にも空リストを渡して描画をリセット。
    /// </summary>
    void ClearEntries()
    {
        mXValues.Clear();
        foreach (string name in Chart.DataSource.CategoryNames)
        {
            var entry = mData[name];
            entry.mVectors.Clear();
            entry.mFeed.SetData(new List<DoubleVector2>());
        }
    }

    /// <summary>
    /// X/Y の初期データを積み上げ（スタック）形式にして LargeDataFeed へ流し込む。
    /// 有効カテゴリのみ積算し、各カテゴリの折れ線に累積値を与える。
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
            categoryIndex--;
        }
    }

    /// <summary>
    /// 現在の有効/無効状態に基づいて、グラフの積み上げ値を再計算。
    /// mAccumilated を再構築し、各カテゴリのベクタを更新して再描画。
    /// </summary>
    void ApplyData()
    {
        mAccumilated.Clear();
        mAccumilated.AddRange(Enumerable.Repeat(0.0, mXValues.Count));
        int categoryIndex = Chart.DataSource.CategoryNames.Count() - 1;

        foreach (string name in Chart.DataSource.CategoryNames.Reverse())
        {
            var entry = mData[name];
            entry.mVectors.Clear();
            if (entry.mEnabled)
            {
                for (int i = 0; i < mXValues.Count; i++)
                    mAccumilated[i] += entry.mYValues[i];
            }
            entry.mVectors.Clear();
            for (int i = 0; i < mXValues.Count; i++)
                entry.mVectors.Add(new DoubleVector2(mXValues[i], mAccumilated[i]));
            entry.mFeed.SetData(entry.mVectors);
            categoryIndex--;
        }
    }

    /// <summary>
    /// 毎フレームの更新は現状未使用。
    /// Inspector からのパラメータ変更にフックする用途があれば追記する。
    /// </summary>
    void Update() { }
}
