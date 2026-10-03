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
    //que tan rapido se desliza entre puntos
    [SerializeField] private float velocidad = 5f;

    //segun el zombi
    //cuantos pasos da de 1, saltenado puntos
    [SerializeField] private int pasosAdelante = 1;
    //cuantos retrocede
    [SerializeField] private int pasosAtras = 0;
    //cuanto retrocee al recibir golpoe
    [SerializeField] private int retrocesoPorGolpe = 1;
    //true: despues de atacar al player el zombi muere. false: vuelve al ultimo punto y repite
    [SerializeField] private bool morirTrasAtacar = false;
    //true para sigueiten avanzar
    private bool avanzando = true;

    //x cual punto va
    private int indice;
    //true mientras esta en el punto de ataq
    private bool atacando;
    private float proximoPaso;
    //hacia donde se desliza
    private Vector3 destino;

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
        destino = transform.position;
        proximoPaso = Time.time + tiempoEntrePasos;
    }

    void Update()
    {
        //si el player murio (se destruyo) se quedan quietos, evita errores
        if (player == null) return;

        //se desliza hacia el destino en vez de teletransportarse
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        if (atacando || Time.time < proximoPaso) return;
        proximoPaso = Time.time + tiempoEntrePasos;

        if (indice < puntos.Length - 1)
        {
            if (avanzando)
            {
                //salta varios puntos sin pasarse del ultimo
                indice = Mathf.Min(indice + pasosAdelante, puntos.Length - 1);
                //si tiene retroceso, el proximo movimiento es hacia atras
                if (pasosAtras > 0) avanzando = false;
            }
            else
            {
                //retrocede sin pasar del primero
                indice = Mathf.Max(0, indice - pasosAtras);
                avanzando = true;
            }

            destino = puntos[indice].position;
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

        //se transporta al punto cerca del player y golpea al player
        transform.position = puntoAtaque.position;
        destino = transform.position;
        player.GetComponent<Vida>().RecibirGolpe();

        yield return new WaitForSeconds(tiempoEnAtaque);

        //si esta activado muere luego de atacar
        if (morirTrasAtacar)
        {
            Destroy(gameObject);
            yield break;
        }

        //vuelve al ultimo punto
        transform.position = puntos[indice].position;
        destino = transform.position;
        atacando = false;

        //reciniciar tiempo, sino antes pegaba muy rapidamente
        proximoPaso = Time.time + tiempoEntrePasos;
    }

    public void Retroceder()
    {
        //o retrocede si esta atacando o esta en el primer punto
        if (atacando || indice <= 0)
        {
            return;
        }
        indice = Mathf.Max(0, indice - retrocesoPorGolpe);
        destino = puntos[indice].position;
        // reinicia todo y el tiempo hasta el proximo paso
        avanzando = true;
        proximoPaso = Time.time + tiempoEntrePasos;
    }
}