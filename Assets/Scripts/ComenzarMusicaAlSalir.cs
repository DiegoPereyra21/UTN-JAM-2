using UnityEngine;

public class ComenzarMusicaAlSalir : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //en el building no hay reloj de musicaasi q ahi no hace nada
        if (RelojMusica.Instance != null) RelojMusica.Instance.Comenzar();
    }
}