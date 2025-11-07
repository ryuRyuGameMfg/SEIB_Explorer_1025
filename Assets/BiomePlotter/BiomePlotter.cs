using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BiomePlotter : MonoBehaviour
{
    // 設置時の注意
    // imageRect.pivot	→	(0, 0)
    // imageRect.anchorMin / Max	→	両方とも (0, 0)
    // markerPrefab.RectTransform	→	pivot=(0.5,0.5), anchor=(0,0)
    // anchoredPosition の基準	→	左下が (0, 0) になる構成に

    [Header("UI参照")]
    public RectTransform imageRect;       // Biome画像 (Part) のRectTransform
    public RectTransform markersRoot;     // マーカー用のコンテナ（imageRectの子が推奨）
    public GameObject markerPrefab;       // マーカーPrefab（円）

    [Header("軸レンジ")]
    public float tmpMin = -10f;           // X軸（気温）の最小値
    public float tmpMax = 30f;            // X軸（気温）の最大値
    public float preMin = 0f;             // Y軸（降水量）の最小値
    public float preMax = 400f;           // Y軸（降水量）の最大値

    [Header("プロット領域のパディング（px, imageRect内部）")]
    public float padLeft = 0f;            // 左余白
    public float padRight = 0f;           // 右余白
    public float padBottom = 0f;          // 下余白
    public float padTop = 0f;             // 上余白

    [Header("描画オプション")]
    public bool clampToRect = true;       // レンジ外は端にクランプ

    [Header("レジェンド（年ごとの色見本）")]
    public bool drawLegend = true;
    public float legendY = 418f;          // レジェンドのY位置
    public float legendXMin = -5.5f;      // レジェンドのX最小値
    public float legendXRange = 25f;      // レジェンドの横幅範囲

    [Header("色のグラデーション")]
    public Color startColor = Color.yellow;                  // 開始色（黄色）
    public Color endColor = new Color(1f, 0f, 1f);           // 終端色（紫 #FF00FF）

    //========================================================================
    // 指定位置に指定の色で打点する（Robust版）
    //========================================================================
    public void PlotMarker(float tmp, float pre, Color color)
    {
        // 正規化（0..1）
        float xn = Mathf.InverseLerp(tmpMin, tmpMax, tmp);
        float yn = Mathf.InverseLerp(preMin, preMax, pre);
        if (clampToRect)
        {
            xn = Mathf.Clamp01(xn);
            yn = Mathf.Clamp01(yn);
        }

        // imageRectローカル → ワールド → markersRootローカルへ変換（スケール/回転に頑健）
        Vector2 localInMarkers = ImageLocal01ToMarkersLocal(xn, yn);

        // マーカー生成
        GameObject marker = Instantiate(markerPrefab, markersRoot);
        RectTransform rt = marker.GetComponent<RectTransform>();
        rt.anchoredPosition = localInMarkers;

        // 色設定（Image必須）
        if (marker.TryGetComponent<Image>(out var img))
            img.color = color;
    }

    //========================================================================
    // すべてのマーカーを削除する（リセット）
    //========================================================================
    public void ResetPlot()
    {
        if (markersRoot == null) return;

        // 破棄中にコレクションが変わらないように退避
        List<Transform> toDelete = new List<Transform>(markersRoot.childCount);
        for (int i = 0; i < markersRoot.childCount; i++)
            toDelete.Add(markersRoot.GetChild(i));

        foreach (var t in toDelete)
            Destroy(t.gameObject);
    }

    //========================================================================
    // 一旦消してから再描画する（便利メソッド）
    //========================================================================
    public void Redraw()
    {
        ResetPlot();
        Setup();
    }

    //========================================================================
    // 本体（元のSetup）
    // 色：黄色 → 紫色 (#FF00FF) のグラデーションを付けながらプロット
    //========================================================================
    public void Setup()
    {
        if (imageRect == null || markersRoot == null || markerPrefab == null)
        {
            Debug.LogWarning("[BiomePlotter] 参照が未設定です（imageRect / markersRoot / markerPrefab）");
            return;
        }

        //グラデーションを付けながらプロット
        for (int yr = 0; yr < Director.SimYearMax; yr++)
        {
            // 色：黄色 → 紫色 (#FF00FF) のグラデーション
            float t = (Director.SimYearMax > 1) ? (yr / (float)(Director.SimYearMax - 1)) : 0f;
            Color c = Color.Lerp(startColor, endColor, t);

            //LegendをPlotする
            if (drawLegend)
            {
                float legend_x = legendXMin + legendXRange * yr / Mathf.Max(1, Director.SimYearMax);
                PlotMarker(legend_x, legendY, c);
            }

            //気候値をPlotする (年降水量の単位がcmであることに注意)
            float tmp = Director.Instance.tmp_air_y[yr];
            float pre = Director.Instance.fluxW_pre_y[yr] * 0.1f;
            PlotMarker(tmp, pre, c);
        }
    }

    //========================================================================
    // imageRect(0..1座標) → markersRootローカル座標に変換
    // （スケールや回転に頑健な座標変換）
    //========================================================================
    private Vector2 ImageLocal01ToMarkersLocal(float xn, float yn)
    {
        // imageRectのローカル座標系で、パディングを加味したpx位置を作る
        Vector2 size = imageRect.rect.size;
        float usableW = Mathf.Max(0f, size.x - padLeft - padRight);
        float usableH = Mathf.Max(0f, size.y - padBottom - padTop);

        float localX = padLeft + xn * usableW;
        float localY = padBottom + yn * usableH;

        // imageRectローカル → ワールド座標へ変換
        Vector3 world = imageRect.TransformPoint(new Vector3(localX, localY, 0f));

        // ワールド → スクリーン → markersRootローカルへ変換
        var canvas = imageRect.GetComponentInParent<Canvas>();
        Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceCamera)
            ? canvas.worldCamera : null;

        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, world);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            markersRoot, screen, cam, out Vector2 localInMarkers
        );

        return localInMarkers;
    }

    //========================================================================
    // インスペクタ上の確認用
    //========================================================================
    private void OnValidate()
    {
        // 片方だけ設定されている等の事故を軽く検知
        if (markersRoot == null)
            Debug.LogWarning("[BiomePlotter] markersRoot が未設定です。");
        if (imageRect == null)
            Debug.LogWarning("[BiomePlotter] imageRect が未設定です。");
    }
}
