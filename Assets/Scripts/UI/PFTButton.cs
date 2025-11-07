using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PFTButton
{
    public string categoryName;   // e.g. "PFTa"
    public Button button;         // assign in Inspector
    public Color enabledColor;    // unique color per button
}