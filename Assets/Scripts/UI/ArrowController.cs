using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class ArrowController : MonoBehaviour
{
    [Header("Parts")]
    [Tooltip("矢の軸（正方形画像）の RectTransform")]
    public RectTransform squareRect;

    [Tooltip("矢の尾（任意）の RectTransform。未設定なら無視される")]
    public RectTransform square2Rect;

    [Tooltip("矢じり（三角形画像）の RectTransform")]
    public RectTransform triangleRect;

    [Header("Geometry")]
    [Min(0f), Tooltip("矢全体の長さ(px)")]
    public float arrowLength = 100f;

    [Min(0f), Tooltip("軸の太さ(px)")]
    public float arrowThickness = 10f;

    [Tooltip("矢の回転角(度)")]
    public float arrowRotation = 0f;

    [Min(0.1f), Tooltip("全体スケールの下限=0.1")]
    public float ScaleFactor = 1f;

    [Header("Shape Factors")]
    [Tooltip("三角形の幅/高さ比に影響")]
    public float triangleScaleFactor = 2.0f;

    [Tooltip("三角形高さ = 長さ * この値")]
    public float triangleScaleFactor2 = 0.6f;

    [Tooltip("軸(正方形)の縦スケール補正")]
    [Range(0.1f, 1.5f)]
    public float squareScaleFactor = 0.88f;

    // 画像の向きを補正するための回転オフセット（多くのUI画像は上向き前提のため-90度）
    const float kImageRotationOffset = -90f;

    // 三角形の見映え調整スケール
    const float kTriangleVisualScale = 0.2f;

    // 尾パーツの縦スケール補正
    const float kTailVerticalScaleBase = 0.825f;

    // 直前に適用した値（無変更フレームでの再計算回避）
    float _lastLength, _lastThickness, _lastRotation, _lastScale;

    /// <summary>
    /// 矢の長さ・太さ・回転を受け取り、各パーツ(RectTransform)のサイズ/位置/回転/スケールを更新する。
    /// 軸と矢じりの整列を維持しつつ、係数に基づき三角形を自動スケーリングする。
    /// エディタ/実行時の両方で呼ばれる想定。
    /// </summary>
    public void SetArrowProperties(float length, float thickness, float rotation)
    {
        if (!squareRect || !triangleRect) return; // 必須パーツが未設定なら何もしない

        // 下限クランプ
        if (ScaleFactor < 0.1f) ScaleFactor = 0.1f;
        if (length < 0f) length = 0f;
        if (thickness < 0f) thickness = 0f;

        arrowLength = length;
        arrowThickness = thickness;
        arrowRotation = rotation;

        // 軸(正方形)のサイズ（横=太さ*Scale、縦=長さ）
        if (triangleScaleFactor > 0f)
        {
            squareRect.sizeDelta = new Vector2(arrowThickness * ScaleFactor, arrowLength);
        }

        // 矢じり（三角形）の自動スケーリング
        float triH = arrowLength * triangleScaleFactor2;
        float triW = triH * triangleScaleFactor;

        triangleRect.sizeDelta = new Vector2(triW, triH);
        triangleRect.localScale = new Vector3(kTriangleVisualScale * ScaleFactor, kTriangleVisualScale * ScaleFactor, 1f);

        // 方向ベクトル（回転角は度→ラジアンへ）
        float rad = arrowRotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        Vector2 offset = new Vector2(cos, sin) * arrowLength;

        // 位置合わせ：矢じりを原点、軸をオフセット先端へ
        triangleRect.anchoredPosition = Vector2.zero;
        squareRect.anchoredPosition = offset;

        // 回転（画像向き補正込み）
        Quaternion rotQ = Quaternion.Euler(0f, 0f, arrowRotation + kImageRotationOffset);
        squareRect.rotation = rotQ;
        triangleRect.rotation = rotQ;

        // 軸のスケール
        if (triangleScaleFactor == 0f)
        {
            squareRect.localScale = new Vector3(ScaleFactor, kTailVerticalScaleBase, 1f);
        }
        else
        {
            // 元式: squareScaleFactor - (ScaleFactor - 1) * (1 - squareScaleFactor)
            float yScale = squareScaleFactor - (ScaleFactor - 1f) * (1f - squareScaleFactor);
            squareRect.localScale = new Vector3(1f, yScale, 1f);
        }

        // 尾パーツ（任意）
        if (square2Rect)
        {
            square2Rect.localScale = new Vector3(ScaleFactor, 1f + ((1f - ScaleFactor) / 2f), 1f);
        }

        // 適用済み値を記録
        _lastLength = arrowLength;
        _lastThickness = arrowThickness;
        _lastRotation = arrowRotation;
        _lastScale = ScaleFactor;
    }

    /// <summary>
    /// 値が変化した場合のみ更新をかける（ランタイム用）。
    /// </summary>
    void Update()
    {
        if (!squareRect || !triangleRect) return;

        // 変更検知（無変更なら何もしない）
        if (Mathf.Approximately(_lastLength, arrowLength) &&
            Mathf.Approximately(_lastThickness, arrowThickness) &&
            Mathf.Approximately(_lastRotation, arrowRotation) &&
            Mathf.Approximately(_lastScale, ScaleFactor))
        {
            return;
        }

        SetArrowProperties(arrowLength, arrowThickness, arrowRotation);
    }

    /// <summary>
    /// エディタ上でInspectorの値が変更されたときにも即時反映する。
    /// </summary>
    void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SetArrowProperties(arrowLength, arrowThickness, arrowRotation);
        }
    }
}
