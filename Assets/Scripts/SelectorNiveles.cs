using UnityEngine;
using UnityEngine.UIElements;

public class SelectorNiveles : MonoBehaviour
{
    //cantidad de niveles, coincidir si o si con los botones, no sirve hardcodear aca
    [SerializeField] private int cantidadNiveles = 3;

    private VisualElement panel;
    private Button mapButton;
    private Button closeButton;

    void OnEnable()
    {
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        panel = root.Q<VisualElement>("LevelsPanel");
        mapButton = root.Q<Button>("MapButton");
        //puede cerrar con este boton o apretando el mapa de nuevo, vere si saco este
        closeButton = root.Q<Button>("LevelsCloseButton");

        if (panel == null || mapButton == null)
        {
            Debug.LogWarning("Faltan cosas en el uxml");
            return;
        }

        //el panel empieza oculto
        panel.style.display = DisplayStyle.None;

        mapButton.clicked += AlternarPanel;
        if (closeButton != null) closeButton.clicked += Cerrar;

        //un boton por nively los bloqueados quedan deshabilitados
        for (int i = 1; i <= cantidadNiveles; i++)
        {
            Button boton = root.Q<Button>("Level" + i + "Button");
            if (boton == null) continue;

            bool desbloqueado = GameManager.Instance.EstaDesbloqueado(i);
            boton.SetEnabled(desbloqueado);
            //para q furia le de otro stylo si quiere
            boton.EnableInClassList("locked", !desbloqueado);

            int nivel = i;
            boton.clicked += () => GameManager.Instance.IniciarNivel(nivel);
        }
    }

    void OnDisable()
    {
        if (mapButton != null) mapButton.clicked -= AlternarPanel;
        if (closeButton != null) closeButton.clicked -= Cerrar;
    }

    void AlternarPanel()
    {
        bool estabaOculto = panel.style.display == DisplayStyle.None;
        panel.style.display = estabaOculto ? DisplayStyle.Flex : DisplayStyle.None;
    }

    void Cerrar()
    {
        panel.style.display = DisplayStyle.None;
    }
}