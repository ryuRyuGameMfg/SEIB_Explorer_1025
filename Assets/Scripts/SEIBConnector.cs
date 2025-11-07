//■■■■■　
// SEIB-DGVMのFortran実行ファイルをUnityで制御するシステム
// 
// Unityはフロントエンド（入力・可視化）、Fortranはバックエンド（シミュレーション計算）として役割分担し、ファイルベースで通信する仕組み。
// 以下は、本システムの実行順序
// 1. Unity UI → CSVファイルでシミュレーション条件を保存
// 2. Unity → Fortran実行ファイルを起動
// 3. Fortran → 1年分のシミュレーション完了ごとに小さなテキストファイルを生成
// 4. そのFortran出力ファイルをUnity側が監視、進行状況を更新
// 5. 停止は stop.txt を書き込んで指示

using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using System.Threading;
using System;
using System.Linq;


public class SEIBConnector : MonoBehaviour
{
    //■■■■■　宣言

    //実行制御に関わる設定値 (インスペクタ上で指定可能)
    public int    yearMin; //実行期間の最大値
    public int    yearMax; //実行期間の最小値
    public string SEIBexe; //SEIB本体のFortran実行ファイル名
    public float maxWaitPerYearSec = 300f; // 1年間分のシミュレーションに許容する最大待機時間（秒）

    //フィールド宣言部
    private int   yearLength;    //実行期間 (ユーザー入力の数字で置き換える)
    private float latitudeNum;    //緯度 (ユーザー入力の数字で置き換える)
    private float longitudeNum;    //経度 (ユーザー入力の数字で置き換える)

    //private int maxLines = 36;   //コンソール画面の最大行数、UI部品のコンソール画面サイズ変更時には要調整
    public Slider   progressBar;    //実行の進捗度プログレスバー
    private Process fortranProcess; //Fortran 実行ファイルを起動・制御するための System.Diagnostics.Process オブジェクト
    private Queue<string> logLines = new Queue<string>();   //コンソール画面制御関係

    //Fortran実行ファイルや入出力ファイルの場所を格納するパス
    private string externalRoot;    //Fortran実行ファイルのあるパス
    private string tempIO_Path;    //一時的な入出力に使うパス
    private string inputPath;       //入力データへのパス
    private string outputPath;      //出力データへのパス
    private string controlPath;     //実行制御ファイルへのパス
    private string paramCsvPath;    //入力パラメーターファイル名

    //private string inputPathCO2;        //入力CO2時系列データのフルパス＋ファイル名
    //private string inputPathClimate;    //入力気候時系列データのフルパス＋ファイル名

    //UI入力部品
    //　UIスライダーやドロップダウンを使って、ユーザーがシミュレーション条件を入力できるようにしている
    //　対応する Text フィールドにスライダー値を即座に反映する仕組みも設定される
    public Dropdown dropdownSite;   //シミュレーションを実施するサイト
    public InputField outFN_Result;  //出力ファイル名（拡張子無し）
    public InputField outSimYear;   //シミュレーションを実施する期間

    public InputField outLat;   //緯度
    public InputField outLon;   //経度
    public InputField outFN_CO2;        //入力CO2データの絶対パス＋ファイル名（拡張子含む）
    public InputField outFN_Climate;    //入力気候データの絶対パス＋ファイル名（拡張子含む）

    public Text consoleText;        // ScrollView の Content 内の Text を割り当て
    public ScrollRect scrollRect;   // ScrollView の ScrollRect を割り当て

    public Slider sliderTmp;       //気温の変化量ユーザー入力
    public Text textTmp;           //↑の値を表示するTextフィールド

    public Slider sliderTmpRange;  //気温日変動幅の変化量ユーザー入力
    public Text textTmpRange;      //↑の値を表示するTextフィールド

    public Slider sliderRH;         //相対湿度の変化量ユーザー入力
    public Text textRH;             //↑の値を表示するTextフィールド

    public Slider sliderPrecip;     //降水量の変化量ユーザー入力
    public Text textPrecip;         //↑の値を表示するTextフィールド

    public Slider sliderRad;        //下向き短波放射強度の変化量ユーザー入力
    public Text textRad;            //↑の値を表示するTextフィールド

    public Slider sliderCO2;        //CO2濃度の変化量ユーザー入力
    public Text textCO2;            //↑の値を表示するTextフィールド

    public Button startButton;
    public Button stopButton;

