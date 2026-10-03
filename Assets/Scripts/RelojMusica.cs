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
    //segundos de espera antes de que arranque la musica
    [SerializeField] private float cuentaInicial = 2f;
    //para probar: suena un click en cada beat arriba de la musica
    [SerializeField] private bool metronomo = false;
    [SerializeField] private AudioClip click;

    //aviso de cada beat, los zombis y el spawner se suscriben
    public event Action<int> OnBeat;

    private double dspInicio;
    //donde congelar el beat
    private double dspPausa;
    private int ultimoBeat = -1;
    private bool pausado;
    private bool detenido;
    private bool jugadorAsignado;

    //si esta pausado o detenido el tiempo queda congelado
    private double TiempoReloj => (pausado || detenido) ? dspPausa : AudioSettings.dspTime;

    //beat actual x decimas
    public float BeatActual => (float)((TiempoReloj - dspInicio - offset) * bpm / 60.0);
    public float SegundosPorBeat => 60f / bpm;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        jugadorAsignado = jugador != null;
        //programa la musica con el reloj del audio, es el mas preciso q encontre
        dspInicio = AudioSettings.dspTime + cuentaInicial;
        musica.PlayScheduled(dspInicio);
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

        if (pausado || detenido) return;

        int beat = Mathf.FloorToInt(BeatActual);

        //avisa cada beat nuevo (el while es por si un frame se salta alguno)
        while (ultimoBeat < beat)
        {
            ultimoBeat++;
            if (metronomo && click != null) musica.PlayOneShot(click);
            OnBeat?.Invoke(ultimoBeat);
        }
    }

    public void Pausar()
    {
        //no se puede pausar antes de que arranque la musica
        if (pausado || detenido || AudioSettings.dspTime < dspInicio) return;
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