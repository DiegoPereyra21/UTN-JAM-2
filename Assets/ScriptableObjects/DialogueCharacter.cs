using UnityEngine;

public enum DialogueSpeakerSide
{
    Player,
    Other
}

[CreateAssetMenu(menuName = "Dialogue/Character")]
public class DialogueCharacter : ScriptableObject
{
    public string characterName;
    public Color nameColor = Color.white;
    public DialogueSpeakerSide speakerSide;
    public Sprite normalPortrait;
    public Sprite happyPortrait;
    public Sprite angryPortrait;

    [Range(0.5f, 2f)]
    public float portraitScale = 1f;
}