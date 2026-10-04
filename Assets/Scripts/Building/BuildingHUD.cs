using UnityEngine;
using UnityEngine.UIElements;

public class BuildingHUD : MonoBehaviour
{
    private UIDocument uiDocument;

    private Button buildButton;
    private Button roofButton;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Update()
    {
        UpdateRoofButton();
    }

    private void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;

        buildButton = root.Q<Button>("BuildButton");
        roofButton = root.Q<Button>("RoofButton");

        if (buildButton != null)
        {
            buildButton.clicked += OnBuildClicked;
        }
        else
        {
            Debug.LogError("BuildingHUD: No se encontró BuildButton.");
        }

        if (roofButton != null)
        {
            roofButton.clicked += OnRoofClicked;
        }
        else
        {
            Debug.LogError("BuildingHUD: No se encontró RoofButton.");
        }

        if (BuildingManager.Instance != null)
            BuildingManager.Instance.OnBuildingChanged += UpdateRoofButton;

        UpdateRoofButton();
    }

    private void OnDisable()
    {
        if (buildButton != null)
        {
            buildButton.clicked -= OnBuildClicked;
        }

        if (roofButton != null)
        {
            roofButton.clicked -= OnRoofClicked;
        }

        if (BuildingManager.Instance != null)
            BuildingManager.Instance.OnBuildingChanged -= UpdateRoofButton;
    }

    private void OnBuildClicked()
    {
        if (BuildingManager.Instance == null)
        {
            Debug.LogError("BuildingHUD: No existe BuildingManager.");
            return;
        }

        BuildingManager.Instance.TryBuildNext();
    }

    private void OnRoofClicked()
    {
        if (BuildingManager.Instance == null)
            return;

        BuildingManager.Instance.ToggleRoof();
    }

    private void UpdateRoofButton()
    {
        if (roofButton == null)
            return;

        if (BuildingManager.Instance == null)
        {
            roofButton.SetEnabled(false);
            return;
        }

        roofButton.SetEnabled(
            BuildingManager.Instance.IsRoofBuilt()
        );
    }
}