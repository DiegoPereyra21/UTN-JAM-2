using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }
    public event System.Action OnBuildingChanged;

    [Header("Building Order")]
    [SerializeField] private List<BuildingPart> buildingParts = new List<BuildingPart>();

    [Header("Dialogues")]
    [SerializeField] private DialogueData introDialogue;
    [SerializeField] private DialogueData finalDialogue;
    [SerializeField] private DialogueData emptyDialogue;
    [SerializeField] private DialogueData tutorialDialogue;
    [SerializeField] private DialogueData insufficientMaterialsDialogue;

    private int currentBuildIndex = -1;
    private bool waitingForDialogue;
    private bool dialogueUnlocksNextPart;
    private DialogueManager dialogueManager;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueEnded += OnDialogueEnded;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueEnded -= OnDialogueEnded;
    }

    private void Start()
    {
        dialogueManager = DialogueManager.Instance;

        if (dialogueManager == null)
        {
            Debug.LogError("BuildingManager: No existe DialogueManager.");
        }

        InitializeBuilding();
    }

    private void InitializeBuilding()
    {
        currentBuildIndex = -1;

        // restauramos visualmente todas las partes
        for (int i = 0; i < buildingParts.Count; i++)
        {
            BuildingPart part = buildingParts[i];

            if (part == null)
                continue;

            bool isBuilt = false;

            if (BuildingState.Instance != null)
            {
                isBuilt = BuildingState.Instance.IsBuilt(part.id);
            }

            part.isBuilt = isBuilt;

            // se muestra la version final de la parte
            if (part.finalObject != null)
            {
                part.finalObject.SetActive(isBuilt);
            }

            // no se muestran partes no construidas
            if (part.previewObject != null)
            {
                part.previewObject.SetActive(false);
            }
        }

        RestoreRoofVisibility();

        if (BuildingState.Instance == null)
        {
            Debug.LogError("BuildingManager: No existe BuildingState.");

            return;
        }

        // la primera vez que empieza la construcción.
        if (!BuildingState.Instance.HasBuildingStarted())
        {
            StartIntroDialogue();
            return;
        }

        // ya hay construccion empezada, buscamos la primera parte no construida
        UnlockNextPart();

        OnBuildingChanged?.Invoke();
    }

    private void StartIntroDialogue()
    {
        waitingForDialogue = true;
        dialogueUnlocksNextPart = true;

        DialogueData dialogue = GetDialogueOrFallback(introDialogue);

        if (dialogue == null)
        {
            Debug.LogWarning("BuildingManager: No hay diálogo inicial válido. " + "Se continuará directamente.");

            waitingForDialogue = false;
            dialogueUnlocksNextPart = false;

            BuildingState.Instance.SetBuildingStarted(true);

            UnlockNextPart();

            OnBuildingChanged?.Invoke();

            return;
        }

        if (dialogueManager == null)
        {
            waitingForDialogue = false;
            dialogueUnlocksNextPart = false;

            BuildingState.Instance.SetBuildingStarted(true);

            UnlockNextPart();

            OnBuildingChanged?.Invoke();

            return;
        }

        dialogueManager.StartDialogue(dialogue);
    }

    private void StartBuildDialogue(BuildingPart part)
    {
        waitingForDialogue = true;
        dialogueUnlocksNextPart = true;

        DialogueData dialogue = GetDialogueOrFallback(part.dialogueAfterBuild);

        if (dialogue == null)
        {
            Debug.LogWarning($"BuildingManager: {part.id} no tiene diálogo válido. " + "Se continuará directamente.");

            waitingForDialogue = false;
            dialogueUnlocksNextPart = false;

            UnlockNextPart();

            OnBuildingChanged?.Invoke();

            return;
        }

        if (dialogueManager == null)
        {
            waitingForDialogue = false;
            dialogueUnlocksNextPart = false;

            UnlockNextPart();

            OnBuildingChanged?.Invoke();

            return;
        }

        dialogueManager.StartDialogue(dialogue);
    }

    private void StartFinalDialogue()
    {
        waitingForDialogue = true;
        dialogueUnlocksNextPart = false;

        DialogueData dialogue = GetDialogueOrFallback(finalDialogue);

        if (dialogue == null)
        {
            waitingForDialogue = false;

            OnBuildingChanged?.Invoke();

            return;
        }

        if (dialogueManager == null)
        {
            waitingForDialogue = false;

            OnBuildingChanged?.Invoke();

            return;
        }

        dialogueManager.StartDialogue(dialogue);
    }

    private void OnDialogueEnded()
    {
        if (!waitingForDialogue)
            return;

        waitingForDialogue = false;

        if (!dialogueUnlocksNextPart)
        {
            OnBuildingChanged?.Invoke();

            return;
        }

        dialogueUnlocksNextPart = false;

        // primer dialogo
        if (BuildingState.Instance != null && !BuildingState.Instance.HasBuildingStarted())
        {
            BuildingState.Instance.SetBuildingStarted(true);

            GiveStartingMaterials();

            StartTutorialDialogue();

            return;
        }

        UnlockNextPart();

        OnBuildingChanged?.Invoke();
    }

    private DialogueData GetDialogueOrFallback(DialogueData dialogue)
    {
        if (dialogue != null)
            return dialogue;

        if (emptyDialogue != null)
        {
            return emptyDialogue;
        }

        return null;
    }

    // desbloqueo de partes
    private void UnlockNextPart()
    {
        currentBuildIndex = -1;

        for (int i = 0; i < buildingParts.Count; i++)
        {
            BuildingPart part = buildingParts[i];

            if (part == null)
                continue;

            if (!part.isBuilt)
            {
                currentBuildIndex = i;
                break;
            }
        }

        // ocultamos todos los previews
        for (int i = 0; i < buildingParts.Count; i++)
        {
            BuildingPart part = buildingParts[i];

            if (part == null)
                continue;

            if (part.previewObject != null)
            {
                part.previewObject.SetActive(false);
            }
        }

        // si no queda ninguna parte lanza el dialogo final
        if (currentBuildIndex == -1)
        {
            StartFinalDialogue();
            return;
        }

        // solamente la construcción actual queda desbloqueada
        BuildingPart currentPart = buildingParts[currentBuildIndex];

        if (currentPart.previewObject != null)
        {
            currentPart.previewObject.SetActive(true);
        }

        Debug.Log($"Construcción desbloqueada: {currentPart.id}");
    }

    public void TryBuildNext()
    {
        if (waitingForDialogue)
        {
            Debug.Log("BuildingManager: No se puede construir durante un diálogo.");

            return;
        }

        if (currentBuildIndex < 0 || currentBuildIndex >= buildingParts.Count)
        {
            Debug.Log("BuildingManager: No hay ninguna construcción disponible.");

            return;
        }

        BuildingPart part = buildingParts[currentBuildIndex];

        if (part == null)
        {
            Debug.LogWarning($"BuildingManager: La parte " + $"{currentBuildIndex} es NULL.");

            return;
        }

        if (part.isBuilt)
        {
            Debug.LogWarning($"BuildingManager: {part.id} ya está construida.");

            return;
        }

        if (!HasEnoughMaterials(part))
        {
            Debug.Log($"No hay suficientes materiales para construir {part.id}.");

            StartInsufficientMaterialsDialogue();

            return;
        }

        if (BuildingTransition.Instance == null)
        {
            Debug.LogError("BuildingManager: No existe BuildingTransition.");

            return;
        }

        BuildingTransition.Instance.PlayTransition(() => BuildPart(part));
    }

    private void BuildPart(BuildingPart part)
    {
        // se consumen los materiales mientras la pantalla está negra
        foreach (BuildingCost cost in part.costs)
        {
            BuildingInventory.Instance.RemoveItem(cost.item, cost.amount);
        }

        // cambiamos preview por objeto final
        if (part.previewObject != null)
        {
            part.previewObject.SetActive(false);
        }

        if (part.finalObject != null)
        {
            part.finalObject.SetActive(true);
        }

        part.isBuilt = true;

        // guardamos el progreso
        if (BuildingState.Instance != null)
        {
            BuildingState.Instance.SetBuilt(part.id, true);
        }

        OnBuildingChanged?.Invoke();

        StartBuildDialogue(part);
    }

    private bool HasEnoughMaterials(
        BuildingPart part)
    {
        if (BuildingInventory.Instance == null)
        {
            Debug.LogError(
                "BuildingManager: No existe BuildingInventory.");

            return false;
        }

        if (part.costs == null || part.costs.Count == 0)
        {
            Debug.LogWarning($"BuildingManager: {part.id} " + "no tiene costos configurados.");

            return false;
        }

        foreach (BuildingCost cost in part.costs)
        {
            if (cost == null || cost.item == null)
            {
                Debug.LogWarning($"BuildingManager: {part.id} " + "tiene un costo inválido.");

                return false;
            }

            if (cost.amount <= 0)
            {
                Debug.LogWarning($"BuildingManager: {part.id} " + "tiene un costo inválido.");

                return false;
            }

            if (!BuildingInventory.Instance.HasItem(cost.item, cost.amount))
            {
                Debug.Log($"{part.id}: falta {cost.item.itemName}. " + $"Necesita {cost.amount}, tiene " + $"{BuildingInventory.Instance.GetItemCount(cost.item)}.");

                return false;
            }
        }

        return true;
    }

    public BuildingPart GetCurrentPart()
    {
        if (currentBuildIndex < 0 || currentBuildIndex >= buildingParts.Count)
        {
            return null;
        }

        return buildingParts[currentBuildIndex];
    }

    public int GetCurrentBuildIndex()
    {
        return currentBuildIndex;
    }

    public int GetTotalParts()
    {
        return buildingParts.Count;
    }

    public bool IsBuildingComplete()
    {
        return currentBuildIndex == -1 && AreAllPartsBuilt();
    }

    private bool AreAllPartsBuilt()
    {
        foreach (BuildingPart part in buildingParts)
        {
            if (part == null)
                continue;

            if (!part.isBuilt)
                return false;
        }

        return true;
    }

    public bool IsRoofBuilt()
    {
        foreach (BuildingPart part in buildingParts)
        {
            if (part != null && part.isRoof)
            {
                return part.isBuilt;
            }
        }

        return false;
    }

    public void ToggleRoof()
    {
        foreach (BuildingPart part in buildingParts)
        {
            if (part == null || !part.isRoof)
                continue;

            if (!part.isBuilt || part.finalObject == null)
                return;

            bool isCurrentlyVisible = part.finalObject.activeSelf;

            bool newVisibility = !isCurrentlyVisible;

            part.finalObject.SetActive(newVisibility);

            if (BuildingState.Instance != null)
            {
                BuildingState.Instance.SetRoofVisible(newVisibility);
            }

            Debug.Log($"Roof {(newVisibility ? "VISIBLE" : "OCULTO")}");

            OnBuildingChanged?.Invoke();

            return;
        }
    }

    private void RestoreRoofVisibility()
    {
        if (BuildingState.Instance == null)
            return;

        bool roofVisible = BuildingState.Instance.IsRoofVisible();

        foreach (BuildingPart part in buildingParts)
        {
            if (part == null || !part.isRoof)
            {
                continue;
            }

            if (!part.isBuilt || part.finalObject == null)
            {
                return;
            }

            part.finalObject.SetActive(roofVisible);

            return;
        }
    }

    public bool IsRoofVisible()
    {
        foreach (BuildingPart part in buildingParts)
        {
            if (part == null || !part.isRoof)
            {
                continue;
            }

            if (!part.isBuilt || part.finalObject == null)
            {
                return false;
            }

            return part.finalObject.activeSelf;
        }

        return false;
    }

    private void StartInsufficientMaterialsDialogue()
    {
        DialogueData dialogue = GetDialogueOrFallback(insufficientMaterialsDialogue);

        if (dialogue == null)
        {
            Debug.LogWarning("BuildingManager: No hay diálogo de materiales insuficientes.");

            return;
        }

        if (dialogueManager == null)
            return;

        dialogueManager.StartDialogue(dialogue);
    }

    // le damos materiales iniciales al jugador
    private void GiveStartingMaterials()
    {
        if (BuildingInventory.Instance == null)
        {
            Debug.LogError("BuildingManager: No existe BuildingInventory.");

            return;
        }

        if (buildingParts == null || buildingParts.Count == 0)
        {
            Debug.LogWarning("BuildingManager: No hay partes de construcción.");

            return;
        }

        BuildingPart firstPart = buildingParts[0];

        if (firstPart == null)
        {
            Debug.LogWarning("BuildingManager: La primera parte de construcción es NULL.");

            return;
        }

        if (firstPart.costs == null || firstPart.costs.Count == 0)
        {
            Debug.LogWarning($"BuildingManager: {firstPart.id} no tiene costos.");

            return;
        }

        foreach (BuildingCost cost in firstPart.costs)
        {
            if (cost == null || cost.item == null)
                continue;

            if (cost.amount <= 0)
                continue;

            BuildingInventory.Instance.AddItem(cost.item, cost.amount);

            Debug.Log($"Material inicial entregado: " + $"{cost.amount}x {cost.item.itemName}");
        }
    }

    private void StartTutorialDialogue()
    {
        waitingForDialogue = true;
        dialogueUnlocksNextPart = true;

        if (tutorialDialogue == null)
        {
            Debug.LogWarning("BuildingManager: No hay diálogo tutorial asignado.");

            waitingForDialogue = false;
            dialogueUnlocksNextPart = false;

            UnlockNextPart();
            OnBuildingChanged?.Invoke();

            return;
        }

        if (dialogueManager == null)
        {
            waitingForDialogue = false;
            dialogueUnlocksNextPart = false;

            UnlockNextPart();
            OnBuildingChanged?.Invoke();

            return;
        }

        dialogueManager.StartDialogue(
            tutorialDialogue, true
        );
    }
}