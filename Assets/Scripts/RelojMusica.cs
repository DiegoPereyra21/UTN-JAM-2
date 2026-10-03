using System;
using UnityEngine;

public class RelojMusica : MonoBehaviour
{
    //para que cualquiera lo encuentre
    public static RelojMusica Instance;

    [SerializeField] private AudioSource musica;
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
    private int ultimoBeat = -1;

    //beat actual con decimales (2.5 = mitad del beat 2), negativo antes de empezar
    public float BeatActual => (float)((AudioSettings.dspTime - dspInicio - offset) * bpm / 60.0);
    public float SegundosPorBeat => 60f / bpm;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        //programa la musica con el reloj del audio, es el mas preciso
        dspInicio = AudioSettings.dspTime + cuentaInicial;
        musica.PlayScheduled(dspInicio);
    }

    void Update()
    {
        int beat = Mathf.FloorToInt(BeatActual);

        //avisa cada beat nuevo (el while es por si un frame se salta alguno)
        while (ultimoBeat < beat)
        {
            ultimoBeat++;
            if (metronomo && click != null) musica.PlayOneShot(click);
            OnBeat?.Invoke(ultimoBeat);
        }
    }
}