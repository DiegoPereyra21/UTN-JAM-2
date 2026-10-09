using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    [Header("Escena del menu")]
    [SerializeField] private string menuSceneName = "MainMenu";

    [Header("Musica final")]
    [SerializeField] private AudioSource audioSource;

    [Header("Perfiles de itch.io")]
    [SerializeField] private string developerOneURL = "https://infuria.itch.io";
    [SerializeField] private string developerTwoURL = "https://diego2k-dev.itch.io";

    private UIDocument uiDocument;
    private VisualElement endGameScreen;
    private Button menuButton;
    private Button developerOneButton;
    private Button developerTwoButton;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        if (uiDocument == null)
        {
            Debug.LogError("EndGameUI: No se encontró UIDocument.");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        endGameScreen = root.Q<VisualElement>("EndGameScreen");
        developerOneButton = root.Q<Button>("DeveloperOneButton");
        developerTwoButton = root.Q<Button>("DeveloperTwoButton");
        menuButton = root.Q<Button>("MenuButton");

        if (endGameScreen == null)
            Debug.LogError("EndGameUI: No se encontró EndGameScreen en el UXML.");

        if (developerOneButton != null)
        {
            developerOneButton.clicked += OpenDeveloperOne;
        }
        else
        {
            Debug.LogError("EndGameUI: No se encontró DeveloperOneButton.");
        }

        if (developerTwoButton != null)
        {
            developerTwoButton.clicked += OpenDeveloperTwo;
        }
        else
        {
            Debug.LogError("EndGameUI: No se encontró DeveloperTwoButton.");
        }

        if (menuButton != null)
            menuButton.clicked += ReturnToMenu;

        Hide();
    }

    public void Show()
    {
        if (endGameScreen == null)
        {
            Debug.LogError("EndGameUI: No se puede mostrar la pantalla final.");
            return;
        }

        endGameScreen.style.display = DisplayStyle.Flex;

        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();

        Debug.Log("EndGameUI: Pantalla final mostrada.");
    }

    private void Hide()
    {
        if (endGameScreen != null)
            endGameScreen.style.display = DisplayStyle.None;
    }

    private void OpenDeveloperOne()
    {
        Application.OpenURL(developerOneURL);
    }

    private void OpenDeveloperTwo()
    {
        Application.OpenURL(developerTwoURL);
    }

    private void ReturnToMenu()
    {
        SceneManager.LoadScene(menuSceneName);
    }

    private void OnDestroy()
    {
        if (developerOneButton != null)
            developerOneButton.clicked -= OpenDeveloperOne;

        if (developerTwoButton != null)
            developerTwoButton.clicked -= OpenDeveloperTwo;

        if (menuButton != null)
            menuButton.clicked -= ReturnToMenu;
    }
}