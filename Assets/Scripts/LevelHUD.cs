using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class LevelHUD : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private string levelTitle = "Nivel 1 test";
    [SerializeField] private float titleDuration = 2.5f;

    private UIDocument uiDocument;
    private Vida vida;

    private VisualElement[] resourceSlots;
    private Label titleLabel;
    private VisualElement[] hearts;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        VisualElement root = uiDocument.rootVisualElement;
        
        titleLabel = root.Q<Label>("TitleLabel");

        if (titleLabel != null)
        {
            titleLabel.text = levelTitle;
            titleLabel.style.display = DisplayStyle.Flex;
            titleLabel.style.opacity = 1f;
        }

        hearts = new VisualElement[3];

        hearts[0] = root.Q<VisualElement>("Heart_01");
        hearts[1] = root.Q<VisualElement>("Heart_02");
        hearts[2] = root.Q<VisualElement>("Heart_03");

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        if (player != null)
        {
            vida = player.GetComponent<Vida>();

            if (vida != null)
            {
                vida.OnHealthChanged += UpdateHearts;
                UpdateHearts(vida.GetCurrentHealth());
            }
        }

        resourceSlots = new VisualElement[]
        {
            root.Q<VisualElement>("Resource_01"),
            root.Q<VisualElement>("Resource_02"),
            root.Q<VisualElement>("Resource_03"),
            root.Q<VisualElement>("Resource_04")
        };

        UpdateResources();


        if (BuildingInventory.Instance != null)
        {
            BuildingInventory.Instance.OnInventoryChanged += UpdateResources;
        }

        StartCoroutine(HideTitleRoutine());
    }

    private void OnDisable()
    {
        if (BuildingInventory.Instance != null)
        {
            BuildingInventory.Instance.OnInventoryChanged -= UpdateResources;
        }

        if (vida != null)
        {
            vida.OnHealthChanged -= UpdateHearts;
        }
    }

    private void UpdateResources()
    {
        if (BuildingInventory.Instance == null)
            return;

        if (resourceSlots == null)
            return;

        int index = 0;

        foreach (KeyValuePair<BuildingItemData, int> entry
            in BuildingInventory.Instance.Items)
        {
            if (index >= resourceSlots.Length)
                break;

            BuildingItemData item = entry.Key;
            int amount = entry.Value;

            if (item == null)
                continue;

            VisualElement slot = resourceSlots[index];

            if (slot == null)
                continue;

            VisualElement icon =
                slot.Q<VisualElement>("Icon");

            Label amountLabel =
                slot.Q<Label>("Amount");

            if (icon != null)
            {
                icon.style.backgroundImage =
                    new StyleBackground(item.icon);
            }

            if (amountLabel != null)
            {
                amountLabel.text = amount.ToString();
            }

            index++;
        }

        // limpiar slots que no esten siendo utilizados
        for (; index < resourceSlots.Length; index++)
        {
            VisualElement slot = resourceSlots[index];

            if (slot == null)
                continue;

            VisualElement icon =
                slot.Q<VisualElement>("Icon");

            Label amountLabel =
                slot.Q<Label>("Amount");

            if (icon != null)
                icon.style.backgroundImage = null;

            if (amountLabel != null)
                amountLabel.text = "";
        }
    }

    private IEnumerator HideTitleRoutine()
    {
        yield return new WaitForSeconds(titleDuration);

        if (titleLabel == null)
            yield break;

        titleLabel.style.opacity = 0f;

        yield return new WaitForSeconds(0.5f);

        titleLabel.style.display = DisplayStyle.None;
    }

    private void UpdateHearts(int currentHealth)
    {
        if (hearts == null)
            return;

        for (int i = 0; i < hearts.Length; i++)
        {
            if (hearts[i] == null)
                continue;

            int heartHealth = currentHealth - (i * 2);

            if (heartHealth >= 2)
            {
                SetHeartState(hearts[i], "heart-full");
            }
            else if (heartHealth == 1)
            {
                SetHeartState(hearts[i], "heart-half");
            }
            else
            {
                SetHeartState(hearts[i], "heart-empty");
            }
        }
    }

    private void SetHeartState(
        VisualElement heart,
        string stateClass)
    {
        heart.RemoveFromClassList("heart-full");
        heart.RemoveFromClassList("heart-half");
        heart.RemoveFromClassList("heart-empty");

        heart.AddToClassList(stateClass);
    }
}