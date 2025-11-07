#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

// このクラスはUnityエディターで使用されます。TreeGizmoのプロパティを設定します。

#if UNITY_EDITOR
// TreeGizmosのカスタムエディタ
[CustomEditor(typeof(TreeGizmos))]
public class TreeGizmosEditor : Editor
{
    // インスペクターGUIをカスタマイズ
    public override void OnInspectorGUI()
    {
        TreeGizmos treeGizmos = (TreeGizmos)target;

        // 木のプロパティのラベル
        GUILayout.Label("Tree Properties", EditorStyles.boldLabel);

        // 木の位置の入力フィールド
        treeGizmos.treePosition = EditorGUILayout.Vector3Field("Tree Position", treeGizmos.treePosition);

        // 木の高さの入力フィールド
        treeGizmos.treeHeight = EditorGUILayout.FloatField("Tree Height", treeGizmos.treeHeight);

        // 木の半径の入力フィールド
        treeGizmos.treeRadius = EditorGUILayout.FloatField("Tree Radius", treeGizmos.treeRadius);

        // 木の色の入力フィールド
        treeGizmos.treeColor = EditorGUILayout.ColorField("Tree Color", treeGizmos.treeColor);

        // 樹冠のプロパティのラベル
        GUILayout.Label("Crown Properties", EditorStyles.boldLabel);

        // 樹冠の位置の入力フィールド
        treeGizmos.crownPosition = EditorGUILayout.Vector3Field("Crown Position", treeGizmos.crownPosition);

        // 樹冠の高さの入力フィールド
        treeGizmos.crownHeight = EditorGUILayout.FloatField("Crown Height", treeGizmos.crownHeight);

        // 樹冠の半径の入力フィールド
        treeGizmos.crownRadius = EditorGUILayout.FloatField("Crown Radius", treeGizmos.crownRadius);

        // 樹冠の色の入力フィールド
        treeGizmos.crownColor = EditorGUILayout.ColorField("Crown Color", treeGizmos.crownColor);

        // 変更があった場合、オブジェクトをマークする
        if (GUI.changed)
        {
            EditorUtility.SetDirty(treeGizmos);
        }
    }
}
#endif

