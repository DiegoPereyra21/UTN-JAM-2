using UnityEngine;

public class SpawnerZombies : MonoBehaviour
{
    [SerializeField] private GameObject[] zombiesPrefabs;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] puntos;
    [SerializeField] private Transform puntoAtaque;
    //cada cuantos beats spawnea un zombi
    [SerializeField] private int beatsEntreSpawns = 4;

    //beats que pasaron desde el ultimo spawn
    private int beatsContados;
    //beats de pausa despues de un zombi de mantener
    private int beatsPausa;

    void Start()
    {
        //se suscribe al reloj, cada beat llama a AlBeat
        RelojMusica.Instance.OnBeat += AlBeat;
    }

    void OnDestroy()
    {
        if (RelojMusica.Instance != null) RelojMusica.Instance.OnBeat -= AlBeat;
    }

    void AlBeat(int beat)
    {
        //si el player murio deja de spawnear
        if (player == null) return;

        //pausa despues del zombi de mantener, para que no se choquen
        if (beatsPausa > 0)
        {
            beatsPausa--;
            return;
        }

        // espera hasta que pasen los beats
        beatsContados++;
        if (beatsContados < beatsEntreSpawns) return;
        beatsContados = 0;

        //elige un prefab al azar y lo crea en el primer punto
        GameObject prefab = zombiesPrefabs[Random.Range(0, zombiesPrefabs.Length)];
        GameObject zombie = Instantiate(prefab, puntos[0].position, Quaternion.identity);

        //le asigna player y puntos
        ZombiePuntos zp = zombie.GetComponent<ZombiePuntos>();
        zp.Iniciar(player, puntos, puntoAtaque);

        //para q si es zomibe de mantener no spawnee nada por los turnos q deba mantener
        beatsPausa = zp.BeatsSinSpawn;
    }
}