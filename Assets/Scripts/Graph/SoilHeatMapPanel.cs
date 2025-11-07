using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoilHeatmapPanel : MonoBehaviour
{
    //固定パラメーター
    public static int depth = 3;           // グラフの段数
    static readonly string[] MonthAbbr = { "Ja","Fe","Mr","Ap","My","Jn","Jl","Au","Se","Oc","Nv","De" };

    //カラーマップの設定
    [Header("カラーマップの設定")]
    [SerializeField] private RawImage targetImage;      //イメージ：ヒートマップ本体
    [SerializeField] private RawImage legendImage;      //イメージ：凡例用 RawImage
    [SerializeField] RectTransform axisArea;   // 目盛りを置く領域（幅を取得）
    [SerializeField] float paddingLeft = 0f;
    [SerializeField] float paddingRight = 0f;
    [SerializeField] float baselineY = -15f;
    [SerializeField] Text[] yearLabels; // TextMeshProなら TMP_Text[] に

    [Header("レジェンド関連")]
    [SerializeField] private Text tempTick0;    // 下軸スケール 0
    [SerializeField] private Text tempTickMid;  // 下軸スケール TmpSoilMax/2
    [SerializeField] private Text tempTickMax;  // 下軸スケール TmpSoilMax
    [SerializeField] private Text moistTickMin; // 左軸スケール W_wilt
    [SerializeField] private Text moistTickMid; // 左軸スケール (W_wilt+W_fi)/2
    [SerializeField] private Text moistTickMax; // 左軸スケール W_fi


    //データ時系列（daily）の数
    private int totalDays;          // SimYearMax * 365（閏年無視）

    // 可視化する最大土壌温度と最低土壌温度 (C)
    private float TmpSoilMax = 30.0f; //初期値
    private float TmpSoilMin = 0.0f; //初期値

    // 規格化済みの入力データ（0～1）: [depth, totalDays]
    private float[,] tempNorm = new float[depth, Director.SimYearMax * 365];
    private float[,] moistNorm = new float[depth, Director.SimYearMax * 365];

    // メイングラフと凡例のテクスチャ
    private Texture2D tex;
    private Texture2D legendTex;


    // 凡例テクスチャ解像度（必要に応じて調整可）
    [SerializeField] private int legendWidth = 256;   // 横=温度（低→高）
    [SerializeField] private int legendHeight = 160;   // 縦=含水率（低→高）

    public void Setup()
    {
        totalDays = Director.SimYearMax * 365;

        // 3層の土壌温度の最大値を見つけて、土壌温度の可視化範囲を設定する
        float maxVal = 0.0f;
        float minVal = 25.0f;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int doy = 0; doy < 365; doy++)
            {
                float a = Director.Instance.tmp_soil1[yr][doy]; if (!float.IsNaN(a) && a > maxVal) maxVal = a; if (!float.IsNaN(a) && a < minVal) minVal = a;
                float b = Director.Instance.tmp_soil2[yr][doy]; if (!float.IsNaN(b) && b > maxVal) maxVal = b; if (!float.IsNaN(b) && b < minVal) minVal = b;
                float c = Director.Instance.tmp_soil3[yr][doy]; if (!float.IsNaN(c) && c > maxVal) maxVal = c; if (!float.IsNaN(c) && c < minVal) minVal = c;
            }
        }
        TmpSoilMax = Mathf.Ceil(maxVal / 5f) * 5f;  // 5℃刻みで切り上げ丸め（例: 27.1→30, 25.0→25）
        TmpSoilMin = Mathf.Floor(minVal / 5f) * 5f; // 5℃刻みで切り上げ丸め（例: 27.1→25, 25.0→25, 24.9→20）
        TmpSoilMin = Mathf.Min(TmpSoilMin, TmpSoilMax - 5f);    //minValを考慮しつつ、かつTmpMax より必ず5は小さくする

        // 土壌温度と土壌含水率を正規化する
        float denomT = Mathf.Max(1e-6f, TmpSoilMax - TmpSoilMin);   //0除算回避
        float denomW = Mathf.Max(1e-6f, Director.W_fi - Director.W_wilt);             //0除算回避
        int count = 0;
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            for (int doy = 0; doy < 365; doy++)
            {
                // 温度: 区間 [TmpSoilMin, TmpSoilMax] を 0～1 に線形正規化（範囲外はクランプ）
                tempNorm[0, count] = Mathf.Clamp01((Director.Instance.tmp_soil1[yr][doy] - TmpSoilMin) / denomT);
                tempNorm[1, count] = Mathf.Clamp01((Director.Instance.tmp_soil2[yr][doy] - TmpSoilMin) / denomT);
                tempNorm[2, count] = Mathf.Clamp01((Director.Instance.tmp_soil3[yr][doy] - TmpSoilMin) / denomT);

                // 水分: 区間 [W_wilt, W_fi] を 0～1 に線形正規化（範囲外はクランプ）
                moistNorm[0, count] = Mathf.Clamp01((Director.Instance.poolW_L1[yr][doy] - Director.W_wilt) / denomW);
                moistNorm[1, count] = Mathf.Clamp01((Director.Instance.poolW_L2[yr][doy] - Director.W_wilt) / denomW);
                moistNorm[2, count] = Mathf.Clamp01((Director.Instance.poolW_L3[yr][doy] - Director.W_wilt) / denomW);

                count = count + 1;
            }
        }

        UpdateValues();
    }

    public void UpdateValues()
    {
        Redraw();

        DrawLegend();
    }

    //カラーマップ本体の描画
    private void Redraw()
    {
        if (targetImage == null || tempNorm == null || moistNorm == null) return;

        int simYearMax = Mathf.Max(1, Director.SimYearMax);
        int simYear = Mathf.Clamp(Director.SimYear, 1, simYearMax);
        int simRange = Mathf.Clamp(Director.SimTimeRange, 1, Director.SimTimeRangeMax);

        int windowDays = simRange * 365;
        int startDay = (simYear - 1) * 365;

        // データが実際に存在する右端
        //int endDay = Mathf.Min((SimYearMax - 1) * 365, startDay + windowDays);
        int endDay = Mathf.Min(Director.SimYearMax * 365, startDay + windowDays);
        int availableDays = Mathf.Max(0, endDay - startDay);

        // テクスチャ生成/再利用
        if (tex == null || tex.width != windowDays || tex.height != depth)
        {
            tex = new Texture2D(windowDays, depth, TextureFormat.RGB24, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            targetImage.texture = tex;
        }

        // ピクセル描画
        for (int z = 0; z < depth; z++)
        {
            for (int x = 0; x < windowDays; x++)
            {
                Color c;
                if (x < availableDays)
                {
                    int day = startDay + x; // ここは必ず < totalDays
                    c = EncodeColor(tempNorm[z, day], moistNorm[z, day]);
                }
                else
                {
                    // データ範囲外 → 黒塗り
                    c = Color.black;
                }
                tex.SetPixel(x, depth - 1 - z, c); //テクスチャの上端に、z=depth-1（深い層）が下端に描画
            }
        }
        tex.Apply();

        ////表示ラベルの更新
        // 1) いったん全ラベルをクリア＆非表示
        foreach (var lbl in yearLabels)
        {
            if (!lbl) continue;
            lbl.text = "";
            lbl.gameObject.SetActive(false);
        }

        // 2) 配置対象の点数（n）と表示テキストを決定
        int n = Mathf.Clamp(simRange == 1 ? 12 : simRange, 1, yearLabels.Length);

        // 3) 等間隔で x を計算（区間の中央に置く）
        float width = axisArea ? axisArea.rect.width : 1000f;
        float left = paddingLeft;
        float right = Mathf.Max(0f, width - paddingRight);

        for (int i = 0; i < n; i++)
        {
            var lbl = yearLabels[i];
            if (!lbl) continue;

            // 中央配置: 区間 [left, right] を n 分割し、その各中央に配置
            float t = (i + 0.5f) / n;                    // 0..1
            float x = Mathf.Lerp(left, right, t);

            lbl.rectTransform.anchoredPosition = new Vector2(x, baselineY);
            lbl.gameObject.SetActive(true);

            if (simRange == 1)
            {
                // 月ラベル
                lbl.text = MonthAbbr[i];
            }
            else
            {
                // 年ラベル（SimYear, SimYear+1, ...）
                lbl.text = (simYear + i).ToString();
            }
        }
    }


    /// <summary>
    /// 凡例テクスチャを生成して legendImage に設定
    /// </summary>
    private void DrawLegend()
    {
        if (legendImage == null) return;

        if (legendTex == null || legendTex.width != legendWidth || legendTex.height != legendHeight)
        {
            legendTex = new Texture2D(legendWidth, legendHeight, TextureFormat.RGB24, false);
            legendTex.filterMode = FilterMode.Bilinear;
            legendTex.wrapMode = TextureWrapMode.Clamp;
            legendImage.texture = legendTex;
        }

        for (int y = 0; y < legendHeight; y++)
        {
            // 含水率：下=0（乾燥）→ 上=1（湿潤）
            float moist = (legendHeight <= 1) ? 0f : (float)y / (legendHeight - 1);

            for (int x = 0; x < legendWidth; x++)
            {
                // 温度：左=0（低温, 青）→ 右=1（高温, 赤）
                float temp = (legendWidth <= 1) ? 0f : (float)x / (legendWidth - 1);

                Color c = this.EncodeColor(temp, moist);
                legendTex.SetPixel(x, y, c);
            }
        }
        legendTex.Apply();

        //軸ラベル
        if (tempTick0) tempTick0.text = TmpSoilMin.ToString("F0");
        if (tempTickMid) tempTickMid.text = ((TmpSoilMax + TmpSoilMin) / 2f).ToString("F0");
        if (tempTickMax) tempTickMax.text = TmpSoilMax.ToString("F0");

        if (moistTickMin) moistTickMin.text = Director.W_wilt.ToString("F2");
        if (moistTickMid) moistTickMid.text = ((Director.W_wilt + Director.W_fi) / 2f).ToString("F2");
        if (moistTickMax) moistTickMax.text = Director.W_fi.ToString("F2");
    }

    // 横=温度（低→高 = 青→赤）、縦=含水率（低→高 = 淡→濃）
    // 温度0℃以下ではグレースケール
    private Color EncodeColor(float temp01, float moist01)
    {
        // 0～1の正規化値 temp01 を実温度へ逆変換
        float realT = Mathf.Lerp(TmpSoilMin, TmpSoilMax, Mathf.Clamp01(temp01));

        // 0°C未満はグレースケールにする
        if (realT < 0f)
        {
            // 明るさは (TmpSoilMin ～ 0°C) の中で線形に変化（より低温ほどやや暗め）
            float tNeg01 = Mathf.InverseLerp(TmpSoilMin, 0f, realT);   // TmpSoilMin→0 で 0→1
            float v = Mathf.Lerp(0.35f, 1f, tNeg01);                   // 低温側は少し暗く
            return Color.HSVToRGB(0f, 0f, v);                          // S=0（無彩）→グレースケール
        }

        // 0°C以上は従来どおり：温度→色相(青→赤)、含水率→彩度
        float hue = Mathf.Lerp(240f / 360f, 0f, Mathf.Clamp01(temp01)); // 青→赤
        float sat = Mathf.Clamp01(moist01);                              // 乾→湿（彩度）
        return Color.HSVToRGB(hue, sat, 1f);                             // 明度固定
    }
}
