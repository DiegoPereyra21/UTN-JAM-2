using System.Collections;
using UnityEngine;

public class ZombiePuntos : MonoBehaviour
{
    [SerializeField] private Transform player;
    //puntos del camino en orden 
    [SerializeField] private Transform[] puntos;
    //punto cerca del player donde ataca
    [SerializeField] private Transform puntoAtaque;
    //cada cuantos beats da un paso (1 = en cada beat, 2 = un paso cada 2 beats)
    [SerializeField] private int beatsPorPaso = 1;
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

    //¨zombie alto, es para agregar alguna mecanica de mantener apretado el boton
    [SerializeField] private bool requiereMantener = false;
    //cuantos pasos tiene que aguantar el player(creo q siempre sera 3, pero por las dudas)
    [SerializeField] private int turnosParaMatar = 3;
    //linea gruesa q guia
    [SerializeField] private LineRenderer lineaHold;
    //punto desde la cabeza, sino salia el line desde el pivot
    [SerializeField] private Transform cabeza;
    private bool sujetado;
    //beat (con decimales) en el que lo agarraron
    private float beatInicioHold;
    //necesario para q no de saltos raros como antes, ahora va fluido
    private Vector3 finalLinea;
    private bool lineaIniciada;

    public bool RequiereMantener => requiereMantener;
    //beats en los que el spawner no crea otros zombis despues de este
    public int BeatsSinSpawn => requiereMantener ? turnosParaMatar * beatsPorPaso : 0;

    //x cual punto va
    private int indice;
    //true mientras esta en el punto de ataq
    private bool atacando;
    //cuantos beats pasaron desde el ultimo paso
    private int beatsContados;
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

        //se suscribe al reloj, cada beat llama a AlBeat
        RelojMusica.Instance.OnBeat += AlBeat;

        if (lineaHold != null)
        {
            lineaHold.useWorldSpace = true;
            lineaHold.positionCount = 2;
        }
    }

    void OnDestroy()
    {
        //se desuscribe, sino da error cuando el zombi muere
        if (RelojMusica.Instance != null) RelojMusica.Instance.OnBeat -= AlBeat;
    }

    void Update()
    {
        //si el player murio (se destruyo) se quedan quietos, evita errores
        if (player == null) return;

        //se desliza hacia el destino en vez de teletransportarse
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
        ActualizarLinea();

        //si lo sujetan y pasaron los beats del hold, muere
        if (sujetado && RelojMusica.Instance.BeatActual - beatInicioHold >= turnosParaMatar * beatsPorPaso)
            Destroy(gameObject);
    }

    //se llama en cada beat de la musica, aca se da el paso
    void AlBeat(int beat)
    {
        if (player == null || atacando || sujetado) return;

        //espera la cantidad de beats por paso
        beatsContados++;
        if (beatsContados < beatsPorPaso) return;
        beatsContados = 0;

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

        //reciniciar el conteo, sino antes pegaba muy rapidamente
        beatsContados = 0;
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
        //reinicia todo y el conteo hasta el proximo paso
        avanzando = true;
        beatsContados = 0;
    }

    //lo llama el player al mantener
    public void Sujetar()
    {
        //si esta atacando no se puede agarrar
        if (atacando) return;
        sujetado = true;
        beatInicioHold = RelojMusica.Instance.BeatActual;
    }

    //por si suelta antes de tiempo, el zombie actua normal
    public void Soltar()
    {
        sujetado = false;
        beatsContados = 0;
    }

    //linea desde el zombi hasta el punto donde termina el hold, avanza mientras mantenes
    void ActualizarLinea()
    {
        if (lineaHold == null) return;

        //solo se ve en el zombi de mantener y no mientras ataca
        lineaHold.enabled = requiereMantener && !atacando;
        if (!lineaHold.enabled) return;

        Vector3 inicio = cabeza != null ? cabeza.position : transform.position;
        //no quedaba a la altura de la cabeza, asi q con esto calc la distancai entre el pivote y la cabeza y agrego offset
        Vector3 offset = inicio - transform.position;
        //el final del hold es la cantidad de turnos para matar
        int indiceFinal = Mathf.Max(indice - turnosParaMatar, 0);
        Vector3 objetivo = puntos[indiceFinal].position + offset;
        //la primera vez queda directo en su lugar despues se desliza a la misma velocidad que el zombi
        if (!lineaIniciada)
        {
            finalLinea = objetivo;
            lineaIniciada = true;
        }

        finalLinea = Vector3.MoveTowards(finalLinea, objetivo, velocidad * Time.deltaTime);
        //0 si no lo sujetan, 1 cuando termino de mantener (ahora medido en beats)
        float progreso = sujetado ? (RelojMusica.Instance.BeatActual - beatInicioHold) / (turnosParaMatar * beatsPorPaso) : 0f;
        progreso = Mathf.Clamp01(progreso);
        //el inicio queda pegado a la cabeza y el extremo lejano se acerca al zombi
        lineaHold.SetPosition(0, inicio);
        lineaHold.SetPosition(1, Vector3.Lerp(finalLinea, inicio, progreso));
    }
}