    private string co2PathRaw, co2PathMapped;
    private string climatePathRaw, climatePathMapped;
    private string resultPathRaw, resultPathMapped;

    private string tempPath;
    private string tempResultPath;
    private string co2PathForFortran;
    private string climatePathForFortran;

    //■■■■■　アプリ起動時の初期化作業
    void Start()
    {
        // サイト候補をドロップダウンに設定（Inspectorで設定してもOK）
        dropdownSite.ClearOptions();
        dropdownSite.AddOptions(new List<string> { "Nakagawa", "SppaskayaPad", "Pasoh", "I_Specify_It" });
        dropdownSite.onValueChanged.AddListener(OnSiteChanged); // 選択変更イベントを登録

        // 初期表示更新
        outSimYear.text    = "20";
        outLat.text        = "40.0";
        outLon.text        = "100.0";
        outFN_CO2.text     = "";
        outFN_Climate.text = "";

        textTmp.text      = $"{sliderTmp.value:F1} ℃";
        textTmpRange.text = $"×{sliderTmpRange.value:F1} fold";
        textRH.text       = $"×{sliderRH.value:F1} fold";
        textPrecip.text   = $"×{sliderPrecip.value:F1} fold";
        textRad.text      = $"×{sliderRad.value:F1} fold";
        textCO2.text      = $"{(int)sliderCO2.value} ppm";

        //スライダーが動かされると、テキストラベルが更新されるイベントリスナーを登録
        sliderTmp.onValueChanged.AddListener(val => {
            textTmp.text = $"{val:F1} ℃";
        });

        sliderTmpRange.onValueChanged.AddListener(val => {
            textTmpRange.text = $"{val:F1} fold";
        });

        sliderRH.onValueChanged.AddListener(val => {
            textRH.text = $"{val:F1} fold";
        });

        sliderPrecip.onValueChanged.AddListener(val => {
            textPrecip.text = $"{val:F1} fold";
        });

        sliderRad.onValueChanged.AddListener(val => {
            textRad.text = $"{val:F1} fold";
        });

        sliderCO2.onValueChanged.AddListener(val => {
            textCO2.text = $"{val:F1} ppm";
        });

        RenderIntroByLocale();

        if (progressBar.handleRect != null)
        {
            progressBar.handleRect.gameObject.SetActive(false);
            progressBar.handleRect = null;       // optional: clear reference
        }

        SetupPicker(outFN_Result, OnClickResultSave);
        SetupPicker(outFN_CO2, OnClickCO2Open);
        SetupPicker(outFN_Climate, OnClickClimateOpen);
    }

    void SetupPicker(InputField field, System.Action action)
    {
        field.readOnly = true;
        var trig = field.gameObject.GetComponent<EventTrigger>() ?? field.gameObject.AddComponent<EventTrigger>();
        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(_ => action());
        trig.triggers.Add(entry);
    }

