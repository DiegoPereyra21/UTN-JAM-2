using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private MainMenuCharacter menuCharacter;
    [SerializeField] private string nextSceneName;

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
        menuCharacter.StartDrink();
    }

    private void OnWalkingStarted()
    {
        StartCoroutine(LoadNextScene());
    }

    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(3.5f);

        SceneManager.LoadScene(nextSceneName);
    }
}