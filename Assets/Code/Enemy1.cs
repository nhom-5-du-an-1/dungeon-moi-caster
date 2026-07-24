using UnityEngine;
using System.Collections;

public class Enemy1 : MonoBehaviour
{
    [Header("Reference")]
    public Transform player;

    private Rigidbody2D rb;
    private Animator anim;

    [Header("Range")]
    public float detectRange = 7f;
    public float attackRange = 1.5f;
    public float moveSpeed = 2f;

    [Header("Attack")]
    public float attackCooldown = 1.2f;

    [Header("Layer")]
    public LayerMask wallLayer;

    [Header("Audio")]
    public AudioSource audioSource;

    public AudioClip scratchSound;
    public AudioClip biteSound;
    public AudioClip hurtSound;
    public AudioClip deathSound;

    private bool canAttack = true;
    private bool dead = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
    }

    void Update()
    {
        if (dead) return;

        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance > detectRange)
        {
            Idle();
            return;
        }

        

        Flip();

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

        if (Random.value < 0.5f)
            anim.SetTrigger("Attack1");
        else
            anim.SetTrigger("Attack2");

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
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

    public void PlayScratchSound()
    {
        audioSource.PlayOneShot(scratchSound);
    }

    public void PlayBiteSound()
    {
        audioSource.PlayOneShot(biteSound);
    }

    public void PlayHurtSound()
    {
        audioSource.PlayOneShot(hurtSound);
    }

    public void PlayDeathSound()
    {
        audioSource.PlayOneShot(deathSound);
    }
}