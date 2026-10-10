using System.Collections;
using UnityEngine;

public class ZombiePuntos : MonoBehaviour
{
    [SerializeField] private Transform player;
    //puntos del camino en orden 
    [SerializeField] private Transform[] puntos;

    //sangre que sale en cada beat mientras lo sujetan
    [SerializeField] private GameObject efectoSangreHold;

    //recibe daño feedback
    [SerializeField] private Color colorDanio = Color.red;
    [SerializeField] private float duracionFlash = 0.2f;
    [SerializeField] private float intensidadShake = 0.1f;
    [SerializeField] private float duracionShake = 0.1f;

    private SpriteRenderer[] sprites;
    private Color[] coloresOriginales;
    private Vector3 shakeActual;
    private float finShake;
    private Coroutine flash;

    //ajuste manual extra por si la cabeza no queda justo en el punto
    [SerializeField] private Vector2 offsetPunto;

    //cuanto hay que mover al zombi para que su cabeza quede en el punto
    private Vector3 ajuste;

    //posicion de un punto ya ajustada a este zombi
    private Vector3 Pos(Transform punto)
    {
        return punto.position + ajuste;
    }

    //punto cerca del player donde ataca
    [SerializeField] private Transform puntoAtaque;
    //cada cuantos beats da un paso (1 = en cada beat, 2 = un paso cada 2 beats)
    [SerializeField] private int beatsPorPaso = 1;
    //cuanto dura el ataque, en beats, 0.25 es 1/4 de beat, cambiar segun eso
    [SerializeField] private float beatsEnAtaque = 0.25f;
    //que tan rapido se desliza entre puntos(me termino gustando mas con esto, ya q va de golpe)
    [SerializeField] private float velocidad = 35f;

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
    //sonido al completar el hold
    [SerializeField] private AudioClip sonidoHoldCompleto;
    //punto desde la cabeza, sino salia el line desde el pivot
    [SerializeField] private Transform cabeza;
    private bool sujetado;
    //beat (con decimales) en el que lo agarraron
    private float beatInicioHold;
    //beat exacto donde termina el hold, siempre cae justo en un beat
    private float beatFinHold;
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
    private Vida vida;
    private bool alreadyDied;
    private BuildingLootTable lootTable;

    //anim: animator del zombi (puede no tener, asi los que no tienen animacion siguen funcionando)
    private Animator animator;


    //lo llamada el spawner para asignar todo
    public void Iniciar(Transform nuevoPlayer, Transform[] nuevosPuntos, Transform nuevoPuntoAtaque)
    {
        player = nuevoPlayer;
        puntos = nuevosPuntos;
        puntoAtaque = nuevoPuntoAtaque;
    }

    void Start()
    {
        //calcula la altura de los pies (parte mas baja del sprite) respecto al pivote
        float base_ = transform.position.y;
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            base_ = Mathf.Min(base_, sr.bounds.min.y);
        ajuste = new Vector3(offsetPunto.x, (transform.position.y - base_) + offsetPunto.y, 0f);

        //empieza en el primer punto
        transform.position = Pos(puntos[0]);
        destino = transform.position;

        //se suscribe al reloj, cada beat llama a AlBeat
        RelojMusica.Instance.OnBeat += AlBeat;

        vida = GetComponent<Vida>();

        if (vida != null)
        {
            vida.OnDeath += Die;
            vida.OnHealthChanged += AlRecibirDanio;
        }

        sprites = GetComponentsInChildren<SpriteRenderer>();
        coloresOriginales = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++) coloresOriginales[i] = sprites[i].color;

        if (lineaHold != null)
        {
            lineaHold.useWorldSpace = true;
            lineaHold.positionCount = 2;
        }

