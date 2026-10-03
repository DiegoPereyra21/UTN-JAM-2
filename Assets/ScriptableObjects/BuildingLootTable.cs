using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBuildingLootTable",
    menuName = "Game/Building Loot Table"
)]
public class BuildingLootTable : ScriptableObject
{
    [Serializable]
    public class LootEntry
    {
        public BuildingItemData item;

        [Range(0f, 100f)]
        public float dropChance;
    }

    [SerializeField]
    private List<LootEntry> entries = new List<LootEntry>();

    public BuildingItemData GetRandomItem()
    {
        float totalChance = 0f;

        foreach (LootEntry entry in entries)
        {
            if (entry.item != null)
                totalChance += entry.dropChance;
        }

        if (totalChance <= 0f)
            return null;

        float randomValue = UnityEngine.Random.Range(0f, totalChance);

        foreach (LootEntry entry in entries)
        {
            if (entry.item == null)
                continue;

            randomValue -= entry.dropChance;

            if (randomValue <= 0f)
                return entry.item;
        }

        return null;
    }
}