using System.Collections;
using UnityEngine;

public class ZombiePuntos : MonoBehaviour
{
    [SerializeField] private Transform player;
    //puntos del camino en orden 
    [SerializeField] private Transform[] puntos;
    //punto cerca del player donde ataca
    [SerializeField] private Transform puntoAtaque;
    [SerializeField] private float tiempoEntrePasos = 1f;
    [SerializeField] private float tiempoEnAtaque = 0.3f;
    //x cual punto va
    private int indice;
    //true mientras esta en el punto de ataq
    private bool atacando;
    private float proximoPaso;
    //lo llamada el spawner para asignar todo
    public void Iniciar(Transform nuevoPlayer, Transform[] nuevosPuntos, Transform nuevoPuntoAtaque)
    {
        player = nuevoPlayer;
        puntos = nuevosPuntos;
        puntoAtaque = nuevoPuntoAtaque;
    }

    void Start()
    {
        //empeiza en el primer punto
        transform.position = puntos[0].position;
        proximoPaso = Time.time + tiempoEntrePasos;
    }

    void Update()
    {
        if (atacando || Time.time < proximoPaso) return;
        proximoPaso = Time.time + tiempoEntrePasos;

        if (indice < puntos.Length - 1)
        {
            //avanza al siguiente punto
            indice++;
            transform.position = puntos[indice].position;
        }
        else
        {
            //en el ultimpopunto ataca
            StartCoroutine(Atacar());
        }
    }

    IEnumerator Atacar()
    {
        atacando = true;

        //va al punto espacial y atacara
        transform.position = puntoAtaque.position;
        player.GetComponent<Vida>().RecibirGolpe();

        yield return new WaitForSeconds(tiempoEnAtaque);

        //leugo del ataque vuelve a su lugar
        transform.position = puntos[indice].position;
        atacando = false;
    }

    public void Retroceder()
    {
        //no retrocedeen caso de estar atacando o si esta fuera
        if (atacando || indice <= 0) return;

        //vuelve al punto atras
        indice--;
        transform.position = puntos[indice].position;

        //reinicia el tiempo hasta el proximao paso, xq aveces si golpeabas a ultimo momento luego avanzada instantaneamente
        proximoPaso = Time.time + tiempoEntrePasos;
    }
}