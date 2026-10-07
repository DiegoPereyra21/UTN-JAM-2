using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingPart
{
    [Header("ID")]
    public string id;

    [Header("Preview")]
    public GameObject previewObject;

    [Header("Final")]
    public GameObject finalObject;

    [Header("Dialogo post construccion")]
    public DialogueData dialogueAfterBuild;

    [Header("Special")]
    public bool isRoof;

    [Header("Cost")]
    public List<BuildingCost> costs = new List<BuildingCost>();

    [HideInInspector]
    public bool isBuilt;

}