using UnityEngine;

public class XpOrb : MonoBehaviour
{
    [Header("=== XP VALUE ===")]
    public int xpValue = 5;

    [Header("=== MAGNETISM SETTINGS ===")]
    public float detectRadius = 4.5f;
    public float initialSpeed = 2.0f;
    public float acceleration = 6.0f;

    private Transform playerTarget;
    private Rigidbody2D rb;
    private float currentSpeed;
    private bool isBeingCollected = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
        }

        currentSpeed = initialSpeed;
        FindPlayerTarget();
    }

    void FixedUpdate()
    {
        if (isBeingCollected) return;

        if (playerTarget == null)
        {
            FindPlayerTarget();
            return;
        }

        float distance = Vector3.Distance(transform.position, playerTarget.position);
        if (distance <= detectRadius)
        {
            // Bay về phía Player
            Vector2 direction = ((Vector2)playerTarget.position - (Vector2)transform.position).normalized;
            currentSpeed += acceleration * Time.fixedDeltaTime;
            rb.linearVelocity = direction * currentSpeed;
        }
        else
        {
            // Lực ma sát nhẹ để giảm lực đẩy ban đầu từ quái chết
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, Time.fixedDeltaTime * 1.5f);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (isBeingCollected) return;

        // Nhận diện Player
        if (col.CompareTag("Player") || col.GetComponent<PlayerStats>() != null || col.GetComponentInParent<PlayerStats>() != null)
        {
            PlayerStats stats = col.GetComponent<PlayerStats>() ?? col.GetComponentInParent<PlayerStats>();
            if (stats == null && PlayerStats.Instance != null)
            {
                stats = PlayerStats.Instance;
            }

            if (stats != null && !stats.isDead)
            {
                isBeingCollected = true;
                rb.linearVelocity = Vector2.zero;

                // Cộng XP
                stats.AddXp(xpValue);

                // Tổng hợp và phát âm thanh "Ding" kiểu Minecraft
                PlayXpDingSound();

                // Hủy đối tượng
                Destroy(gameObject);
            }
        }
    }

    private void FindPlayerTarget()
    {
        if (PlayerStats.Instance != null)
        {
            playerTarget = PlayerStats.Instance.transform;
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
            }
        }
    }

    /// <summary>
    /// Tạo và phát tiếng "Ding" đặc trưng của Minecraft bằng cách tổng hợp sóng âm hình sin trực tiếp từ code.
    /// </summary>
    private void PlayXpDingSound()
    {
        GameObject soundObj = new GameObject("[Synth] XpDingSound");
        AudioSource audioSource = soundObj.AddComponent<AudioSource>();

        int sampleRate = 44100;
        float duration = 0.08f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        float frequency = 950f + Random.Range(-75f, 150f);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * (1.0f - (t / duration)) * 0.12f;
        }

        AudioClip clip = AudioClip.Create("XpDingClip", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);

        audioSource.clip = clip;
        audioSource.pitch = Random.Range(0.92f, 1.08f);
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.Play();

        Destroy(soundObj, duration + 0.1f);
    }
}