    //■■■■■　実行ボタンが押されたときの処理
    public void OnClickStartSimulation()
    {
        startButton.interactable = false;
        stopButton.interactable = true;

        //outFN_Resulte が空か、禁止文字を含んでいないかチェックし、問題があればコンソールボックスにエラーを出して処理を中断
        string fileOut = outFN_Result.text;
        char[] invalidChars = Path.GetInvalidFileNameChars();   // ファイル名に使えない文字（OS依存）

        if (string.IsNullOrWhiteSpace(fileOut) /*|| fileOut.IndexOfAny(invalidChars) >= 0*/)
        {
            AppendLog("The specified file name is invalid.");
            SetInputInteractable(true); // 入力UIはそのまま有効に戻す
            return;                     // ここで処理を中断
        }

        //数字のみ & 正の整数 以外はNG
        string simYear = outSimYear.text?.Trim();
        if (!int.TryParse(simYear, NumberStyles.None, CultureInfo.InvariantCulture, out int SimPeriod) || SimPeriod <= 0)
        {
            AppendLog("Please enter a positive integer in the input filed for Simulation Year.");
            return;
        }

        //各種ファイルのあるパスを設定
        externalRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../External"));

        string tempIO_Path = Path.GetTempPath();  // ●一時フォルダへのパス

        inputPath = Path.Combine(tempIO_Path, "input");
        outputPath      = Path.Combine(tempIO_Path, "output");
        controlPath     = Path.Combine(tempIO_Path, "control");
        paramCsvPath    = Path.Combine(inputPath, "params.csv");
        tempPath        = Path.Combine(tempIO_Path, "temp");

        //inputPath = Path.Combine(externalRoot, "input");
        //outputPath = Path.Combine(externalRoot, "output");
        //controlPath = Path.Combine(externalRoot, "control");
        //paramCsvPath = Path.Combine(inputPath, "params.csv");
        //tempPath = Path.Combine(externalRoot, "temp");


        //必要なディレクトリを生成する
        Directory.CreateDirectory(inputPath);
        Directory.CreateDirectory(outputPath);
        Directory.CreateDirectory(controlPath);
        Directory.CreateDirectory(tempPath);

        //古い出力や制御ファイルを削除
        ClearOutputDirectory();
        ClearControlFiles();
        ClearDirectory(tempPath);

        tempResultPath = Path.Combine(tempPath, "resultOutput");

        //プログレスバーのリセット
        progressBar.value = 0.0f;

        //入力UIを無効化して、ユーザーが実行中にパラメータを変更できないようにする
        SetInputInteractable(false);

        // Resolve inputs into External\temp when "I_Specify_It"
        var site = dropdownSite.options[dropdownSite.value].text;
        try
        {
            if (site == "I_Specify_It")
            {
                var co2Src = co2PathRaw;
                var climateSrc = climatePathRaw;

                if (string.IsNullOrWhiteSpace(co2Src) || !File.Exists(co2Src))
                    throw new FileNotFoundException("CO₂ file not found.", co2Src);
                if (string.IsNullOrWhiteSpace(climateSrc) || !File.Exists(climateSrc))
                    throw new FileNotFoundException("Climate file not found.", climateSrc);

                co2PathForFortran = CopyIntoFolder(co2Src, tempPath);
                climatePathForFortran = CopyIntoFolder(climateSrc, tempPath);

                //AppendLog("Inputs staged to temp:\n  " + co2PathForFortran + "\n  " + climatePathForFortran);
            }
            else
            {
                // For preset sites you may not need external files; leave empty.
                co2PathForFortran = "";
                climatePathForFortran = "";
            }
        }
        catch (Exception ex)
        {
            AppendLog("Error staging inputs: " + ex.Message);
            SetInputInteractable(true);
            return;
        }

        //Fortranコードに渡すための、シミュレーション制御パラメーターをファイルに書き出す
        //(Fortranコードとの通信は、ファイルを通じて行われる)
        WriteParamsCsv();

        //Fortran 実行プログラムを起動し、その進行を監視するコルーチンを開始
        StartCoroutine(RunFortranAndWatchOutput());
    }

    //■■■■■　Stopボタンが押された場合の処理
    public void OnClickStopSimulation()
    {
        if (!stopButton.interactable) return;

        startButton.interactable = true;
        stopButton.interactable = false;

        //control/stop.txt を書き出すことで Fortran プログラムに停止指令を送る
        string stopFilePath = Path.Combine(controlPath, "stop.txt");
        try
        {
            File.WriteAllText(stopFilePath, "stop");
            //consoleText.text += "Simulation aborted." + "\n";
            AppendLog("Simulation aborted.");
            UnityEngine.Debug.Log("[Unity] stop.txt を書き出しました。Fortranに中断指示を出しました。");
        }
        catch (System.Exception ex)
        {
            //consoleText.text += "Failed to write the stop signal file: " + ex.Message + "\n";
            AppendLog("Failed to write the stop signal file.");
            UnityEngine.Debug.LogError("[Unity] stop.txt 書き出し失敗: " + ex.Message);
        }

        // Fortran プロセスが残っていれば強制終了
        TerminateFortranProcess();

        //パラメーター入力ボックスを再び有効化
        SetInputInteractable(true);

        progressBar.value = 0.0f;
    }

