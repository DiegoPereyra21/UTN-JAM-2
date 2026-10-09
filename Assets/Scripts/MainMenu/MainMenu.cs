using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    [Header("Personaje del menu")]
    [SerializeField] private MainMenuCharacter menuCharacter;

    [Header("Nombre de escena inicial")]
    [SerializeField] private string nextSceneName;

    [Header("Duracion de caminata del personaje")]
    [SerializeField] private float walkDuration = 6f;

    private Button startButton;

    private void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        startButton = root.Q<Button>("StartButton");

        startButton.clicked += OnStartClicked;
        menuCharacter.WalkingStarted += OnWalkingStarted;
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.clicked -= OnStartClicked;

        if (menuCharacter != null)
            menuCharacter.WalkingStarted -= OnWalkingStarted;
    }

    private void OnStartClicked()
    {
        startButton.SetEnabled(false);

        GameManager.Instance.NewRun();

        menuCharacter.StartDrink();
    }

    private void OnWalkingStarted()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(walkDuration);

        SceneManager.LoadScene(nextSceneName);
    }
}