using UnityEngine;

// このクラスは凡例画面に使用され、TreeItemのデータを保持します。
[System.Serializable]
public class TreeItemData : ISerializationCallbackReceiver
{
    [Header("Visual")]
    [Tooltip("木の見た目を表すスプライト（Realistic表示で使用）")]
    public Sprite treeImage;

    [Header("Texts")]
    [Tooltip("凡例のタイトル。空での運用は非推奨")]
    public string title;

    [Tooltip("説明文 (英語)。非jaロケールで優先使用")]
    [TextArea(2, 6)]
    public string description;

    [Tooltip("説明文 (日本語)。jaロケールで優先使用")]
    [TextArea(2, 6)]
    public string descriptionJp;

    // --- 便利メソッド: ロケールコードに応じて説明文を返す（空なら相互フォールバック） ---
    /// <summary>
    /// ロケールコード（例: "ja", "en"）に応じて説明文を返す。空の場合は相互にフォールバック。
    /// 不明なコードやどちらも空なら空文字を返す。
    /// </summary>
    public string GetDescriptionByLocaleCode(string localeCode)
    {
        // 安全側: nullを空文字扱い
        var jp = descriptionJp ?? string.Empty;
        var en = description ?? string.Empty;

        if (localeCode == "ja")
            return !string.IsNullOrWhiteSpace(jp) ? jp : en;

        // 非ja: まず英語、空なら日本語へ
        return !string.IsNullOrWhiteSpace(en) ? en : jp;
    }

    // --- シリアライズ時の堅牢化（null→空文字、Trim） ---
    public void OnBeforeSerialize()
    {
        Normalize();
    }

    public void OnAfterDeserialize()
    {
        Normalize();
    }

    private void Normalize()
    {
        title = Sanitize(title);
        description = Sanitize(description);
        descriptionJp = Sanitize(descriptionJp);
        // treeImageはnull許容（未設定時のプレースホルダ運用を許す）
    }

    private static string Sanitize(string s)
    {
        if (s == null) return string.Empty;
        var t = s.Trim();
        // 必要なら全角スペースのトリム、改行正規化などもここで実施可
        return t;
    }
}
