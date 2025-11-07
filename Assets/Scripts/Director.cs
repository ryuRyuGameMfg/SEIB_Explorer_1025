using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using System.Globalization;
using UnityEngine.UI;               //UI部品を使うために必要
using UnityEngine.Localization;
using System.Linq;
using System;  //LoadSceneを使うために必須
using UnityEngine.UIElements;
using System.IO;
using System.Collections;
using Unity.VisualScripting;
using System.Windows.Forms;
using UnityEngine.Rendering;


public class Director : MonoBehaviour
{
    public enum AppState { TopMenu, FunctionA, FunctionB }

    [HideInInspector] 
    public AppState appState = AppState.TopMenu;

    // Culture-invariant number parsing
    static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    //■■■■■■ キー入力の受信用にUImanagerへの参照を設定する(Inspector で設定) ■■■■■■
    [FormerlySerializedAs("uiManager")]
    public UImanager uiManager;

    //■■■■■■ 固定パラメーター定義 ■■■■■■

    //各月に含まれる日数（閏日は無視）
    [HideInInspector]
    public static int[] Day_in_month = new int[12]
        { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

    //"Day of Year"から"Day of Month"への変換行列
    [HideInInspector]
    public static int[] Day_of_month = new int[365]
        {1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,   //Jan
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,             //Feb
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,    //Mar
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,       //Apr
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,    //May
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,       //Jun
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,    //Jul
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,    //Aug
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,       //Sep
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,    //Oct
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,       //Nov
        1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31};   //Dec

    //"Day of Year"から"Month"への変換行列'
    [HideInInspector]
    public static int[] Month = new int[365]
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,   //Jan
        2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,             //Feb
        3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,    //Mar
        4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,       //Apr
        5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,    //May
        6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,       //Jun
        7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,    //Jul
        8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8, 8,    //Aug
        9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9, 9,       //Sep
        10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,10,   //Oct
        11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,11,      //Nov
        12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12,12 }; //Dec

    // 入力ファイルの種類を定義する列挙型定義
    public enum InputFileType
    {
        Output_forest,          // output_forest.txt (木本動態のみのデータファイル)
        Output_VegStructure,    // output_VegStructure (上記に加えて、草本動態のデータを含むファイル)
        Output_for_viewer2      // output_for_viewer2 (上記に加えて、各種物質循環などを含むファイル)
    }

    //■■■■■■ 入力データを保存する変数の定義 ■■■■■■

    //リスト変数に関するメモ書き
    //★3D表示に関わる木本PFT参照番号（リスト形式変数PFT_no_woodyなど）のみ、1から開始させているので注意。いずれ直すこと。


    // 現在の入力ファイルタイプを保存する列挙型（初期値は"Output_forest"）
    [HideInInspector] public static InputFileType currentFileType = InputFileType.Output_forest;

    //各種のシミュレーション設定を保存する変数 (時間に対して不変の値）
    //初期値を入れている場合もあるが、入力ファイル形式がOutput_forest以外の場合には読み出しデータに基づいてUpdateされるはず
    [HideInInspector] public static float GroundMax = 30;      //林床の一辺の長さ（m）
    [HideInInspector] public static int DivedG = 15;        //草本層タイルが、林床の一辺を分割する数
    [HideInInspector] public static int SimYearMax = 100;   //シミュレーションした年数 
    [HideInInspector] public static int PFT_no_Max;         //PFT種類数（草本込み）
    [HideInInspector] public static int NumSoil;            //Number of soil layer
    [HideInInspector] public static float LAT;                //Latitude for simulate
    [HideInInspector] public static float LON;                //Longitude for simulate
    [HideInInspector] public static float ALT;                //Altitude (m above MSL)
    [HideInInspector] public static float Albedo_soil0;       //Albedo, default
    [HideInInspector] public static float W_fi;               //Filed capacity   (m3/m3, 0.0 -> 1.0)
    [HideInInspector] public static float W_wilt;             //Wilting point    (m3/m3, 0.0 -> 1.0)
    [HideInInspector] public static float W_sat;              //Saturate point   (m3/m3, 0.0 -> 1.0)

    //Yearly時系列データ
    [HideInInspector] public List<float> aCO2 = new List<float>(); //大気中CO2濃度 (ppm)
    [HideInInspector] public List<float> tmp_air_y = new List<float>(); //年平均気温 (C)    

    //炭素フラックス時系列データ, 入れ子のListで2次元配列状構造とした。例えば、fluxC_GPP[0][6]で、シミュレーション1年目の7月のGPPが取り出せる
    //   [Monthly, MgC/ha/month]
    [HideInInspector] public List<List<float>> fluxC_gpp = new List<List<float>>(); //総光合成生産
    [HideInInspector] public List<List<float>> fluxC_atr = new List<List<float>>(); //植物の呼吸
    [HideInInspector] public List<List<float>> fluxC_htr = new List<List<float>>(); //従属栄養生物による呼吸
    [HideInInspector] public List<List<float>> fluxC_lit = new List<List<float>>(); //バイオマスからリターへのフラックス
    [HideInInspector] public List<List<float>> fluxC_som = new List<List<float>>(); //リターから土壌炭素へのフレックス
    [HideInInspector] public List<List<float>> fluxC_fir = new List<List<float>>(); //林野火災による炭素ロスト
    //   [Yearly, MgC/ha/yr]
    [HideInInspector] public List<float> fluxC_gpp_y = new List<float>(); //総光合成生産
    [HideInInspector] public List<float> fluxC_atr_y = new List<float>(); //植物の呼吸
    [HideInInspector] public List<float> fluxC_htr_y = new List<float>(); //従属栄養生物による呼吸
    [HideInInspector] public List<float> fluxC_lit_y = new List<float>(); //バイオマスからリターへのフラックス
    [HideInInspector] public List<float> fluxC_som_y = new List<float>(); //リターから土壌炭素へのフレックス
    [HideInInspector] public List<float> fluxC_fir_y = new List<float>(); //林野火災による炭素ロスト

    //炭素貯留量の時系列データ, Monthly, MgC/ha
    [HideInInspector] public List<List<float>> poolC_Litter = new List<List<float>>(); //Litter
    [HideInInspector] public List<List<float>> poolC_SOM1 = new List<List<float>>(); //Soil organic matter, Intermediate decomposion rate
    [HideInInspector] public List<List<float>> poolC_SOM2 = new List<List<float>>(); //Soil organic matter, slow decomposition rate
    [HideInInspector] public List<List<float>> poolC_Woody = new List<List<float>>(); //Biomass of woody PFTs
    [HideInInspector] public List<List<float>> poolC_Grass = new List<List<float>>(); //Biomass of grass PFTs

    //水フラックス時系列データ
    //   [Monthly, mm/month]
    [HideInInspector] public List<List<float>> fluxW_pre = new List<List<float>>(); //降水
    [HideInInspector] public List<List<float>> fluxW_ro1 = new List<List<float>>(); //地表からの系外への流出
    [HideInInspector] public List<List<float>> fluxW_ro2 = new List<List<float>>(); //基底流出
    [HideInInspector] public List<List<float>> fluxW_ic = new List<List<float>>(); //樹冠による遮断蒸発
    [HideInInspector] public List<List<float>> fluxW_ev = new List<List<float>>(); //土壌表面からの蒸発
    [HideInInspector] public List<List<float>> fluxW_tr = new List<List<float>>(); //蒸散
    [HideInInspector] public List<List<float>> fluxW_sl = new List<List<float>>(); //積雪からの昇華
    [HideInInspector] public List<List<float>> fluxW_tw = new List<List<float>>(); //積雪の融解
    [HideInInspector] public List<List<float>> fluxW_sn = new List<List<float>>(); //降雪
    //   [Yearly, mm/yr]
    [HideInInspector] public List<float> fluxW_pre_y = new List<float>(); //年降水量 (mm/yr)
    [HideInInspector] public List<float> fluxW_ro1_y = new List<float>(); //地表からの系外への流出
    [HideInInspector] public List<float> fluxW_ro2_y = new List<float>(); //基底流出
    [HideInInspector] public List<float> fluxW_ic_y = new List<float>();  //樹冠による遮断蒸発
    [HideInInspector] public List<float> fluxW_ev_y = new List<float>();  //土壌表面からの蒸発
    [HideInInspector] public List<float> fluxW_tr_y = new List<float>();  //蒸散
    [HideInInspector] public List<float> fluxW_sl_y = new List<float>();  //積雪からの昇華
    [HideInInspector] public List<float> fluxW_tw_y = new List<float>();  //積雪の融解
    [HideInInspector] public List<float> fluxW_sn_y = new List<float>();  //降雪

    //水の貯留量の時系列データ, Daily
    //例えば、poolW_L1[0][20]で、シミュレーション1年目のDOY=21（1月21日）の土壌含水率が取り出せる
    [HideInInspector] public List<List<float>> poolW_L1 = new List<List<float>>(); //Average fraction of water in soil layers  1- 5 (fraction)
    [HideInInspector] public List<List<float>> poolW_L2 = new List<List<float>>(); //Average fraction of water in soil layers  6-10 (fraction)
    [HideInInspector] public List<List<float>> poolW_L3 = new List<List<float>>(); //Average fraction of water in soil layers 11-20 (fraction)
    [HideInInspector] public List<List<float>> poolW_snow = new List<List<float>>(); //Water equivalent snow pool (mm)

    //放射関係, Daily
    [HideInInspector] public List<List<float>> par_direct = new List<List<float>>(); //PAR, direct part  (micro mol photon m-2 s-1)
    [HideInInspector] public List<List<float>> par_diffuse = new List<List<float>>(); //PAR, diffuse part (micro mol photon m-2 s-1)

    [HideInInspector] public List<List<float>> rad_short_direct = new List<List<float>>(); //Downward shortwave radiation, direct  part (W/m2)
    [HideInInspector] public List<List<float>> rad_short_diffuse = new List<List<float>>(); //Downward shortwave radiation, diffuse part (W/m2)
    [HideInInspector] public List<List<float>> rad_short_up = new List<List<float>>(); //Upward   shortwave radiation, diffuse part (W/m2)

    [HideInInspector] public List<List<float>> rad_long_down = new List<List<float>>(); //Downward longwave radiation, diffuse part (W/m2)
    [HideInInspector] public List<List<float>> rad_long_up = new List<List<float>>(); //Upward   longwave radiation, diffuse part (W/m2)

    [HideInInspector] public List<List<float>> albedo_mean = new List<List<float>>(); //Albedo, mean field         (fraction)
    [HideInInspector] public List<List<float>> albedo_veg = new List<List<float>>(); //Albedo, vegetation surface (fraction)
    [HideInInspector] public List<List<float>> albedo_soil = new List<List<float>>(); //Albedo, soil surface       (fraction)

    //大気関係の時系列データ, Daily
    //例えば、tmp_air[0][20]で、シミュレーション1年目のDOY=21（1月21日）の気温が取り出せる
    [HideInInspector] public List<List<float>> tmp_air = new List<List<float>>(); //temperature of air (Celcius)
    [HideInInspector] public List<List<float>> tmp_soil1 = new List<List<float>>(); //temperature of soil layer 1 (Celcius)
    [HideInInspector] public List<List<float>> tmp_soil2 = new List<List<float>>(); //temperature of soil layer 2 (Celcius)
    [HideInInspector] public List<List<float>> tmp_soil3 = new List<List<float>>(); //temperature of soil layer 3 (Celcius)
    [HideInInspector] public List<List<float>> rh = new List<List<float>>(); //Relative Humidity (%)
    [HideInInspector] public List<List<float>> wind = new List<List<float>>(); //wind velocity (m/s)

