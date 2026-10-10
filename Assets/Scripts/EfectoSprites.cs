using UnityEngine;

public class EfectoSprites : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 14f;
    //corrimiento del efecto respecto al punto donde aparece (se invierte solo si el efecto esta espejado)
    [SerializeField] private Vector2 offset;

    private SpriteRenderer sr;
    private float tiempo;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
    }
    private void Start()
    {
        float lado = Mathf.Sign(transform.localScale.x);
        transform.position += new Vector3(offset.x * lado, offset.y, 0f);
    }
    private void Update()
    {
        //avanza un frame segun el tiempo, al terminar se destruye
        tiempo += Time.deltaTime;
        int indice = (int)(tiempo * fps);
        if (indice >= frames.Length) { Destroy(gameObject); return; }
        sr.sprite = frames[indice];
    }
}