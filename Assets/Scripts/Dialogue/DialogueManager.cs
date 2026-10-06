using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [SerializeField] private DialogueUI dialogueUI;

    public static event System.Action OnDialogueStarted;
    public static event System.Action OnDialogueEnded;

    private DialogueData currentDialogue;
    private int currentLineIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        currentLineIndex = 0;

        OnDialogueStarted?.Invoke();

        dialogueUI.Show();

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = currentDialogue.lines[currentLineIndex];

        dialogueUI.ShowLine(line);
    }

    public void NextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Count)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void EndDialogue()
    {
        dialogueUI.Hide();

        OnDialogueEnded?.Invoke();

        currentDialogue = null;
        currentLineIndex = 0;
    }
}