using System.Collections.Generic;
using UnityEngine;

public class BuildingState : MonoBehaviour
{
    public static BuildingState Instance { get; private set; }

    private Dictionary<string, bool> builtParts =
        new Dictionary<string, bool>();

    private bool roofVisible = true;

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

    public bool IsBuilt(string id)
    {
        if (string.IsNullOrEmpty(id))
            return false;

        if (builtParts.TryGetValue(id, out bool isBuilt))
            return isBuilt;

        return false;
    }

    public void SetBuilt(string id, bool value)
    {
        if (string.IsNullOrEmpty(id))
            return;

        builtParts[id] = value;
    }

    public bool IsRoofVisible()
    {
        return roofVisible;
    }

    public void SetRoofVisible(bool visible)
    {
        roofVisible = visible;
    }
}