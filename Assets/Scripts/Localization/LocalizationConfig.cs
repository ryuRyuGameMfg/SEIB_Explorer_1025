using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;

/// <summary>
/// ローカライズ設定を一括管理するScriptableObject
/// Inspectorで直接編集可能
/// </summary>
[CreateAssetMenu(fileName = "LocalizationConfig", menuName = "SEIB Explorer/Localization Config")]
public class LocalizationConfig : ScriptableObject
{
    [Header("ローカライゼーションテーブル")]
    [Tooltip("使用するStringTableCollectionの名前（例: \"StringTable\"）")]
    public string tableCollectionName = "StringTable";

    [Header("UI要素とローカライズキーのマッピング")]
    [Tooltip("UI要素のパスとローカライズキーの対応表")]
    public List<LocalizationMapping> mappings = new List<LocalizationMapping>();

    [Header("日本語・英語翻訳ペア")]
    [Tooltip("日本語と英語の翻訳ペア（コード生成用）")]
    public List<TranslationPair> translationPairs = new List<TranslationPair>();

    [System.Serializable]
    public class TranslationPair
    {
        [Tooltip("ローカライズキー名")]
        public string key;

        [Tooltip("日本語テキスト")]
        public string japanese;

        [Tooltip("英語テキスト")]
        public string english;

        [Tooltip("このペアを有効にするか")]
        public bool enabled = true;
    }

    [System.Serializable]
    public class LocalizationMapping
    {
        [Tooltip("GameObjectのパス（Hierarchy内のパス、例: Canvas/Panel/TitleText）")]
        public string gameObjectPath;

        [Tooltip("コンポーネントタイプ（Text, TextMeshProUGUI, Button等）")]
        public ComponentType componentType;

        [Tooltip("ローカライゼーションテーブル内のキー名")]
        public string localizationKey;

        [Tooltip("使用するStringTableCollectionの名前（空欄の場合はデフォルトのテーブルを使用）")]
        public string tableCollectionName = "";

        [Tooltip("動的な値の引数（例: {0} の値）")]
        public List<string> arguments = new List<string>();

        [Tooltip("このマッピングを有効にするか")]
        public bool enabled = true;
    }

    public enum ComponentType
    {
        Text,
        TextMeshProUGUI,
        Button,
        SliderTimeRange,
        UImanager
    }
}