        //anim
        animator = GetComponentInChildren<Animator>();
        Reproducir("Walk", beatsPorPaso);
    }

    void OnDestroy()
    {
        //se desuscribe, sino da error cuando el zombi muere
        if (RelojMusica.Instance != null) RelojMusica.Instance.OnBeat -= AlBeat;

        if (vida != null) vida.OnDeath -= Die;

        if (vida != null) vida.OnHealthChanged -= AlRecibirDanio;
    }

    void Update()
    {
        //si el player murio (se destruyo) se quedan quietos, evita errores
        if (player == null) return;

        //saca el shake de la posicion para que no afecte el movimiento
        transform.position -= shakeActual;
        shakeActual = Vector3.zero;

        //anim: mientras muere se queda quieto
        if (alreadyDied) return;

        //se desliza hacia el destino en vez de teletransportarse
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
        ActualizarLinea();

        //si lo sujetan y llego el beat final del hold, muere
        if (sujetado && RelojMusica.Instance.BeatActual >= beatFinHold)
        {
            //suena en la camara porque el zombi se destruye, sino se cortaria
            if (sonidoHoldCompleto != null && Camera.main != null)
                AudioSource.PlayClipAtPoint(sonidoHoldCompleto, Camera.main.transform.position);
            Die();
        }
    }

    //se llama en cada beat de la musica, aca se da el paso
    void AlBeat(int beat)
    {
        if (player == null || atacando || alreadyDied) return;

        //mientras lo sujetan, sangra en cada beat y no avanza
        if (sujetado)
        {
            if (efectoSangreHold != null)
                Instantiate(efectoSangreHold, cabeza != null ? cabeza.position : transform.position, Quaternion.identity);
            return;
        }

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

            destino = Pos(puntos[indice]);
            //anim: un ciclo de caminata por paso, arranca justo en el beat
            Reproducir("Walk", beatsPorPaso);
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
        transform.position = Pos(puntoAtaque);
        destino = transform.position;
        //anim: el ataque dura lo mismo que beatsEnAtaque
        Reproducir("Attack", beatsEnAtaque);
        player.GetComponent<Vida>().RecibirGolpe();

        yield return new WaitForSeconds(beatsEnAtaque * RelojMusica.Instance.SegundosPorBeat);

        //si el golpe mato al player, se queda quieto en el punto de ataque
        if (player == null) yield break;

        //si esta activado muere luego de atacar
        if (morirTrasAtacar)
        {
            Die();
            yield break;
        }

        //vuelve al ultimo punto
        transform.position = Pos(puntos[indice]);
        destino = transform.position;
        atacando = false;
        Reproducir("Walk", beatsPorPaso);

        //reciniciar el conteo, sino antes pegaba muy rapidamente
        beatsContados = 0;
    }

    public void Retroceder()
    {
        //o retrocede si esta atacando o esta en el primer punto
        if (atacando || alreadyDied || indice <= 0)
        {
            return;
        }
        indice = Mathf.Max(0, indice - retrocesoPorGolpe);
        destino = Pos(puntos[indice]);
        Reproducir("Walk", beatsPorPaso);
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
        //termina justo en un beat: redondea al beat mas cercano y suma los turnos
        beatFinHold = Mathf.Round(beatInicioHold) + turnosParaMatar * beatsPorPaso;
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
        Vector3 objetivo = Pos(puntos[indiceFinal]) + offset;
        //la primera vez queda directo en su lugar despues se desliza a la misma velocidad que el zombi
        if (!lineaIniciada)
        {
            finalLinea = objetivo;
            lineaIniciada = true;
        }

        finalLinea = Vector3.MoveTowards(finalLinea, objetivo, velocidad * Time.deltaTime);
        //0 si no lo sujetan, 1 cuando termino de mantener (del agarre al beat final)
        float progreso = 0f;
        if (sujetado)
            progreso = Mathf.Clamp01((RelojMusica.Instance.BeatActual - beatInicioHold) / (beatFinHold - beatInicioHold));
        //el inicio queda pegado a la cabeza y el extremo lejano se acerca al zombi
        lineaHold.SetPosition(0, inicio);
        lineaHold.SetPosition(1, Vector3.Lerp(finalLinea, inicio, progreso));
    }

    public void Die()
    {
        if (alreadyDied)
        {
            return;
        }

        alreadyDied = true;

        Debug.Log($"[{name}] MUERE. LootTable: {lootTable}");

        //anim: reproduce la muerte, ya no se puede golpear y se destruye cuando termina
        float espera = 0f;
        if (animator != null)
        {
            Reproducir("Die", 0f);
            espera = LargoClip("Die");
            foreach (Collider2D c in GetComponentsInChildren<Collider2D>()) c.enabled = false;
            if (lineaHold != null) lineaHold.enabled = false;
        }

        //el loot se entrega al momento de morir
        if (lootTable != null && BuildingInventory.Instance != null)
        {
            BuildingItemData droppedItem = lootTable.GetRandomItem();
            if (droppedItem != null)
                BuildingInventory.Instance.AddItem(droppedItem);
        }

        Destroy(gameObject, espera);
    }

    // define el loot del enemigo
    public void SetLootTable(BuildingLootTable table)
    {
        lootTable = table;
    }

    //anim: reproduce un estado desde el principio. beatsDuracion = cuantos beats dura el clip (0 = velocidad normal)
    void Reproducir(string estado, float beatsDuracion)
    {
        if (animator == null) return;
        float largo = LargoClip(estado);
        animator.speed = beatsDuracion > 0f ? largo / (beatsDuracion * RelojMusica.Instance.SegundosPorBeat) : 1f;
        animator.Play(estado, 0, 0f);
    }

    //anim: duracion en segundos de un clip del animator
    float LargoClip(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return 0f;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            if (clip.name == nombre) return clip.length;
        return 0f;
    }

    void AlRecibirDanio(int vidaRestante)
    {
        finShake = Time.time + duracionShake;
        if (flash != null) StopCoroutine(flash);
        flash = StartCoroutine(FlashDanio());
    }

    //aplica el shake al final del frame, asi no pisa el movimiento
    void LateUpdate()
    {
        if (Time.time >= finShake) return;
        shakeActual = (Vector3)Random.insideUnitCircle * intensidadShake;
        transform.position += shakeActual;
    }

    IEnumerator FlashDanio()
    {
        float t = 0f;
        while (t < duracionFlash)
        {
            t += Time.deltaTime;
            //arranca rojo y vuelve de a poco a su color
            for (int i = 0; i < sprites.Length; i++)
                sprites[i].color = Color.Lerp(colorDanio, coloresOriginales[i], t / duracionFlash);
            yield return null;
        }
        for (int i = 0; i < sprites.Length; i++) sprites[i].color = coloresOriginales[i];
    }
}