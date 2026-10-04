using System.Collections;
using UnityEngine;

public class ControlNivel : MonoBehaviour
{
    //arrastrar el player y si desaparece es que murio
    [SerializeField] private Transform jugador;
    //espera antes de volver al building si muere, par amostrar anim de muerte y tal
    [SerializeField] private float segundosTrasMorir = 2f;

    private bool jugadorAsignado;
    private bool terminando;

    void Start()
    {
        jugadorAsignado = jugador != null;
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

        //si termina bien vuelve al building y desbloquea el siguiente
        if (RelojMusica.Instance.SegundosCancion >= RelojMusica.Instance.DuracionCancion)
            StartCoroutine(Terminar(true, 0f));
    }

    IEnumerator Terminar(bool gano, float espera)
    {
        terminando = true;
        yield return new WaitForSeconds(espera);
        GameManager.Instance.TerminarNivel(gano);
    }
}