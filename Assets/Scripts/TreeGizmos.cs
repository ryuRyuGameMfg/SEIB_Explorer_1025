using UnityEngine;

// このクラスは木の高さと樹冠の幅を設定するために使用されます。計算に使用されます。

[ExecuteAlways]
public class TreeGizmos : MonoBehaviour
{
    // 木の位置
    public Vector3 treePosition = Vector3.zero;

    // 木の高さ
    public float treeHeight = 5f;

    // 木の半径
    public float treeRadius = 0.5f;

    // 木の色
    public Color treeColor = new Color(0.6f, 0.3f, 0.1f, 0.5f);

    // 樹冠の位置
    public Vector3 crownPosition = new Vector3(0, 5f, 0);

    // 樹冠の高さ
    public float crownHeight = 3f;

    // 樹冠の半径
    public float crownRadius = 2f;

    // 樹冠の色
    public Color crownColor = new Color(0.1f, 0.6f, 0.1f, 0.5f);

    private void OnDrawGizmos()
    {
        Vector3 scale = transform.localScale;

        // スケールされた位置を計算
        Vector3 scaledTrunkPosition = Vector3.Scale(treePosition, scale);
        Vector3 scaledCrownPosition = Vector3.Scale(crownPosition, scale);

        // 幹を描画
        Gizmos.color = treeColor;
        Gizmos.DrawMesh(CreateCylinderMesh(treeHeight * scale.y, treeRadius * scale.x), transform.position + scaledTrunkPosition, transform.rotation);

        // 樹冠を描画
        Gizmos.color = crownColor;
        Gizmos.DrawMesh(CreateCylinderMesh(crownHeight * scale.y, crownRadius * scale.x), transform.position + scaledCrownPosition, transform.rotation);
    }

    // 円柱メッシュを作成するメソッド
    private Mesh CreateCylinderMesh(float height, float radius)
    {
        Mesh mesh = new Mesh();
        int segments = 20;

        // 頂点配列
        Vector3[] vertices = new Vector3[(segments + 1) * 2];

        // 三角形インデックス配列
        int[] triangles = new int[segments * 12];

        // 法線ベクトル配列
        Vector3[] normals = new Vector3[(segments + 1) * 2];

        float angleStep = 360.0f / segments;

        // 頂点と法線ベクトルを計算
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            vertices[i] = new Vector3(x, 0, z);
            vertices[i + segments + 1] = new Vector3(x, height, z);

            normals[i] = new Vector3(x, 0, z).normalized;
            normals[i + segments + 1] = new Vector3(x, height, z).normalized;
        }

        // 三角形インデックスを計算
        for (int i = 0; i < segments; i++)
        {
            int baseIndex = i * 12;

            triangles[baseIndex] = i;
            triangles[baseIndex + 1] = i + segments + 1;
            triangles[baseIndex + 2] = i + 1;

            triangles[baseIndex + 3] = i + 1;
            triangles[baseIndex + 4] = i + segments + 1;
            triangles[baseIndex + 5] = i + segments + 2;

            // 内側面の三角形を重複させる
            triangles[baseIndex + 6] = i;
            triangles[baseIndex + 7] = i + 1;
            triangles[baseIndex + 8] = i + segments + 1;

            triangles[baseIndex + 9] = i + 1;
            triangles[baseIndex + 10] = i + segments + 2;
            triangles[baseIndex + 11] = i + segments + 1;
        }

        // メッシュにデータを設定
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;

        return mesh;
    }
}