    //■■■■■　パラメーターファイルの書き出し
    // Unity UIで指定したパラメータをCSV形式で保存
    // Fortran側はこのファイルを読み取ってシミュレーション条件を取得する。
    private void WriteParamsCsv()
    {
        float tmp       = sliderTmp.value;
        float tmpRange  = sliderTmpRange.value;
        float rh        = sliderRH.value;
        float precip    = sliderPrecip.value;
        float rad       = sliderRad.value;
        float co2       = sliderCO2.value;
        string latitudeText    = outLat.text?.Trim();
        string longitudeText   = outLon.text?.Trim();
        string site            = dropdownSite.options[dropdownSite.value].text;
        string simYearText     = outSimYear.text?.Trim();
        //string fileNameResult  = outFN_Result.text;
        string fileNameResult  = tempResultPath;
        string fileNameCO2     = co2PathForFortran; // outFN_CO2.text;
        string fileNameClimate = climatePathForFortran; // outFN_Climate.text;

        // simYear を整数化 → [yearMin,yearMax] にクランプ（失敗時は yearMin にフォールバック）
        //int simYear;
        if (!int.TryParse(simYearText, NumberStyles.None, CultureInfo.InvariantCulture, out yearLength))
        {
            yearLength = yearMin;
        }
        yearLength = Mathf.Clamp(yearLength, yearMin, yearMax); // または: simYear = System.Math.Min(1000, System.Math.Max(10, simYear));

        // latitudeを実数化 → [-90,90] にクランプ（失敗時は 0.0 にフォールバック）
        //int simYear;

        if (!float.TryParse(latitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out latitudeNum))
        {
            latitudeNum = 0.0f;
        }
        latitudeNum = Mathf.Clamp(latitudeNum, -90.0f, 90.0f);

        // longitudeを実数化 → [-180,180] にクランプ（失敗時は 0.0 にフォールバック）
        //int simYear;
        if (!float.TryParse(longitudeText, NumberStyles.Float, CultureInfo.InvariantCulture, out longitudeNum))
        {
            longitudeNum = 0.0f;
        }
        longitudeNum = Mathf.Clamp(longitudeNum, -180.0f, 180.0f);



        // 文字列フィールドをCSV用にクォート（内部の " は "" に二重化）
        string CsvQuote(string s)
        {
            if (s == null) return "\"\"";
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        }
        
        string header = "AirTmp_bias, TmpRange_bias, RH_bias, Precip_bias, Rad_bias, CO2_bias, site, outFN_Result, simYear, latitude, longitude, fileNameCO2, fileNameClimate\n";

        string line = string.Format(
            CultureInfo.InvariantCulture,
            "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}",
            tmp, tmpRange, rh, precip, rad, co2,
            CsvQuote(site), CsvQuote(fileNameResult), yearLength,
            latitudeNum, longitudeNum,
            CsvQuote(fileNameCO2), CsvQuote(fileNameClimate)
        );

        string csv = header + line + "\n";

        string paramFilePath = Path.Combine(inputPath, "params.csv");
        
        File.WriteAllText(paramFilePath, csv);
        
        //File.WriteAllText(paramFilePath, csv, new UTF8Encoding(true));

        //var sjis = Encoding.GetEncoding(932);
        //File.WriteAllText(paramFilePath, csv, sjis);
    }

    //■■■■■　Fortranファイルの実行と監視
    private IEnumerator RunFortranAndWatchOutput()
    {
        string tempIO_Path = Path.GetTempPath();  // 一時フォルダへのパス
        string exePath = Path.Combine(externalRoot, SEIBexe);   // 実行ファイル所在

        if (!File.Exists(exePath))
        {
            AppendLog("The executable file (the main simulator .exe) was not found.");
            yield break;
        }

        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            WorkingDirectory = externalRoot,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true
        };
        // Fortran実行ファイルに一時読み書きティレクトリを引数として与える
        // --io-root <tempDir> の2引数として渡される（スペースや日本語OK）
        startInfo.ArgumentList.Add("--io-root");
        startInfo.ArgumentList.Add(Path.GetTempPath());

        try
        {
            fortranProcess = Process.Start(startInfo);
            AppendLog("Simulation started.");

            // Fortranからの出力ログを非同期で読む
            Task.Run(() =>
            {
                while (!fortranProcess.StandardOutput.EndOfStream)
                {
                    string line = fortranProcess.StandardOutput.ReadLine();
                    UnityEngine.Debug.Log("[Fortran] " + line);
                }
            });
        }
        catch (System.Exception e)
        {
            AppendLog("Failed to run the simulator executable");
            yield break;
        }

        // ★追加：abort 検出で外側forを抜けるためのフラグ
        bool abortedByCore = false;

