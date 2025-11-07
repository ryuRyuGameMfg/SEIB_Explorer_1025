using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class GlobeViewer : MonoBehaviour
{
    //========================================================================
    // 参照設定
    //========================================================================
    public Camera globeCamera;           // 地球を映すカメラ
    public Transform globeLight;         // ライト
    public GameObject markerPrefab;      // マーカーPrefab（球など）
    public float earthRadius = 5f;       // 地球の半径（描画スケール用）

    [Header("カメラ距離設定")]
    public float cameraDistance = 10f;   // 初期距離
    public Slider distanceSlider;        // スライダーreference

    [Header("マーカー用コンテナ（必須）")]
    public Transform markersRoot;        // マーカー専用の親（この子だけ消す）

    [Header("位置補正（微調整用）")]
    public float latOffset = 0.0f; // 緯度方向の補正角（度単位）
    public float lonOffset = 0.0f; // 経度方向の補正角（度単位）
    
    //========================================================================
    // 内部変数
    //========================================================================
    private float currentLat = 0f;       // 現在の緯度
    private float currentLon = 0f;       // 現在の経度

    // 生成されたマーカーを記録しておくリスト（リセット用）
    private readonly List<GameObject> markers = new List<GameObject>();

    //========================================================================
    // 初期化
    //========================================================================
    public void Setup()
    {
        // まず既存マーカーを全削除（★markersRoot配下のみ）
        ResetMarkers();

        currentLat = Director.LAT;
        currentLon = Director.LON;

        Debug.Log("currentLat: " + currentLat);
        Debug.Log("currentLon: " + currentLon);

        // 視点設定とマーカー生成
        LookAtLatLon(currentLat, currentLon);
        PlaceMarker(currentLat, currentLon);

        // スライダーにリスナーを登録（多重登録防止）
        if (distanceSlider != null)
        {
            distanceSlider.onValueChanged.RemoveListener(OnDistanceSliderChanged);
            distanceSlider.onValueChanged.AddListener(OnDistanceSliderChanged);
        }
    }

    //========================================================================
    // 指定の緯度・経度へカメラを向ける
    //========================================================================
    public void LookAtLatLon(float latitude, float longitude)
    {
        currentLat = latitude;
        currentLon = longitude;

        float latRad = (latitude + latOffset) * Mathf.Deg2Rad;
        float lonRad = (longitude + lonOffset) * Mathf.Deg2Rad;

        // 経緯度を3D座標へ変換
        Vector3 target = new Vector3(
            Mathf.Cos(latRad) * Mathf.Cos(lonRad),
            Mathf.Sin(latRad),
            Mathf.Cos(latRad) * Mathf.Sin(lonRad)
        ) * earthRadius;

        // カメラとライトを配置
        Vector3 cameraPos = target.normalized * cameraDistance;
        if (globeCamera != null)
        {
            globeCamera.transform.position = cameraPos;
            globeCamera.transform.LookAt(Vector3.zero);
        }

        if (globeLight != null)
        {
            globeLight.transform.position = cameraPos;
            globeLight.transform.LookAt(Vector3.zero);
        }
    }

    //========================================================================
    // 指定の緯度経度にマーカーを配置
    //========================================================================
    public void PlaceMarker(float latitude, float longitude)
    {
        if (markerPrefab == null || markersRoot == null) return;

        double latRad = (latitude + latOffset) * Mathf.Deg2Rad;
        double lonRad = (longitude + lonOffset) * Mathf.Deg2Rad;

        // 緯度経度 → 3D座標（Y軸が北極方向）
        Vector3 pos = new Vector3(
            (float)Mathf.Cos((float)latRad) * Mathf.Cos((float)lonRad),
            (float)Mathf.Sin((float)latRad),
            Mathf.Cos((float)latRad) * Mathf.Sin((float)lonRad)
        ) * earthRadius;

        // マーカー生成（★markersRoot 配下に限定）
        GameObject markerObj = Instantiate(markerPrefab, pos, Quaternion.identity, markersRoot);
        markers.Add(markerObj);
    }

    //========================================================================
    // スライダー値変更時に呼ばれる処理
    //========================================================================
    public void OnDistanceSliderChanged(float value)
    {
        cameraDistance = value;
        LookAtLatLon(currentLat, currentLon); // 現在の緯度経度で再設定
    }

    //========================================================================
    // 全マーカーを削除（★markersRoot配下のみ）
    //========================================================================
    public void ResetMarkers()
    {
        // リストに残っている参照を破棄
        foreach (var marker in markers)
        {
            if (marker != null) Destroy(marker);
        }
        markers.Clear();

        // 念のため markersRoot の残存子も掃除（★markersRoot限定）
        if (markersRoot != null)
        {
            for (int i = markersRoot.childCount - 1; i >= 0; i--)
            {
                var child = markersRoot.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
        }
    }

    //========================================================================
    // インスペクタ誤設定チェック（任意）
    //========================================================================
    private void OnValidate()
    {
        if (markersRoot == null)
            Debug.LogWarning("[GlobeViewer] markersRoot が未設定です（マーカーが消えません）");
    }
}