    //DailyをMonthlyに変換した環境変数
    //例えば、poolW_L1[0][2]で、シミュレーション1年目の3月の土壌含水率が取り出せる
    [HideInInspector] public List<List<float>> poolW_L1_m = new List<List<float>>(); //Average fraction of water in soil layers  1- 5 (fraction)
    [HideInInspector] public List<List<float>> poolW_L2_m = new List<List<float>>(); //Average fraction of water in soil layers  6-10 (fraction)
    [HideInInspector] public List<List<float>> poolW_L3_m = new List<List<float>>(); //Average fraction of water in soil layers 11-20 (fraction)
    [HideInInspector] public List<List<float>> poolW_snow_m = new List<List<float>>(); //Water equivalent snow pool (mm)

    [HideInInspector] public List<List<float>> par_direct_m = new List<List<float>>(); //PAR, direct part  (micro mol photon m-2 s-1)
    [HideInInspector] public List<List<float>> par_diffuse_m = new List<List<float>>(); //PAR, diffuse part (micro mol photon m-2 s-1)

    [HideInInspector] public List<List<float>> rad_short_direct_m = new List<List<float>>(); //Downward shortwave radiation, direct  part (W/m2)
    [HideInInspector] public List<List<float>> rad_short_diffuse_m = new List<List<float>>(); //Downward shortwave radiation, diffuse part (W/m2)
    [HideInInspector] public List<List<float>> rad_short_up_m = new List<List<float>>(); //Upward   shortwave radiation, diffuse part (W/m2)

    [HideInInspector] public List<List<float>> rad_long_down_m = new List<List<float>>(); //Downward longwave radiation, diffuse part (W/m2)
    [HideInInspector] public List<List<float>> rad_long_up_m = new List<List<float>>(); //Upward   longwave radiation, diffuse part (W/m2)

    [HideInInspector] public List<List<float>> albedo_mean_m = new List<List<float>>(); //Albedo, mean field         (fraction)
    [HideInInspector] public List<List<float>> albedo_veg_m = new List<List<float>>(); //Albedo, vegetation surface (fraction)
    [HideInInspector] public List<List<float>> albedo_soil_m = new List<List<float>>(); //Albedo, soil surface       (fraction)

    [HideInInspector] public List<List<float>> tmp_air_m = new List<List<float>>(); //temperature of air (Celcius)
    [HideInInspector] public List<List<float>> tmp_soil1_m = new List<List<float>>(); //temperature of soil layer 1 (Celcius)
    [HideInInspector] public List<List<float>> tmp_soil2_m = new List<List<float>>(); //temperature of soil layer 2 (Celcius)
    [HideInInspector] public List<List<float>> tmp_soil3_m = new List<List<float>>(); //temperature of soil layer 3 (Celcius)
    [HideInInspector] public List<List<float>> rh_m = new List<List<float>>(); //relative humidity (%)
    [HideInInspector] public List<List<float>> wind_m = new List<List<float>>(); //wind velocity (m/s)

    //可視化する時系列データ: 炭素の貯留量関係の変数（time series）
    //入れ子のListで3次元配列状構造とした、Monthly、PFTごと
    //例えば、 poolC_whole[0][14][6] で、シミュレーション1年目7月のPFT15番の葉群炭素量が取り出せる
    [HideInInspector] public List<List<List<float>>> poolC_leaf = new List<List<List<float>>>(); //Carbon pool in leaves    (Mg C / ha)
    [HideInInspector] public List<List<List<float>>> poolC_trunk = new List<List<List<float>>>(); //Carbon pool in trunk     (Mg C / ha)
    [HideInInspector] public List<List<List<float>>> poolC_root = new List<List<List<float>>>(); //Carbon pool in root      (Mg C / ha)
    [HideInInspector] public List<List<List<float>>> poolC_stock = new List<List<List<float>>>(); //Carbon pool in stock     (Mg C / ha)
    [HideInInspector] public List<List<List<float>>> poolC_available = new List<List<List<float>>>(); //Carbon pool in available (Mg C / ha)
    [HideInInspector] public List<List<List<float>>> poolC_whole = new List<List<List<float>>>(); //Carbon pool in whole plant body (Mg C / ha)

    //★★★今回の依頼で可視化する
    //葉面積指数の3次元配列、daily、PFTごと
    //例えば、 lai_PFT[0][13][6] で、シミュレーション1年目1月7日のPFT14番の葉面積指数が取り出せる
    //例えば、 lai_PFT_m[0][13][6] で、シミュレーション1年目7月のPFT14番の葉面積指数が取り出せる
    [HideInInspector] public List<List<List<float>>> lai_PFT = new List<List<List<float>>>(); //Leaf Area Index for each PFT (m2/m2)
    [HideInInspector] public List<List<List<float>>> lai_PFT_m = new List<List<List<float>>>(); //Leaf Area Index for each PFT, monthly (m2/m2)

    //★★★今回の依頼で使用する(存在するPFTの数だけあれば良いが、大きめの値を指定している)
    [HideInInspector] public bool[] PFT_available = new bool[50]; //全要素がfalseで初期化される


    //放射フラックス年平均値の時系列, yearly, W/m2, Dailyデータから本スクリプト内で計算
    [HideInInspector] public List<float> rad_short_direct_amean = new List<float>();
    [HideInInspector] public List<float> rad_short_diffuse_amean = new List<float>();
    [HideInInspector] public List<float> rad_short_up_amean = new List<float>();
    [HideInInspector] public List<float> rad_long_down_amean = new List<float>();
    [HideInInspector] public List<float> rad_long_up_amean = new List<float>();

    public static int SimTimeRangeMax = 10;
    public static int CarbonScaleMax = 0;
    public static int WaterScaleMax = 0;
    public static int RadiationScaleMax = 0;

    //■■■■■■ プレファブ・テクスチャ関連 ■■■■■■
    //Woody PFTのいるいないパラメーター（※配列1番から使用する点に注意）
    public static int PFT_no_Max_Woody = 21;                 //木本PFTの種類数（配列の0番にはダミー数を入れる、木本PFT数+1で設定する）
    public static bool[] PFT_OnOff_woody = new bool[PFT_no_Max_Woody];      // このデータセットにどの木本PFTが出現するか否かのスイッチ

    //木本数関連
    public List<int> treeNo_list = new List<int>();

    //個木のパラメーターList
    List<TreeData> treeDataList = new List<TreeData>();

    //Grassデータのリスト
    List<GrassData> grassDataList = new List<GrassData>();

    Dictionary<(float BoleX, float BoleY), List<TreeData>> treeDataGroups = new Dictionary<(float BoleX, float BoleY), List<TreeData>>();


    Dictionary<int, List<TreeData>> treesByYear = new Dictionary<int, List<TreeData>>();

    // (x,y,pft) -> 配列インデックスのルックアップ表（GetTreeIndexのO(1)化）
    Dictionary<(float x, float y, int pft), int> treeIndex = new Dictionary<(float x, float y, int pft), int>();

    // 草レイヤのテクスチャを年×表示モードでキャッシュ
    Dictionary<(int year, ViewModes mode), Texture2D> grassTextureCache = new Dictionary<(int year, ViewModes mode), Texture2D>();
    //木本プレハブ関連：配列数入れているけれど、Inspectorの設定が優先されるっぽい
    // これらの変数は、リアルなツリーに使用されます。
    public GameObject[] WoodyPFTs = new GameObject[PFT_no_Max_Woody];  //木のprefab。どのprefabを割り当てるかは、インスペクターを介して設定する。
    public GameObject[] tree_3d = new GameObject[10000];    //個木にassigneされるprefab
    public Transform parent;

    // これらの変数は、簡易ツリーに使用されます。
    public GameObject[] WoodyPFTs2 = new GameObject[PFT_no_Max_Woody];  //木のprefab。どのprefabを割り当てるかは、インスペクターを介して設定する。
    public GameObject[] tree_3d_2 = new GameObject[10000];    //個木にassigneされるprefab
    public Transform parent2;

    // デフォルトの地面テクスチャ
    public Texture2D groundTexture;

    // 草のテクスチャ
    public Texture2D[] GrassTextures; // Realistic表示モード
    public Texture2D[] GrassTextures2; // Simple表示モード

    // 草テクスチャのピクセルデータ
    private Color[][] texturePixelData; // Realistic表示モード
    private Color[][] texturePixelData2; // Simple表示モード

    public GameObject planePrefab;
    GameObject planeInstance;

    //■■■■■■ 動作状態を記録する変数など ■■■■■■
    // ファイル名
    [HideInInspector] public string fileName;

    //シミュレーション年の表示関係
    public static int SimYear = 1;             //表示中のシミュレーション年
    public static bool SimYearMove = false;     //シミュレーション年の移動中フラグ
    public static int SimYear_LastDraw = 1;    //最後に森林描画をアップデートしたシミュレーション年

    public static int SimTimeRange = 1;
    public static int SimTimeRange_LastDraw = 1;

    //描画のスケーラー
    public static float Scale_All = 1.0f;

    public LocalizedString localString;
    public SliderYear sliderYear;
    public SliderTimeRange sliderTimeRange;
    public Text textComponent;
    string forestSizeString;

    int treeCount = 0;
    TreeModel[] treeModels = new TreeModel[10000];

    // 表示モードを定義する列挙型
    public enum ViewModes
    {
        Simplified, // 簡略化モード
        Realistic   // リアルモード
    }

    // 現在の表示モード（初期値は簡略化モード）
    [HideInInspector] public ViewModes currentViewMode = ViewModes.Simplified;

    // 草の表示モード
    public enum GrassViewModes
    {
        TreeOnly,     // 木本のみ表示
        TreeAndGrass, // 木本＋草本層の表示
        GrassOnly     // 草本層のみ表示
    }

    // 現在の草の表示モード
    GrassViewModes currentGrassViewMode = GrassViewModes.TreeOnly;

    // リアルモード用のボタン
    public GameObject realisticButton;

    // 簡略化モード用のボタン
    public GameObject simplifiedButton;

    // 草本層表示のボタン
    public GameObject grassButton;

    // リアルモード用のScale
    public GameObject realisticScale;

    // 簡略化モード用のScale
    public GameObject simplifiedScale;

    // Directorクラスのインスタンス（シングルトンパターン）
    public static Director Instance { get; private set; }


    // 地面のテクスチャが生成されたものかデフォルトかを判定
    public bool isGeneratedTexture = false;

    //■■■■■■ 演出関係 ■■■■■■

    // ランダムな回転を適用するかどうかを制御する
    public bool applyRandomRotation = false;

    // 音声ソース
    public AudioClip sound1;
    public AudioClip sound2;
    public AudioClip sound3;
    public AudioClip sound4;
    public AudioClip sound5;
    public AudioClip sound6;
    public AudioClip sound7;
    public AudioClip sound8;
    public AudioClip Sound_Poka;
    public AudioClip Sound_Pichi;
    public AudioClip Sound_Fon;
    public AudioClip Sound_Von;
    AudioSource audioSource;

    //グラフ
    public CarbonPool carbonPool;
    public CarbonFlux carbonFlux;
    public CarbonReloc carbonReloc;
    public WaterFlux waterFlux;
    public SnowFlux snowFlux;
    public SnowPool snowPool;
    //public WaterPool waterPool;
    public RadShort radShort;
    public RadLong radLong;
    public Albedo albedo;
    public AirTemperature airTemperature;
    public RelativeHumid relativeHumid;
    public WindVelocity windVelocity;
    public Biomass biomass;
    public LAI lai;
    public AtomosphericCO2 co2;
    public SoilHeatmapPanel soilHeatmap;

    public GlobeViewer globeViewer;
    public BiomePlotter biomePlotter;
    //■■■■■■ Awake ■■■■■■
    private void Awake()
    {
        // インスタンスが存在しない場合、現在のオブジェクトをインスタンスとして設定し、シーンを跨いで保持
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 任意：このマネージャーをシーンを跨いで保持する場合
        }
        else
        {
            // 既にインスタンスが存在する場合、現在のオブジェクトを破棄
            Destroy(gameObject);
        }