        //シミュレーション年数に従って 年ごとに1ファイルずつ検出を試みるループ。
        for (int year = 1; year < yearLength + 1; year++)
        {
            string filePath = Path.Combine(outputPath, $"year{year}.csv");
            string abortPath = Path.Combine(controlPath, $"abort.txt");
            float elapsed = 0f; // 待機時間カウンタ

            //出力ファイルがまだ存在していない場合は…
            while (!File.Exists(filePath))
            {
                // abort.txt を検出したら「完了処理」へ
                if (File.Exists(abortPath))
                {
                    // Fortranプロセスを停止（Stopボタン押下時と同じ動作へ）
                    //様々な要因でabortさせるのであれば、abortした理由をabort.txtに書き出して、その内容を表示させる
                    TerminateFortranProcess();
                    AppendLog("Simulation aborted by model core.");
                    AppendLog("Out of land surface.");

                    // 入力UIを再び有効化（stop処理と同じ）
                    SetInputInteractable(true);

                    // ボタン状態復帰
                    startButton.interactable = true;
                    stopButton.interactable = false;

                    // 進捗バーをリセット
                    progressBar.value = 0.0f;

                    // ※ 可視化などの完了処理(DoAfterCompleted)へは進まず終了
                    yield break;
                }

                yield return new WaitForSeconds(1.0f);
                elapsed += 1.0f;

                // 最大待機時間を超過した場合
                if (elapsed > maxWaitPerYearSec)
                {
                    AppendLog($"Error: Timeout while waiting for year {year} output file.");
                    TerminateFortranProcess();  // 強制終了（前回提案の共通関数）
                    SetInputInteractable(true); // 入力UIを再び有効化
                    yield break; // コルーチン終了
                }
            }

            // abort を検出したら外側 for も抜ける
            if (abortedByCore)
                break;

            //出力ファイルを検出した場合、その内容を読み取り…
            string result = File.ReadAllText(filePath);



            AppendLog($"Detected an output file for year {year}");

            //プログレスバーの更新
            float progress = (float)(year) / yearLength;
            progressBar.value = progress;
        }

        //シミュレーション完了、
        TerminateFortranProcess();   //念のためFortran実行ファイルを強制停止する処理を行う
        AppendLog("Simulation completed.");

        startButton.interactable = true;
        stopButton.interactable = false;

        //パラメーター入力ボックスを再び有効化
        SetInputInteractable(true);

        progressBar.value = 0.0f;

        if (outFN_Result.text == "SEIB_result")
        {
            resultPathRaw = tempIO_Path + "/" + outFN_Result.text + ".txt"; 
        }

