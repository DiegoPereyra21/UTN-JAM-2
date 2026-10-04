using System;
using UnityEngine;

public class Vida : MonoBehaviour
{
    [SerializeField] private int golpesMaximos = 6;

    private int golpesActuales = -1;

    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        Inicializar();
    }

    private void Inicializar()
    {
        if (golpesActuales < 0) golpesActuales = golpesMaximos;
    }

    public void RecibirGolpe()
    {
        Inicializar();
        if (golpesActuales <= 0)
            return;

        golpesActuales--;

        OnHealthChanged?.Invoke(golpesActuales);

        if (golpesActuales <= 0)
        {
            OnDeath?.Invoke();
        }
    }

    public bool IsDead()
    {
        Inicializar();
        return golpesActuales <= 0;
    }

    public int GetCurrentHealth()
    {
        Inicializar();
        return golpesActuales;
    }

    public int GetMaxHealth()
    {
        return golpesMaximos;
    }
}