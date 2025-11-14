using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LocalizedStringを簡単に設定できるコンポーネント
/// LocalizationConfigから自動設定される
/// </summary>
public class LocalizedStringComponent : MonoBehaviour
{
    [SerializeField] private LocalizedString localizedString;
    private Text textComponent;
    private TextMeshProUGUI textMeshComponent;

    private void Awake()
    {
        textComponent = GetComponent<Text>();
        textMeshComponent = GetComponent<TextMeshProUGUI>();
    }

    private void OnEnable()
    {
        if (localizedString != null && localizedString.IsEmpty == false)
        {
            localizedString.Arguments = new string[] { };
            localizedString.StringChanged += UpdateText;
        }
    }

    private void OnDisable()
    {
        if (localizedString != null)
        {
            localizedString.StringChanged -= UpdateText;
        }
    }

    /// <summary>
    /// LocalizedStringを設定（エディタスクリプトから呼ばれる）
    /// </summary>
    public void SetLocalizedString(string tableCollectionName, string key, string[] arguments = null)
    {
        localizedString = new LocalizedString();
        localizedString.TableReference = tableCollectionName;
        localizedString.TableEntryReference = key;

        if (arguments != null && arguments.Length > 0)
        {
            localizedString.Arguments = arguments;
        }

        if (Application.isPlaying)
        {
            localizedString.StringChanged += UpdateText;
        }
    }

    private void UpdateText(string value)
    {
        if (textComponent != null)
        {
            textComponent.text = value;
        }
        else if (textMeshComponent != null)
        {
            textMeshComponent.text = value;
        }
    }
}

