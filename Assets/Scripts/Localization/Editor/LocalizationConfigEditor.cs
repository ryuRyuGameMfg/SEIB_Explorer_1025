#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LocalizationConfigのエディタ拡張
/// ScriptableObjectで一括管理、自動設定機能を提供
/// </summary>
[CustomEditor(typeof(LocalizationConfig))]
public class LocalizationConfigEditor : Editor
{
    private LocalizationConfig config;

    private void OnEnable()
    {
        config = (LocalizationConfig)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("ローカライズ設定一括管理", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("このScriptableObjectでUI要素のローカライズ設定を一括管理できます。\nInspectorで直接マッピングを追加・編集できます。", MessageType.Info);

        EditorGUILayout.Space();
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("設定の読み込み", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("現在のシーン内の既存のローカライズ設定を読み込んで、マッピングリストに追加します。", MessageType.Info);

        if (GUILayout.Button("シーン内の設定を読み込む", GUILayout.Height(30)))
        {
            LoadFromScene();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("自動設定", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("現在のシーン内のUI要素に自動的にローカライズ設定を適用します。", MessageType.Info);

        if (GUILayout.Button("シーン内のUI要素に自動設定", GUILayout.Height(30)))
        {
            ApplyToScene();
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("マッピングは上記の「Mappings」リストで直接編集できます。\n「+」ボタンで追加、「-」ボタンで削除できます。", MessageType.Info);

        serializedObject.ApplyModifiedProperties();
    }


    /// <summary>
    /// シーン内の既存のローカライズ設定を読み込む
    /// </summary>
    private void LoadFromScene()
    {
        int loadedCount = 0;
        int skippedCount = 0;

        // シーン内のすべてのGameObjectを走査
        GameObject[] allObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        
        foreach (GameObject rootObj in allObjects)
        {
            LoadFromGameObject(rootObj, "", ref loadedCount, ref skippedCount);
        }

        EditorUtility.SetDirty(config);
        EditorUtility.DisplayDialog("完了", 
            $"設定を読み込みました\n追加: {loadedCount}件\nスキップ: {skippedCount}件", "OK");
    }

    /// <summary>
    /// GameObjectとその子を再帰的に走査して設定を読み込む
    /// </summary>
    private void LoadFromGameObject(GameObject obj, string parentPath, ref int loadedCount, ref int skippedCount)
    {
        string currentPath = string.IsNullOrEmpty(parentPath) ? obj.name : parentPath + "/" + obj.name;

        // Textコンポーネントをチェック
        Text text = obj.GetComponent<Text>();
        if (text != null)
        {
            if (LoadLocalizedStringFromComponent(obj, currentPath, LocalizationConfig.ComponentType.Text, ref loadedCount, ref skippedCount))
            {
                // 読み込み成功
            }
        }

        // TextMeshProUGUIコンポーネントをチェック
        TextMeshProUGUI textMesh = obj.GetComponent<TextMeshProUGUI>();
        if (textMesh != null)
        {
            if (LoadLocalizedStringFromComponent(obj, currentPath, LocalizationConfig.ComponentType.TextMeshProUGUI, ref loadedCount, ref skippedCount))
            {
                // 読み込み成功
            }
        }

        // SliderTimeRangeコンポーネントをチェック
        SliderTimeRange slider = obj.GetComponent<SliderTimeRange>();
        if (slider != null)
        {
            if (LoadLocalizedStringFromSliderTimeRange(obj, currentPath, ref loadedCount, ref skippedCount))
            {
                // 読み込み成功
            }
        }

        // UImanagerコンポーネントをチェック
        UImanager uiManager = obj.GetComponent<UImanager>();
        if (uiManager != null)
        {
            LoadLocalizedStringFromUImanager(obj, currentPath, ref loadedCount, ref skippedCount);
        }

        // 子オブジェクトを再帰的に処理
        foreach (Transform child in obj.transform)
        {
            LoadFromGameObject(child.gameObject, currentPath, ref loadedCount, ref skippedCount);
        }
    }

    /// <summary>
    /// LocalizedStringComponentから設定を読み込む
    /// </summary>
    private bool LoadLocalizedStringFromComponent(GameObject obj, string path, LocalizationConfig.ComponentType componentType, ref int loadedCount, ref int skippedCount)
    {
        var localizedStringComp = obj.GetComponent<LocalizedStringComponent>();
        if (localizedStringComp == null)
        {
            return false;
        }

        var serializedObject = new SerializedObject(localizedStringComp);
        var localStringProp = serializedObject.FindProperty("localizedString");
        
        if (localStringProp == null) return false;

        var tableRef = localStringProp.FindPropertyRelative("m_TableReference");
        var entryRef = localStringProp.FindPropertyRelative("m_TableEntryReference");

        if (tableRef == null || entryRef == null) return false;

        string tableName = tableRef.FindPropertyRelative("m_TableCollectionName").stringValue;
        string entryName = entryRef.FindPropertyRelative("m_Entry").stringValue;

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(entryName))
        {
            skippedCount++;
            return false;
        }

        // 既に同じマッピングが存在するかチェック
        if (config.mappings.Exists(m => m.gameObjectPath == path && m.componentType == componentType))
        {
            skippedCount++;
            return false;
        }

        // マッピングを追加
        var mapping = new LocalizationConfig.LocalizationMapping
        {
            gameObjectPath = path,
            componentType = componentType,
            localizationKey = entryName,
            tableCollectionName = tableName,
            enabled = true
        };

        config.mappings.Add(mapping);
        loadedCount++;
        return true;
    }

    /// <summary>
    /// SliderTimeRangeから設定を読み込む
    /// </summary>
    private bool LoadLocalizedStringFromSliderTimeRange(GameObject obj, string path, ref int loadedCount, ref int skippedCount)
    {
        SliderTimeRange slider = obj.GetComponent<SliderTimeRange>();
        if (slider == null) return false;

        var serializedObject = new SerializedObject(slider);
        var localStringProp = serializedObject.FindProperty("localString");
        
        if (localStringProp == null) return false;

        var tableRef = localStringProp.FindPropertyRelative("m_TableReference");
        var entryRef = localStringProp.FindPropertyRelative("m_TableEntryReference");

        if (tableRef == null || entryRef == null) return false;

        string tableName = tableRef.FindPropertyRelative("m_TableCollectionName").stringValue;
        string entryName = entryRef.FindPropertyRelative("m_Entry").stringValue;

        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(entryName))
        {
            skippedCount++;
            return false;
        }

        // 既に同じマッピングが存在するかチェック
        if (config.mappings.Exists(m => m.gameObjectPath == path && m.componentType == LocalizationConfig.ComponentType.SliderTimeRange))
        {
            skippedCount++;
            return false;
        }

        // マッピングを追加
        var mapping = new LocalizationConfig.LocalizationMapping
        {
            gameObjectPath = path,
            componentType = LocalizationConfig.ComponentType.SliderTimeRange,
            localizationKey = entryName,
            tableCollectionName = tableName,
            enabled = true
        };

        config.mappings.Add(mapping);
        loadedCount++;
        return true;
    }

    /// <summary>
    /// UImanagerから設定を読み込む
    /// </summary>
    private void LoadLocalizedStringFromUImanager(GameObject obj, string path, ref int loadedCount, ref int skippedCount)
    {
        UImanager uiManager = obj.GetComponent<UImanager>();
        if (uiManager == null) return;

        var serializedObject = new SerializedObject(uiManager);
        
        // UImanagerのLocalizedStringフィールドを探す
        // 一般的なフィールド名をチェック
        string[] commonFieldNames = { "latString", "lonString", "altString", "fieldCapString", "wiltPoiString", "albedoString" };
        
        foreach (string fieldName in commonFieldNames)
        {
            var prop = serializedObject.FindProperty(fieldName);
            if (prop == null) continue;

            var tableRef = prop.FindPropertyRelative("m_TableReference");
            var entryRef = prop.FindPropertyRelative("m_TableEntryReference");

            if (tableRef == null || entryRef == null) continue;

            string tableName = tableRef.FindPropertyRelative("m_TableCollectionName").stringValue;
            string entryName = entryRef.FindPropertyRelative("m_Entry").stringValue;

            if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(entryName))
            {
                skippedCount++;
                continue;
            }

            // 既に同じマッピングが存在するかチェック
            if (config.mappings.Exists(m => m.gameObjectPath == path && 
                m.componentType == LocalizationConfig.ComponentType.UImanager && 
                m.localizationKey == fieldName))
            {
                skippedCount++;
                continue;
            }

            // マッピングを追加（UImanagerの場合はフィールド名をキーとして使用）
            var mapping = new LocalizationConfig.LocalizationMapping
            {
                gameObjectPath = path,
                componentType = LocalizationConfig.ComponentType.UImanager,
                localizationKey = fieldName, // UImanagerの場合はフィールド名をキーとして使用
                tableCollectionName = tableName,
                enabled = true
            };

            config.mappings.Add(mapping);
            loadedCount++;
        }
    }

    /// <summary>
    /// シーン内のUI要素に自動設定を適用
    /// </summary>
    private void ApplyToScene()
    {
        if (string.IsNullOrEmpty(config.tableCollectionName))
        {
            EditorUtility.DisplayDialog("エラー", "Table Collection Nameが設定されていません。", "OK");
            return;
        }

        int appliedCount = 0;
        int errorCount = 0;

        foreach (var mapping in config.mappings)
        {
            if (!mapping.enabled) continue;

            try
            {
                GameObject targetObj = GameObject.Find(mapping.gameObjectPath);
                if (targetObj == null)
                {
                    Debug.LogWarning($"GameObjectが見つかりません: {mapping.gameObjectPath}");
                    errorCount++;
                    continue;
                }

                ApplyLocalization(targetObj, mapping);
                appliedCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"設定エラー ({mapping.gameObjectPath}): {e.Message}");
                errorCount++;
            }
        }

        EditorUtility.DisplayDialog("完了", 
            $"設定完了\n成功: {appliedCount}件\nエラー: {errorCount}件", "OK");
    }

    /// <summary>
    /// 個別のGameObjectにローカライズ設定を適用
    /// </summary>
    private void ApplyLocalization(GameObject obj, LocalizationConfig.LocalizationMapping mapping)
    {
        switch (mapping.componentType)
        {
            case LocalizationConfig.ComponentType.Text:
                ApplyToText(obj, mapping);
                break;
            case LocalizationConfig.ComponentType.TextMeshProUGUI:
                ApplyToTextMeshPro(obj, mapping);
                break;
            case LocalizationConfig.ComponentType.SliderTimeRange:
                ApplyToSliderTimeRange(obj, mapping);
                break;
            case LocalizationConfig.ComponentType.UImanager:
                ApplyToUImanager(obj, mapping);
                break;
        }
    }

    private void ApplyToText(GameObject obj, LocalizationConfig.LocalizationMapping mapping)
    {
        Text text = obj.GetComponent<Text>();
        if (text == null)
        {
            Debug.LogWarning($"Textコンポーネントが見つかりません: {obj.name}");
            return;
        }

        // LocalizedStringコンポーネントを追加または取得
        var localizedStringComp = obj.GetComponent<LocalizedStringComponent>();
        if (localizedStringComp == null)
        {
            localizedStringComp = obj.AddComponent<LocalizedStringComponent>();
        }

        // SerializedObjectでLocalizedStringを設定
        var serializedObject = new SerializedObject(localizedStringComp);
        var localStringProp = serializedObject.FindProperty("localizedString");
        
        if (localStringProp != null)
        {
            // TableReferenceを設定（マッピングごとのテーブル名、なければデフォルト）
            string tableName = string.IsNullOrEmpty(mapping.tableCollectionName) 
                ? config.tableCollectionName 
                : mapping.tableCollectionName;
            var tableRef = localStringProp.FindPropertyRelative("m_TableReference");
            if (tableRef != null)
            {
                tableRef.FindPropertyRelative("m_TableCollectionName").stringValue = tableName;
            }

            // EntryReferenceを設定
            var entryRef = localStringProp.FindPropertyRelative("m_TableEntryReference");
            if (entryRef != null)
            {
                entryRef.FindPropertyRelative("m_Entry").stringValue = mapping.localizationKey;
            }

            serializedObject.ApplyModifiedProperties();
            Debug.Log($"設定完了: {obj.name} -> {mapping.localizationKey}");
        }
    }

    private void ApplyToTextMeshPro(GameObject obj, LocalizationConfig.LocalizationMapping mapping)
    {
        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            Debug.LogWarning($"TextMeshProUGUIコンポーネントが見つかりません: {obj.name}");
            return;
        }

        var localizedStringComp = obj.GetComponent<LocalizedStringComponent>();
        if (localizedStringComp == null)
        {
            localizedStringComp = obj.AddComponent<LocalizedStringComponent>();
        }

        // SerializedObjectでLocalizedStringを設定
        var serializedObject = new SerializedObject(localizedStringComp);
        var localStringProp = serializedObject.FindProperty("localizedString");
        
        if (localStringProp != null)
        {
            // TableReferenceを設定（マッピングごとのテーブル名、なければデフォルト）
            string tableName = string.IsNullOrEmpty(mapping.tableCollectionName) 
                ? config.tableCollectionName 
                : mapping.tableCollectionName;
            var tableRef = localStringProp.FindPropertyRelative("m_TableReference");
            if (tableRef != null)
            {
                tableRef.FindPropertyRelative("m_TableCollectionName").stringValue = tableName;
            }

            var entryRef = localStringProp.FindPropertyRelative("m_TableEntryReference");
            if (entryRef != null)
            {
                entryRef.FindPropertyRelative("m_Entry").stringValue = mapping.localizationKey;
            }

            serializedObject.ApplyModifiedProperties();
            Debug.Log($"設定完了: {obj.name} -> {mapping.localizationKey}");
        }
    }

