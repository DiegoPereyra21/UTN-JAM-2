using UnityEngine;

public class CameraShake : MonoBehaviour
{
    //para q se encuentre siempre, iria en la main camera de cada lvl
    public static CameraShake Instance;
    private Vector3 offsetActual;
    private float intensidad;
    private float duracion = 0.01f;
    private float tiempoRestante;

    void Awake()
    {
        Instance = this;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
    //intensidad, 0.05 suavecito, 0.3 fuerte
    public void Sacudir(float nuevaIntensidad, float nuevaDuracion)
    {
        //si ya esta sacuediendo, no vuelve a sacudir
        float restante = tiempoRestante > 0f ? intensidad * (tiempoRestante / duracion) : 0f;
        if (nuevaIntensidad < restante) return;

        intensidad = nuevaIntensidad;
        duracion = Mathf.Max(nuevaDuracion, 0.01f);
        tiempoRestante = duracion;
    }

    void LateUpdate()
    {
        //se pausa tmabien, luego ver si queremos agregar algun pausa de congelacion y tal
        if (Time.timeScale == 0f) return;

        transform.position -= offsetActual;
        offsetActual = Vector3.zero;

        if (tiempoRestante <= 0f) return;
        tiempoRestante -= Time.deltaTime;

        //baja de a poco la sacudida
        float fuerza = intensidad * Mathf.Clamp01(tiempoRestante / duracion);
        Vector2 random = Random.insideUnitCircle * fuerza;
        offsetActual = new Vector3(random.x, random.y, 0f);
        transform.position += offsetActual;
    }
}