        // AudioSourceコンポーネントを取得
        //audioSource = GetComponent<AudioSource>();

    }

    //■■■■■■ FileOpenボタンを押したときの挙動 ■■■■■■
    public void OnClick_Button_OpenFile()
    {
        ReadFile();
    }

    public void ReadFile()
    {
        //★★★★
        // ファイルダイアログを開く
        // このページを参考にした
        // https://qiita.com/otochan/items/0f20fad94467bb2c2572
        // https://megalodon.jp/2024-1016-2250-17/https://qiita.com:443/otochan/items/0f20fad94467bb2c2572
        // ただし、「System.Windows.Forms」は下記パスからdllファイルをAssetフォルダ下にコピーした
        // C:¥Windows¥Microsoft.NET¥assembly¥GAC_MSIL¥System.Windows.Forms¥v4.0_4.0.0.0__b77a5c561934e089¥System.Windows.Forms.dll
        OpenFileDialog dlg = new OpenFileDialog();
        dlg.Filter = "txt(*.txt)|*.txt|All files(*.*)|*.*"; //txtファイルを開くことを指定
        dlg.CheckFileExists = false;                        //ファイルが実在しない場合は警告を出す(true)、警告を出さない(false)
        dlg.ShowDialog();                                   //ファイルダイヤログを開く
        //MessageBox.Show(dlg.FileName);                    //例：メッセージボックスをポップアップ

        OnReadFile(dlg.FileName);
    }

    //■■■■■■ シミュレーション結果ファイルの読み込み ■■■■■■
    public void OnReadFile(string filePath)
    {
        //データファイルの読込とデータの整理
        using (System.IO.StreamReader file = new System.IO.StreamReader(filePath))
        {
            // 選択されたファイルの名前（パスなし）を取得
            fileName = Path.GetFileName(filePath);

            //読込年カウンターを初期化
            int yearRead = 0;

            //シーンの各種設定を初期化
            ResetScene();





            //    //★★★★アセット内に格納されているファイルから直接読み出す場合(ファイル名の指定では拡張子を記述しないこと)
            //    //TextAsset textAsset = Resources.Load<TextAsset>("output_for_viewer2_NorthmostJapan");
            //    //TextAsset textAsset = Resources.Load<TextAsset>("output_for_viewer2_PasohMalaysia");
            //    TextAsset textAsset = Resources.Load<TextAsset>("output_for_viewer2_EastSiberia");

            //// ファイルの内容を直接取得
            //string fileContent = textAsset.text;

            //// ファイル名を設定 (拡張子なしで取得)
            //string fileName = "output_for_viewer2_NorthmostJapan";

            //// 読込年カウンターを初期化
            //int yearRead = 0;

            //// シーンの各種設定を初期化
            //ResetScene();

            //using (StringReader file = new StringReader(fileContent))
            //{






            //最初の1行目を読んで、入力ファイルタイプを判別する
            string lineHead = file.ReadLine();
            if (lineHead == "out_VegStructure") { currentFileType = InputFileType.Output_VegStructure; }  //output_VegStructure.txt形式
            else if (lineHead == "output_for_viewer2") { currentFileType = InputFileType.Output_for_viewer2; }   //output_for_viewer2.txt形式
            else { currentFileType = InputFileType.Output_forest; }        //output_forest.txt形式

            //■■■判別した入力ファイルタイプに応じた、ヘッダ読込処理
            switch (currentFileType)
            {
                case InputFileType.Output_forest:
                    //入力ファイルタイプ判別のために１行だけ読んでしまったストリーム位置を、戻す
                    //file.BaseStream.Position = 0;
                    //file.DiscardBufferedData();
                    break;

                case InputFileType.Output_VegStructure:
                //このタイプの入力ファイルでは処理無し

                case InputFileType.Output_for_viewer2:
                    //各種シミュレーション設定の読込、2行目
                    lineHead = file.ReadLine();
                    string[] datReadA = lineHead.Split(','); //1行読込とコンマでの分割 (以下、続く)
                    GroundMax = int.Parse(datReadA[0], Inv); //林床1辺の長さ
                    DivedG = int.Parse(datReadA[1], Inv); //草本層タイルによる林床1辺の分割数

                    //各種シミュレーション設定の読込、3行目
                    lineHead = file.ReadLine();
                    string[] datReadB = lineHead.Split(',');
                    SimYearMax = int.Parse(datReadB[0], Inv);  //シミュレーション年数
                    PFT_no_Max = int.Parse(datReadB[1], Inv);  //PFTの総数（草本PFTs込み）
                    NumSoil = int.Parse(datReadB[2], Inv);  //土壌層の数

                    //各種シミュレーション設定の読込、4行目
                    lineHead = file.ReadLine();
                    string[] datReadC = lineHead.Split(',');
                    LAT = float.Parse(datReadC[0], Inv); //緯度
                    LON = float.Parse(datReadC[1], Inv); //経度
                    ALT = float.Parse(datReadC[2], Inv); //標高
                    Albedo_soil0 = float.Parse(datReadC[3], Inv); //土壌アルベド
                    W_fi = float.Parse(datReadC[4], Inv); //FIeld capacity
                    W_wilt = float.Parse(datReadC[5], Inv); //Wilting point
                    W_sat = float.Parse(datReadC[6], Inv); //Saturation point

                    // Yearly Data
                    aCO2 = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    tmp_air_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_pre_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();

                    fluxC_gpp_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxC_atr_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxC_htr_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxC_lit_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxC_som_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxC_fir_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();

                    fluxW_pre_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_ro1_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_ro2_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_ic_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_ev_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_tr_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_sl_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_tw_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();
                    fluxW_sn_y = Enumerable.Repeat(0.0f, SimYearMax).ToList();

                    //仮想林分の状態に関する時系列データのリストを準備
                    List<List<float>>[] list_fluxC = { fluxC_gpp, fluxC_atr, fluxC_htr, fluxC_lit, fluxC_som, fluxC_fir };
                    List<List<float>>[] list_poolC = { poolC_Litter, poolC_SOM1, poolC_SOM2, poolC_Woody, poolC_Grass };
                    List<List<float>>[] list_fluxW = { fluxW_pre, fluxW_ro1, fluxW_ro2, fluxW_ic, fluxW_ev, fluxW_tr, fluxW_sl, fluxW_tw, fluxW_sn };

                    //Daily Data
                    List<List<float>>[] list_poolW = { poolW_L1, poolW_L2, poolW_L3, poolW_snow };
                    List<List<float>>[] list_etc = { tmp_air, tmp_soil1, tmp_soil2, tmp_soil3, rh, wind };
                    List<List<float>>[] list_par = { par_direct, par_diffuse };
                    List<List<float>>[] list_rad = { rad_short_direct, rad_short_diffuse, rad_short_up, rad_long_down, rad_long_up };
                    List<List<float>>[] list_albedo = { albedo_mean, albedo_veg, albedo_soil };

                    //Daily DataをMonthlyに変換したもの
                    List<List<float>>[] list_poolW_m = { poolW_L1_m, poolW_L2_m, poolW_L3_m, poolW_snow_m };
                    List<List<float>>[] list_etc_m = { tmp_air_m, tmp_soil1_m, tmp_soil2_m, tmp_soil3_m, rh_m, wind_m };
                    List<List<float>>[] list_par_m = { par_direct_m, par_diffuse_m };
                    List<List<float>>[] list_rad_m = { rad_short_direct_m, rad_short_diffuse_m, rad_short_up_m, rad_long_down_m, rad_long_up_m };
                    List<List<float>>[] list_albedo_m = { albedo_mean_m, albedo_veg_m, albedo_soil_m };


                    for (int yr = 0; yr < SimYearMax; yr++)
                    {
                        //Monthly data
                        foreach (var n in list_fluxC) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                        foreach (var n in list_poolC) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                        foreach (var n in list_fluxW) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }

                        //Daily data
                        foreach (var n in list_poolW) { n.Add(Enumerable.Repeat(0.0f, 365).ToList()); }
                        foreach (var n in list_etc) { n.Add(Enumerable.Repeat(0.0f, 365).ToList()); }
                        foreach (var n in list_par) { n.Add(Enumerable.Repeat(0.0f, 365).ToList()); }
                        foreach (var n in list_rad) { n.Add(Enumerable.Repeat(0.0f, 365).ToList()); }
                        foreach (var n in list_albedo) { n.Add(Enumerable.Repeat(0.0f, 365).ToList()); }

                        //★Daily dataをmonthlyに変換したデータ
                        foreach (var n in list_poolW_m) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                        foreach (var n in list_etc_m) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                        foreach (var n in list_par_m) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                        foreach (var n in list_rad_m) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                        foreach (var n in list_albedo_m) { n.Add(Enumerable.Repeat(0.0f, 12).ToList()); }
                    }

                    //PFTごとの時系列データ
                    for (int yr = 0; yr < SimYearMax; yr++)
                    {
                        List<List<float>> list_lai_PFT = new List<List<float>>(); //pftとdoyのリスト
                        List<List<float>> list_lai_PFT_m = new List<List<float>>(); //pftとmonthのリスト
                        List<List<float>> list_poolC_leaf = new List<List<float>>();
                        List<List<float>> list_poolC_trunk = new List<List<float>>();
                        List<List<float>> list_poolC_root = new List<List<float>>();
                        List<List<float>> list_poolC_stock = new List<List<float>>();
                        List<List<float>> list_poolC_available = new List<List<float>>();
                        List<List<float>> list_poolC_whole = new List<List<float>>();

                        for (int p = 0; p < PFT_no_Max; p++)
                        {
                            list_lai_PFT.Add(Enumerable.Repeat(0.0f, 365).ToList());
                            list_lai_PFT_m.Add(Enumerable.Repeat(0.0f, 12).ToList());
                            list_poolC_leaf.Add(Enumerable.Repeat(0.0f, 12).ToList());
                            list_poolC_trunk.Add(Enumerable.Repeat(0.0f, 12).ToList());
                            list_poolC_root.Add(Enumerable.Repeat(0.0f, 12).ToList());
                            list_poolC_stock.Add(Enumerable.Repeat(0.0f, 12).ToList());
                            list_poolC_available.Add(Enumerable.Repeat(0.0f, 12).ToList());
                            list_poolC_whole.Add(Enumerable.Repeat(0.0f, 12).ToList());
                        }
                        lai_PFT.Add(list_lai_PFT);
                        lai_PFT_m.Add(list_lai_PFT_m);
                        poolC_leaf.Add(list_poolC_leaf);
                        poolC_trunk.Add(list_poolC_trunk);
                        poolC_root.Add(list_poolC_root);
                        poolC_stock.Add(list_poolC_stock);
                        poolC_available.Add(list_poolC_available);
                        poolC_whole.Add(list_poolC_whole);
                    }

                    break;
            }

            //■■■InputFileType.Output_for_viewer2の場合のデータ本体のストリーム読込
            if (currentFileType == InputFileType.Output_for_viewer2)
            {
                lineHead = file.ReadLine(); //5行目読み飛ばし, Life_type(p)
                lineHead = file.ReadLine(); //6行目読み飛ばし, Phenology_type(p)
                lineHead = file.ReadLine(); //7行目読み飛ばし, PN_s(p)
                lineHead = file.ReadLine(); //8行目読み飛ばし, PN_r(p)

                for (int yr = 0; yr < SimYearMax; yr++)
                {
                    string line0 = file.ReadLine(); //'######'マークの読み飛ばし
                    bool[] PFT_available_yearly = new bool[PFT_no_Max];

                    for (int doy = 0; doy < 365; doy++)
                    {
                        //mo: このdoyの月から1を引いた整数
                        int mo = Month[doy] - 1;

                        //Update PFT existance flag @ 1st day of the each year
                        if (doy == 0)
                        {
                            Array.Fill(PFT_available_yearly, false);
                            lineHead = file.ReadLine();
                            string[] datReadA = lineHead.Split(',');
                            for (int p = 0; p < PFT_no_Max; p++)
                            {
                                if (int.Parse(datReadA[p], Inv) == 1)
                                {
                                    PFT_available_yearly[p] = true;
                                    PFT_available[p] = true;
                                }
                            }

                            //　aCO2[yr]にCO2濃度を読み込む（datReadAの後続要素があれば）
                            if (datReadA.Length > PFT_no_Max)  // CO2値が存在する場合
                            {
                                float co2value;
                                if (float.TryParse(datReadA[PFT_no_Max], out co2value))
                                {
                                    aCO2[yr] = co2value;
                                }
                            }
                        }

                        //□□□ Daily Output
                        //1行目読み飛ばし、Simulation day counter
                        file.ReadLine();

                        //●2行目 (Climatic variables)
                        lineHead = file.ReadLine();
                        string[] datRead2 = lineHead.Split(',');

                        tmp_air[yr][doy] = (float.Parse(datRead2[0], Inv));
                        tmp_soil1[yr][doy] = (float.Parse(datRead2[1], Inv));
                        tmp_soil2[yr][doy] = (float.Parse(datRead2[2], Inv));
                        tmp_soil3[yr][doy] = (float.Parse(datRead2[3], Inv));
                        rh[yr][doy] = (float.Parse(datRead2[6], Inv));
                        wind[yr][doy] = (float.Parse(datRead2[7], Inv));
                        rad_long_down[yr][doy] = (float.Parse(datRead2[9], Inv));

                        //Montly time seriesとして集計
                        tmp_air_m[yr][mo] += tmp_air[yr][doy];
                        tmp_soil1_m[yr][mo] += tmp_soil1[yr][doy];
                        tmp_soil2_m[yr][mo] += tmp_soil2[yr][doy];
                        tmp_soil3_m[yr][mo] += tmp_soil3[yr][doy];
                        rh_m[yr][mo] += rh[yr][doy];
                        wind_m[yr][mo] += wind[yr][doy];
                        rad_long_down_m[yr][mo] += rad_long_down[yr][doy];

                        //Yearly time seriesとして集計
                        tmp_air_y[yr] += tmp_air[yr][doy];

                        //●3行目 (Radiation properties on vegetation surface)
                        lineHead = file.ReadLine();
                        string[] datRead3 = lineHead.Split(',');
                        albedo_mean[yr][doy] = (float.Parse(datRead3[0], Inv));
                        albedo_soil[yr][doy] = (float.Parse(datRead3[1], Inv));
                        albedo_veg[yr][doy] = (float.Parse(datRead3[2], Inv));

                        //Montly time seriesとして集計
                        albedo_mean_m[yr][mo] += albedo_mean[yr][doy];
                        albedo_soil_m[yr][mo] += albedo_soil[yr][doy];
                        albedo_veg_m[yr][mo] += albedo_veg[yr][doy];

                        //●4行目、Physical status of radiation
                        lineHead = file.ReadLine();
                        string[] datRead4 = lineHead.Split(',');
                        par_direct[yr][doy] = (float.Parse(datRead4[0], Inv));
                        par_diffuse[yr][doy] = (float.Parse(datRead4[1], Inv));
                        rad_long_up[yr][doy] = (float.Parse(datRead4[5], Inv));

                        rad_short_direct[yr][doy] = par_direct[yr][doy] / (4.6f * 0.43f);
                        rad_short_diffuse[yr][doy] = par_diffuse[yr][doy] / (4.2f * 0.57f);
                        rad_short_up[yr][doy] = albedo_mean[yr][doy] * (rad_short_direct[yr][doy] + rad_short_diffuse[yr][doy]);

                        //Montly time seriesとして集計
                        rad_short_direct_m[yr][mo] += rad_short_direct[yr][doy];
                        rad_short_diffuse_m[yr][mo] += rad_short_diffuse[yr][doy];
                        rad_short_up_m[yr][mo] += rad_short_up[yr][doy];

                        //●5行目、貯水量
                        lineHead = file.ReadLine();
                        string[] datRead5 = lineHead.Split(',');
                        poolW_L1[yr][doy] = (float.Parse(datRead5[1], Inv)) / 500.0f; //深さ  0～ 50cmの含水率 (fraction)
                        poolW_L2[yr][doy] = (float.Parse(datRead5[2], Inv)) / 500.0f; //深さ 50～100cmの含水率 (fraction)
                        poolW_L3[yr][doy] = (float.Parse(datRead5[3], Inv)) / 1000.0f; //深さ100～200cmの含水率 (fraction)
                        poolW_snow[yr][doy] = float.Parse(datRead5[4], Inv);            //pool_snow (mm)                        

                        //Montly time seriesとして集計
                        poolW_L1_m[yr][mo] += poolW_L1[yr][doy];
                        poolW_L2_m[yr][mo] += poolW_L2[yr][doy];
                        poolW_L3_m[yr][mo] += poolW_L3[yr][doy];
                        poolW_snow_m[yr][mo] += poolW_snow[yr][doy];

                        //●6+n行目(nはこのシミュレーション中に出現するPFT数) LAI
                        for (int p = 0; p < PFT_no_Max; p++)
                        {
                            if (PFT_available_yearly[p])
                            {
                                lineHead = file.ReadLine();
                                lai_PFT[yr][p][doy] = float.Parse(lineHead, Inv);
                                lai_PFT_m[yr][p][mo] += lai_PFT[yr][p][doy];
                            }
                        }

                        //□□□ Monthly output (@last day of each month)
                        if (Day_of_month[doy] == Day_in_month[mo])
                        {
                            //Daily dataのMonthlyデータへの変換
                            tmp_air_m[yr][mo] /= Day_in_month[mo];
                            tmp_soil1_m[yr][mo] /= Day_in_month[mo];
                            tmp_soil2_m[yr][mo] /= Day_in_month[mo];
                            tmp_soil3_m[yr][mo] /= Day_in_month[mo];
                            rh_m[yr][mo] /= Day_in_month[mo];
                            wind_m[yr][mo] /= Day_in_month[mo];
                            rad_long_down_m[yr][mo] /= Day_in_month[mo];
                            albedo_mean_m[yr][mo] /= Day_in_month[mo];
                            albedo_soil_m[yr][mo] /= Day_in_month[mo];
                            albedo_veg_m[yr][mo] /= Day_in_month[mo];
                            rad_short_direct_m[yr][mo] /= Day_in_month[mo];
                            rad_short_diffuse_m[yr][mo] /= Day_in_month[mo];
                            rad_short_up_m[yr][mo] /= Day_in_month[mo];
                            poolW_L1_m[yr][mo] /= Day_in_month[mo];
                            poolW_L2_m[yr][mo] /= Day_in_month[mo];
                            poolW_L3_m[yr][mo] /= Day_in_month[mo];
                            poolW_snow_m[yr][mo] /= Day_in_month[mo];

                            for (int p = 0; p < PFT_no_Max; p++) { lai_PFT_m[yr][p][mo] /= Day_in_month[mo]; }

                            //1st line読み飛ばし
                            file.ReadLine();

                            //2nd line (Carbon flux & strage)
                            lineHead = file.ReadLine();
                            string[] datReadA = lineHead.Split(',');
                            fluxC_gpp[yr][mo] = float.Parse(datReadA[0], Inv);
                            fluxC_atr[yr][mo] = float.Parse(datReadA[1], Inv);
                            fluxC_htr[yr][mo] = float.Parse(datReadA[2], Inv);
                            fluxC_lit[yr][mo] = float.Parse(datReadA[3], Inv);
                            fluxC_som[yr][mo] = float.Parse(datReadA[4], Inv);
                            fluxC_fir[yr][mo] = float.Parse(datReadA[5], Inv);

                            poolC_Litter[yr][mo] = float.Parse(datReadA[6], Inv);
                            poolC_SOM1[yr][mo] = float.Parse(datReadA[7], Inv);
                            poolC_SOM2[yr][mo] = float.Parse(datReadA[8], Inv);
                            poolC_Woody[yr][mo] = float.Parse(datReadA[9], Inv);
                            poolC_Grass[yr][mo] = float.Parse(datReadA[10], Inv);

                            //3rd line (Water flux) 
                            lineHead = file.ReadLine();
                            string[] datReadB = lineHead.Split(',');
                            fluxW_pre[yr][mo] = float.Parse(datReadB[0], Inv);
                            fluxW_ro1[yr][mo] = float.Parse(datReadB[1], Inv);
                            fluxW_ro2[yr][mo] = float.Parse(datReadB[2], Inv);
                            fluxW_ic[yr][mo] = float.Parse(datReadB[3], Inv);
                            fluxW_ev[yr][mo] = float.Parse(datReadB[4], Inv);
                            fluxW_tr[yr][mo] = float.Parse(datReadB[5], Inv);
                            fluxW_sl[yr][mo] = float.Parse(datReadB[6], Inv);
                            fluxW_tw[yr][mo] = float.Parse(datReadB[7], Inv);
                            fluxW_sn[yr][mo] = float.Parse(datReadB[8], Inv);

                            //4th~ lines (PFT specific variables)
                            for (int p = 0; p < PFT_no_Max; p++)
                            {
                                if (PFT_available_yearly[p])
                                {
                                    lineHead = file.ReadLine();
                                    string[] datReadG = lineHead.Split(',');
                                    poolC_leaf[yr][p][mo] = float.Parse(datReadG[2], Inv);
                                    poolC_trunk[yr][p][mo] = float.Parse(datReadG[3], Inv);
                                    poolC_root[yr][p][mo] = float.Parse(datReadG[4], Inv);
                                    poolC_stock[yr][p][mo] = float.Parse(datReadG[5], Inv);
                                    poolC_available[yr][p][mo] = float.Parse(datReadG[6], Inv);
                                }
                            }
                        }

                        //□□□ Yearly output (@last fay of each year)
                        if (doy == 364)
                        {
                            //Daily dataのYearlyデータへの変換
                            tmp_air_y[yr] /= 365.0f;

                            //草本データの読込
                            float[,] lai_floor = new float[DivedG, DivedG]; //1年分の草本LAIデータ読み込み用のリスト
                            for (int j = 0; j < DivedG; j++)
                            {
                                string lineG = file.ReadLine();     //1行読込
                                string[] dataG = lineG.Split(',');  //コンマでの分割

                                for (int i = 0; i < DivedG; i++)
                                {
                                    float laiG = float.Parse(dataG[i], Inv);
                                    lai_floor[DivedG - 1 - i, j] = laiG; //X軸位置座標を逆順して（これを行わないと表示がずれる）データを格納
                                }
                            }

                            //草本LAIデータを格納するGrassDataクラスのgrassDataインスタンスを定義
                            GrassData grassData = new GrassData(yearRead, lai_floor);

                            // grassDataをリストに追加
                            grassDataList.Add(grassData);


                            //木本データの読込
                            //この年の生存木数を読む
                            string line1 = file.ReadLine();
                            string[] data1 = line1.Split(','); //1行読込とコンマでの分割

                            int treeNo = int.Parse(data1[0], Inv); //この年の生存木数
                            treeNo_list.Add(treeNo); //この年の生存木数を記録

                            //この年の木本インベントリーデータを読んで整理する
                            if (treeNo != 0)
                            {
                                for (int no = 0; no < treeNo; no++)
                                {
                                    //1行読込とコンマでの分割
                                    string line2 = file.ReadLine();
                                    string[] data2 = line2.Split(',');

                                    TreeData treeData = new TreeData
                                    {
                                        //とりあえず一時変数に読込
                                        Year = yearRead,
                                        BoleX = float.Parse(data2[0], Inv),  //bole location x (m)
                                        BoleY = float.Parse(data2[1], Inv),  //bole location y (m)
                                        CrownX = float.Parse(data2[2], Inv), //crown location x (m)
                                        CrownY = float.Parse(data2[3], Inv), //crown location y (m)
                                        BoleH = float.Parse(data2[4], Inv),  //bole height (m)
                                        CrownH = float.Parse(data2[5], Inv), //crown foliage height (m)
                                        BoleD = float.Parse(data2[6], Inv),  //DBH (m)
                                        CrownD = float.Parse(data2[7], Inv), //crown radias (m)
                                        PFT = int.Parse(data2[8], Inv),      //PFT (m)
                                        Height = float.Parse(data2[4], Inv) + float.Parse(data2[5], Inv) //tree height (m)
                                    };

                                    treeDataList.Add(treeData);
                                }
                            }
                        }
                    }
                    yearRead++;
                }
            }


            //■■■InputFileType.Output_VegStructureとInputFileType.Output_forestの場合のデータ本体のストリーム読込
            if (currentFileType != InputFileType.Output_for_viewer2)
            {
                while (!file.EndOfStream)
                {
                    float[,] lai_floor = new float[DivedG, DivedG]; //1年分の草本LAIデータ読み込み用のリスト

                    //InputFileType.Output_VegStructureの場合は草本LAIデータを読み込む
                    switch (currentFileType)
                    {
                        case InputFileType.Output_VegStructure:

                            for (int j = 0; j < DivedG; j++)
                            {
                                string lineG = file.ReadLine();     //1行読込
                                string[] dataG = lineG.Split(',');  //コンマでの分割

                                for (int i = 0; i < DivedG; i++)
                                {
                                    float laiG = float.Parse(dataG[i], Inv);
                                    lai_floor[DivedG - 1 - i, j] = laiG; //X軸位置座標を逆順して（これを行わないと表示がずれる）データを格納
                                }
                            }

                            //草本LAIデータを格納するGrassDataクラスのgrassDataインスタンスを定義
                            GrassData grassData = new GrassData(yearRead, lai_floor);

                            // grassDataをリストに追加
                            grassDataList.Add(grassData);

                            break;
                    }

                    //木本動態データの読込
                    //この年の生存木数を読む
                    string line1 = file.ReadLine();
                    string[] data1 = line1.Split(','); //1行読込とコンマでの分割

                    int treeNo = int.Parse(data1[0], Inv); //この年の生存木数
                    treeNo_list.Add(treeNo); //この年の生存木数を記録

                    //この年の木本インベントリーデータを読んで整理する
                    if (treeNo != 0)
                    {
                        for (int no = 0; no < treeNo; no++)
                        {
                            //1行読込とコンマでの分割
                            string line2 = file.ReadLine();
                            string[] data2 = line2.Split(',');

                            TreeData treeData = new TreeData
                            {
                                //とりあえず一時変数に読込
                                Year = yearRead,
                                BoleX = float.Parse(data2[0], Inv),  //bole location x (m)
                                BoleY = float.Parse(data2[1], Inv),  //bole location y (m)
                                CrownX = float.Parse(data2[2], Inv), //crown location x (m)
                                CrownY = float.Parse(data2[3], Inv), //crown location y (m)
                                BoleH = float.Parse(data2[4], Inv),  //bole height (m)
                                CrownH = float.Parse(data2[5], Inv), //crown foliage height (m)
                                BoleD = float.Parse(data2[6], Inv),  //DBH (m)
                                CrownD = float.Parse(data2[7], Inv), //crown radias (m)
                                PFT = int.Parse(data2[8], Inv),      //PFT (m)
                                Height = float.Parse(data2[4], Inv) + float.Parse(data2[5], Inv) //tree height (m)
                            };

                            treeDataList.Add(treeData);
                        }
                    }
                    yearRead++;

                } //ファイル読込の終了
                SimYearMax = yearRead;
            }

        }

        //草本層のLAIが取り出せるかの動作チェック
        //(currentFileType=InputFileType.Output_VegStructureでないと、エラーになるのに注意)
        //float[,] retrievedLAI_floor = grassData.GetLAI_floor_byYear(SimYearMax-1);
        //Debug.Log("GrassLai: " + retrievedLAI_floor[1,1]);


        //■■■■■■ 入力データから得られた仮想林分の一辺の大きさを描画スケーラーに反映 ■■■■■■
        switch (currentFileType)
        {
            //ファイルタイプがOutput_forestの場合のみ実行
            case InputFileType.Output_forest:

                float boleX_max = treeDataList.Max(tree => tree.BoleX);
                float boleY_max = treeDataList.Max(tree => tree.BoleY);
                GroundMax = Math.Max(boleX_max, boleY_max);
                double divided = GroundMax / 10.0;      // 10の位で丸めるために10で割る
                double rounded = Math.Round(divided);   // Math.Roundを使用して丸める
                GroundMax = (float)(rounded * 10.0);    // 結果を10倍して元のスケールに戻す
                break;
        }
        Scale_All = 50.0f / GroundMax;


        //■■■■■■ PFTごとに合計バイオマス密度を集計する ■■■■■■
        if (currentFileType != InputFileType.Output_forest)
        {
            for (int yr = 0; yr < SimYearMax; yr++)
            {
                for (int mo = 0; mo < 12; mo++)
                {
                    for (int p = 0; p < PFT_no_Max; p++)
                    {
                        poolC_whole[yr][p][mo] =
                             poolC_leaf[yr][p][mo] +
                             poolC_trunk[yr][p][mo] +
                             poolC_root[yr][p][mo] +
                             poolC_stock[yr][p][mo] +
                             poolC_available[yr][p][mo];
                    }
                }
            }
        }

        ////PFTごとの値が取り出せるかの動作チェック
        ////(currentFileType=InputFileType.output_for_viewer2でないと、エラーになるのに注意)
        //Debug.Log("PFT10 biomass at July of 30yrs  (Mg C / ha): " + poolC_whole[29][9][6]);
        //Debug.Log("PFT10 LAI at doy201 of 30yrs  (m2/m2): " + lai_PFT[29][9][200]);
        //Debug.Log("PFT10 LAI at July of 30yrs  (m2/m2): " + lai_PFT_m[29][9][6]);
        //Debug.Log("Exisatence frag for PFT1 and PFT10: " + PFT_available[0] + PFT_available[9]);


        //■■■■■■ 放射関連変数の年平均値を作成する ■■■■■■
        if (currentFileType == InputFileType.Output_for_viewer2)
        {
            //日長 (hour)の計算部分
            List<float> DayTime = new List<float>();

            for (int i = 0; i < 365; i++)
            {
                //sl_dec: solar declination (degree)
                double sl_dec = 23.45f * Math.Sin(2 * Math.PI * (i - 80) / 365);

                //sl_hgt: solar hight at midday (degree)
                double x = Math.Sin(LAT * Math.PI / 180) * Math.Sin(sl_dec * Math.PI / 180)
                         + Math.Cos(LAT * Math.PI / 180) * Math.Cos(sl_dec * Math.PI / 180);
                x = Math.Min(1.0, Math.Max(-1.0, x));
                double sl_hgt = Math.Asin(x) * 180.0 / Math.PI;

                //dlen: day length (hour)
                double dlen = 0.0;
                if (sl_hgt > 0.1f)
                {
                    double y = -Math.Tan(LAT * Math.PI / 180) * Math.Tan(sl_dec * Math.PI / 180);
                    double ha = (180 / Math.PI) * Math.Acos(Math.Min(1.0, Math.Max(-1.0, y))); //angle from sun-rise to median passage
                    dlen = 2.0 * (ha / 15.0);
                }
                DayTime.Add((float)dlen);

            }
            //Debug.Log("冬至の日長" + DayTime[354] + "時間");
            //Debug.Log("春分の日長" + DayTime[78]  + "時間");
            //Debug.Log("夏至の日長" + DayTime[171] + "時間");
            //Debug.Log("秋分の日長" + DayTime[265] + "時間");

            rad_short_direct_amean.Clear();
            rad_short_diffuse_amean.Clear();
            rad_short_up_amean.Clear();
            rad_long_down_amean.Clear();
            rad_long_up_amean.Clear();

            for (int yr = 0; yr < SimYearMax; yr++)
            {
                float x1 = 0;
                float x2 = 0;
                float x3 = 0;
                float x4 = 0;
                float x5 = 0;
                for (int doy = 0; doy < 365; doy++)
                {
                    x1 = x1 + rad_short_direct[yr][doy] * 0.5f * DayTime[doy] / 24;
                    x2 = x2 + rad_short_diffuse[yr][doy] * 0.5f * DayTime[doy] / 24;
                    x3 = x3 + rad_short_up[yr][doy] * 0.5f * DayTime[doy] / 24;
                    x4 = x4 + rad_long_down[yr][doy];
                    x5 = x5 + rad_long_down[yr][doy] + rad_long_up[yr][doy];
                }
                rad_short_direct_amean.Add(x1 / 365);
                rad_short_diffuse_amean.Add(x2 / 365);
                rad_short_up_amean.Add(x3 / 365);
                rad_long_down_amean.Add(x4 / 365);
                rad_long_up_amean.Add(x5 / 365);
            }
            //Debug.Log("最終年の下向き短波放射、直達光 (年平均, W/m2) =" + rad_short_direct_amean [SimYearMax-1] );
            //Debug.Log("最終年の下向き短波放射、拡散光 (年平均, W/m2) =" + rad_short_diffuse_amean[SimYearMax - 1]);
            //Debug.Log("最終年の上向き短波放射 　　　　(年平均, W/m2) =" + rad_short_up_amean     [SimYearMax - 1]);
            //Debug.Log("最終年の下向き長波放射         (年平均, W/m2) =" + rad_long_down_amean    [SimYearMax - 1]);
            //Debug.Log("最終年の上向き長波放射         (年平均, W/m2) =" + rad_long_up_amean      [SimYearMax - 1]);
        }


        //■■■■■■ その他、データファイル読み込み後処理 ■■■■■■
        //年合計値の計算
        //fluxW_pre_y = Enumerable.Repeat(0.0f, SimYearMax).ToList(); //初期化
        for (int yr = 0; yr < SimYearMax; yr++)
        {
            fluxC_gpp_y[yr] = fluxC_gpp[yr].Sum();
            fluxC_atr_y[yr] = fluxC_atr[yr].Sum();
            fluxC_htr_y[yr] = fluxC_htr[yr].Sum();
            fluxC_lit_y[yr] = fluxC_lit[yr].Sum();
            fluxC_som_y[yr] = fluxC_som[yr].Sum();
            fluxC_fir_y[yr] = fluxC_fir[yr].Sum();
            fluxW_pre_y[yr] = fluxW_pre[yr].Sum();
            fluxW_ro1_y[yr] = fluxW_ro1[yr].Sum();
            fluxW_ro2_y[yr] = fluxW_ro2[yr].Sum();
            fluxW_ic_y[yr] = fluxW_ic[yr].Sum();
            fluxW_ev_y[yr] = fluxW_ev[yr].Sum();
            fluxW_tr_y[yr] = fluxW_tr[yr].Sum();
            fluxW_sl_y[yr] = fluxW_sl[yr].Sum();
            fluxW_tw_y[yr] = fluxW_tw[yr].Sum();
            fluxW_sn_y[yr] = fluxW_sn[yr].Sum();
        }

        //木本のY軸位置座標を逆順にする（これを行わないと表示がずれる）
        foreach (var tree in treeDataList)
        {
            tree.BoleY = GroundMax - tree.BoleY;
            tree.CrownY = GroundMax - tree.CrownY;
        }

        //木本PFTオンオフスイッチ
        for (int p = 1; p < PFT_no_Max_Woody; p++) { PFT_OnOff_woody[p] = false; } //初期化

        foreach (var treeData in treeDataList)
        {
            PFT_OnOff_woody[treeData.PFT] = true;
        }

        // treeDataList を BoleX と BoleY でグループ化する
        treeDataGroups = treeDataList
            .GroupBy(t => (t.BoleX, t.BoleY))
            .ToDictionary(g => g.Key, g => g.ToList());


        // 年→木リストのインデックス化
        treesByYear = treeDataList.GroupBy(t => t.Year).ToDictionary(g => g.Key, g => g.ToList());
        Debug.Log("Scale: " + Scale_All);

        //textComponent.text = "Length of one side: " + GroundMax.ToString() + " m";

        forestSizeString = GroundMax.ToString();
        localString.Arguments[0] = forestSizeString;
        localString.RefreshString();

        sliderYear.Init();
        sliderTimeRange.Init();

        planeInstance.SetActive(true);

        // 全ての木をインスタンス化して準備する。
        PrepareTrees();

        SpawnTrees2(0);

        // ピクセルデータを読み込む
        LoadPixelDatas();

        // 入力ファイルタイプに応じて表示するボタンを変える
        if (currentFileType == InputFileType.Output_forest)
        {
            grassButton.SetActive(false);
        }
        else
        {
            grassButton.SetActive(true);
        }

        if (currentFileType == InputFileType.Output_for_viewer2)
        {
            CalculateMaxValues();

            UpdateFluxValues();

            carbonPool.Setup();
            carbonFlux.Setup();
            carbonReloc.Setup();
            waterFlux.Setup();
            snowFlux.Setup();
            snowPool.Setup();
            //waterPool.Setup();
            radShort.Setup();
            radLong.Setup();
            albedo.Setup();
            airTemperature.Setup();
            relativeHumid.Setup();
            windVelocity.Setup();
            biomass.Setup();
            lai.Setup();
            co2.Setup();
            soilHeatmap.Setup();

            globeViewer.Setup();
            biomePlotter.Redraw();

            uiManager.UpdateInfoValues();

            uiManager.ShowArrowButtons(true);

            /*
            uiManager.ShowMaterialCycleButton(true);
            uiManager.ShowCarbonCycleButton(true);
            uiManager.ShowWaterCycleButton(true);
            uiManager.ShowRadiationBalanceButton(true);
            */
        }
        else
        {
            uiManager.ShowArrowButtons(false);

            /*
            uiManager.ShowMaterialCycleButton(false);
            uiManager.ShowCarbonCycleButton(false);
            uiManager.ShowWaterCycleButton(false);
            uiManager.ShowRadiationBalanceButton(false);
            */
        }

        //uiManager.ShowLegend();

        uiManager.ShowSimulationInfo();

        uiManager.Button_Pause_OnClick();

        StartCoroutine(UpdateViewMode()); //★★★★
    }

    IEnumerator UpdateViewMode()
    {
        yield return new WaitForSeconds(0.5f);
        uiManager.SetupTreeItems();
    }

    //■■■■■■ Start ■■■■■■
    void Start()
    {
        //入力データから得られた最大シミュレーション年数を、得られたスライダーバーの最大値に反映させる
        //1行目：Findを使って"SliderYear"という名を持つGameObjectを探す
        //2行目：GameObjectのSliderYearがアタッチするスクリプト"SliderYear"(たまたま同じ名前)のPublicメソッドInit()を実行
        //GameObject sliderYearObj = GameObject.Find("SliderYear");
        //sliderYear = sliderYearObj.GetComponent<SliderYear>();

        //森林サイズを表示
        //GameObject Text_Forest_Size = GameObject.Find("Text_Forest_Size"); //現在年を表示するラベルを探索して取得する
        //textComponent = Text_Forest_Size.GetComponent<Text>();

        //地面の描画
        planeInstance = Instantiate(planePrefab);
        planeInstance.SetActive(false);

        //音声Componentを取得
        audioSource = GetComponent<AudioSource>();

        //★★★★
        //ReadFile();


        //★★★★
        //uiManager.OnClick_Button_ViewChange();


    } //Start()の終わり

    //■■■■■■ 各種サブルーチン ■■■■■■
    void CalculateMaxValues()
    {
        double ValueMax = 0.0;

        for (int yr = 0; yr < SimYearMax; yr++)
        {
            ValueMax = Math.Max(ValueMax, fluxC_gpp[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxC_atr[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxC_htr[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxC_lit[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxC_som[yr].Sum());
        }

        CarbonScaleMax = (int)Math.Ceiling(ValueMax / 6.0) * 6;

        //-------------------

        ValueMax = 0.0;

        for (int yr = 0; yr < SimYearMax; yr++)
        {
            ValueMax = Math.Max(ValueMax, fluxW_pre[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_ro1[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_ro2[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_ic[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_ev[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_tr[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_sl[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_tw[yr].Sum());
            ValueMax = Math.Max(ValueMax, fluxW_sn[yr].Sum());
        }

        WaterScaleMax = (int)Math.Ceiling(ValueMax / 60.0) * 60;

        //-------------------

        ValueMax = 0.0;

        for (int yr = 0; yr < SimYearMax; yr++)
        {
            ValueMax = Math.Max(ValueMax, rad_short_direct_amean[yr]);
            ValueMax = Math.Max(ValueMax, rad_short_diffuse_amean[yr]);
            ValueMax = Math.Max(ValueMax, rad_short_up_amean[yr]);
            ValueMax = Math.Max(ValueMax, rad_long_down_amean[yr]);
            ValueMax = Math.Max(ValueMax, rad_long_up_amean[yr]);
        }

        RadiationScaleMax = (int)Math.Ceiling(ValueMax / 50.0) * 50;

        Debug.Log("CalculateMaxValues: " + CarbonScaleMax + " " + WaterScaleMax + " " + RadiationScaleMax);
    }

    void UpdateFluxValues()
    {
        uiManager.UpdateValues(
            fluxC_gpp_y[SimYear - 1],
            fluxC_atr_y[SimYear - 1],
            fluxC_htr_y[SimYear - 1],
            fluxC_lit_y[SimYear - 1],
            fluxC_som_y[SimYear - 1],
            fluxW_pre_y[SimYear - 1],
            fluxW_ro1_y[SimYear - 1],
            fluxW_ro2_y[SimYear - 1],
            fluxW_ic_y[SimYear - 1],
            fluxW_ev_y[SimYear - 1],
            fluxW_tr_y[SimYear - 1],
            fluxW_sl_y[SimYear - 1],
            fluxW_tw_y[SimYear - 1],
            fluxW_sn_y[SimYear - 1],
            rad_short_direct_amean[SimYear - 1],
            rad_short_diffuse_amean[SimYear - 1],
            rad_short_up_amean[SimYear - 1],
            rad_long_down_amean[SimYear - 1],
            rad_long_up_amean[SimYear - 1]
        );
    }

    public void UpdateGraphsDelayed(float delaySeconds)
    {
        StartCoroutine(UpdateGraphsCoroutine(delaySeconds));
    }

    IEnumerator UpdateGraphsCoroutine(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        UpdateGraphs();
    }

    public void UpdateGraphs()
    {
        //Debug.Log("Director UpdateGraphs");

        carbonPool.UpdateValues();
        carbonFlux.UpdateValues();
        carbonReloc.UpdateValues();
        waterFlux.UpdateValues();
        snowFlux.UpdateValues();
        snowPool.UpdateValues();
        //waterPool.UpdateValues();
        radShort.UpdateValues();
        radLong.UpdateValues();
        albedo.UpdateValues();
        airTemperature.UpdateValues();
        relativeHumid.UpdateValues();
        windVelocity.UpdateValues();
        biomass.UpdateValues();
        lai.UpdateValues();
        co2.UpdateValues();
        soilHeatmap.UpdateValues();

    }

    public void QuitApp()
    {
        Debug.Log("QuitApp");

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            UnityEngine.Application.Quit();
        #endif
    }

    public void OnSimulationCompleted(string filename)
    {
        Debug.Log("OnSimulationCompleted: " + filename);

        uiManager.SEIBConnector.SetActive(false);

        OnReadFile(filename);
    }

    //■■■■■■ Update ■■■■■■
    // Update is called once per frame
    void Update()
    {
        //■■■■■■ キー入力後の応答
        if (uiManager.confirmOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                uiManager.OnConfirmYes();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                uiManager.OnConfirmNo();
            }
        }
        else if (appState == AppState.TopMenu)
        {
            //ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                uiManager.ShowConfirm("Quit the SEIB-Explorer?");
            }
            //A
            else if (Input.GetKeyDown(KeyCode.A))
            {
                uiManager.Button_A_OnClick();
            }
            //B
            else if (Input.GetKeyDown(KeyCode.B)) 
            { 
                uiManager.Button_B_OnClick(); 
            }
        }
        else if (appState == AppState.FunctionB)
        {
            //ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                uiManager.ShowConfirm("Return to the Top?");
            }
        }
        else if (appState == AppState.FunctionA)
        {
            //ESC
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Debug.Log("FunctionA Escape");

                uiManager.ShowConfirm("Return to the Top?");
            }

            //A: 言語切り替え
            if (Input.GetKeyDown(KeyCode.A)) { uiManager.OnClick_Button_Setting(); }
            //B: 表示対象変更
            if (Input.GetKeyDown(KeyCode.B)) { uiManager.OnClick_Button_ViewChange(); }
            //C: 表示対象の切り替え
            if (Input.GetKeyDown(KeyCode.C)) { ChangeGrassViewMode(); }

            //<: 情報パネルを左側に切り替え
            if (Input.GetKeyDown(KeyCode.Comma)) { uiManager.OnClick_Button_LeftArrow(); }

            //>: 情報パネルを右側に切り替え
            if (Input.GetKeyDown(KeyCode.Period)) { uiManager.OnClick_Button_RightArrow(); }

            //F1-6: 表示パネル切り替え
            if (Input.GetKeyDown(KeyCode.F1)) { uiManager.OnClick_Button(0); }
            if (Input.GetKeyDown(KeyCode.F2)) { uiManager.OnClick_Button(1); }
            if (Input.GetKeyDown(KeyCode.F3)) { uiManager.OnClick_Button(2); }
            if (Input.GetKeyDown(KeyCode.F4)) { uiManager.OnClick_Button(3); }
            if (Input.GetKeyDown(KeyCode.F5)) { uiManager.OnClick_Button(4); }
            if (Input.GetKeyDown(KeyCode.F6)) { uiManager.OnClick_Button(5); }
            if (Input.GetKeyDown(KeyCode.F7)) { uiManager.OnClick_Button(6); }
            if (Input.GetKeyDown(KeyCode.F8)) { uiManager.OnClick_Button(7); }

            //Space: PlayとPauseの切り替え
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (SimYearMove == true)
                {
                    uiManager.Button_Pause_OnClick();
                    SimYearMove = false;
                }
                else
                {
                    uiManager.Button_Play_OnClick();
                    SimYearMove = true;
                }
            }

            //P, O: 時間軸、進行と後退
            bool simYearChanged = false;
            if (Input.GetKeyDown(KeyCode.P)) { SimYear = Math.Min(SimYear + 1, SimYearMax); simYearChanged = true; }
            if (Input.GetKeyDown(KeyCode.O)) { SimYear = Math.Max(SimYear - 1, 1); simYearChanged = true; }
            if (simYearChanged) { sliderYear.ChangeValue(); }

            //I: 左パネルスケーラー増加
            if (Input.GetKeyDown(KeyCode.I))
            {
                SimTimeRange = Math.Min(SimTimeRange + 1, SimTimeRangeMax);
                sliderTimeRange.ChangeValue();
                audioSource.PlayOneShot(Sound_Von); //効果音
            }
            //U: 左パネルスケーラー減少
            if (Input.GetKeyDown(KeyCode.U))
            {
                SimTimeRange = Math.Max(SimTimeRange - 1, 1);
                sliderTimeRange.ChangeValue();
                audioSource.PlayOneShot(Sound_Fon); //効果音
            }
        }

        //■■■■■■ 時間軸スライダーが動かされた場合の応答
        if (SimYear != SimYear_LastDraw || SimTimeRange != SimTimeRange_LastDraw)
        {
            if (currentFileType == InputFileType.Output_for_viewer2)
            {
                //Debug.Log(SimYear + "年の降水量(mm/年)＝ " + fluxW_pre[SimYear-1].Sum());
                //Debug.Log(SimYear + "年のGPP (MgC/ha/年)＝" + fluxC_gpp[SimYear-1].Sum());
                //Debug.Log(SimYear + "年の植物呼吸 (MgC/ha/年)＝" + fluxC_atr[SimYear-1].Sum());

                //poolW_L1[0] の最初の365個の要素の合計（nは0から364の範囲なので 365個の要素を取り出して合計している）
                //float sum = poolW_L1[0].Take(365).Sum();
                //Debug.Log(SimYear + "年_土壌第一層の含水率の年平均値: " + sum / 365f);

                UpdateFluxValues();

                UpdateGraphs();
            }

            SimTimeRange_LastDraw = SimTimeRange;

            if (SimYear == SimYear_LastDraw) return;

            //効果音鳴らす
            audioSource.Play();   //ka
            //int i = SimYear % 18 + 1;
            //switch (i)
            //{
            //    case 1: audioSource.PlayOneShot(sound1); break;
            //    case 2: audioSource.PlayOneShot(sound1); break;
            //    case 3: audioSource.PlayOneShot(sound1); break;
            //    case 4: audioSource.PlayOneShot(sound4); break;
            //    case 5: audioSource.PlayOneShot(sound1); break;
            //    case 6: audioSource.PlayOneShot(sound4); break;
            //    case 7: audioSource.PlayOneShot(sound1); break;
            //    case 8: audioSource.PlayOneShot(sound4); break;
            //    case 9: audioSource.PlayOneShot(sound4); break;
            //    case 10: audioSource.PlayOneShot(sound4); break;
            //    case 11: audioSource.PlayOneShot(sound5); break;
            //    case 12: audioSource.PlayOneShot(sound6); break;
            //    case 13: audioSource.PlayOneShot(sound4); break;
            //    case 14: audioSource.PlayOneShot(sound5); break;
            //    case 15: audioSource.PlayOneShot(sound6); break;
            //    case 16: audioSource.PlayOneShot(sound3); break;
            //    case 17: audioSource.PlayOneShot(sound5); break;
            //    case 18: audioSource.PlayOneShot(sound4); break;
            //}

            CleanTrees();

            if (currentViewMode == ViewModes.Realistic)
            {
                SpawnTrees(SimYear - 1);
            }
            else if (currentViewMode == ViewModes.Simplified)
            {
                SpawnTrees2(SimYear - 1);
            }

            if (isGrassViewOn())
            {
                UpdateGrassLayer(SimYear - 1);
            }

            SimYear_LastDraw = SimYear;
        }
    }


    //■■■■■■ データ読み込み後の各種挙動 ■■■■■■

    // ピクセルデータを読み込む
    void LoadPixelDatas()
    {
        texturePixelData = new Color[GrassTextures.Length][];
        texturePixelData2 = new Color[GrassTextures2.Length][];

        // 各テクスチャからピクセルデータを読み取り、配列に保存
        for (int i = 0; i < GrassTextures.Length; i++)
        {
            Texture2D texture = GrassTextures[i];
            if (texture != null)
            {
                // テクスチャが読み取り可能か確認
                if (texture.isReadable)
                {
                    texturePixelData[i] = texture.GetPixels();
                }
                else
                {
                    Debug.LogWarning($"テクスチャ {texture.name} は読み取り不可です。インポート設定で読み取り可能に設定してください。");
                }
            }
            else
            {
                Debug.LogWarning($"インデックス {i} のテクスチャはnullです。");
            }
        }

        for (int i = 0; i < GrassTextures2.Length; i++)
        {
            Texture2D texture = GrassTextures2[i];
            if (texture != null)
            {
                // テクスチャが読み取り可能か確認
                if (texture.isReadable)
                {
                    texturePixelData2[i] = texture.GetPixels();
                }
                else
                {
                    Debug.LogWarning($"テクスチャ {texture.name} は読み取り不可です。インポート設定で読み取り可能に設定してください。");
                }
            }
            else
            {
                Debug.LogWarning($"インデックス {i} のテクスチャはnullです。");
            }
        }
    }

    void AddTree(int idx, TreeData tree)
    {
        // リアルなツリーをインスタンス化して非アクティブに設定
        tree_3d[idx] = Instantiate(WoodyPFTs[tree.PFT], parent, true) as GameObject;

        // 木にランダムな回転を設定する
        if (applyRandomRotation)
        {
            tree_3d[idx].transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);
        }

        tree_3d[idx].SetActive(false);

        // 簡易ツリーをインスタンス化して非アクティブに設定
        tree_3d_2[idx] = Instantiate(WoodyPFTs2[tree.PFT], parent2, true) as GameObject;
        tree_3d_2[idx].SetActive(false);

        // TreeModelコンポーネントを取得して、initialTreeDataプロパティにクローンしたデータを設定
        TreeModel _treeModel = tree_3d_2[idx].transform.GetComponent<TreeModel>();
        if (_treeModel)
        {
            _treeModel.initialTreeData = tree.Clone();

            treeModels[idx] = _treeModel;
        }
    }

    void PrepareTrees()
    {

        treeIndex.Clear();
        int idx = 0;
        foreach (var kvp in treeDataGroups) // treeDataGroups内の各キーと値のペアを反復処理
        {
            var key = kvp.Key; // 現在のキーを取得
            var treeList = kvp.Value; // 現在の値（ツリーのリスト）を取得

            var tree = treeList[0]; // リストの最初のツリーを取得

            AddTree(idx, tree);
            idx++;
            if (!treeIndex.ContainsKey((tree.BoleX, tree.BoleY, tree.PFT)))
            {
                treeIndex[(tree.BoleX, tree.BoleY, tree.PFT)] = idx - 1;
            }

            // その位置に他の種類の木があるかどうかを確認する
            int lastPFT = tree.PFT;
            foreach (var tree2 in treeList)
            {
                if (tree2.PFT != lastPFT)
                {
                    AddTree(idx, tree2);
                    idx++;
                    if (!treeIndex.ContainsKey((tree2.BoleX, tree2.BoleY, tree2.PFT)))
                    {
                        treeIndex[(tree2.BoleX, tree2.BoleY, tree2.PFT)] = idx - 1;
                    }

                    lastPFT = tree2.PFT;
                }
            }
        }

        treeCount = idx;

        Debug.Log("PrepareTrees treeCount: " + treeCount);
    }

    void UpdateText(string value)
    {
        // テキストコンポーネントのテキストを更新
        textComponent.text = value;
    }

    void ClearTimeSeries()
    {
        // Monthly carbon
        fluxC_gpp.Clear(); fluxC_atr.Clear(); fluxC_htr.Clear(); fluxC_lit.Clear(); fluxC_som.Clear(); fluxC_fir.Clear();

        // Monthly carbon pools
        poolC_Litter.Clear(); poolC_SOM1.Clear(); poolC_SOM2.Clear(); poolC_Woody.Clear(); poolC_Grass.Clear();

        // Monthly water
        fluxW_pre.Clear(); fluxW_ro1.Clear(); fluxW_ro2.Clear(); fluxW_ic.Clear(); fluxW_ev.Clear();
        fluxW_tr.Clear(); fluxW_sl.Clear(); fluxW_tw.Clear(); fluxW_sn.Clear();

        // Daily
        poolW_L1.Clear(); poolW_L2.Clear(); poolW_L3.Clear(); poolW_snow.Clear();
        tmp_air.Clear(); tmp_soil1.Clear(); tmp_soil2.Clear(); tmp_soil3.Clear(); rh.Clear(); wind.Clear();
        par_direct.Clear(); par_diffuse.Clear();
        rad_short_direct.Clear(); rad_short_diffuse.Clear(); rad_short_up.Clear(); rad_long_down.Clear(); rad_long_up.Clear();
        albedo_mean.Clear(); albedo_veg.Clear(); albedo_soil.Clear();

        // Daily - Monthly
        poolW_L1_m.Clear(); poolW_L2_m.Clear(); poolW_L3_m.Clear(); poolW_snow_m.Clear();
        tmp_air_m.Clear(); tmp_soil1_m.Clear(); tmp_soil2_m.Clear(); tmp_soil3_m.Clear(); rh_m.Clear(); wind_m.Clear();
        par_direct_m.Clear(); par_diffuse_m.Clear();
        rad_short_direct_m.Clear(); rad_short_diffuse_m.Clear(); rad_short_up_m.Clear(); rad_long_down_m.Clear(); rad_long_up_m.Clear();
        albedo_mean_m.Clear(); albedo_veg_m.Clear(); albedo_soil_m.Clear();

        // PFT 3D
        lai_PFT.Clear(); lai_PFT_m.Clear();
        poolC_leaf.Clear(); poolC_trunk.Clear(); poolC_root.Clear(); poolC_stock.Clear(); poolC_available.Clear(); poolC_whole.Clear();

        // Yearly scalars
        aCO2.Clear(); tmp_air_y.Clear();
        fluxC_gpp_y.Clear(); fluxC_atr_y.Clear(); fluxC_htr_y.Clear(); fluxC_lit_y.Clear(); fluxC_som_y.Clear(); fluxC_fir_y.Clear();
        fluxW_pre_y.Clear(); fluxW_ro1_y.Clear(); fluxW_ro2_y.Clear(); fluxW_ic_y.Clear(); fluxW_ev_y.Clear(); fluxW_tr_y.Clear();
        fluxW_sl_y.Clear(); fluxW_tw_y.Clear(); fluxW_sn_y.Clear();

        // Radiative yearly
        rad_short_direct_amean.Clear(); rad_short_diffuse_amean.Clear();
        rad_short_up_amean.Clear(); rad_long_down_amean.Clear(); rad_long_up_amean.Clear();
    }

    public void ResetScene()
    {
        // Clear previous data
        Array.Clear(PFT_available, 0, PFT_available.Length);
        Array.Clear(PFT_OnOff_woody, 0, PFT_OnOff_woody.Length);
        ClearTimeSeries();

        treeDataGroups = new Dictionary<(float, float), List<TreeData>>();
        treesByYear = new Dictionary<int, List<TreeData>>();

        treeIndex.Clear();

        // 草テクスチャキャッシュを破棄
        foreach (var kv in grassTextureCache)
        {
            if (kv.Value != null) { Destroy(kv.Value); }
        }
        grassTextureCache.Clear();
        // 木データのリストを初期化
        treeDataList = new List<TreeData>();

        // 草データリストをリセット
        grassDataList = new List<GrassData>();

        // シミュレーションの最大年と現在の年をリセット
        SimYearMax = 1;
        SimYear = 1;
        SimYearMove = false;
        SimYear_LastDraw = 1;

        // 草の表示モードを「木本のみ」に設定し、モードを適用
        currentGrassViewMode = GrassViewModes.TreeOnly;
        SetGrassViewMode(currentGrassViewMode);

        // 表示モードを簡略化モードにリセットし、設定
        currentViewMode = ViewModes.Simplified;
        SetViewMode(currentViewMode);

        // 全ての木を破棄
        DestroyTrees();

        // デフォルトのテクスチャを設定
        SetDefaultTexture();
    }

    void SetDefaultTexture()
    {
        planeInstance.GetComponent<Renderer>().material.mainTexture = groundTexture;
        isGeneratedTexture = false;
    }

    void CleanTrees()
    {
        // 3D木オブジェクトの非表示処理
        for (int i = 0; i < treeCount; i++)
        {
            if (tree_3d[i] != null)
            {
                tree_3d[i].SetActive(false);
            }
        }

        // 3D木オブジェクト2の非表示処理
        for (int i = 0; i < treeCount; i++)
        {
            if (tree_3d_2[i] != null)
            {
                tree_3d_2[i].SetActive(false);
            }
        }
    }

    void DestroyTrees()
    {
        // 3D木オブジェクトの破棄処理
        for (int i = 0; i < treeCount; i++)
        {
            if (tree_3d[i] != null)
            {
                Destroy(tree_3d[i]);
                tree_3d[i] = null;
            }
        }

        // 3D木オブジェクト2の破棄処理
        for (int i = 0; i < treeCount; i++)
        {
            if (tree_3d_2[i] != null)
            {
                Destroy(tree_3d_2[i]);
                tree_3d_2[i] = null;
            }
        }
    }

    void SpawnTrees(int year)
    {
        // 指定した年の木のみをアクティブ化
        if (!treesByYear.TryGetValue(year, out var list)) return;

        foreach (var tree in list)
        {
            int idx = GetTreeIndex(tree);
            if (idx == -1) continue;

            tree_3d[idx].SetActive(true);

            float scaleHeight = GetScaleHeight(tree.PFT);
            float scaleCrown = GetScaleCrown(tree.PFT);

            // 木の位置とスケールを設定
            tree_3d[idx].transform.position = new Vector3(tree.BoleX * Scale_All, 0f, tree.BoleY * Scale_All);
            tree_3d[idx].transform.localScale = new Vector3(tree.CrownD * scaleCrown, tree.Height * scaleHeight, tree.CrownD * scaleCrown);
        }
    }

    void SpawnTrees2(int year)
    {
        // 指定した年の木のみをアクティブ化（簡易表示）
        if (!treesByYear.TryGetValue(year, out var list)) return;

        foreach (var tree in list)
        {
            int idx = GetTreeIndex(tree);
            if (idx == -1) continue;

            tree_3d_2[idx].SetActive(true);

            float scaleHeight = GetScaleHeight(tree.PFT);
            float scaleCrown = GetScaleCrown(tree.PFT);

            // 木の位置を設定
            tree_3d_2[idx].transform.position = new Vector3(tree.BoleX * Scale_All, 0f, tree.BoleY * Scale_All);

            TreeModel treeModel = tree_3d_2[idx].transform.GetComponent<TreeModel>();
            if (treeModel)
            {
                treeModel.SetTreeData(tree, Scale_All);

                // 初期データを使ってスケールを設定
                TreeData initialTree = treeModel.initialTreeData;
                tree_3d_2[idx].transform.localScale =
                    new Vector3(initialTree.CrownD * scaleCrown, initialTree.Height * scaleHeight, initialTree.CrownD * scaleCrown);
            }
        }
    }

    public int GetTreeIndex(TreeData treeData)
    {
        if (treeIndex.TryGetValue((treeData.BoleX, treeData.BoleY, treeData.PFT), out var idxFast))
        {
            return idxFast;
        }

        // フォールバック（安全のため。万一辞書にない場合は従来の線形探索）
        for (int i = 0; i < treeCount; i++)
        {
            if (treeModels[i] != null)
            {
                TreeModel treeModel = treeModels[i];
                if (treeModel &&
                    treeModel.initialTreeData.BoleX == treeData.BoleX &&
                    treeModel.initialTreeData.BoleY == treeData.BoleY &&
                    treeModel.initialTreeData.PFT == treeData.PFT)
                {
                    // 見つかったら辞書にも登録して次回から高速化
                    treeIndex[(treeData.BoleX, treeData.BoleY, treeData.PFT)] = i;
                    return i;
                }
            }
        }
        return -1;
    }

    private float GetScaleHeight(int pft)
    {
        // TreeGizmosから木の高さを取得し、スケールを計算
        TreeGizmos treeGizmos = WoodyPFTs[pft].GetComponent<TreeGizmos>();
        float treeHeight = treeGizmos.treeHeight;

        float scaleHeight = 1 / treeHeight;

        return scaleHeight * Scale_All;
    }

    private float GetScaleCrown(int pft)
    {
        // TreeGizmosから樹冠の半径を取得し、スケールを計算
        TreeGizmos treeGizmos = WoodyPFTs[pft].GetComponent<TreeGizmos>();
        float crownRadius = treeGizmos.crownRadius;

        float scaleCrown = 1 / crownRadius;

        return scaleCrown * Scale_All;
    }

    private void OnEnable()
    {
        // ローカライズされた文字列が変更されたときにUpdateTextメソッドを呼び出す
        localString.Arguments = new string[] { forestSizeString };
        localString.StringChanged += UpdateText;
    }

    private void OnDisable()
    {
        // ローカライズされた文字列の変更イベントからUpdateTextメソッドを解除
        localString.StringChanged -= UpdateText;
    }

    private void OnDestroy()
    {
        // 木を破棄
        DestroyTrees();
    }

    // 草の表示モードを変更するメソッド
    public void ChangeGrassViewMode()
    {
        // 草の表示モードを変更
        if (currentGrassViewMode == GrassViewModes.TreeOnly)
        {
            currentGrassViewMode = GrassViewModes.TreeAndGrass;
        }
        else if (currentGrassViewMode == GrassViewModes.TreeAndGrass)
        {
            currentGrassViewMode = GrassViewModes.GrassOnly;
        }
        else if (currentGrassViewMode == GrassViewModes.GrassOnly)
        {
            currentGrassViewMode = GrassViewModes.TreeOnly;
        }

        // モード変更後、設定を適用
        SetGrassViewMode(currentGrassViewMode);
    }

    // 草の表示モードを設定するメソッド
    public void SetGrassViewMode(GrassViewModes grassViewModes)
    {
        bool treeOn = false;
        bool grassOn = false;

        // 草の表示モードに応じて、木と草の表示を切り替える
        if (currentGrassViewMode == GrassViewModes.TreeOnly)
        {
            treeOn = true;
            grassOn = false;
        }
        else if (currentGrassViewMode == GrassViewModes.TreeAndGrass)
        {
            treeOn = true;
            grassOn = true;
        }
        else if (currentGrassViewMode == GrassViewModes.GrassOnly)
        {
            treeOn = false;
            grassOn = true;
        }

        // 木の表示設定
        if (treeOn)
        {
            SetViewMode(currentViewMode);
        }
        else
        {
            parent.gameObject.SetActive(false);
            parent2.gameObject.SetActive(false);
        }

        // 草の表示設定
        if (grassOn)
        {
            if (currentViewMode == ViewModes.Realistic)
            {
                realisticScale.SetActive(true);
                simplifiedScale.SetActive(false);
            }
            else if (currentViewMode == ViewModes.Simplified)
            {
                realisticScale.SetActive(false);
                simplifiedScale.SetActive(true);
            }

            UpdateGrassLayer(SimYear - 1);
        }
        else
        {
            realisticScale.SetActive(false);
            simplifiedScale.SetActive(false);

            SetDefaultTexture();
        }

        audioSource.PlayOneShot(Sound_Poka); //効果音

    }

    // 草の表示がオンかどうかを判定するメソッド
    bool isGrassViewOn()
    {
        return (currentGrassViewMode == GrassViewModes.TreeAndGrass ||
            currentGrassViewMode == GrassViewModes.GrassOnly);
    }

    public void ChangeViewMode()
    {
        // 現在の表示モードを切り替える
        if (currentViewMode == ViewModes.Simplified)
        {
            currentViewMode = ViewModes.Realistic;
        }
        else
        {
            currentViewMode = ViewModes.Simplified;
        }

        SetViewMode(currentViewMode);
    }

    public void SetViewMode(ViewModes newViewMode)
    {
        //Debug.Log("SetViewMode: " + newViewMode + " " + isGrassViewOn() + " " + currentFileType);

        // 新しい表示モードに応じてボタンや親オブジェクトのアクティブ状態を設定
        if (newViewMode == ViewModes.Realistic)
        {
            realisticButton.SetActive(false);
            simplifiedButton.SetActive(true);

            parent.gameObject.SetActive(true);
            parent2.gameObject.SetActive(false);

            // 現在の年の前の年に基づいて木を生成
            SpawnTrees(SimYear - 1);

            if (isGrassViewOn())
            {
                if (currentFileType == InputFileType.Output_VegStructure
                    || currentFileType == InputFileType.Output_for_viewer2)
                {
                    realisticScale.SetActive(true);
                    simplifiedScale.SetActive(false);
                }

                UpdateGrassLayer(SimYear - 1);
            }
        }
        else if (newViewMode == ViewModes.Simplified)
        {
            realisticButton.SetActive(true);
            simplifiedButton.SetActive(false);

            parent.gameObject.SetActive(false);
            parent2.gameObject.SetActive(true);

            // 現在の年の前の年に基づいて木を生成（バージョン2）
            SpawnTrees2(SimYear - 1);

            if (isGrassViewOn())
            {
                if (currentFileType == InputFileType.Output_VegStructure
                    || currentFileType == InputFileType.Output_for_viewer2)
                {
                    realisticScale.SetActive(false);
                    simplifiedScale.SetActive(true);
                }

                UpdateGrassLayer(SimYear - 1);
            }
        }

        if (currentGrassViewMode == GrassViewModes.GrassOnly)
        {
            parent.gameObject.SetActive(false);
            parent2.gameObject.SetActive(false);
        }
    }

    public GameObject GetTreeObject(int idx)
    {
        // 現在の表示モードに応じて適切な木オブジェクトを返す
        if (currentViewMode == ViewModes.Realistic)
        {
            return WoodyPFTs[idx];
        }
        else
        {
            return WoodyPFTs2[idx];
        }
    }

    // 草のレイヤーを更新するメソッド
    void UpdateGrassLayer(int year)
    {

        // --- キャッシュチェック ---
        var key = (year, currentViewMode);
        if (grassTextureCache.TryGetValue(key, out var cachedTex))
        {
            var renderer = planeInstance.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.mainTexture = cachedTex;
            }
            isGeneratedTexture = true;
            return;
        }
        // 年と草データリストの数を確認
        if (year < 0 || grassDataList.Count == 0) return;

        GrassData grassData = grassDataList[year];

        float[,] retrievedLAI_floor = grassData.GetLAI_floor_byYear(year);

        // フロアデータの長さを確認
        if (retrievedLAI_floor.GetLength(0) == 0) return;

        // 画像サイズを設定 (32x32)
        int imageSize = 32;

        Texture2D oldTexture = planeInstance.GetComponent<Renderer>().material.mainTexture as Texture2D;

        Texture2D gridTexture = new Texture2D((int)(DivedG * imageSize), (int)(DivedG * imageSize));

        //グリッドテクスチャの各ピクセルにテクスチャを設定
        for (int y = 0; y < DivedG; y++)
        {
            for (int x = 0; x < DivedG; x++)
            {
                float value = retrievedLAI_floor[x, y];
                int textureIndex = Mathf.Clamp((int)Mathf.Floor(value), 0, 7); //Mathf.Floorで小数点以下を切り捨てて整数化、Clampで値を0から7の範囲内に制限

                // smallImageからグリッドテクスチャの正しい位置にピクセルを設定
                if (currentViewMode == ViewModes.Realistic)
                {
                    gridTexture.SetPixels(x * imageSize, y * imageSize, imageSize, imageSize, texturePixelData[textureIndex]);
                }
                else
                {
                    gridTexture.SetPixels(x * imageSize, y * imageSize, imageSize, imageSize, texturePixelData2[textureIndex]);
                }
            }
        }

        // テクスチャの変更を適用
        gridTexture.Apply();

        // マテリアルにテクスチャを割り当て
        planeInstance.GetComponent<Renderer>().material.mainTexture = gridTexture;


        // キャッシュへ保存（年×表示モード）
        grassTextureCache[(year, currentViewMode)] = gridTexture;
        // 以前のテクスチャが存在する場合は破棄
        // キャッシュ運用のため、ここでは旧テクスチャを破棄しない（ResetSceneで破棄）

        isGeneratedTexture = true;
    }
}