        yield return StartCoroutine(DoAfterCompleted());
    }

    IEnumerator DoAfterCompleted()
    {
        yield return new WaitForSeconds(0.5f);

        TryCopyResult(tempResultPath + ".txt", resultPathRaw);

        Director.Instance.OnSimulationCompleted(resultPathRaw);
    }

    //補助関数（outputPath 内の既存ファイルをすべて削除）
    private void ClearOutputDirectory()
    {
        try
        {
            if (Directory.Exists(outputPath))
            {
                string[] files = Directory.GetFiles(outputPath);
                foreach (string file in files)
                {
                    File.Delete(file);
                }

                AppendLog("Deleted old files in the output folder.");
                //consoleText.text += "Deleted old files in the output folder." + "\n";
                //UnityEngine.Debug.Log("[Unity] output フォルダ内の旧ファイルを削除しました。");
            }
        }
        catch (System.Exception ex)
        {
            AppendLog("Error: Can not delete old files in the output folder.");
        }
    }

    //補助関数（制御ファイルの消去）
    private void ClearControlFiles()
    {
        string stopFilePath = Path.Combine(controlPath, "stop.txt");
        string abortFilePath = Path.Combine(controlPath, "abort.txt");
        if (File.Exists(stopFilePath)) File.Delete(stopFilePath);
        if (File.Exists(abortFilePath)) File.Delete(abortFilePath);
    }

    //補助関数（サイト選択ドロップボックスの操作時）
    private void OnSiteChanged(int index)
    {
        string selected = dropdownSite.options[index].text;

        if (selected == "I_Specify_It")
        {
            outLat.interactable = true;
            outLon.interactable = true;
            outFN_CO2.interactable = true;
            outFN_Climate.interactable = true;
        }
        else
        {
            outLat.interactable = false;
            outLon.interactable = false;
            outFN_CO2.interactable = false;
            outFN_Climate.interactable = false;
        }
    }


    //補助関数（入力UIの有効・無効切り替え）
    private void SetInputInteractable(bool interactable)
    {
        // 共通で制御するフィールド
        dropdownSite.interactable = interactable;
        outFN_Result.interactable = interactable;
        outSimYear.interactable = interactable;
        sliderTmp.interactable = interactable;
        sliderPrecip.interactable = interactable;
        sliderCO2.interactable = interactable;

        // サイト選択状態を反映して特殊フィールドを制御
        string selected = dropdownSite.options[dropdownSite.value].text;
        bool isSpecify = (selected == "I_Specify_It");

        if (interactable && isSpecify)
        {
            // 実行中でなければ & I_Specify_It のときのみ有効化
            outLat.interactable = true;
            outLon.interactable = true;
            outFN_CO2.interactable = true;
            outFN_Climate.interactable = true;
        }
        else
        {
            // それ以外は常に無効化
            outLat.interactable = false;
            outLon.interactable = false;
            outFN_CO2.interactable = false;
            outFN_Climate.interactable = false;
        }
    }

    //コンソール画面に文字列を表示、リフレッシュ、常に最新行が表示されるようにスクロール
    private void AppendLog(string message)
    {
        logLines.Enqueue(message);

        // UIサイズに基づいた最大行数を計算
        int maxLines = GetMaxLogLines();

        // 最大行数を超えたら古い行を削除
        while (logLines.Count > maxLines)
        {
            logLines.Dequeue();
        }

        // テキストを再構築
        consoleText.text = string.Join("\n", logLines);

        // 高さを正しく反映
        LayoutRebuilder.ForceRebuildLayoutImmediate(consoleText.rectTransform);
        Canvas.ForceUpdateCanvases();

        // 最新行へスクロール
        scrollRect.verticalNormalizedPosition = 0f;
    }


    /// consoleText の高さとフォントサイズから収まる最大行数を計算する
    private int GetMaxLogLines()
    {
        if (consoleText == null) return 50; // 安全策としてデフォルト50行

        // コンソールの高さを取得
        float consoleHeight = consoleText.rectTransform.rect.height;

        // 行の高さ = フォントサイズ × 行間係数
        float lineHeight = consoleText.fontSize * 1.2f;

        // 最小でも10行は保持するようにする
        return Mathf.Max(10, Mathf.FloorToInt(consoleHeight / lineHeight));
    }


    //アプリ終了処理（Unity終了時に Fortran プロセスが残っていたら強制終了）
    void OnApplicationQuit()
    {
        TerminateFortranProcess();
    }

    // Fortran プロセスを安全に終了させる共通関数
    private void TerminateFortranProcess()
    {
        try
        {
            if (fortranProcess != null && !fortranProcess.HasExited)
            {
                fortranProcess.Kill();
                AppendLog("Forced terminated the Fortran process.");
                UnityEngine.Debug.Log("[Unity] Fortran process was killed safely.");
            }
        }
        catch (System.Exception ex)
        {
            AppendLog("Failed to terminate the Fortran process: " + ex.Message);
            UnityEngine.Debug.LogError("[Unity] Fortran process termination failed: " + ex.Message);
        }
        finally
        {
            if (fortranProcess != null)
            {
                fortranProcess.Dispose();  // 後始末を明示
                fortranProcess = null;
            }
        }
    }

    private void OnClickResultSave()
    {
        SaveFileDialog("txt(*.txt)|*.txt|All files(*.*)|*.*", "SEIB_result", path =>
        {
            if (string.IsNullOrEmpty(path)) return;

            if (Path.GetExtension(path).ToLowerInvariant() != ".txt")
                path += ".txt";

            //outFN_Result.text = Path.ChangeExtension(path, null);

            resultPathRaw = path;
            resultPathMapped = MapOneDriveToLocalKnownFolder(resultPathRaw);

            outFN_Result.text = Path.ChangeExtension(resultPathMapped, null);

            UnityEngine.Debug.Log("outFN_Result: " + outFN_Result.text);
        });
    }

    private void OnClickCO2Open()
    {
        OpenFileDialog("dat(*.dat)|*.dat|All files(*.*)|*.*", false, paths =>
        {
            if (paths == null || paths.Length == 0) return;

            //outFN_CO2.text = paths[0];

            co2PathRaw = paths[0];
            co2PathMapped = MapOneDriveToLocalKnownFolder(co2PathRaw);

            outFN_CO2.text = co2PathMapped;

            UnityEngine.Debug.Log("outFN_CO2: " + outFN_CO2.text);
        });
    }

    private void OnClickClimateOpen()
    {
        OpenFileDialog("txt(*.txt)|*.txt|All files(*.*)|*.*", false, paths =>
        {
            if (paths == null || paths.Length == 0) return;

            //outFN_Climate.text = paths[0];

            climatePathRaw = paths[0];
            climatePathMapped = MapOneDriveToLocalKnownFolder(climatePathRaw);

            outFN_Climate.text = climatePathMapped;

            UnityEngine.Debug.Log("outFN_Climate: " + outFN_Climate.text);
        });
    }

    private void OpenFileDialog(string filter, bool multiselect, System.Action<string[]> onPicked)
    {
        string[] result = null;

        var t = new Thread(() =>
        {
            using (var dlg = new System.Windows.Forms.OpenFileDialog())
            {
                dlg.Filter = filter;               
                dlg.Multiselect = multiselect;
                dlg.CheckFileExists = true;

                var res = dlg.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    result = multiselect ? dlg.FileNames : new[] { dlg.FileName };
                }
                else
                {
                    result = System.Array.Empty<string>();
                }
            }
        });

        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join(); 

        onPicked?.Invoke(result);
    }

    private void SaveFileDialog(string filter, string defaultName, System.Action<string> onPicked)
    {
        string result = null;

        var t = new Thread(() =>
        {
            using (var dlg = new System.Windows.Forms.SaveFileDialog())
            {
                dlg.Filter = filter;               
                dlg.FileName = defaultName;       

                var res = dlg.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK)
                {
                    result = dlg.FileName;
                }
            }
        });

        t.SetApartmentState(ApartmentState.STA);
        t.Start();
        t.Join();

        onPicked?.Invoke(result);
    }

    private string FirstExisting(params string[] candidates)
    {
        foreach (var p in candidates)
            if (!string.IsNullOrEmpty(p) && Directory.Exists(p)) return p;
        return candidates.FirstOrDefault(); 
    }

    private string GetUserProfile() =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    private string GetLocalDesktopPath() =>
        FirstExisting(
            Path.Combine(GetUserProfile(), "Desktop"),
            Path.Combine(GetUserProfile(), "デスクトップ")
        );

    private string GetLocalDocumentsPath() =>
        FirstExisting(
            Path.Combine(GetUserProfile(), "Documents"),
            Path.Combine(GetUserProfile(), "ドキュメント")
        );

    private IEnumerable<string> GetOneDriveRoots()
    {
        var roots = new List<string>();
        string user = GetUserProfile();

        var env = Environment.GetEnvironmentVariable("OneDrive");
        if (!string.IsNullOrEmpty(env) && Directory.Exists(env)) roots.Add(env);

        var consumer = Path.Combine(user, "OneDrive");
        if (Directory.Exists(consumer)) roots.Add(consumer);

        try
        {
            foreach (var dir in Directory.GetDirectories(user, "OneDrive*"))
                roots.Add(dir);
        }
        catch { }

        return roots
            .Select(r => Path.GetFullPath(r).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private IEnumerable<string> GetOneDriveDesktopCandidates()
    {
        foreach (var root in GetOneDriveRoots())
        {
            yield return Path.Combine(root, "Desktop");
            yield return Path.Combine(root, "デスクトップ");
        }
    }

    private IEnumerable<string> GetOneDriveDocumentsCandidates()
    {
        foreach (var root in GetOneDriveRoots())
        {
            yield return Path.Combine(root, "Documents");
            yield return Path.Combine(root, "ドキュメント");
        }
    }

    private string MapOneDriveToLocalKnownFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;

        string full = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Desktop?
        foreach (var odDesk in GetOneDriveDesktopCandidates())
        {
            var prefix = Path.GetFullPath(odDesk)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string tail = full.Substring(prefix.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string local = Path.Combine(GetLocalDesktopPath(), tail);
                Directory.CreateDirectory(Path.GetDirectoryName(local) ?? GetLocalDesktopPath());
                return local;
            }
        }

        // Documents?
        foreach (var odDocs in GetOneDriveDocumentsCandidates())
        {
            var prefix = Path.GetFullPath(odDocs)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            if (full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                string tail = full.Substring(prefix.Length)
                    .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string local = Path.Combine(GetLocalDocumentsPath(), tail);
                Directory.CreateDirectory(Path.GetDirectoryName(local) ?? GetLocalDocumentsPath());
                return local;
            }
        }

        return path;
    }

    private void ClearDirectory(string dir)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var f in Directory.GetFiles(dir)) { try { File.Delete(f); } catch { } }
        foreach (var d in Directory.GetDirectories(dir)) { try { Directory.Delete(d, true); } catch { } }
    }

    private string CopyIntoFolder(string srcPath, string dstFolder)
    {
        if (string.IsNullOrWhiteSpace(srcPath) || !File.Exists(srcPath))
            throw new FileNotFoundException("Source file not found.", srcPath);

        Directory.CreateDirectory(dstFolder);
        string dstPath = Path.Combine(dstFolder, Path.GetFileName(srcPath));
        File.Copy(srcPath, dstPath, true);
        return dstPath;
    }

    private void TryCopyResult(string src, string dst)
    {
        UnityEngine.Debug.Log("TryCopyResult: " + src + " " + dst);

        try
        {
            if (string.IsNullOrWhiteSpace(src) || string.IsNullOrWhiteSpace(dst)) return;

            var fullSrc = Path.GetFullPath(src);
            var fullDst = Path.GetFullPath(dst);

            if (string.Equals(fullSrc, fullDst, StringComparison.OrdinalIgnoreCase)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(fullDst) ?? ".");

            File.Copy(fullSrc, fullDst, true);

            UnityEngine.Debug.Log($"Result also saved to: {fullDst}");
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log($"Warning: failed to save result to original path. ({ex.Message})");
        }
    }

    public void OnClick_Button_Translation()
    {
        Director.Instance.uiManager.OnClick_Button_Setting();

        ClearLog();

        RenderIntroByLocale();
    }

    void ClearLog()
    {
        logLines.Clear();
        consoleText.text = string.Empty;
        LayoutRebuilder.ForceRebuildLayoutImmediate(consoleText.rectTransform);
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1f;
    }

    void RenderIntroByLocale()
    {
        bool isJapanese = Director.Instance.uiManager.CheckJapanese();

        //説明文章をコンソールに表示する
        if (!isJapanese)
        {
            AppendLog("_________________________________________________");
            AppendLog("When you set the simulation conditions and press Start, the simulation will run.");
            AppendLog("After completion, the results will be output to a file and visualized.");
            AppendLog("");
            AppendLog("When a preset location is selected:");
            AppendLog("The simulation will use climate data and CO₂ concentration data from 1981-1990 repeatedly, for the number of years specified.");
            AppendLog("");
            AppendLog("When no preset location is selected (I_Specify_It):");
            AppendLog("Please enter the latitude and longitude (North latitude = positive, East longitude = negative, decimals in base-10) of the simulation site, as well as time-series data for climate and CO2.");
            AppendLog("If the simulation period exceeds the length of the provided datasets, the data will automatically loop from the beginning.");
            AppendLog("Therefore, it is recommended that the climate and CO2 datasets have the same length (in years).");
            AppendLog("");
            AppendLog("Effects of the climate adjustment sliders:");
            AppendLog("Temperature & CO2 --> The specified value is directly added to each year's data.");
            AppendLog("Other variables --> The specified value is applied as a multiplier (e.g., 1.2 → 20% increase).");
            AppendLog("_________________________________________________");
            AppendLog("");
        }
        else
        {
            AppendLog("_________________________________________________");
            AppendLog("シミュレーション条件を設定して「スタート」を押すと、シミュレーションが実行され、完了後に結果ファイルが出力されると同時に可視化されます。");
            AppendLog("");
            AppendLog("地点をプリセットから選んだ場合：");
            AppendLog("1981〜1990年の10年間の気象データとCO2濃度データを繰り返し利用して、指定した年数分のシミュレーションを行います。");
            AppendLog("");
            AppendLog("地点を選ばない場合（I_Specify_It を選択時）：");
            AppendLog("シミュレーションを行う緯度・経度（北緯は正、東経は正、小数点以下は10進法）、気候の時系列データ、CO2の時系列データを入力してください。シミュレーション年数が入力データの長さを超えた場合は、自動的に巻き戻して繰り返し利用されます。そのため、気候データとCO2データの年数は揃えておくことを推奨します。");
            AppendLog("");
            AppendLog("気候操作スライダーの効果:");
            AppendLog("気温・CO2 → 各年のデータに指定値をそのまま加算");
            AppendLog("その他の変数 → 指定値を倍率として適用（例：1.2 → 20 % 増）");
            AppendLog("_________________________________________________");
            AppendLog("");
        }
    }
}

