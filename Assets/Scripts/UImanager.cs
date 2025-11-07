using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;               //UI部品を使うために必要
using UnityEngine.SceneManagement;
using UnityEngine.Localization.Settings;  //Scene遷移を扱うのに必要
using Unity.VisualScripting;
using System;
using System.Linq;
using UnityEngine.Localization;

public class UImanager : MonoBehaviour
{
    //参考にした記事
    // https://xr-hub.com/archives/11782
    // https://teratail.com/questions/155388

    //Panelを格納する変数
    //インスペクターウィンドウからゲームオブジェクトを設定すること
    [SerializeField] GameObject PanelOption;
    [SerializeField] GameObject PanelButtons;
    [SerializeField] GameObject PanelYear;

    [SerializeField] GameObject Text_FileName;
    [SerializeField] GameObject Text_Forest_Size;

    [SerializeField] GameObject PlayButton;
    [SerializeField] GameObject PauseButton;
    [SerializeField] GameObject ReplayButton;

    [SerializeField] GameObject LeftButtons;
    [SerializeField] GameObject LeftSlider;
    [SerializeField] GameObject PanelLegend;
    [SerializeField] GameObject PanelMaterialCycle;
    [SerializeField] GameObject PanelCarbonCycle;
    [SerializeField] GameObject PanelWaterCycle;
    [SerializeField] GameObject PanelAtmospheric;
    [SerializeField] GameObject PanelRadiationBalance;
    [SerializeField] GameObject PanelPlantProperties;
    [SerializeField] GameObject PanelSimulationInfo;

    [SerializeField] GameObject PanelRight;

    [SerializeField] GameObject PanelInfo;
    [SerializeField] GameObject PanelHelp;
    [SerializeField] RectTransform PanelHelpContent;
    [SerializeField] ScrollRect helpScrollRect;
    [SerializeField] Text CaptionText;
    [SerializeField] Text VersionText;

    [SerializeField] GameObject MaterialCycleButton;
    [SerializeField] GameObject CarbonCycleButton;
    [SerializeField] GameObject WaterCycleButton;
    [SerializeField] GameObject RadiationBalanceButton;

    [SerializeField] GameObject LegendTitle;
    [SerializeField] GameObject MaterialCycleTitle;
    [SerializeField] GameObject CarbonCycleTitle;
    [SerializeField] GameObject WaterCycleTitle;
    [SerializeField] GameObject AtmosphericTitle;
    [SerializeField] GameObject RadiationBalanceTitle;
    [SerializeField] GameObject PlantPropertiesTitle;
    [SerializeField] GameObject SimulationInfoTitle;

    [SerializeField] GameObject FluxAnnotation;
    [SerializeField] GameObject CarbonAnnotation;
    [SerializeField] GameObject WaterAnnotation;
    [SerializeField] GameObject AtmosphericAnnotation;
    [SerializeField] GameObject RadiationAnnotation;

    [SerializeField] GameObject LeftArrowButton;
    [SerializeField] GameObject RightArrowButton;

    [SerializeField] GameObject Buttons;
    [SerializeField] Image[] ButtonImages;
    [SerializeField] Sprite[] ButtonOffSprites;
    [SerializeField] Sprite[] ButtonOnSprites;

    [SerializeField] Text fluxC_gpp;
    [SerializeField] Text fluxC_atr;
    [SerializeField] Text fluxC_htr;
    [SerializeField] Text fluxC_lit;
    [SerializeField] Text fluxC_som;

    [SerializeField] Text fluxW_pre;
    [SerializeField] Text fluxW_ro1;
    [SerializeField] Text fluxW_ro2;
    [SerializeField] Text fluxW_ic;
    [SerializeField] Text fluxW_ev;
    [SerializeField] Text fluxW_tr;
    [SerializeField] Text fluxW_sl;
    [SerializeField] Text fluxW_tw;
    [SerializeField] Text fluxW_sn;
    [SerializeField] Text yText;

