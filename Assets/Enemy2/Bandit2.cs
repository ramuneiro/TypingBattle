using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Bandit2 : MonoBehaviour {

    [SerializeField] float      m_speed = 4.0f;
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] bool       m_enableDirectControl = false;
    [SerializeField] float      m_gravityScale = 3.0f;
    [SerializeField] bool       m_useTypingInput = false;

    [Header("攻撃SE（強さ4段階）")]
    [SerializeField] private AudioSource m_seSource;
    [SerializeField] private AudioClip m_attackSeLv1;
    [SerializeField] private AudioClip m_attackSeLv2;
    [SerializeField] private AudioClip m_attackSeLv3;
    [SerializeField] private AudioClip m_attackSeLv4;

    [Header("自動移動設定")]
    [SerializeField] Vector2    m_spawnPosition = new Vector2(-10f, -3.2f);
    [SerializeField] float      m_targetX = 4f;
    [SerializeField] bool       m_autoWalkOnStart = true;
    [SerializeField] float      m_playerScale = 3.0f;

    [Header("敵自動攻撃")]
    [SerializeField] float      m_attackInterval = 3.0f;
    [SerializeField] int        m_attackMinDamage = 11;
    [SerializeField] int        m_attackMaxDamage = 18;
    [SerializeField, Range(0f, 1f)] float m_heavyAttackChance = 0.25f;
    [SerializeField] float      m_attackDamageDelay = 0.55f;

    [Header("撃破後遷移")]
    [SerializeField] float      m_exitTargetX = 12f;
    [SerializeField] float      m_sceneFadeDuration = 1.2f;
    [SerializeField] string     m_nextSceneName = "ClearScene";
    [SerializeField] float      m_victoryMoveDelay = 3.4f;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_Bandit       m_groundSensor;
    private PlayerHealth        m_playerHealth;
    private Enemy2Helth         m_enemy2Health;
    private bool                m_grounded = false;
    private bool                m_combatIdle = false;
    private bool                m_isDead = false;
    private bool                m_isAutoWalking = false;
    private bool                m_hasReachedTarget = false;
    private bool                m_hasLanded = false;
    private bool                m_isVictoryMoving = false;
    private bool                m_isSceneLoading = false;
    private bool                m_isAttackInProgress = false;
    private float               m_enemyAttackTimer = 0.0f;
    private SpriteRenderer      m_spriteRenderer;
    private SpriteRenderer[]    m_allSpriteRenderers;

    void Start () {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_Bandit>();
        m_playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        m_enemy2Health = Object.FindFirstObjectByType<Enemy2Helth>();
        m_spriteRenderer = GetComponent<SpriteRenderer>();
        m_allSpriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (m_seSource == null)
            m_seSource = GetComponent<AudioSource>();
        if (m_seSource == null)
            m_seSource = gameObject.AddComponent<AudioSource>();

        if (m_seSource != null)
        {
            m_seSource.playOnAwake = false;
            m_seSource.loop = false;
            m_seSource.spatialBlend = 0f;
            m_seSource.volume = 1f;
        }

        if (m_body2d != null)
            m_body2d.gravityScale = m_gravityScale;

        if (m_autoWalkOnStart)
        {
            transform.position = m_spawnPosition;
            m_isAutoWalking = true;
            m_grounded = false;
            m_hasLanded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }
        else
        {
            m_grounded = true;
            m_hasLanded = true;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // 左向き固定
        SetFacingLeft(true);

        if (m_useTypingInput && TypingCombatSystem.Instance != null)
        {
            TypingCombatSystem.Instance.SkillExecuted += OnSkillExecuted;
            TypingCombatSystem.Instance.SetInputActive(!m_autoWalkOnStart);
        }

        Enemy2Helth.EnemyDied += OnEnemyDied;
        Enemy2Helth.EnemyDodged += OnEnemyDodged;
    }

    void OnDestroy()
    {
        if (m_useTypingInput && TypingCombatSystem.Instance != null)
            TypingCombatSystem.Instance.SkillExecuted -= OnSkillExecuted;

        Enemy2Helth.EnemyDied -= OnEnemyDied;
        Enemy2Helth.EnemyDodged -= OnEnemyDodged;
    }

    void OnEnemyDied()
    {
        if (m_useTypingInput && TypingCombatSystem.Instance != null)
            TypingCombatSystem.Instance.SetInputActive(false);

        m_isAutoWalking = false;
        m_combatIdle = false;
        m_body2d.linearVelocity = new Vector2(0f, m_body2d.linearVelocity.y);
        m_animator.SetInteger("AnimState", 0);
        // シーン遷移はEnemy2Helth側で処理するため勝利移動は開始しない
    }

    void OnEnemyDodged()
    {
        if (!m_grounded)
            return;

        m_animator.SetTrigger("Jump");
        m_grounded = false;
        m_animator.SetBool("Grounded", m_grounded);
        m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, m_jumpForce);
        m_groundSensor.Disable(0.35f);
    }

    IEnumerator BeginVictoryMoveAfterDelay()
    {
        m_body2d.linearVelocity = new Vector2(0f, m_body2d.linearVelocity.y);
        m_animator.SetInteger("AnimState", 0);

        if (m_victoryMoveDelay > 0f)
            yield return new WaitForSeconds(m_victoryMoveDelay);

        m_isVictoryMoving = true;
    }

    void OnSkillExecuted(SkillData skill, int damage)
    {
        switch (skill.skillType)
        {
            case SkillType.Attack:
                m_animator.SetTrigger("Attack");
                PlayAttackSeByDamage(damage);
                break;
            case SkillType.Defend:
                m_combatIdle = true;
                break;
            case SkillType.Dodge:
                if (m_grounded)
                {
                    m_animator.SetTrigger("Jump");
                    m_grounded = false;
                    m_animator.SetBool("Grounded", m_grounded);
                    m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, m_jumpForce);
                    float invincible = 0.2f;
                    if (TypingCombatSystem.Instance != null)
                        invincible = TypingCombatSystem.Instance.GetDodgeInvincibleDuration(skill);
                    m_groundSensor.Disable(invincible);
                    if (m_playerHealth != null)
                        m_playerHealth.SetInvincible(invincible);
                }
                break;
            case SkillType.Heal:
                if (m_playerHealth != null && TypingCombatSystem.Instance != null)
                {
                    int healAmount = TypingCombatSystem.Instance.GetHealAmount(skill);
                    m_playerHealth.Heal(healAmount);
                }
                break;
            case SkillType.Magic:
                m_animator.SetTrigger("Attack");
                PlayAttackSeByDamage(damage);
                break;
        }
    }

    void PlayAttackSeByDamage(int damage)
    {
        if (m_seSource == null) return;

        AudioClip clip;
        if (damage <= 30) clip = m_attackSeLv1;
        else if (damage <= 60) clip = m_attackSeLv2;
        else if (damage <= 100) clip = m_attackSeLv3;
        else clip = m_attackSeLv4;

        if (clip == null)
            clip = m_attackSeLv1 ?? m_attackSeLv2 ?? m_attackSeLv3 ?? m_attackSeLv4;

        float seVol = Mathf.Clamp01(GameSettings.SEVolume);
        if (seVol <= 0.001f) seVol = 1f;
        if (clip != null)
            m_seSource.PlayOneShot(clip, seVol);
    }

    void SetFacingLeft(bool left)
    {
        float xSign = left ? 1f : -1f;
        transform.localScale = new Vector3(xSign * m_playerScale, m_playerScale, m_playerScale);
    }

    void Update () {
        if (!m_grounded && m_groundSensor.State()) {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
            if (!m_hasLanded && m_isAutoWalking)
                m_hasLanded = true;
        }
        if (m_grounded && !m_groundSensor.State()) {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        if (m_isVictoryMoving) { ProcessVictoryMove(); return; }

        if (m_isAutoWalking && !m_hasReachedTarget) { ProcessAutoWalk(); return; }

        // 敵自動攻撃
        if (!m_enableDirectControl)
        {
            m_body2d.linearVelocity = new Vector2(0, m_body2d.linearVelocity.y);
            m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);

            bool enemyDead = m_enemy2Health != null && m_enemy2Health.GetCurrentHealth() <= 0;
            if (!enemyDead && !m_isAttackInProgress)
            {
                m_enemyAttackTimer += Time.deltaTime;
                if (m_enemyAttackTimer >= m_attackInterval)
                {
                    m_enemyAttackTimer = 0f;
                    StartCoroutine(PerformEnemyAttack());
                }
            }

            if (m_combatIdle)
                m_animator.SetInteger("AnimState", 1);
            else
                m_animator.SetInteger("AnimState", 0);

            return;
        }

        // 直接操作が有効な場合
        float inputX = Input.GetAxis("Horizontal");
        if (inputX > 0) SetFacingLeft(false);
        else if (inputX < 0) SetFacingLeft(true);

        m_body2d.linearVelocity = new Vector2(inputX * m_speed, m_body2d.linearVelocity.y);
        m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);

        if (Input.GetKeyDown("e")) {
            if (!m_isDead) m_animator.SetTrigger("Death");
            else m_animator.SetTrigger("Recover");
            m_isDead = !m_isDead;
        }
        else if (Input.GetKeyDown("q")) m_animator.SetTrigger("Hurt");
        else if (Input.GetKeyDown("f")) m_combatIdle = !m_combatIdle;
        else if (Input.GetKeyDown("space") && m_grounded) {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }
        else if (Mathf.Abs(inputX) > Mathf.Epsilon) m_animator.SetInteger("AnimState", 2);
        else if (m_combatIdle) m_animator.SetInteger("AnimState", 1);
        else m_animator.SetInteger("AnimState", 0);
    }

    IEnumerator PerformEnemyAttack()
    {
        m_isAttackInProgress = true;

        bool heavy = Random.value < m_heavyAttackChance;
        float speedMul = heavy ? (1f / 3f) : 1f;

        if (m_animator != null)
            m_animator.speed = speedMul;

        m_animator.SetTrigger("Attack");

        float delay = Mathf.Max(0.01f, m_attackDamageDelay / speedMul);
        yield return new WaitForSeconds(delay);

        // 待機中に敵が死んでいたら攻撃しない
        bool enemyDead = m_enemy2Health != null && m_enemy2Health.GetCurrentHealth() <= 0;
        if (!enemyDead && m_playerHealth != null)
        {
            int damage = Random.Range(m_attackMinDamage, m_attackMaxDamage + 1);
            if (heavy) damage *= 2;
            m_playerHealth.TakeDamage(damage);
        }

        if (m_animator != null)
            m_animator.speed = 1f;

        m_isAttackInProgress = false;
    }

    void ProcessAutoWalk()
    {
        if (!m_hasLanded)
        {
            m_animator.SetInteger("AnimState", 0);
            m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);
            return;
        }

        float dir = Mathf.Sign(m_targetX - transform.position.x);
        if (Mathf.Abs(m_targetX - transform.position.x) <= 0.05f)
        {
            m_hasReachedTarget = true;
            m_isAutoWalking = false;
            m_body2d.linearVelocity = new Vector2(0, m_body2d.linearVelocity.y);
            m_animator.SetInteger("AnimState", 0);
            m_combatIdle = false;
            if (m_useTypingInput && TypingCombatSystem.Instance != null)
                TypingCombatSystem.Instance.SetInputActive(true);
            return;
        }

        m_body2d.linearVelocity = new Vector2(dir * m_speed, m_body2d.linearVelocity.y);
        SetFacingLeft(dir < 0f);
        m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);
        m_animator.SetInteger("AnimState", 2);
        m_animator.SetBool("Grounded", true);
    }

    void ProcessVictoryMove()
    {
        if (transform.position.x >= m_exitTargetX)
        {
            m_body2d.linearVelocity = new Vector2(0, m_body2d.linearVelocity.y);
            m_animator.SetInteger("AnimState", 0);
            if (!m_isSceneLoading)
            {
                m_isSceneLoading = true;
                StartCoroutine(FadeOutAndLoadNextScene());
            }
            return;
        }

        m_body2d.linearVelocity = new Vector2(m_speed, m_body2d.linearVelocity.y);
        SetFacingLeft(false);
        m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);
        m_animator.SetInteger("AnimState", 2);
        m_animator.SetBool("Grounded", true);
    }

    IEnumerator FadeOutAndLoadNextScene()
    {
        GameObject fadeCanvasObject = new GameObject("SceneFadeCanvas");
        Canvas canvas = fadeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        CanvasGroup group = fadeCanvasObject.AddComponent<CanvasGroup>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvasObject.transform, false);
        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        float t = 0f;
        float duration = Mathf.Max(0.01f, m_sceneFadeDuration);
        group.alpha = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        SceneManager.LoadScene(m_nextSceneName);
    }
}
