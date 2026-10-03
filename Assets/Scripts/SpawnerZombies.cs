using UnityEngine;

public class SpawnerZombies : MonoBehaviour
{
    [SerializeField] private GameObject[] zombiesPrefabs;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] puntos;
    [SerializeField] private Transform puntoAtaque;
    [SerializeField] private float tiempoEntreSpawns = 3f;



    private float proximoSpawn;

    void Update()
    {
        //si el player murio deja de spawnear
        if (player == null) return;

        // espera hasta que pase el tiempo
        if (Time.time < proximoSpawn) return;
        proximoSpawn = Time.time + tiempoEntreSpawns;

        //elige un prefab al azar y lo crea en el primer punto
        GameObject prefab = zombiesPrefabs[Random.Range(0, zombiesPrefabs.Length)];
        GameObject zombie = Instantiate(prefab, puntos[0].position, Quaternion.identity);

        //le asigna player y puntos
        ZombiePuntos zp = zombie.GetComponent<ZombiePuntos>();
        zp.Iniciar(player, puntos, puntoAtaque);

        //para q si es zomibe de mantener no spawnee nada por los turnos q deba mantener
        proximoSpawn += zp.TiempoSinSpawn;
    }
}