using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderYear : MonoBehaviour
{
    //スライダーの取得
    Slider slider;

    //ローカル変数の定義
    //int  sliderValue=1;

    //表示用のGameObjectを定義
    GameObject ViewYear;

    //Autoボタンが押されているときの挙動制御用
    float time_span = 0.25f; //1年を移動させる実時間(秒)
    float time_delta = 0f; //前回のフレームから経過した時間を記録する変数

    // Start is called before the first frame update
    void Start()
    {
        slider = GetComponent<Slider>();
        slider.value = 1;
        slider.minValue = 1;
        slider.maxValue = Director.SimYearMax;
        //sliderValue = (int)slider.value;

        //現在年を表示するラベルを探索して取得する
        this.ViewYear = GameObject.Find("ViewYear");
    }

    //読み取ったシミュレーションデータから得られた最大年数を、スライダーバーの最大値に反映する
    public void Init()
    {
        slider.value = 1;
        slider.maxValue = Director.SimYearMax;
    }

    //キー入力による値変更を反映させるための受信機
    public void ChangeValue()
    {
        slider.value = Director.SimYear;
    }

    // Update is called once per frame
    void Update()
    {
        //シミュレーション年をスライダーから取得して、その値を表示する
        Director.SimYear =  (int)slider.value;
        this.ViewYear.GetComponent<Text>().text = Director.SimYear.ToString("D4") + "yrs";

        //Autoボタン押されているときに、シミュレーション年を自動更新する
        if (Director.SimYearMove == true && Director.SimYear < Director.SimYearMax)
        {

            this.time_delta += Time.deltaTime;
            if (this.time_delta > this.time_span)
            {
                this.time_delta = 0f;
                Director.SimYear = Director.SimYear + 1;
                if (Director.SimYear == Director.SimYearMax) { Director.SimYearMove = false; }
                slider.value = Director.SimYear;
            }
        }
    }
}