    [SerializeField] Text rad_short_direct_amean;
    [SerializeField] Text rad_short_diffuse_amean;
    [SerializeField] Text rad_short_up_amean;
    [SerializeField] Text rad_long_down_amean;
    [SerializeField] Text rad_long_up_amean;

    [SerializeField] Text[] carbonUnits = new Text[6];
    [SerializeField] Text[] waterUnits = new Text[6];
    [SerializeField] Text[] radiationUnits = new Text[5];

    [SerializeField] ArrowController arrow_rad_short_direct_amean;
    [SerializeField] ArrowController arrow_rad_short_diffuse_amean;
    [SerializeField] ArrowController arrow_rad_short_up_amean;
    [SerializeField] ArrowController arrow_rad_long_down_amean;
    [SerializeField] ArrowController arrow_rad_long_up_amean;

    [SerializeField] ArrowController arrow_fluxC_gpp;
    [SerializeField] ArrowController arrow_fluxC_atr;
    [SerializeField] ArrowController arrow_fluxC_htr;
    [SerializeField] ArrowController arrow_fluxC_lit;
    [SerializeField] ArrowController arrow_fluxC_som;

    [SerializeField] ArrowController arrow_fluxW_pre;
    [SerializeField] ArrowController arrow_fluxW_;
    [SerializeField] ArrowController arrow_fluxW_ro1;
    [SerializeField] ArrowController arrow_fluxW_ro2;
    [SerializeField] ArrowController arrow_fluxW_ic;
    [SerializeField] ArrowController arrow_fluxW_ev;
    [SerializeField] ArrowController arrow_fluxW_tr;
    [SerializeField] ArrowController arrow_fluxW_sl;
    [SerializeField] ArrowController arrow_fluxW_tw;
    [SerializeField] ArrowController arrow_fluxW_sn;
    [SerializeField] ArrowController arrow_x;
    [SerializeField] ArrowController arrow_y;

    [SerializeField] Text latData;
    [SerializeField] Text lonData;
    [SerializeField] Text altData;
    [SerializeField] Text fieldCapData;
    [SerializeField] Text wiltPoiData;
    [SerializeField] Text albedoData;

    [SerializeField] LocalizedString latString;
    [SerializeField] LocalizedString lonString;
    [SerializeField] LocalizedString altString;
    [SerializeField] LocalizedString fieldCapString;
    [SerializeField] LocalizedString wiltPoiString;
    [SerializeField] LocalizedString albedoString;

    [SerializeField] GameObject confirmPanel;
    [SerializeField] Text confirmText;

    public GameObject SEIBConnector;

    public bool confirmOpen = false;

    public List<TreeItemData> WoodyPFTs = new List<TreeItemData>();

    public GameObject treeItemPrefab;
    public Transform legendTransform;

    List<GameObject> treeItemInstances = new List<GameObject>();

    //public GameObject[] WoodyPFTs_ = new GameObject[10];  //木のprefab。どのprefabを割り当てるかは、インスペクターを介して設定する。
    //public GameObject[] tree_3d_ = new GameObject[5];    //個木にassigneされるprefab

    // false english, true japanese
    //public static bool Lang = false;

    private int panelIndex = 0;
    private int panelCount = 8;

    //効果音
    public AudioClip Sound_Kachi;
    public AudioClip Sound_Pi;
    public AudioClip Sound_Pichi;
    public AudioClip Sound_Piro;
    public AudioClip Sound_Poka;
    public AudioClip Sound_WindowOpen;
    public AudioClip Sound_WindowClose;
    public AudioClip Sound_HelpOpen;

    AudioSource audioSource;

    CaptionCollection captions;
    Coroutine activeAnimation;
    float animationDuration = 0.25f;
    AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    void Awake()
    {
        var ta = Resources.Load<TextAsset>("caption");
        captions = JsonUtility.FromJson<CaptionCollection>(ta.text);
    }

