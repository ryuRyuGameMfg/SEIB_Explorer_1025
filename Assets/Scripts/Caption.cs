using System;

[Serializable]
public class Caption
{
    public string loc;
    public string jp;
    public string eg;
}

[Serializable]
public class CaptionCollection
{
    public Caption[] captions;
}

