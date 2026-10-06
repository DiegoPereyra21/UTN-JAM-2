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
    //cooldown del golpe en beats (0.5 = medio beat)
    [SerializeField] private float cooldownBeats = 0.5f;
    [SerializeField] private LayerMask capaEnemigos;
    //salto para q se pause un ratito arriba
    [SerializeField] private float alturaMaxima = 3f;
    //cuanto tarda en subir, en beats
    [SerializeField] private float beatsSubida = 0.25f;
    //cuanto espera arriba, en beats
    [SerializeField] private float beatsArriba = 1f;
    [SerializeField] private float gravedadCaida = 8f;
    //sonidos del golpe segun la altura, capaz ponga el mismo sonido ent odos pero x las dudas
    [SerializeField] private AudioSource fuenteSonido;
    [SerializeField] private AudioClip sonidoBajo;
    [SerializeField] private AudioClip sonidoMedio;
    [SerializeField] private AudioClip sonidoAlto;
    //sonido del golpe al aire
    [SerializeField] private AudioClip sonidoSlash;

    //animaciones
    private Animator animator;
    private bool muerto;
    //espera antes de destruirse para que se vea la animacion de muerte, y clavar el tiempo de duracion de dead
    [SerializeField] private float tiempoMuerte = 1.5f;

    //privadas
    private Rigidbody2D rb;
    private float gravedadNormal;
    private float proximoGolpe = -999f;
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
        //usa el audiosource del propio player si no aisgno
        if (fuenteSonido == null) fuenteSonido = GetComponent<AudioSource>();
        //busca el animator en el player o en su hijo
        animator = GetComponentInChildren<Animator>();
    }
    void Start()
    {
        //cuando la vida llega a 0 avisa y el player muere
        Vida vida = GetComponent<Vida>();
        if (vida != null) vida.OnDeath += Morir;
    }

    void OnDestroy()
    {
        Vida vida = GetComponent<Vida>();
        if (vida != null) vida.OnDeath -= Morir;
    }

    //al morir hace la animacion y despues se destruye asi se frena todo
    void Morir()
    {
        //por si lo golpean mas veces mientras muere
        if (muerto) return;
        muerto = true;
        animator.SetTrigger("Dead");
        Destroy(gameObject, tiempoMuerte);
    }
    void Update()
    {
        //en pausa, muerto o en la intro (antes del primer beat) no se puede hacer nada
        if (Time.timeScale == 0f || muerto || RelojMusica.Instance.BeatActual < 0f) return;

        //revisa si toca el suelo
        enSuelo = Physics2D.OverlapCircle(suelo.position, 0.1f, capaSuelo);

        //agacharse s o Lctrl
        agachado = enSuelo && !saltando && (Keyboard.current.sKey.isPressed || Keyboard.current.leftCtrlKey.isPressed);

        //saltar espacio o w, tambien si sigue agachado osea sale del agachado saltando
        if (enSuelo && !saltando && (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame))
        {
            agachado = false;
            StartCoroutine(Saltar());
        }

        //animacion de agachado (despues del salto, porque al saltar agachado se pone en false)
        animator.SetBool("Crouching", agachado);


        //para q si suelta caiga el player
        bool apretando = Keyboard.current.dKey.isPressed || Mouse.current.leftButton.isPressed || Mouse.current.rightButton.isPressed || Mouse.current.middleButton.isPressed;
        if (zombieSujeto != null && !apretando)
        {
            zombieSujeto.Soltar();
            zombieSujeto = null;
        }

        //atac con d o clicks
        bool click = Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame || Mouse.current.middleButton.wasPressedThisFrame;
        if ((Keyboard.current.dKey.wasPressedThisFrame || click) && RelojMusica.Instance.BeatActual >= proximoGolpe)
        {
            proximoGolpe = RelojMusica.Instance.BeatActual + cooldownBeats; // empieza el cooldown (en beats)
            Golpear();
        }
        //se queda en el ultimo frame al holdear el ataque
        animator.SetBool("Holding", zombieSujeto != null);
    }

    void Golpear()
    {
        //animacion de ataque aunq no pegue a algo, no se reinicia si ya esta sujetando
        if (zombieSujeto == null) animator.SetTrigger("Attack");
        //ifs para elegir donde golpear y que sonido usar
        Transform punto = golpeMedio;
        AudioClip sonido = sonidoMedio;
        if (agachado)
        {
            punto = golpeBajo;
            sonido = sonidoBajo;
        }
        else if (saltando)
        {
            punto = golpeAlto;
            sonido = sonidoAlto;
        }

        //verifica si hay algo ahi
        Collider2D[] golpeados = Physics2D.OverlapCircleAll(punto.position, radioGolpe, capaEnemigos);
        //guarda a quien ya golpee, si un zombi tiene varios colliders lo golpeaba varias veces
        HashSet<Vida> yaGolpeados = new HashSet<Vida>();
        bool huboGolpe = false;
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
                    huboGolpe = true;
                }
                continue;
            }

            //quita vida
            vida.RecibirGolpe();
            huboGolpe = true;

            if (!vida.IsDead())
            {
                //si sobrevivio, retrocede
                if (zombie != null)
                    zombie.Retroceder();
            }
        }

        //si le pego suena el sonido de su altura, si no pego suena el slash (no suena si esta sujetando un zombi)
        if (fuenteSonido != null)
        {
            if (huboGolpe)
            {
                if (sonido != null) fuenteSonido.PlayOneShot(sonido);
            }
            else if (zombieSujeto == null && sonidoSlash != null)
            {
                fuenteSonido.PlayOneShot(sonidoSlash);
            }
        }
    }

    IEnumerator Saltar()
    {
        animator.SetBool("Jumping", true);
        saltando = true;
        //max altura
        float yTope = transform.position.y + alturaMaxima;
        //velocidad para subir justo en los beats elegidos
        float velocidadSubida = alturaMaxima / (beatsSubida * RelojMusica.Instance.SegundosPorBeat);
        //corta gravedad al subir
        rb.gravityScale = 0;

        //sube hasta el max altura
        while (transform.position.y < yTope)
        {
            rb.linearVelocity = new Vector2(0, velocidadSubida);
            yield return null;
        }

        //espera arriba unos beats
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(beatsArriba * RelojMusica.Instance.SegundosPorBeat);
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
        animator.SetBool("Jumping", false);
    }
}