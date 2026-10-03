using System;
using UnityEngine;

public class Vida : MonoBehaviour
{
    [SerializeField] private int golpesMaximos = 1;

    private int golpesActuales;

    public event Action OnDeath;

    void Awake()
    {
        golpesActuales = golpesMaximos;
    }

    public void RecibirGolpe()
    {
        golpesActuales--;

        if (golpesActuales <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    public bool IsDead()
    {
        return golpesActuales <= 0;
    }
}