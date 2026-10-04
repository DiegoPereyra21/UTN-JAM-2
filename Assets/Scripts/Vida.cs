using System;
using UnityEngine;

public class Vida : MonoBehaviour
{
    [SerializeField] private int golpesMaximos = 6;

    private int golpesActuales;

    public event Action<int> OnHealthChanged;
    public event Action OnDeath;

    private void Awake()
    {
        golpesActuales = golpesMaximos;
    }

    public void RecibirGolpe()
    {
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
        return golpesActuales <= 0;
    }

    public int GetCurrentHealth()
    {
        return golpesActuales;
    }

    public int GetMaxHealth()
    {
        return golpesMaximos;
    }
}