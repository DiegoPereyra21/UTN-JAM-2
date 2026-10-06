using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class RelojMusica : MonoBehaviour
{
    //para que cualquiera lo encuentre
    public static RelojMusica Instance;

    [SerializeField] private AudioSource musica;
    //arrastrar el player, cuando muere se frena la musica y el reloj
    [SerializeField] private Transform jugador;
    //bpm de la cancion
    [SerializeField] private float bpm = 120f;
    //segundo de la cancion donde cae el primer beat
    [SerializeField] private float offset = 0f;
    //true: la musica arranca cuando se llama a Comenzar() (al terminar la animacion del player)
    //false: arranca sola despues de cuentaInicial
    [SerializeField] private bool esperarAnimacion = true;
    //segundos de espera antes de que arranque la musica (solo si esperarAnimacion es false)
    [SerializeField] private float cuentaInicial = 2f;
    //segundos que tarda en subir el volumen al empezar (0 = sin fade)
    [SerializeField] private float fadeIn = 3f;
    //para probar: suena un click en cada beat arriba de la musica
    [SerializeField] private bool metronomo = false;
    [SerializeField] private AudioClip click;

    //aviso de cada beat, los zombis y el spawner se suscriben
    public event Action<int> OnBeat;

    private double dspInicio;
    //donde congelar el beat
    private double dspPausa;
    private int ultimoBeat = -1;
    private bool iniciado;
    private bool pausado;
    private bool detenido;
    private bool jugadorAsignado;
    //volumen que tiene el audiosource en el inspector, es al que llega el fade
    private float volumenMusica;

    //si esta pausado o detenido el tiempo queda congelado
    private double TiempoReloj => (pausado || detenido) ? dspPausa : AudioSettings.dspTime;


    public float BeatActual => iniciado ? (float)((TiempoReloj - dspInicio - offset) * bpm / 60.0) : -1f;
    public float SegundosPorBeat => 60f / bpm;
    //posicion de la cancion en segundos, -1 mientras no empezo
    public float SegundosCancion => iniciado ? (float)(TiempoReloj - dspInicio) : -1f;
    //largo de la cancion en segundos, poner si o si, sino nunca termina
    public float DuracionCancion => musica.clip != null ? musica.clip.length : float.MaxValue;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        jugadorAsignado = jugador != null;

        //guarda el volumen y arranca en 0 para el fade
        volumenMusica = musica.volume;
        musica.volume = 0f;

        //si no espera la animacion, arranca sola como antes
        if (!esperarAnimacion) Programar(cuentaInicial);
    }

    void OnDestroy()
    {
        //por si se recarga la escena estando en pausa
        Time.timeScale = 1f;
    }

    void Update()
    {
        //si el player murio frena todo
        if (jugadorAsignado && jugador == null) Detener();

        //esc para pausar y reanudar (para probar, despues se puede llamar desde un menu)
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (pausado) Reanudar();
            else Pausar();
        }

        if (pausado || detenido || !iniciado) return;

        //el volumen sube de a poco desde que empieza la cancion
        float t = fadeIn > 0f ? Mathf.Clamp01(SegundosCancion / fadeIn) : 1f;
        musica.volume = volumenMusica * t * t;

        int beat = Mathf.FloorToInt(BeatActual);

        //avisa cada beat nuevo (el while es por si un frame se salta alguno)
        while (ultimoBeat < beat)
        {
            ultimoBeat++;
            if (metronomo && click != null) musica.PlayOneShot(click);
            OnBeat?.Invoke(ultimoBeat);
        }
    }

    //lo llama el animator del player cuando termina la animacion de la botella
    public void Comenzar()
    {
        Programar(0.1f);
    }

    void Programar(float espera)
    {
        //si ya empezo no hace nada, asi se puede llamar mas de una vez sin problema
        if (iniciado || detenido) return;
        iniciado = true;

        //programa la musica con el reloj del audio, es el mas preciso q encontre
        dspInicio = AudioSettings.dspTime + Mathf.Max(espera, 0.1f);
        musica.PlayScheduled(dspInicio);
    }

    public void Pausar()
    {
        //no se puede pausar antes de que arranque la musica
        if (!iniciado || pausado || detenido || AudioSettings.dspTime < dspInicio) return;
        pausado = true;
        dspPausa = AudioSettings.dspTime;
        musica.Pause();
        Time.timeScale = 0f;
    }

    public void Reanudar()
    {
        if (!pausado) return;
        //corre el inicio lo que duro la pausa, asi el beat sigue donde quedo
        dspInicio += AudioSettings.dspTime - dspPausa;
        pausado = false;
        musica.UnPause();
        Time.timeScale = 1f;
    }

    //si moris deja de dar beats y la musica frena
    void Detener()
    {
        if (detenido) return;
        dspPausa = AudioSettings.dspTime;
        detenido = true;
        musica.Stop();
    }
}