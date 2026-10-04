using UnityEngine;

public class SpawnerZombies : MonoBehaviour
{
    [SerializeField] private GameObject[] zombiesPrefabs;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] puntos;
    [SerializeField] private Transform puntoAtaque;
    //cada cuantos beats spawnea un zombi
    [SerializeField] private int beatsEntreSpawns = 4;
    [SerializeField] private BuildingLootTable lootTableLevel1;
    [SerializeField] private BuildingLootTable lootTableLevel2;
    [SerializeField] private BuildingLootTable lootTableLevel3;
    //ultimos segundos de la cancion en los que ya no spawnean zombis
    [SerializeField] private float segundosSinSpawn = 5f;

    //beats que pasaron desde el ultimo spawn
    private int beatsContados;
    //beats de pausa despues de un zombi de mantener
    private int beatsPausa;

    private int currentLevel = 1;

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

        //ultimos segundos de la cancion, donde no spawnean zombies
        if (RelojMusica.Instance.DuracionCancion - RelojMusica.Instance.SegundosCancion <= segundosSinSpawn) return;

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
        zp.SetLootTable(GetLootTable());
        zp.Iniciar(player, puntos, puntoAtaque);

        //para q si es zomibe de mantener no spawnee nada por los turnos q deba mantener
        beatsPausa = zp.BeatsSinSpawn;
    }

    private BuildingLootTable GetLootTable()
    {
        switch (currentLevel)
        {
            case 1:
                return lootTableLevel1;

            case 2:
                return lootTableLevel2;

            case 3:
                return lootTableLevel3;

            default:
                //return null;
                return lootTableLevel1;
        }
    }
}