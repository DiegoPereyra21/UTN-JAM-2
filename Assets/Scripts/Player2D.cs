using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player2D : MonoBehaviour
{
    //para el piso
    [SerializeField] private Transform suelo;
    [SerializeField] private LayerMask capaSuelo;
    //ataque
    [SerializeField] private Transform golpeBajo;
    [SerializeField] private Transform golpeMedio;
    [SerializeField] private Transform golpeAlto;
    [SerializeField] private float radioGolpe = 0.5f;
    [SerializeField] private float cooldownGolpe = 0.5f;
    [SerializeField] private LayerMask capaEnemigos;
    //salto para q se pause un ratito arriba
    [SerializeField] private float alturaMaxima = 3f;
    [SerializeField] private float velocidadSubida = 25f;
    [SerializeField] private float tiempoArriba = 1f;
    [SerializeField] private float gravedadCaida = 8f;
    //privadas
    private Rigidbody2D rb;
    private float gravedadNormal;
    private float proximoGolpe;
    private bool enSuelo;
    private bool agachado;
    private bool saltando;
    //zombi que esta sujetando con el golpe alto
    private ZombiePuntos zombieSujeto;
    private bool cayendo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        //guardo la gravedad pero igual no planeo cambiarla
        gravedadNormal = rb.gravityScale;
    }
    void Update()
    {
        //revisa si toca el suelo
        enSuelo = Physics2D.OverlapCircle(suelo.position, 0.1f, capaSuelo);

        //agacharse s o Lctrl
        agachado = enSuelo && !saltando && (Keyboard.current.sKey.isPressed || Keyboard.current.leftCtrlKey.isPressed);

        //saltar espacio o w
        if (enSuelo && !saltando && !agachado && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
            StartCoroutine(Saltar());


        //para q si suelta caiga el player
        bool apretando = Keyboard.current.dKey.isPressed || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;
        if (zombieSujeto != null && !apretando)
        {
            zombieSujeto.Soltar();
            zombieSujeto = null;
        }

        //atac con d o clicks
        bool click = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame;
        if ((Keyboard.current.dKey.wasPressedThisFrame || click) && Time.time >= proximoGolpe)
        {
            proximoGolpe = Time.time + cooldownGolpe; // empieza el cooldown
            Golpear();
        }
    }

    void Golpear()
    {
        //ifs para elegir donde golpear 
        Transform punto = golpeMedio;
        if (agachado) punto = golpeBajo;
        else if (saltando) punto = golpeAlto;

        //verifica si hay algo ahi
        Collider2D[] golpeados = Physics2D.OverlapCircleAll(punto.position, radioGolpe, capaEnemigos);
        //guarda a quien ya golpee, si un zombi tiene varios colliders lo golpeaba varias veces
        HashSet<Vida> yaGolpeados = new HashSet<Vida>();
        foreach (Collider2D c in golpeados)
        {
            Vida vida = c.GetComponentInParent<Vida>();
            //si no tiene vida o ya lo golpee, sigue con el otro
            if (vida == null || !yaGolpeados.Add(vida)) continue;

            ZombiePuntos zombie = c.GetComponentInParent<ZombiePuntos>();

            //zombie q se mantiene, funciona distinto
            if (zombie != null && zombie.RequiereMantener)
            {
                if (punto == golpeAlto && !cayendo && zombieSujeto == null)
                {
                    zombieSujeto = zombie;
                    zombie.Sujetar();
                }
                continue;
            }

            //quita vida
            vida.RecibirGolpe();
            //los hace retroceder 1 espacio
            if (zombie != null) zombie.Retroceder();
        }
    }

    IEnumerator Saltar()
    {
        saltando = true;
        //max altura
        float yTope = transform.position.y + alturaMaxima;
        //corta gravedad al subir
        rb.gravityScale = 0;

        //sube rapido hasta el max altura
        while (transform.position.y < yTope)
        {
            rb.linearVelocity = new Vector2(0, velocidadSubida);
            yield return null;
        }

        //espera arriba un ratito
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(tiempoArriba);
        //si esta sujetando un zombi se queda arriba hasta soltar o hasta que muera
        yield return new WaitUntil(() => zombieSujeto == null);
        //baja rapidamente
        cayendo = true;
        rb.gravityScale = gravedadCaida;
        yield return new WaitUntil(() => enSuelo);
        //vuelve a normalidad
        cayendo = false;
        rb.gravityScale = gravedadNormal;
        saltando = false;
    }
}