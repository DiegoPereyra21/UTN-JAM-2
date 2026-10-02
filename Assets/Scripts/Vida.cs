using UnityEngine;

public class Vida : MonoBehaviour
{
    [SerializeField] private int golpesMaximos = 1;

    private int golpesActuales;

    void Awake()
    {
        golpesActuales = golpesMaximos;
    }

    public void RecibirGolpe()
    {
        golpesActuales--;
        if (golpesActuales <= 0)
            Destroy(gameObject);
    }
}