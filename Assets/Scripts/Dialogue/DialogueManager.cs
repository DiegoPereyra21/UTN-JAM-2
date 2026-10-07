using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }
    public static event System.Action OnDialogueStarted;
    public static event System.Action OnDialogueEnded;

    public DialogueData CurrentDialogue => currentDialogue;
    public bool IsTutorialDialogue { get; private set; }

    [Header("UI")]
    [SerializeField] private DialogueUI dialogueUI;

    private DialogueData currentDialogue;
    
    private int currentLineIndex;
    private bool isChangingDialogue;
    private bool transitionToNextDialogue;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);

        if (dialogueUI == null)
        {
            Debug.LogError("DialogueManager: No existe DialogueUI.");
            return;
        }

        dialogueUI.Hide();
    }

    private void Update()
    {
        if (currentDialogue == null)
            return;

        if (isChangingDialogue)
            return;

        if (dialogueUI.IsTransitioning)
            return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            NextLine();
        }
    }

    public void StartDialogue(DialogueData dialogue, bool isTutorial = false)
    {
        if (dialogue == null)
        {
            Debug.LogWarning("DialogueManager: Se intentó iniciar un diálogo NULL.");

            return;
        }

        if (dialogue.lines == null || dialogue.lines.Count == 0)
        {
            Debug.LogWarning("DialogueManager: El diálogo no tiene líneas.");

            return;
        }

        IsTutorialDialogue = isTutorial;

        if (transitionToNextDialogue)
        {
            transitionToNextDialogue = false;

            StartCoroutine(TransitionToDialogue(dialogue));

            return;
        }

        StartDialogueInternal(dialogue);
    }

    private void StartDialogueInternal(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        currentLineIndex = 0;

        isChangingDialogue = false;

        OnDialogueStarted?.Invoke();

        dialogueUI.Show();

        ShowCurrentLine();
    }

    private System.Collections.IEnumerator TransitionToDialogue(DialogueData nextDialogue)
    {
        if (isChangingDialogue)
            yield break;

        isChangingDialogue = true;

        // fade out del dialogo anterior
        yield return dialogueUI.FadeOut();

        // pequenha pausa
        yield return dialogueUI.Pause();

        // siguiente dialogo
        currentDialogue = nextDialogue;
        currentLineIndex = 0;

        // carga la nueva linea mientras sigue invisible
        dialogueUI.Show();

        ShowCurrentLine();

        // fade in al nuevo dialogo
        yield return dialogueUI.FadeIn();

        isChangingDialogue = false;

        OnDialogueStarted?.Invoke();
    }

    private void ShowCurrentLine()
    {
        if (currentDialogue == null)
            return;

        if (currentLineIndex < 0 || currentLineIndex >= currentDialogue.lines.Count)
        {
            return;
        }

        DialogueLine line = currentDialogue.lines[currentLineIndex];

        dialogueUI.ShowLine(line);
    }

    public void NextLine()
    {
        if (currentDialogue == null)
            return;

        if (isChangingDialogue)
            return;

        if (dialogueUI.IsTransitioning)
            return;

        currentLineIndex++;

        if (currentLineIndex < currentDialogue.lines.Count)
        {
            ShowCurrentLine();
            return;
        }

        EndDialogue();
    }

    private void EndDialogue()
    {
        if (isChangingDialogue)
            return;

        dialogueUI.Hide();

        currentDialogue = null;
        currentLineIndex = 0;

        IsTutorialDialogue = false;

        transitionToNextDialogue = true;

        OnDialogueEnded?.Invoke();
    }
}