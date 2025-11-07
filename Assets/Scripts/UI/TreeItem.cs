using UI.ThreeDimensional;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.UI;

// このクラスは凡例画面に使用され、UI要素のプロパティを設定します。
public class TreeItem : MonoBehaviour
{
    [Header("Display Targets")]
    [Tooltip("3DオブジェクトをUIに表示するためのコンポーネント")]
    public UIObject3D treeObject;

    [Tooltip("木のサムネイル画像を表示するUI")]
    public Image treeImage;

    [Header("Texts")]
    [Tooltip("タイトル表示用テキスト")]
    public Text titleText;

    [Tooltip("説明表示用テキスト")]
    public Text descriptionText;

    /// <summary>
    /// UIアイテムの見た目をTreeItemDataと表示モードに基づいて構成する。
    /// Realisticでは画像を、その他では3Dオブジェクトを表示する。
    /// ロケールに応じて説明文を切り替える。
    /// </summary>
    public void SetTreeItem(TreeItemData treeItemData, int idx)
    {
        // ---- 防御的チェック ----
        if (treeItemData == null) return;
        if (!titleText || !descriptionText) return;
        if (!treeImage || !treeObject) return;

        // ---- 表示モードに応じて画像/3Dを出し分け ----
        bool isRealistic = Director.Instance &&
                           Director.Instance.currentViewMode == Director.ViewModes.Realistic;

        SetActiveIfChanged(treeImage.gameObject, isRealistic);
        SetActiveIfChanged(treeObject.gameObject, !isRealistic);

        if (isRealistic)
        {
            // 画像表示モード
            treeImage.sprite = treeItemData.treeImage;
        }
        else
        {
            // 3D表示モード
            var treeGo = Director.Instance ? Director.Instance.GetTreeObject(idx) : null;
            if (treeGo != null)
            {
                Transform treeTransform = treeGo.transform;
                // UIObject3Dの設定先がTransformである前提（元コード踏襲）
                treeObject.ObjectPrefab = treeTransform;
            }
        }

        // ---- タイトル ----
        titleText.text = treeItemData.title ?? string.Empty;

        // ---- 説明（ロケールに応じて選択 + フォールバック） ----
        Locale locale = LocalizationSettings.SelectedLocale;
        bool useJp = locale && locale.Identifier.Code == "ja";

        // 優先: JP→EN、 それぞれ空ならフォールバック
        string jp = treeItemData.descriptionJp;
        string en = treeItemData.description;

        string chosen = useJp ? (string.IsNullOrEmpty(jp) ? en : jp)
                              : (string.IsNullOrEmpty(en) ? jp : en);

        descriptionText.text = chosen ?? string.Empty;
    }

    // 現在の状態と異なるときのみSetActiveを行う小ヘルパー
    private static void SetActiveIfChanged(GameObject go, bool active)
    {
        if (go && go.activeSelf != active) go.SetActive(active);
    }
}
