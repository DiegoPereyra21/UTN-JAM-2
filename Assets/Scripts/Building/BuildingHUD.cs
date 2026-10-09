using UnityEngine;
using UnityEngine.UIElements;

public class BuildingHUD : MonoBehaviour
{
    public static BuildingHUD Instance { get; private set; }

    [Header("Tutorial Pulse")]
    [SerializeField] private float tutorialPulseIntensity = 0.1f;
    [SerializeField] private float tutorialPulseSpeed = 10f;

    private float tutorialPulse;
    private UIDocument uiDocument;
    private Button buildButton;
    private Button roofButton;
    private Button mapButton;
    private VisualElement roofBackground;
    private VisualElement buildRequirements;
    private VisualElement requirementsContainer;
    private VisualElement tutorialIndicator;
    private VisualElement inventoryTarget;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        uiDocument = GetComponent<UIDocument>();
    }

    private void Start()
    {
        if (BuildingManager.Instance != null)
        {
            BuildingManager.Instance.OnBuildingChanged += UpdateRoofButton;

            BuildingManager.Instance.OnBuildingChanged += UpdateBuildingRequirements;
        }

        if (BuildingInventory.Instance != null)
        {
            BuildingInventory.Instance.OnInventoryChanged += UpdateBuildingRequirements;
        }

        UpdateRoofButton();
        UpdateBuildingRequirements();
    }

    private void Update()
    {
        if (tutorialIndicator == null || tutorialIndicator.style.display != DisplayStyle.Flex)
            return;

        float pulse = (Mathf.Sin(Time.time * tutorialPulseSpeed) + 1f) / 2f;

        float scale = 1f + pulse * tutorialPulseIntensity;

        tutorialIndicator.style.scale =
            new Scale(
                new Vector3(
                    scale,
                    scale,
                    1f
                )
            );
    }

    private void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;

        // asignaciones generales
        buildButton = root.Q<Button>("BuildButton");

        roofButton = root.Q<Button>("RoofButton");

        mapButton = root.Q<Button>("MapButton");

        roofBackground = root.Q<VisualElement>("Background");

        buildRequirements = root.Q<VisualElement>("BuildRequirements");

        requirementsContainer = root.Q<VisualElement>("RequirementsContainer");

        tutorialIndicator = root.Q<VisualElement>("TutorialIndicator");

        inventoryTarget = root.Q<VisualElement>("InventoryButton");


        // indicador para el tutorial
        if (tutorialIndicator == null)
        {
            Debug.LogError(
                "BuildingHUD: No se encontró TutorialIndicator."
            );
        }
        else
        {
            tutorialIndicator.style.display =
                DisplayStyle.None;

            tutorialIndicator.pickingMode =
                PickingMode.Ignore;
        }

        // validaciones
        if (roofBackground == null)
        {
            Debug.LogError("BuildingHUD: No se encontró Background.");
        }

        if (buildRequirements == null)
        {
            Debug.LogError("BuildingHUD: No se encontró BuildRequirements.");
        }

        if (requirementsContainer == null)
        {
            Debug.LogError("BuildingHUD: No se encontró RequirementsContainer.");
        }

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

        DialogueManager.OnDialogueStarted += HideHUD;
        DialogueManager.OnDialogueEnded += ShowHUD;
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
        {
            BuildingManager.Instance.OnBuildingChanged -= UpdateRoofButton;

            BuildingManager.Instance.OnBuildingChanged -= UpdateBuildingRequirements;
        }

        if (BuildingInventory.Instance != null)
        {
            BuildingInventory.Instance.OnInventoryChanged -= UpdateBuildingRequirements;
        }

        DialogueManager.OnDialogueStarted -= HideHUD;
        DialogueManager.OnDialogueEnded -= ShowHUD;
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

    private void UpdateBuildingRequirements()
    {
        if (buildRequirements == null || requirementsContainer == null)
            return;

        // el panel de recursos siempre visible
        buildRequirements.style.display = DisplayStyle.Flex;

        requirementsContainer.Clear();

        if (BuildingManager.Instance == null)
            return;

        BuildingPart currentPart = BuildingManager.Instance.GetCurrentPart();

        // si no hay construccion disponible dejamos el panel visible y vacio
        if (currentPart == null)
            return;

        if (currentPart.costs == null || currentPart.costs.Count == 0)
            return;
        
        foreach (BuildingCost cost in currentPart.costs)
        {
            if (cost == null || cost.item == null)
            {
                continue;
            }

            CreateRequirementRow(cost);
        }
    }

    private void CreateRequirementRow(BuildingCost cost)
    {
        BuildingItemData item = cost.item;

        int requiredAmount = cost.amount;

        int currentAmount = 0;

        if (BuildingInventory.Instance != null)
        {
            currentAmount = BuildingInventory.Instance.GetItemCount(item);
        }

        bool hasEnough = currentAmount >= requiredAmount;

        VisualElement row = new VisualElement();

        row.AddToClassList("requirement");


        // icono
        VisualElement icon = new VisualElement();

        icon.AddToClassList("requirement-icon");

        if (item.icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(item.icon);
        }

        row.Add(icon);


        // nombre
        Label nameLabel = new Label(item.itemName);

        nameLabel.AddToClassList("requirement-name");

        row.Add(nameLabel);


        // cantidad
        Label amountLabel = new Label($"{currentAmount} / {requiredAmount}");

        amountLabel.AddToClassList("requirement-amount");

        if (hasEnough)
        {
            amountLabel.AddToClassList("requirement-enough");
        }
        else
        {
            amountLabel.AddToClassList("requirement-missing");
        }

        row.Add(amountLabel);

        requirementsContainer.Add(row);
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

        bool roofBuilt = BuildingManager.Instance.IsRoofBuilt();

        roofButton.SetEnabled(roofBuilt);

        if (roofBackground == null)
            return;

        if (!roofBuilt)
        {
            roofBackground.style.backgroundColor = Color.gray;

            return;
        }

        bool roofVisible = BuildingManager.Instance.IsRoofVisible();

        if (roofVisible)
        {
            roofBackground.style.backgroundColor = new Color(0.27f, 0.7f, 0.35f);
        }
        else
        {
            roofBackground.style.backgroundColor = new Color(0.75f, 0.27f, 0.27f);
        }
    }

    private void HideHUD()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsTutorialDialogue)
            return;

        if (buildRequirements != null)
        {
            buildRequirements.style.display = DisplayStyle.None;
        }

        if (roofButton != null)
        {
            roofButton.style.display = DisplayStyle.None;
        }

        VisualElement buttonsContainer = uiDocument.rootVisualElement.Q<VisualElement>("ButtonsContainer");

        if (buttonsContainer != null)
        {
            buttonsContainer.style.display = DisplayStyle.None;
        }
    }

    private void ShowHUD()
    {
        if (buildRequirements != null)
        {
            buildRequirements.style.display = DisplayStyle.Flex;
        }

        if (roofButton != null)
        {
            roofButton.style.display = DisplayStyle.Flex;
        }

        VisualElement buttonsContainer = uiDocument.rootVisualElement.Q<VisualElement>("ButtonsContainer");

        if (buttonsContainer != null)
        {
            buttonsContainer.style.display = DisplayStyle.Flex;
        }

        UpdateBuildingRequirements();
        UpdateRoofButton();

        HideTutorialIndicator();
    }

    public void ShowTutorialIndicator(DialogueIndicator indicator)
    {
        if (tutorialIndicator == null)
            return;

        if (indicator == DialogueIndicator.None)
        {
            HideTutorialIndicator();
            return;
        }

        VisualElement target = GetTutorialTarget(indicator);

        if (target == null)
        {
            HideTutorialIndicator();
            return;
        }

        tutorialIndicator.style.display = DisplayStyle.Flex;

        ConfigureTutorialIndicator(indicator, target);
    }

    public void HideTutorialIndicator()
    {
        if (tutorialIndicator == null)
            return;

        tutorialIndicator.style.display = DisplayStyle.None;
    }

    private VisualElement GetTutorialTarget(DialogueIndicator indicator)
    {
        switch (indicator)
        {
            case DialogueIndicator.Resources:
                return buildRequirements;

            case DialogueIndicator.Build:
                return buildButton;

            case DialogueIndicator.Map:
                return mapButton;

            case DialogueIndicator.Inventory:
                return inventoryTarget;

            case DialogueIndicator.Roof:
                return roofButton;

            case DialogueIndicator.None:
            default:
                return null;
        }
    }

    // valores de posicion para el indicador segun el target
    private void ConfigureTutorialIndicator(DialogueIndicator indicator, VisualElement target)
    {
        float rotation = 0f;

        float horizontalOffset = 0f;
        float verticalOffset = 10f;

        switch (indicator)
        {

            case DialogueIndicator.Resources:

                rotation = 0f;

                horizontalOffset = 0f;
                verticalOffset = 150f;

                break;

            case DialogueIndicator.Build:

                rotation = 180f;

                horizontalOffset = 0f;
                verticalOffset = -140f;

                break;

            case DialogueIndicator.Map:

                rotation = 180f;

                horizontalOffset = 0f;
                verticalOffset = -140f;

                break;

            case DialogueIndicator.Inventory:

                rotation = 180f;

                horizontalOffset = 0f;
                verticalOffset = -60f;

                break;

            case DialogueIndicator.Roof:

                rotation = 90f;

                horizontalOffset = -100f;
                verticalOffset = 1f;

                break;
        }

        tutorialIndicator.style.rotate = new Rotate(rotation);

        PositionTutorialIndicator(target, horizontalOffset, verticalOffset);
    }

    private void PositionTutorialIndicator(VisualElement target, float horizontalOffset, float verticalOffset)
    {
        if (target == null || tutorialIndicator == null)
            return;
        
        tutorialIndicator.schedule.Execute(() =>
        {
            VisualElement parent = tutorialIndicator.parent;

            if (parent == null)
                return;

            // centro del objetivo en coordenadas de mundo
            Vector2 targetCenter = target.worldBound.center;

            // convertimos el centro al espacio del padre del TutorialIndicator
            Vector2 localCenter = parent.WorldToLocal(targetCenter);

            float indicatorWidth = tutorialIndicator.resolvedStyle.width;

            float indicatorHeight = tutorialIndicator.resolvedStyle.height;

            if (float.IsNaN(indicatorWidth) || float.IsNaN(indicatorHeight))
                return;
            

            float x = localCenter.x - indicatorWidth / 2f;

            float y = localCenter.y - indicatorHeight / 2f;

            x += horizontalOffset;
            y += verticalOffset;

            tutorialIndicator.style.left = x;
            tutorialIndicator.style.top = y;

        }).ExecuteLater(2);
    }
}