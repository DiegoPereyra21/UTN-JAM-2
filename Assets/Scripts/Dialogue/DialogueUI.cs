using UnityEngine;
using UnityEngine.UIElements;

public class DialogueUI : MonoBehaviour
{
    [Header("Transiciones")]
    [SerializeField] private float fadeDuration = 0.15f;
    [SerializeField] private float pauseDuration = 0.08f;

    [Header("Tutorial")]
    [SerializeField] private float tutorialDialogueRight = 22f;
    [SerializeField] private Color tutorialDialogueColor;

    public bool IsTransitioning { get; private set; }

    private UIDocument uiDocument;
    private Label characterName;
    private Label dialogueText;
    private VisualElement portrait;
    private VisualElement portraitContainer;
    private Button continueButton;
    private VisualElement dialogueBox;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();

        VisualElement root = uiDocument.rootVisualElement;

        characterName = root.Q<Label>("CharacterName");

        dialogueText = root.Q<Label>("DialogueText");

        dialogueBox = root.Q<VisualElement>("DialogueBox");

        portraitContainer = root.Q<VisualElement>("PortraitContainer");

        portrait = root.Q<VisualElement>("Portrait");

        if (portraitContainer != null)
        {
            portraitContainer.pickingMode = PickingMode.Ignore;
        }

        continueButton = root.Q<Button>("ContinueButton");

        if (continueButton != null)
        {
            continueButton.clicked += OnContinueClicked;
        }
    }

    private void OnContinueClicked()
    {
        if (IsTransitioning)
            return;

        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.NextLine();
        }
    }

    public void ShowLine(DialogueLine line)
    {
        if (line == null)
            return;

        ApplyLine(line);
    }

    private void ApplyLine(DialogueLine line)
    {
        //excepcion de lado del portrait para el tutorial
        bool isTutorial = DialogueManager.Instance != null && DialogueManager.Instance.IsTutorialDialogue;

        if (characterName != null && line.character != null)
        {
            characterName.text = line.character.characterName;

            characterName.style.color = line.character.nameColor;
        }

        if (dialogueText != null)
        {
            dialogueText.text = line.text;
        }

        if (portrait != null && line.character != null)
        {
            portrait.style.backgroundImage = new StyleBackground(line.character.normalPortrait);

            float scale = line.character.portraitScale;

            if (isTutorial || line.character.speakerSide == DialogueSpeakerSide.Player)
            {
                if (dialogueBox != null) // ajustes para el tutorial
                {
                    dialogueBox.style.left = Length.Percent(isTutorial ? 16 : 17);

                    dialogueBox.style.right = Length.Percent(isTutorial ? tutorialDialogueRight : 7 );
                }

                if (portraitContainer != null)
                {
                    portraitContainer.style.left = Length.Percent(isTutorial ? 5 : 8);

                    portraitContainer.style.right = StyleKeyword.Auto;
                }

                if (isTutorial)
                {
                    dialogueBox.style.backgroundColor = tutorialDialogueColor;
                }
                else
                {
                    dialogueBox.style.backgroundColor = StyleKeyword.Null;
                }

                portrait.style.scale = new Scale(new Vector3(scale, scale, 1));
            }
            else
            {
                if (dialogueBox != null)
                {
                    dialogueBox.style.left = Length.Percent(7);

                    dialogueBox.style.right = Length.Percent(17);
                }

                if (portraitContainer != null)
                {
                    portraitContainer.style.left = StyleKeyword.Auto;

                    portraitContainer.style.right = Length.Percent(6);
                }

                portrait.style.scale = new Scale(new Vector3(-scale, scale, 1));
            }
        }

        if (BuildingHUD.Instance != null)
        {
            BuildingHUD.Instance.ShowTutorialIndicator(line.indicator);
        }
    }

    public void Show()
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;

        SetOpacity(1f);

        IsTransitioning = false;
    }

    public void Hide()
    {
        uiDocument.rootVisualElement.style.display = DisplayStyle.None;

        SetOpacity(1f);

        IsTransitioning = false;
    }

    public System.Collections.IEnumerator FadeOut()
    {
        IsTransitioning = true;

        yield return Fade(1f, 0f);
    }

    public System.Collections.IEnumerator FadeIn()
    {
        yield return Fade(0f, 1f);

        IsTransitioning = false;
    }

    public System.Collections.IEnumerator Pause()
    {
        yield return new WaitForSeconds(pauseDuration);
    }

    private System.Collections.IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        SetOpacity(from);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / fadeDuration);

            float opacity = Mathf.Lerp(from, to, t);

            SetOpacity(opacity);

            yield return null;
        }

        SetOpacity(to);
    }

    private void SetOpacity(float opacity)
    {
        if (uiDocument == null)
            return;

        uiDocument.rootVisualElement.style.opacity = opacity;
    }
}