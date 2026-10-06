using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public DialogueCharacter character;
    [TextArea(2, 5)]
    public string text;
    public float textSpeed = 0.03f;
}