    private void OnEnable()
    {
        latString.StringChanged += UpdateLat;
        lonString.StringChanged += UpdateLon;
        altString.StringChanged += UpdateAlt;
        fieldCapString.StringChanged += UpdateFieldCap;
        wiltPoiString.StringChanged += UpdateWiltPoi;
        albedoString.StringChanged += UpdateAlbedo;
    }

    private void OnDisable()
    {
        latString.StringChanged -= UpdateLat;
        lonString.StringChanged -= UpdateLon;
        altString.StringChanged -= UpdateAlt;
        fieldCapString.StringChanged -= UpdateFieldCap;
        wiltPoiString.StringChanged -= UpdateWiltPoi;
        albedoString.StringChanged -= UpdateAlbedo;
    }

    private void UpdateLat(string s) => latData.text = s;
    private void UpdateLon(string s) => lonData.text = s;
    private void UpdateAlt(string s) => altData.text = s;
    private void UpdateFieldCap(string s) => fieldCapData.text = s;
    private void UpdateWiltPoi(string s) => wiltPoiData.text = s;
    private void UpdateAlbedo(string s) => albedoData.text = s;

    //■■■ Start
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        
        //SetupTreeItems();

        //BackToMain();

        VersionText.text = "Version " + Application.version.ToString();
    }

    //■■■ Update
    void Update()
    {
        // 各ツリーアイテムインスタンスのアクティブ状態をDirector.PFT_OnOffに基づいて更新
        for (int i = 0; i < treeItemInstances.Count; i++)
        {
            GameObject treeItemInstance = treeItemInstances[i];
            treeItemInstance.SetActive(Director.PFT_OnOff_woody[i]);
        }

        // ファイル名が設定されている場合、ファイル名をテキストとして表示します
        if (Director.Instance.fileName.Length > 0)
        {
            Text_FileName.GetComponent<Text>().text = Director.Instance.fileName;
        }

        if (Director.SimYear == Director.SimYearMax && ReplayButton.activeSelf == false)
        {
            ReplayButton.SetActive(true);
            PlayButton.SetActive(false);
            PauseButton.SetActive(false);
        }

        if (ReplayButton.activeSelf && Director.SimYear != Director.SimYearMax)
        {
            ReplayButton.SetActive(false);
            PlayButton.SetActive(false);
            PauseButton.SetActive(false);

            if (Director.SimYearMove)
            {
                PauseButton.SetActive(true);
            }
            else
            {
                PlayButton.SetActive(true);
            }
        }
    }

    public bool CheckJapanese()
    {
        var loc = LocalizationSettings.SelectedLocale;
        var code = loc?.Identifier.Code ?? string.Empty;
        return code.StartsWith("ja", System.StringComparison.OrdinalIgnoreCase);
    }

    //■■■ Help Box
    public void ShowHelp(string loc)
    {
        var cap = captions.captions.FirstOrDefault(c => c.loc == loc);
        if (cap == null) return;

        bool forMenuB = !string.IsNullOrEmpty(loc) && loc.StartsWith("B", System.StringComparison.OrdinalIgnoreCase);
        PanelHelpContent.anchoredPosition = forMenuB ? new Vector2(0f, 20f) : new Vector2(150f, 20f);

        bool isJapanese = CheckJapanese();
        string text = isJapanese ? cap.jp : cap.eg;

        if (activeAnimation != null)
            StopCoroutine(activeAnimation);

        CaptionText.text = text;

        PanelHelp.SetActive(true);

        StartCoroutine(ScrollToTopNextFrame());



        PanelHelpContent.localScale = Vector3.zero;
        activeAnimation = StartCoroutine(ScaleRoutine(PanelHelpContent, Vector3.one, animationDuration));

        audioSource.PlayOneShot(Sound_HelpOpen);
    }

    IEnumerator ScrollToTopNextFrame()
    {
        yield return null; 
        helpScrollRect.verticalNormalizedPosition = 1f;
    }

    public void CloseHelp()
    {
        if (activeAnimation != null)
            StopCoroutine(activeAnimation);

        activeAnimation = StartCoroutine(HideRoutine());
    }

    private IEnumerator ScaleRoutine(RectTransform target, Vector3 targetScale, float duration)
    {
        Vector3 startScale = target.localScale;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = animationCurve.Evaluate(t);
            target.localScale = Vector3.LerpUnclamped(startScale, targetScale, eased);
            yield return null;
        }

        target.localScale = targetScale;
    }

    private IEnumerator HideRoutine()
    {
        yield return ScaleRoutine(PanelHelpContent, Vector3.zero, animationDuration);

        PanelHelp.SetActive(false);
        activeAnimation = null;
    }

    //■■■ レジェンドリスト用の設定
    public void SetupTreeItems()
    {
        // 既存のツリーアイテムインスタンスをクリーンアップ
        
        foreach (var instance in treeItemInstances)
        {
            Destroy(instance);
        }
        
        treeItemInstances.Clear();

        int idx = 0;
        foreach (var treeItemData in WoodyPFTs)
        {
            // ツリーアイテムプレハブの新しいインスタンスを生成し、レジェンドの親トランスフォームに設定
            GameObject treeItemInstance = Instantiate(treeItemPrefab, legendTransform);
            TreeItem treeItem = treeItemInstance.GetComponent<TreeItem>();
            treeItem.SetTreeItem(treeItemData, idx);
            treeItemInstances.Add(treeItemInstance);

            idx++;
        }
    }

    ////■■■　Main画面に戻る
    //public void BackToMain()
    //{
    //    PanelOption.SetActive(false);
    //    PanelButtons.SetActive(true);
    //    PanelYear.SetActive(true);
    //    PanelLegend.SetActive(false);

    //    Text_FileName.SetActive(true);
    //    Text_Forest_Size.SetActive(true);

    //    /*
    //    Destroy(tree_3d_[1]);
    //    Destroy(tree_3d_[2]);
    //    Destroy(tree_3d_[3]);
    //    */
    //}

    //■■■　言語切り替えボタンを押したときの挙動
    public void OnClick_Button_Setting()
    {
        /*
        PanelOption.SetActive(true);
        PanelButtons.SetActive(false);
        PanelYear.SetActive(false);
        */

        string currentCode = LocalizationSettings.SelectedLocale.Identifier.Code;

        Debug.Log("currentCode: " + currentCode);

        if (currentCode == "ja")
        {
            SetLanguage("en"); // 英語に設定
        }
        else
        {
            SetLanguage("ja"); // 日本語に設定
        }

        audioSource.PlayOneShot(Sound_Pi); //効果音
    }

    void SetLanguage(string languageCode)
    {
        // 利用可能なロケールから指定された言語コードに一致するロケールを検索
        var selectedLocale = LocalizationSettings.AvailableLocales.Locales.Find(locale => locale.Identifier.Code == languageCode);

        if (selectedLocale != null)
        {
            // 一致するロケールが見つかった場合、そのロケールを選択
            LocalizationSettings.SelectedLocale = selectedLocale;

            // ツリーアイテムの設定を更新
            SetupTreeItems();
        }
    }

    void HideAllPanels() 
    {
        PanelInfo.SetActive(false);
        PanelLegend.SetActive(false);
        PanelMaterialCycle.SetActive(false);
        PanelCarbonCycle.SetActive(false);
        PanelWaterCycle.SetActive(false);
        PanelAtmospheric.SetActive(false);
        PanelRadiationBalance.SetActive(false);
        PanelPlantProperties.SetActive(false);
        PanelSimulationInfo.SetActive(false);
    }

    void HideAllTitles()
    {
        LegendTitle.SetActive(false);
        MaterialCycleTitle.SetActive(false);
        CarbonCycleTitle.SetActive(false);
        WaterCycleTitle.SetActive(false);
        AtmosphericTitle.SetActive(false);
        RadiationBalanceTitle.SetActive(false);
        PlantPropertiesTitle.SetActive(false);
        SimulationInfoTitle.SetActive(false);
    }

    void HideAllAnnotations()
    {
        FluxAnnotation.SetActive(false);
        CarbonAnnotation.SetActive(false);
        WaterAnnotation.SetActive(false);
        AtmosphericAnnotation.SetActive(false);
        RadiationAnnotation.SetActive(false);
    }

    //public void OnClick_Button_Legend()
    //{
    //    HideAllPanels();
    //    PanelLegend.SetActive(true);
    //}

    //public void OnClick_Button_MaterialCycle()
    //{
    //    HideAllPanels();
    //    PanelMaterialCycle.SetActive(true);
    //}

    //public void OnClick_Button_CarbonCycle()
    //{
    //    HideAllPanels();
    //    PanelCarbonCycle.SetActive(true);
    //}

    //public void OnClick_Button_WaterCycle()
    //{
    //    HideAllPanels();
    //    PanelWaterCycle.SetActive(true);
    //}

    //public void OnClick_Button_RadiationBalance()
    //{
    //    HideAllPanels();
    //    PanelRadiationBalance.SetActive(true);
    //}

    public void OnClick_Button_LeftArrow()
    {
        panelIndex--;
        if (panelIndex < 0)
        {
            panelIndex = panelCount - 1;
        }

        UpdatePanels();
        audioSource.PlayOneShot(Sound_Pichi); //効果音
    }

    public void OnClick_Button_RightArrow()
    {
        panelIndex++;
        if (panelIndex > panelCount - 1)
        {
            panelIndex = 0;
        }

        UpdatePanels();
        audioSource.PlayOneShot(Sound_Pichi); //効果音
    }

    public void OnClick_Button(int idx)
    {
        panelIndex = idx;
        UpdatePanels();
        audioSource.PlayOneShot(Sound_Pichi); //効果音
    }

    void SetupButtons(int idx) 
    {
        for (int i = 0; i < ButtonImages.Length; i++)
        {
            ButtonImages[i].sprite = ButtonOffSprites[i];
            ButtonImages[i].gameObject.GetComponent<Button>().interactable = true;
        }

        ButtonImages[idx].sprite = ButtonOnSprites[idx];
        ButtonImages[idx].gameObject.GetComponent<Button>().interactable = false;
    }

    void UpdatePanels()
    {
        //Debug.Log("UImanager UpdatePanels");

        HideAllPanels();
        HideAllTitles();
        HideAllAnnotations();

        SetupButtons(panelIndex);

        if (panelIndex == 0)
        {
            PanelSimulationInfo.SetActive(true);
            SimulationInfoTitle.SetActive(true);
        }
        else if (panelIndex == 1)
        {
            PanelLegend.SetActive(true);
            LegendTitle.SetActive(true);
        }
        else if (panelIndex == 2)
        {
            PanelMaterialCycle.SetActive(true);
            MaterialCycleTitle.SetActive(true);
            FluxAnnotation.SetActive(true);
        }
        else if (panelIndex == 3)
        {
            PanelCarbonCycle.SetActive(true);
            CarbonCycleTitle.SetActive(true);
            CarbonAnnotation.SetActive(true);
        }
        else if (panelIndex == 4)
        {
            PanelWaterCycle.SetActive(true);
            WaterCycleTitle.SetActive(true);
            WaterAnnotation.SetActive(true);
        }
        else if (panelIndex == 5)
        {
            PanelAtmospheric.SetActive(true);
            AtmosphericTitle.SetActive(true);
            AtmosphericAnnotation.SetActive(true);
        }
        else if (panelIndex == 6)
        {
            PanelRadiationBalance.SetActive(true);
            RadiationBalanceTitle.SetActive(true);
            RadiationAnnotation.SetActive(true);
        }
        else if (panelIndex == 7)
        {
            PanelPlantProperties.SetActive(true);
            PlantPropertiesTitle.SetActive(true);
        }

        if (panelIndex > 2)
        {
            LeftSlider.SetActive(true);
        } 
        else 
        {
            LeftSlider.SetActive(false);
        }

        //Director.Instance.UpdateGraphs();

        Director.Instance.UpdateGraphsDelayed(0.5f);
    }

    public void ShowSimulationInfo()
    {
        panelIndex = 0;

        HideAllPanels();
        HideAllTitles();
        HideAllAnnotations();

        SetupButtons(0);

        PanelSimulationInfo.SetActive(true);

        SimulationInfoTitle.SetActive(true);

        LeftSlider.SetActive(false);
    }

    public void ShowLegend()
    {
        panelIndex = 0;

        HideAllPanels();
        HideAllTitles();
        HideAllAnnotations();

        SetupButtons(0);

        PanelLegend.SetActive(true);

        LegendTitle.SetActive(true);
    }

    public void ShowArrowButtons(bool active)
    {
        LeftArrowButton.SetActive(active);
        RightArrowButton.SetActive(active);

        Buttons.SetActive(active);
    }

    //public void ShowMaterialCycleButton(bool active)
    //{
    //    MaterialCycleButton.SetActive(active);
    //}

    //public void ShowCarbonCycleButton(bool active)
    //{
    //    CarbonCycleButton.SetActive(active);
    //}

    //public void ShowWaterCycleButton(bool active)
    //{
    //    WaterCycleButton.SetActive(active);
    //}

    //public void ShowRadiationBalanceButton(bool active)
    //{
    //    RadiationBalanceButton.SetActive(active);
    //}

    //■■■　シンプル・リアルモードの切り替えを押したときの挙動
    public void OnClick_Button_ViewChange()
    {
        //効果音
        audioSource.PlayOneShot(Sound_Kachi);

        // Directorインスタンスの表示モードを変更
        Director.Instance.ChangeViewMode();

        // ツリーアイテムの設定を更新
        SetupTreeItems();
    }

    //■■■　Button_Pauseを押したときの挙動
    public void Button_Pause_OnClick()
    {
        PlayButton.SetActive(true);
        PauseButton.SetActive(false);
        ReplayButton.SetActive(false);
        Director.SimYearMove = false;
        if (audioSource)
            audioSource.PlayOneShot(Sound_Piro); //効果音
    }

    //■■■　Button_Playを押したときの挙動
    public void Button_Play_OnClick()
    {
        PlayButton.SetActive(false);
        PauseButton.SetActive(true);
        ReplayButton.SetActive(false);
        Director.SimYearMove = true;
        if (audioSource)
            audioSource.PlayOneShot(Sound_Piro); //効果音

        //if (Director.SimYearMove == true)
        //{ Director.SimYearMove = false; }
        //else
        //{ Director.SimYearMove = true; }
    }

    public void Button_Replay_OnClick()
    {
        Director.SimYear = 1;
        Director.SimYearMove = true;
        Director.Instance.sliderYear.Init();

        PlayButton.SetActive(false);
        PauseButton.SetActive(true);
        ReplayButton.SetActive(false);
        if (audioSource)
            audioSource.PlayOneShot(Sound_Piro);
    }

    public void Button_A_OnClick()
    {
        //Debug.Log("Button A");

        Director.Instance.appState = Director.AppState.FunctionA;

        PanelRight.SetActive(false);

        Director.Instance.Invoke(nameof(Director.ReadFile), 0.5f);
    }

    public void Button_B_OnClick()
    {
        //Debug.Log("Button B");

        Director.Instance.appState = Director.AppState.FunctionB;

        PanelRight.SetActive(false);

        SEIBConnector.SetActive(true);
    }

    public void ShowConfirm(string message)
    {
        if (confirmOpen) return;

        confirmOpen = true;
        confirmText.text = message;
        confirmPanel.SetActive(true);
    }

    public void OnConfirmYes()
    {
        CloseConfirm();

        if (Director.Instance.appState == Director.AppState.TopMenu)
        {
            Director.Instance.QuitApp();
        }
        else
        {
            ShowTopMenu();
        }
    }

    public void ShowTopMenu()
    {
        Director.Instance.appState = Director.AppState.TopMenu;

        HideAllPanels();
        HideAllTitles();
        HideAllAnnotations();

        ShowArrowButtons(false);

        LeftSlider.SetActive(false);

        if (SEIBConnector.activeSelf)
        {
            SEIBConnector.GetComponent<SEIBConnector>().OnClickStopSimulation();

            SEIBConnector.SetActive(false);
        }

        PanelInfo.SetActive(true);

        PanelRight.SetActive(true);

        Director.Instance.ResetScene();

        Director.Instance.sliderYear.Init();
    }

    public void OnConfirmNo()
    {
        CloseConfirm();
    }

    void CloseConfirm()
    {
        confirmOpen = false;
        confirmPanel.SetActive(false);
    }

    public void UpdateInfoValues()
    {
        //Debug.Log("UpdateInfoValues: " + Director.LAT + " " + Director.LON + " " + Director.ALT);

        latString.Arguments = new string[] { Director.LAT.ToString("F2") };
        latString.RefreshString();

        lonString.Arguments = new string[] { Director.LON.ToString("F2") };
        lonString.RefreshString();

        altString.Arguments = new string[] { Director.ALT.ToString("F2") };
        altString.RefreshString();

        fieldCapString.Arguments = new string[] { Director.W_fi.ToString("F2") };
        fieldCapString.RefreshString();

        wiltPoiString.Arguments = new string[] { Director.W_wilt.ToString("F2") };
        wiltPoiString.RefreshString();

        albedoString.Arguments = new string[] { Director.Albedo_soil0.ToString("F2") };
        albedoString.RefreshString();
    }

    //■■■　入力データファール読んだ直後に、年間フラックス値を記録させる
    public void UpdateValues(
        float _fluxC_gpp, 
        float _fluxC_atr,
        float _fluxC_htr,
        float _fluxC_lit,
        float _fluxC_som,
        float _fluxW_pre,
        float _fluxW_ro1,
        float _fluxW_ro2,
        float _fluxW_ic,
        float _fluxW_ev,
        float _fluxW_tr,
        float _fluxW_sl,
        float _fluxW_tw,
        float _fluxW_sn,
        float _rad_short_direct_amean,
        float _rad_short_diffuse_amean,
        float _rad_short_up_amean,
        float _rad_long_down_amean,
        float _rad_long_up_amean
        )
    {
        int carbonScaleMax = Director.CarbonScaleMax;
        int waterScaleMax = Director.WaterScaleMax;
        int radiationScaleMax = Director.RadiationScaleMax;

        double carbonStep = carbonScaleMax / carbonUnits.Length;
        for (int i = 0; i < carbonUnits.Length; i++)
        {
            int value = (int)((i + 1) * carbonStep);
            carbonUnits[i].text = value.ToString();
        }

        double waterStep = waterScaleMax / waterUnits.Length;
        for (int i = 0; i < waterUnits.Length; i++)
        {
            int value = (int)((i + 1) * waterStep);
            waterUnits[i].text = value.ToString();
        }

        double radiationStep = radiationScaleMax / radiationUnits.Length;
        for (int i = 0; i < radiationUnits.Length; i++)
        {
            int value = (int)((i + 1) * radiationStep);
            radiationUnits[i].text = value.ToString();
        }

        fluxC_gpp.text = _fluxC_gpp.ToString("F1");
        fluxC_atr.text = _fluxC_atr.ToString("F1");
        fluxC_htr.text = _fluxC_htr.ToString("F1");
        fluxC_lit.text = _fluxC_lit.ToString("F1");
        fluxC_som.text = _fluxC_som.ToString("F1");

        arrow_fluxC_gpp.ScaleFactor = _fluxC_gpp / carbonScaleMax;
        arrow_fluxC_atr.ScaleFactor = _fluxC_atr / carbonScaleMax;
        arrow_fluxC_htr.ScaleFactor = _fluxC_htr / carbonScaleMax;
        arrow_fluxC_lit.ScaleFactor = _fluxC_lit / carbonScaleMax;
        arrow_fluxC_som.ScaleFactor = _fluxC_som / carbonScaleMax;

        float x = _fluxW_pre - _fluxW_ic - _fluxW_sn;
        float y = x + _fluxW_tw - _fluxW_ev - _fluxW_tr - _fluxW_ro1;

        fluxW_pre.text = ((int)(_fluxW_pre-_fluxW_sn)).ToString("");
        fluxW_ro1.text = ((int)_fluxW_ro1).ToString("");
        fluxW_ro2.text = ((int)_fluxW_ro2).ToString("");
        fluxW_ic.text = ((int)_fluxW_ic).ToString("");
        fluxW_ev.text = ((int)_fluxW_ev).ToString("");
        fluxW_tr.text = ((int)_fluxW_tr).ToString("");
        fluxW_sl.text = ((int)_fluxW_sl).ToString("");
        fluxW_tw.text = ((int)_fluxW_tw).ToString("");
        fluxW_sn.text  = ((int)_fluxW_sn).ToString("");
        yText.text = ((int)y).ToString("");

        arrow_fluxW_pre.ScaleFactor = (_fluxW_pre- _fluxW_sn) / waterScaleMax;
        arrow_fluxW_sn.ScaleFactor = _fluxW_sn / waterScaleMax;
        arrow_fluxW_ro1.ScaleFactor = _fluxW_ro1 / waterScaleMax;
        arrow_fluxW_ro2.ScaleFactor = _fluxW_ro2 / waterScaleMax;
        arrow_fluxW_ic.ScaleFactor = _fluxW_ic / waterScaleMax;
        arrow_fluxW_ev.ScaleFactor = _fluxW_ev / waterScaleMax;
        arrow_fluxW_tr.ScaleFactor = _fluxW_tr / waterScaleMax;
        arrow_fluxW_sl.ScaleFactor = _fluxW_sl / waterScaleMax;
        arrow_fluxW_tw.ScaleFactor = _fluxW_tw / waterScaleMax;
        arrow_x.ScaleFactor = x / waterScaleMax;
        arrow_y.ScaleFactor = y / waterScaleMax;

        rad_short_direct_amean.text = _rad_short_direct_amean.ToString("F0");
        rad_short_diffuse_amean.text = _rad_short_diffuse_amean.ToString("F0");
        rad_short_up_amean.text = _rad_short_up_amean.ToString("F0");
        rad_long_down_amean.text = _rad_long_down_amean.ToString("F0");
        rad_long_up_amean.text = _rad_long_up_amean.ToString("F0");

        arrow_rad_short_direct_amean.ScaleFactor = _rad_short_direct_amean / radiationScaleMax;
        arrow_rad_short_diffuse_amean.ScaleFactor = _rad_short_diffuse_amean / radiationScaleMax;
        arrow_rad_short_up_amean.ScaleFactor = _rad_short_up_amean / radiationScaleMax;
        arrow_rad_long_down_amean.ScaleFactor = _rad_long_down_amean / radiationScaleMax;
        arrow_rad_long_up_amean.ScaleFactor = _rad_long_up_amean / radiationScaleMax;
    }
}
