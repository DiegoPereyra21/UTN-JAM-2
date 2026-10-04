using System;
using System.Collections.Generic;
using UnityEngine;

public class BuildingInventory : MonoBehaviour
{
    public static BuildingInventory Instance { get; private set; }

    private Dictionary<BuildingItemData, int> items =
        new Dictionary<BuildingItemData, int>();

    public IReadOnlyDictionary<BuildingItemData, int> Items => items;
    public event Action OnInventoryChanged;

    // test
    [Header("Test Inventory")]
    [SerializeField] private List<BuildingItemData> testItems = new List<BuildingItemData>();

    [SerializeField] private int testAmount = 50;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddItem(BuildingItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return;

        if (items.ContainsKey(item))
            items[item] += amount;
        else
            items.Add(item, amount);

        Debug.Log($"Added {amount}x {item.itemName}. Total: {items[item]}");

        OnInventoryChanged?.Invoke();
    }

    public bool HasItem(BuildingItemData item, int amount = 1)
    {
        if (item == null || amount <= 0)
            return false;

        return items.ContainsKey(item) && items[item] >= amount;
    }

    public int GetItemCount(BuildingItemData item)
    {
        if (item == null)
            return 0;

        if (items.TryGetValue(item, out int amount))
            return amount;

        return 0;
    }

    public bool RemoveItem(BuildingItemData item, int amount = 1)
    {
        if (!HasItem(item, amount))
            return false;

        items[item] -= amount;

        if (items[item] <= 0)
            items.Remove(item);

        OnInventoryChanged?.Invoke();

        return true;
    }

    public void ClearInventory()
    {
        items.Clear();

        OnInventoryChanged?.Invoke();
    }

    // test
    [ContextMenu("Load Test Inventory")]
    private void LoadTestInventory()
    {
        foreach (BuildingItemData item in testItems)
        {
            if (item == null)
                continue;

            AddItem(item, testAmount);
        }

        Debug.Log($"Inventario de prueba cargado: {testAmount} de cada material.");
    }
}