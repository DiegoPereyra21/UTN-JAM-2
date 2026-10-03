using UnityEngine;

public class PulsoBeat : MonoBehaviour
{
    //cuanto crece, 0.1 es 10%
    [SerializeField] private float intensidad = 0.1f;
    //que tan rapido vuelve a su tamaño normal(luego hay q jugr con este valor)
    [SerializeField] private float velocidadVuelta = 10f;
    //ta obvio, cada cuantos beats hace el pulso, la idea es clavarlo segun la cancion, ya vere si crear varios prefbas de zombies con valores distintos
    [SerializeField] private int cadaBeats = 1;
    //privadas
    private Vector3 escalaBase;
    private float pulso;

    void Start()
    {
        escalaBase = transform.localScale;
        //se suscribe al reloj
        RelojMusica.Instance.OnBeat += AlBeat;
    }

    void OnDestroy()
    {
        if (RelojMusica.Instance != null) RelojMusica.Instance.OnBeat -= AlBeat;
    }

    void AlBeat(int beat)
    {
        //en el beat crece de golpe
        if (beat % cadaBeats == 0) 
        { 
        pulso = intensidad;
        }
    }

    void Update()
    {
        //y vuelve suave a su tamaño normal
        pulso = Mathf.Lerp(pulso, 0f, velocidadVuelta * Time.deltaTime);
        transform.localScale = escalaBase * (1f + pulso);
    }
}