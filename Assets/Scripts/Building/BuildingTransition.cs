using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingTransition : MonoBehaviour
{
    public static BuildingTransition Instance { get; private set; }

    [Header("Transition")]
    [SerializeField] private float fadeDuration = 0.25f;

    private UIDocument uiDocument;
    private VisualElement fade;

    private bool isTransitioning;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError(
                "BuildingTransition: No se encontró UIDocument."
            );

            return;
        }
    }

    private void OnEnable()
    {
        if (uiDocument == null)
            return;

        fade = uiDocument.rootVisualElement.Q<VisualElement>("Fade");

        if (fade == null)
            return;
        
        HideFade();
    }

    private void HideFade()
    {
        fade.style.opacity = 0f;
        fade.style.display = DisplayStyle.None;
    }

    public void PlayTransition(Action action)
    {
        if (isTransitioning)
            return;

        if (fade == null)
        {
            Debug.LogError(
                "BuildingTransition: Fade no esta inicializado."
            );

            return;
        }

        StartCoroutine(TransitionRoutine(action));
    }

    private IEnumerator TransitionRoutine(Action action)
    {
        isTransitioning = true;

        fade.style.display = DisplayStyle.Flex;

        yield return Fade(0f, 1f);

        action?.Invoke();

        yield return Fade(1f, 0f);

        fade.style.display = DisplayStyle.None;

        isTransitioning = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        float elapsed = 0f;

        fade.style.opacity = from;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / fadeDuration;
            t = Mathf.Clamp01(t);

            fade.style.opacity =
                Mathf.Lerp(from, to, t);

            yield return null;
        }

        fade.style.opacity = to;
    }

    public bool IsTransitioning()
    {
        return isTransitioning;
    }
}