    private void ApplyToSliderTimeRange(GameObject obj, LocalizationConfig.LocalizationMapping mapping)
    {
        SliderTimeRange slider = obj.GetComponent<SliderTimeRange>();
        if (slider == null)
        {
            Debug.LogWarning($"SliderTimeRangeコンポーネントが見つかりません: {obj.name}");
            return;
        }

        // LocalizedStringフィールドを設定
        var serializedObject = new SerializedObject(slider);
        var localStringProp = serializedObject.FindProperty("localString");
        
        if (localStringProp != null)
        {
            // TableReferenceを設定（マッピングごとのテーブル名、なければデフォルト）
            string tableName = string.IsNullOrEmpty(mapping.tableCollectionName) 
                ? config.tableCollectionName 
                : mapping.tableCollectionName;
            var tableRef = localStringProp.FindPropertyRelative("m_TableReference");
            if (tableRef != null)
            {
                tableRef.FindPropertyRelative("m_TableCollectionName").stringValue = tableName;
            }
            localStringProp.FindPropertyRelative("m_TableEntryReference.m_Entry").stringValue = mapping.localizationKey;
            serializedObject.ApplyModifiedProperties();
        }
    }

    private void ApplyToUImanager(GameObject obj, LocalizationConfig.LocalizationMapping mapping)
    {
        UImanager uiManager = obj.GetComponent<UImanager>();
        if (uiManager == null)
        {
            Debug.LogWarning($"UImanagerコンポーネントが見つかりません: {obj.name}");
            return;
        }

        // UImanagerのLocalizedStringフィールドを設定
        var serializedObject = new SerializedObject(uiManager);
        var fieldName = mapping.localizationKey; // キー名をフィールド名として使用（例: "latString"）

        var prop = serializedObject.FindProperty(fieldName);
        if (prop != null)
        {
            // TableReferenceを設定（マッピングごとのテーブル名、なければデフォルト）
            string tableName = string.IsNullOrEmpty(mapping.tableCollectionName) 
                ? config.tableCollectionName 
                : mapping.tableCollectionName;
            var tableRef = prop.FindPropertyRelative("m_TableReference");
            if (tableRef != null)
            {
                tableRef.FindPropertyRelative("m_TableCollectionName").stringValue = tableName;
            }
            prop.FindPropertyRelative("m_TableEntryReference.m_Entry").stringValue = mapping.localizationKey;
            serializedObject.ApplyModifiedProperties();
        }
    }

}
#endif

