using UnityEngine;

public class EfectoSprites : MonoBehaviour
{
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float fps = 14f;

    private SpriteRenderer sr;
    private float tiempo;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.sprite = frames[0];
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