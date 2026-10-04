using System.Collections.Generic;
using UnityEngine;

public class BuildingManager : MonoBehaviour
{
    public static BuildingManager Instance { get; private set; }

    [Header("Building Order")]
    [SerializeField]
    private List<BuildingPart> buildingParts = new List<BuildingPart>();

    private int currentBuildIndex = 0;

    public event System.Action OnBuildingChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        InitializeBuilding();
    }

    private void InitializeBuilding()
    {
        currentBuildIndex = 0;

        foreach (BuildingPart part in buildingParts)
        {
            if (part == null)
                continue;

            bool isBuilt = false;

            if (BuildingState.Instance != null)
            {
                isBuilt = BuildingState.Instance.IsBuilt(part.id);
            }

            part.isBuilt = isBuilt;

            if (part.previewObject != null)
                part.previewObject.SetActive(!isBuilt);

            if (part.finalObject != null)
                part.finalObject.SetActive(isBuilt);

            if (!isBuilt && currentBuildIndex == 0)
            {
                currentBuildIndex =
                    buildingParts.IndexOf(part);
            }
        }

        // si todas las paredes estan construidas
        if (currentBuildIndex == 0 &&
            buildingParts.Count > 0 &&
            buildingParts.TrueForAll(part =>
                part == null || part.isBuilt))
        {
            currentBuildIndex = buildingParts.Count;
        }

        RestoreRoofVisibility();
        OnBuildingChanged?.Invoke();
    }

    public void TryBuildNext()
    {
        if (buildingParts.Count == 0)
        {
            Debug.LogWarning(
                "BuildingManager: No hay partes de construcción configuradas."
            );

            return;
        }

        if (currentBuildIndex >= buildingParts.Count)
        {
            Debug.Log(
                "BuildingManager: La construcción ya está completa."
            );

            return;
        }

        BuildingPart part = buildingParts[currentBuildIndex];

        if (part == null)
        {
            Debug.LogWarning(
                $"BuildingManager: La parte {currentBuildIndex} es NULL."
            );

            return;
        }

        if (part.isBuilt)
        {
            MoveToNextUnbuiltPart();
            return;
        }

        if (!HasEnoughMaterials(part))
        {
            Debug.Log(
                $"No hay suficientes materiales para construir {part.id}. " +
                "Debe completarse esta parte antes de continuar."
            );

            return;
        }

        BuildPart(part);
    }

    private void BuildPart(BuildingPart part)
    {
        foreach (BuildingCost cost in part.costs)
        {
            BuildingInventory.Instance.RemoveItem(
                cost.item,
                cost.amount
            );
        }

        if (part.previewObject != null)
            part.previewObject.SetActive(false);

        if (part.finalObject != null)
            part.finalObject.SetActive(true);

        part.isBuilt = true;

        if (BuildingState.Instance != null)
        {
            BuildingState.Instance.SetBuilt(
                part.id,
                true
            );
        }

        Debug.Log($"CONSTRUIDO: {part.id}");

        MoveToNextUnbuiltPart();

        OnBuildingChanged?.Invoke();

        if (currentBuildIndex >= buildingParts.Count)
        {
            Debug.Log("CONSTRUCCION COMPLETADA.");
        }
        else
        {
            BuildingPart nextPart =
                buildingParts[currentBuildIndex];

            if (nextPart != null)
            {
                Debug.Log(
                    $"Siguiente construccion: {nextPart.id}"
                );
            }
        }
    }

    private void MoveToNextUnbuiltPart()
    {
        for (int i = 0; i < buildingParts.Count; i++)
        {
            BuildingPart part = buildingParts[i];

            if (part == null)
                continue;

            if (!part.isBuilt)
            {
                currentBuildIndex = i;
                return;
            }
        }

        currentBuildIndex = buildingParts.Count;
    }

    public bool CanBuildNext()
    {
        if (currentBuildIndex >= buildingParts.Count)
            return false;

        BuildingPart part =
            buildingParts[currentBuildIndex];

        if (part == null)
            return false;

        return HasEnoughMaterials(part);
    }

    private bool HasEnoughMaterials(BuildingPart part)
    {
        if (BuildingInventory.Instance == null)
        {
            Debug.LogError(
                "BuildingManager: No existe BuildingInventory."
            );

            return false;
        }

        if (part.costs == null || part.costs.Count == 0)
        {
            Debug.LogWarning(
                $"BuildingManager: {part.id} no tiene costos configurados."
            );

            return false;
        }

        foreach (BuildingCost cost in part.costs)
        {
            if (cost == null || cost.item == null)
            {
                Debug.LogWarning(
                    $"BuildingManager: {part.id} tiene un costo invalido."
                );

                return false;
            }

            if (cost.amount <= 0)
            {
                Debug.LogWarning(
                    $"BuildingManager: {part.id} tiene un costo invalido."
                );

                return false;
            }

            if (!BuildingInventory.Instance.HasItem(
                    cost.item,
                    cost.amount))
            {
                Debug.Log(
                    $"{part.id}: falta {cost.item.itemName}. " +
                    $"Necesita {cost.amount}, tiene " +
                    $"{BuildingInventory.Instance.GetItemCount(cost.item)}."
                );

                return false;
            }
        }

        return true;
    }

    public BuildingPart GetCurrentPart()
    {
        if (currentBuildIndex >= buildingParts.Count)
            return null;

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
        return currentBuildIndex >= buildingParts.Count;
    }

    public bool IsRoofBuilt()
    {
        foreach (BuildingPart part in buildingParts)
        {
            if (part != null && part.isRoof)
                return part.isBuilt;
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

            bool isCurrentlyVisible =
                part.finalObject.activeSelf;

            bool newVisibility = !isCurrentlyVisible;

            part.finalObject.SetActive(newVisibility);

            if (BuildingState.Instance != null)
            {
                BuildingState.Instance.SetRoofVisible(
                    newVisibility
                );
            }

            Debug.Log(
                $"Roof {(newVisibility ? "VISIBLE" : "OCULTO")}"
            );

            OnBuildingChanged?.Invoke();

            return;
        }
    }

    private void RestoreRoofVisibility()
    {
        if (BuildingState.Instance == null)
            return;

        bool roofVisible =
            BuildingState.Instance.IsRoofVisible();

        foreach (BuildingPart part in buildingParts)
        {
            if (part == null || !part.isRoof)
                continue;

            if (!part.isBuilt || part.finalObject == null)
                return;

            part.finalObject.SetActive(roofVisible);

            return;
        }
    }

    public bool IsRoofVisible()
    {
        foreach (BuildingPart part in buildingParts)
        {
            if (part == null || !part.isRoof)
                continue;

            if (!part.isBuilt || part.finalObject == null)
                return false;

            return part.finalObject.activeSelf;
        }

        return false;
    }
}