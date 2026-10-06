using UnityEngine;
using UnityEngine.InputSystem;

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

        dialogueUI.Hide();
    }

    private void Update()
    {
        //si no hay dialogo en curso no hace nada
        if (currentDialogue == null) return;

        //E pasa a la siguiente linea, igual que el boton
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            NextLine();
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
        //por si se llama sin dialogo activo
        if (currentDialogue == null) return;

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