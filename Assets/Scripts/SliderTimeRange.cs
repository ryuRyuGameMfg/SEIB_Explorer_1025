using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;

public class SliderTimeRange : MonoBehaviour
{
    public Slider slider;

    public Text textComponent;
    public LocalizedString localString;

    void Start()
    {
        slider.value = 1;
        slider.minValue = 1;
        slider.maxValue = Director.SimTimeRangeMax;

        slider.onValueChanged.AddListener(UpdateValue);
    }

    private void OnEnable()
    {
        localString.Arguments = new string[] { slider.value.ToString() };
        localString.StringChanged += UpdateText;
    }

    private void OnDisable()
    {
        localString.StringChanged -= UpdateText;
    }

    void UpdateText(string value)
    {
        textComponent.text = value;
    }

    public void Init()
    {
        slider.value = 1;
        slider.maxValue = Director.SimTimeRangeMax;
    }

    //キー入力による値変更を反映させるための受信機
    public void ChangeValue()
    {
        slider.value = Director.SimTimeRange;
    }

    void UpdateValue(float value)
    {
        Director.SimTimeRange = (int)value;

        if (localString != null)
        {
            localString.Arguments[0] = Director.SimTimeRange.ToString();
            localString.RefreshString();
        }
    }
}
