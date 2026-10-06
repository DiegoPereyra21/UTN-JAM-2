using System;
using UnityEngine;

public class MainMenuCharacter : MonoBehaviour
{
    public event Action WalkingStarted;

    private Rigidbody2D rb;

    [SerializeField] private float walkSpeed = 2f;

    private Animator animator;
    private bool isWalking;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void StartDrink()
    {
        animator.SetTrigger("StartDrink");
    }

    // animation event
    public void StartWalking()
    {
        isWalking = true;
        //pasa directo a la animacion de caminar, sin esperar la transicion del animator
        animator.CrossFadeInFixedTime("walk", 0.05f);//pa q se sienta mas fluido

        WalkingStarted?.Invoke();
    }

    private void FixedUpdate()
    {
        if (!isWalking)
            return;

        rb.linearVelocity = new Vector2(
            walkSpeed,
            rb.linearVelocity.y
        );
    }
}