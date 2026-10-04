using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    //nombre de la escena del building
    private const string escenaBuilding = "Building";
    //cantidad de niveles q por ahora solo 3
    private const int totalNiveles = 3;

    //hasta que nivel esta desbloqueado, siempre tiene minimo 1(luego debatir si es posible repetir nivel y tal)
    public int NivelesDesbloqueados { get; private set; } = 1;
    //nivel que se esta jugando
    public int NivelActual { get; private set; }

    //se crea solo al abrir el juego, no hace falta ponerlo en ninguna escena (importantsimio)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Crear()
    {
        if (Instance != null) return;
        new GameObject("GameManager").AddComponent<GameManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public bool EstaDesbloqueado(int nivel)
    {
        return nivel <= NivelesDesbloqueados;
    }

    public void IniciarNivel(int nivel)
    {
        if (nivel > totalNiveles || !EstaDesbloqueado(nivel)) return;
        NivelActual = nivel;
        SceneManager.LoadScene("Nivel " + nivel);
    }

    //lo llama el nivel al terminar, true termino la cancion, flase murio
    public void TerminarNivel(bool gano)
    {
        //abre el prox level solo si ganas el ultimo nivel
        if (gano && NivelActual == NivelesDesbloqueados && NivelesDesbloqueados < totalNiveles)
            NivelesDesbloqueados++;

        NivelActual = 0;
        SceneManager.LoadScene(escenaBuilding);
    }
}