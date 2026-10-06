using UnityEngine;
using UnityEngine.UIElements;

public class DialogueUI : MonoBehaviour
{
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
        portraitContainer.pickingMode = PickingMode.Ignore;

        continueButton = root.Q<Button>("ContinueButton");

        continueButton.clicked += OnContinueClicked;
    }

    private void OnContinueClicked()
    {
        DialogueManager.Instance.NextLine();
    }

    public void ShowLine(DialogueLine line)
    {
        characterName.text = line.character.characterName;
        dialogueText.text = line.text;

        portrait.style.backgroundImage =
            new StyleBackground(line.character.normalPortrait);

        float scale = line.character.portraitScale;

        // si el que habla es el jugador
        if (line.character.speakerSide == DialogueSpeakerSide.Player)
        {
            dialogueBox.style.left = Length.Percent(17);
            dialogueBox.style.right = Length.Percent(7);

            portraitContainer.style.left = Length.Percent(7);
            portraitContainer.style.right = StyleKeyword.Auto;

            portrait.style.scale =
                new Scale(new Vector3(scale, scale, 1));
        }
        else
        {
            dialogueBox.style.left = Length.Percent(7);
            dialogueBox.style.right = Length.Percent(17);

            portraitContainer.style.left = StyleKeyword.Auto;
            portraitContainer.style.right = Length.Percent(6);

            // flip horizontal
            portrait.style.scale =
                new Scale(new Vector3(-scale, scale, 1));
        }
    }

    public void Hide()
    {
        uiDocument.rootVisualElement.style.display =
            DisplayStyle.None;
    }

    public void Show()
    {
        uiDocument.rootVisualElement.style.display =
            DisplayStyle.Flex;
    }
}