using UnityEngine;
using System.Collections;

public class HeroKnight : MonoBehaviour {

    [SerializeField] float      m_speed = 4.0f;
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] float      m_rollForce = 6.0f;
    [SerializeField] bool       m_noBlood = false;
    [SerializeField] GameObject m_slideDust;
    [SerializeField] Vector2    m_enemyStartPosition = new Vector2(4f, -3.16f);
    [SerializeField] float      m_attackInterval = 3.0f;
    [SerializeField] int        m_enemyAttackMinDamage = 11;
    [SerializeField] int        m_enemyAttackMaxDamage = 18;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_HeroKnight   m_groundSensor;
    private Sensor_HeroKnight   m_wallSensorR1;
    private Sensor_HeroKnight   m_wallSensorR2;
    private Sensor_HeroKnight   m_wallSensorL1;
    private Sensor_HeroKnight   m_wallSensorL2;
    private bool                m_isWallSliding = false;
    private bool                m_grounded = false;
    private bool                m_rolling = false;
    private int                 m_facingDirection = 1;
    private int                 m_currentAttack = 0;
    private float               m_timeSinceAttack = 0.0f;
    private float               m_delayToIdle = 0.0f;
    private float               m_rollDuration = 8.0f / 14.0f;
    private float               m_rollCurrentTime;
    private float               m_enemyAttackTimer = 0.0f;
    private PlayerHealth        m_playerHealth;
    private EnemyHealth         m_enemyHealth;


    // Use this for initialization
    void Start ()
    {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR1 = transform.Find("WallSensor_R1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorR2 = transform.Find("WallSensor_R2").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL1 = transform.Find("WallSensor_L1").GetComponent<Sensor_HeroKnight>();
        m_wallSensorL2 = transform.Find("WallSensor_L2").GetComponent<Sensor_HeroKnight>();

        m_playerHealth = FindObjectOfType<PlayerHealth>();
        m_enemyHealth = GetComponent<EnemyHealth>();

        // 敵の初期位置に配置
        transform.position = new Vector3(m_enemyStartPosition.x, m_enemyStartPosition.y, transform.position.z);

        // 左向きに設定
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
            sr.flipX = true;
        m_facingDirection = -1;
    }

    // Update is called once per frame
    void Update ()
    {
        // HP0なら攻撃停止
        bool dead = (m_enemyHealth != null && m_enemyHealth.GetCurrentHealth() <= 0);

        // Increase timer that controls attack combo
        m_timeSinceAttack += Time.deltaTime;

        // Increase timer that checks roll duration
        if(m_rolling)
            m_rollCurrentTime += Time.deltaTime;

        // Disable rolling if timer extends duration
        if(m_rollCurrentTime > m_rollDuration)
            m_rolling = false;

        //Check if character just landed on the ground
        if (!m_grounded && m_groundSensor.State())
        {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        //Check if character just started falling
        if (m_grounded && !m_groundSensor.State())
        {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // 敵は移動しない
        m_body2d.linearVelocity = new Vector2(0, m_body2d.linearVelocity.y);

        //Set AirSpeed in animator
        m_animator.SetFloat("AirSpeedY", m_body2d.linearVelocity.y);

        //Wall Slide
        m_isWallSliding = (m_wallSensorR1.State() && m_wallSensorR2.State()) || (m_wallSensorL1.State() && m_wallSensorL2.State());
        m_animator.SetBool("WallSlide", m_isWallSliding);

        // 死亡中は攻撃しない
        if (!dead)
        {
            // 3秒に1回自動攻撃
            m_enemyAttackTimer += Time.deltaTime;
            if (m_enemyAttackTimer >= m_attackInterval && !m_rolling)
            {
                m_enemyAttackTimer = 0.0f;
                m_animator.SetTrigger("Attack1");
                m_timeSinceAttack = 0.0f;

                // コライダー不要の直接命中（11～18ランダム）
                if (m_playerHealth != null)
                {
                    int damage = Random.Range(m_enemyAttackMinDamage, m_enemyAttackMaxDamage + 1);
                    m_playerHealth.TakeDamage(damage);
                }
            }
        }

        //Idle
        m_delayToIdle -= Time.deltaTime;
        if(m_delayToIdle < 0)
            m_animator.SetInteger("AnimState", 0);
    }

    // Animation Events
    // Called in slide animation.
    void AE_SlideDust()
    {
        Vector3 spawnPosition;

        if (m_facingDirection == 1)
            spawnPosition = m_wallSensorR2.transform.position;
        else
            spawnPosition = m_wallSensorL2.transform.position;

        if (m_slideDust != null)
        {
            // Set correct arrow spawn position
            GameObject dust = Instantiate(m_slideDust, spawnPosition, gameObject.transform.localRotation) as GameObject;
            // Turn arrow in correct direction
            dust.transform.localScale = new Vector3(m_facingDirection, 1, 1);
        }
    }
}
