using UnityEngine;

[System.Serializable]

public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(2, 5)]
    public string text;

    public DialogueIndicator indicator;
    public float textSpeed = 0.03f;
}

public enum DialogueIndicator
{
    None,
    Resources,
    Build,
    Map,
    Inventory,
    Roof
}