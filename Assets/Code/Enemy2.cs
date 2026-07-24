using UnityEngine;
using System.Collections;

public class Enemy2 : MonoBehaviour
{
    [Header("Reference")]
    public Transform player;

    private Rigidbody2D rb;
    private Animator anim;

    [Header("Range")]
    public float detectRange = 7f;
    public float attackRange = 1.5f;
    public float moveSpeed = 2f;

    [Header("Dash")]
    public float dashSpeed = 8f;
    public float dashDuration = 0.3f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;

    [Header("Layer")]
    public LayerMask wallLayer;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip dashSound;
    public AudioClip slashSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    private bool canAttack = true;
    private bool dead = false;

    private bool hasDashed = false;
    private bool isDashing = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (dead) return;
        if (isDashing) return;
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Ngoài tầm phát hiện
        if (distance > detectRange)
        {
            Idle();
            return;
        }

        // Nếu muốn quái không nhìn xuyên tường thì bỏ comment 4 dòng dưới
        /*
        if (!CanSeePlayer())
        {
            Idle();
            return;
        }
        */

        Flip();

        // Dash đúng 1 lần
        if (!hasDashed)
        {
            StartCoroutine(DashAttack());
            return;
        }

        // Đi tới Player
        if (distance > attackRange)
        {
            Chase();
        }
        else
        {
            Attack();
        }
    }

    void Chase()
    {
        Vector2 dir = (player.position - transform.position).normalized;

        rb.linearVelocity = dir * moveSpeed;

        anim.SetFloat("Speed", rb.linearVelocity.magnitude);
    }

    void Attack()
    {
        rb.linearVelocity = Vector2.zero;

        anim.SetFloat("Speed", 0);

        if (!canAttack) return;

        StartCoroutine(AttackRoutine());
    }

    IEnumerator AttackRoutine()
    {
        canAttack = false;

        anim.SetTrigger("Attack2");

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    IEnumerator DashAttack()
    {
        hasDashed = true;
        isDashing = true;

        rb.linearVelocity = Vector2.zero;

        anim.SetTrigger("Attack1");

        yield return new WaitForSeconds(0.25f);

        Vector2 dir = (player.position - transform.position).normalized;

        rb.linearVelocity = dir * dashSpeed;

        yield return new WaitForSeconds(dashDuration);

        rb.linearVelocity = Vector2.zero;

        isDashing = false;
    }

    void Idle()
    {
        rb.linearVelocity = Vector2.zero;

        anim.SetFloat("Speed", 0);
    }

    void Flip()
    {
        if (player.position.x > transform.position.x)
            transform.localScale = new Vector3(1, 1, 1);
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    bool CanSeePlayer()
    {
        Vector2 dir = player.position - transform.position;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            dir.normalized,
            detectRange,
            wallLayer | LayerMask.GetMask("Player"));

        if (hit.collider == null)
            return false;

        return hit.collider.CompareTag("Player");
    }

    public void Hurt()
    {
        anim.SetTrigger("Hurt");
    }

    public void Die()
    {
        dead = true;

        rb.linearVelocity = Vector2.zero;

        anim.SetBool("Dead", true);

        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }

    //================= AUDIO =================

    public void PlayDashSound()
    {
        if (dashSound != null)
            audioSource.PlayOneShot(dashSound);
    }

    public void PlaySlashSound()
    {
        if (slashSound != null)
            audioSource.PlayOneShot(slashSound);
    }

    public void PlayHurtSound()
    {
        if (hurtSound != null)
            audioSource.PlayOneShot(hurtSound);
    }

    public void PlayDeathSound()
    {
        if (deathSound != null)
            audioSource.PlayOneShot(deathSound);
    }
}