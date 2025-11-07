using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ViewPointControl : MonoBehaviour
{
    Camera cam;             //Camera型の変数cam
    private Vector3 cameraLookAt = new Vector3(25, 5, 25);   //カメラ視線格納用のVector3
    private Vector2 startPos; //マウス操作検出用、マウスボタン押された始点座標の座標格納用

    private float camAngle = 0f;    //カメラ旋回位置（0〜2PI）
    private float camHight = 25f;   //カメラ高さ
    private float camDist  = 47f;    //カメラ距離

    private float cameraOffsetX = 0.265f;

    private float camAngle_dt = 0f; //カメラ旋回速度（0〜0.01）
    private float camHight_dt = 0f;  //カメラ高さ移動速度

    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        //マウス操作によるカメラ移動速度の更新
        if (Input.GetMouseButtonDown(0))
            //マウスをクリックした座標を保存
        {   this.startPos=Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            //マウスを離した座標を取得
            Vector2 endPost = Input.mousePosition;

            //スワイプの長さを初速度に変換
            this.camAngle_dt = (endPost.x- this.startPos.x) /2000.0f;
            this.camHight_dt = (this.startPos.y-endPost.y)/500.0f;

            //マウス押した場所が、タイムスライダーのあるあたりの場合は、加速度を0にする
            if (this.startPos.y < 90f)
            {   this.camAngle_dt = 0.0f;
                this.camHight_dt = 0.0f;
            }

        }

        //マウスホール
        camDist -= Input.mouseScrollDelta.y;


        //キー入力によるカメラ移動速度の更新
        if (Input.GetKey(KeyCode.RightArrow))
        { //右回転
            GetComponent<AudioSource>().Play();
            this.camAngle_dt = 0.02f;
        }
        if (Input.GetKey(KeyCode.LeftArrow))
        { //左回転
            this.camAngle_dt = -0.02f;
            GetComponent<AudioSource>().Play();
        }
        if (Input.GetKey(KeyCode.UpArrow))
        { //上昇
            GetComponent<AudioSource>().Play();
            this.camHight_dt = 0.2f;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        { //下降
            GetComponent<AudioSource>().Play();
            this.camHight_dt = -0.2f;
        }

        if (Input.GetKey(KeyCode.PageUp))
        { //ズーム
            GetComponent<AudioSource>().Play();
            this.camDist -= 0.2f;
        }
        if (Input.GetKey(KeyCode.PageDown))
        { //パン
            GetComponent<AudioSource>().Play();
            this.camDist += 0.2f;
        }

        //カメラ慣性のダンピング
        this.camAngle_dt *= 0.95f;
        this.camHight_dt *= 0.95f;

        //カメラ距離の上限下限
        camDist = Mathf.Max(camDist, 3f);
        camDist = Mathf.Min(camDist, 200f);

        //カメラ旋回角度の更新
        this.camAngle += this.camAngle_dt;
        if (this.camAngle >= 2f*Mathf.PI) { this.camAngle = 0f; }

        //カメラ距離の更新
        this.camHight += this.camHight_dt;
        if (this.camHight <= 5f) { this.camHight = 5f; }
        if (this.camHight >= 100f) { this.camHight = 100f; }

        //カメラ位置の更新
        float x = 25f + camDist * Mathf.Sin(Mathf.PI/2f + this.camAngle);
        float z = 25f + camDist * Mathf.Cos(Mathf.PI/2f + this.camAngle);

        //camera_x = Side_length / 2 + Math.Cos(Math.PI / 2 + Math.PI * camera_rotation / 180) * camera_distance
        //camera_y = Side_length / 2 + Math.Sin(Math.PI / 2 + Math.PI * camera_rotation / 180) * camera_distance
        //camera_z = -Side_length

        cam.transform.position = new Vector3(x, camHight, z);
        cam.transform.LookAt(cameraLookAt);

        cam.rect = new Rect(cameraOffsetX, 0f, 1f - cameraOffsetX, 1f);
    }
}
