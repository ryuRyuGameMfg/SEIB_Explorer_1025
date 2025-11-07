using UnityEngine;

// このクラスはツリーの簡易表示に使用されます

[ExecuteInEditMode]
public class TreeModel : MonoBehaviour
{
    // 幹のゲームオブジェクト
    public GameObject trunk;
    // 冠のゲームオブジェクト
    public GameObject crown;

    // 幹の高さ
    public float trunkHeight = 4.5f;
    // 冠の高さ
    public float crownHeight = 12f;
    // 幹の直径
    public float trunkDiameter = 2f;
    // 冠の半径
    public float crownRadius = 16f;
    // 冠のX位置
    public float crownXPosition = 0f;
    // 冠のZ位置
    public float crownZPosition = 0f;

    // これらの変数は凡例画面で使用されます
    private float baseTrunkHeight = 4.5f;
    private float baseCrownHeight = 12f;
    private float baseTrunkDiameter = 2f;
    private float baseCrownRadius = 16f;
    private float baseCrownXPosition = 0f;
    private float baseCrownZPosition = 0f;

    // 初期ツリーデータ（隠しインスペクタ）
    [HideInInspector]
    public TreeData initialTreeData = null;

    [HideInInspector]
    public TreeData treeData = null;

    // デフォルトの幹の高さ
    private float defaultTrunkHeight;
    // デフォルトの冠の高さ
    private float defaultCrownHeight;
    // デフォルトの幹の直径
    private float defaultTrunkDiameter;
    // デフォルトの冠の半径
    private float defaultCrownRadius;
    // デフォルトの冠のX位置
    private float defaultCrownXPosition;
    // デフォルトの冠のZ位置
    private float defaultCrownZPosition;

    // 冠の色
    public Color crownColor = new Color(0.1f, 0.6f, 0.1f, 1.0f);

    // 最後に設定された冠の色
    Color lastCrownColor;

    private void Awake()
    {
        // デフォルト値の設定
        defaultTrunkHeight = trunkHeight;
        defaultCrownHeight = crownHeight;
        defaultTrunkDiameter = trunkDiameter;
        defaultCrownRadius = crownRadius;
        defaultCrownXPosition = crownXPosition;
        defaultCrownZPosition = crownZPosition;

        lastCrownColor = crownColor;

        // 冠のマテリアルカラーの設定
        SetMaterialColor(crown, crownColor);
    }

    private void Update()
    {
        // ツリーモデルの更新
        UpdateTreeModel();
    }

    // ツリーデータの設定
    public void SetTreeData(TreeData treeData, float scale)
    {
        this.treeData = treeData;

        float initialHeight = initialTreeData.BoleH + initialTreeData.CrownH;
        float height = treeData.BoleH + treeData.CrownH;

        float defaultHeight = defaultTrunkHeight + defaultCrownHeight;
        float newHeight = defaultHeight * (height / initialHeight);

        trunkHeight = (treeData.BoleH / height) * newHeight;
        crownHeight = (treeData.CrownH / height) * newHeight;

        // 幹の直径にスケールを掛ける
        trunkDiameter = defaultTrunkDiameter * (treeData.BoleD / initialTreeData.BoleD) * 0.5f; // * scale;
        crownRadius = defaultCrownRadius * (treeData.CrownD / initialTreeData.CrownD);

        float crownRatio = (treeData.CrownD / initialTreeData.CrownD);
        crownXPosition = (treeData.CrownX - treeData.BoleX) * crownRatio;
        crownZPosition = (treeData.CrownY - treeData.BoleY) * crownRatio;
    }

    // ツリーモデルの更新
    private void UpdateTreeModel()
    {
        // 幹の更新
        trunk.transform.localScale = new Vector3(trunkDiameter, trunkHeight, trunkDiameter);
        trunk.transform.localPosition = new Vector3(0, trunkHeight, 0);

        // 冠の更新
        crown.transform.localScale = new Vector3(crownRadius, crownHeight, crownRadius);
        crown.transform.localPosition = new Vector3(crownXPosition, (trunkHeight * 2) + crownHeight, crownZPosition);

        if (crownColor != lastCrownColor)
        {
            SetMaterialColor(crown, crownColor);
            lastCrownColor = crownColor;
        }
    }

    // ゲームオブジェクトのマテリアルカラーを設定
    private void SetMaterialColor(GameObject obj, Color color)
    {
        Renderer renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial.color = color;
        }
    }

    // デフォルト値にリセット
    public void Reset()
    {
        trunkHeight = baseTrunkHeight;
        crownHeight = baseCrownHeight;
        trunkDiameter = baseTrunkDiameter;
        crownRadius = baseCrownRadius;
        crownXPosition = baseCrownXPosition;
        crownZPosition = baseCrownZPosition;
        UpdateTreeModel();
    }
}
