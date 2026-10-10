using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
public class ControlNivel : MonoBehaviour
{
    //arrastrar el player y si desaparece es que murio
    [SerializeField] private Transform jugador;
    //espera antes de volver al building si muere, par amostrar anim de muerte y tal
    [SerializeField] private float segundosTrasMorir = 2f;
    //segundos extra que sigue el nivel despues de que termina la cancion
    [SerializeField] private float segundosExtraFinal = 3f;
    //cuanto dura el fade a negro al morir
    [SerializeField] private float segundosFadeMuerte = 1f;
    //fade oscuro
    //cualquier UIDocument de la escena, ahi se agrega la capa negra
    [SerializeField] private UIDocument documento;
    //cuantos segundos antes del final empieza a oscurecer
    [SerializeField] private float segundosOscurecer = 2f;
    //que tan oscuro llega al final (1 = negro total)
    [SerializeField, Range(0f, 1f)] private float oscuridadMaxima = 1f;

    private VisualElement oscuridad;

    private bool jugadorAsignado;
    private bool terminando;

    void Start()
    {
        jugadorAsignado = jugador != null;

        if (documento != null)
        {
            oscuridad = new VisualElement();
            oscuridad.style.position = Position.Absolute;
            oscuridad.style.left = 0;
            oscuridad.style.top = 0;
            oscuridad.style.right = 0;
            oscuridad.style.bottom = 0;
            oscuridad.style.backgroundColor = Color.black;
            oscuridad.style.opacity = 0f;
            //no bloquea clicks
            oscuridad.pickingMode = PickingMode.Ignore;
            //en el indice 0 queda detras del resto, asi el HUD no se oscurece
            documento.rootVisualElement.Insert(0, oscuridad);
        }
    }

    void Update()
    {
        if (terminando) return;

        //vuelve al building sin desbloquear nada
        if (jugadorAsignado && jugador == null)
        {
            StartCoroutine(Terminar(false, segundosTrasMorir));
            return;
        }

        //momento real en que termina el nivel (fin de la cancion + el extra)
        float duracionNivel = RelojMusica.Instance.DuracionCancion + segundosExtraFinal;

        //oscurece la pantalla en los ultimos segundos del nivel
        if (oscuridad != null && RelojMusica.Instance.SegundosCancion >= 0f)
        {
            float restante = duracionNivel - RelojMusica.Instance.SegundosCancion;
            oscuridad.style.opacity = (1f - Mathf.Clamp01(restante / segundosOscurecer)) * oscuridadMaxima;
        }

        //si termina bien vuelve al building y desbloquea el siguiente
        if (RelojMusica.Instance.SegundosCancion >= duracionNivel)
            StartCoroutine(Terminar(true, 0f));


    }

    IEnumerator Terminar(bool gano, float espera)
    {
        terminando = true;

        if (!gano && oscuridad != null)
        {
            //primero se ve la animacion de muerte y despues oscurece, justo hasta que termina la espera
            yield return new WaitForSeconds(Mathf.Max(0f, espera - segundosFadeMuerte));
            float duracion = Mathf.Max(0.01f, segundosFadeMuerte);
            float t = 0f;
            while (t < duracion)
            {
                t += Time.deltaTime;
                oscuridad.style.opacity = Mathf.Clamp01(t / duracion) * oscuridadMaxima;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(espera);
        }

        GameManager.Instance.TerminarNivel(gano);
    }
}