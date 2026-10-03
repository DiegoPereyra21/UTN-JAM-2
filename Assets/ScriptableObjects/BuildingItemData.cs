using UnityEngine;

[CreateAssetMenu(
    fileName = "NewBuildingItem",
    menuName = "Game/Building Item"
)]
public class BuildingItemData : ScriptableObject
{
    public string itemName;
    public Sprite icon;
}