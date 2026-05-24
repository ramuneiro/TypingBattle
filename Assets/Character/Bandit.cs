using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class Bandit : MonoBehaviour {

    [SerializeField] float      m_speed = 4.0f;
    [SerializeField] float      m_jumpForce = 7.5f;
    [SerializeField] bool       m_enableDirectControl = false;  // 直接操作を許可するか（デフォルト: false）
    [SerializeField] float      m_gravityScale = 3.0f; // 重力

    [Header("攻撃SE（強さ4段階）")]
    [SerializeField] private AudioSource m_seSource;
    [SerializeField] private AudioClip m_attackSeLv1;
    [SerializeField] private AudioClip m_attackSeLv2;
    [SerializeField] private AudioClip m_attackSeLv3;
    [SerializeField] private AudioClip m_attackSeLv4;

    [Header("自動移動設定")]
    [SerializeField] Vector2    m_spawnPosition = new Vector2(-10f, -3.2f);  // スポーン位置（Y座標は落下用）
    [SerializeField] float      m_targetX = 4f;  // 目標X座標（スポーンより右）
    [SerializeField] bool       m_autoWalkOnStart = true;  // ゲーム開始時に自動歩行するか
    [SerializeField] float      m_playerScale = 3.0f;  // プレイヤーのサイズ（デフォルト: 3）

    [Header("撃破後遷移")]
    [SerializeField] float      m_exitTargetX = 12f;
    [SerializeField] float      m_sceneFadeDuration = 1.2f;
    [SerializeField] string     m_nextSceneName = "Game2Scene";
    [SerializeField] float      m_victoryMoveDelay = 3.4f;

    private Animator            m_animator;
    private Rigidbody2D         m_body2d;
    private Sensor_Bandit       m_groundSensor;
    private PlayerHealth        m_playerHealth;
    private bool                m_grounded = false;
    private bool                m_combatIdle = false;
    private bool                m_isDead = false;
    private bool                m_isAutoWalking = false;  // 自動歩行中かどうか
    private bool                m_hasReachedTarget = false;  // 目標地点に到達したか
    private bool                m_hasLanded = false;  // 最初に着地したかどうか
    private bool                m_isVictoryMoving = false;
    private bool                m_isSceneLoading = false;

    void Start () {
        m_animator = GetComponent<Animator>();
        m_body2d = GetComponent<Rigidbody2D>();
        m_groundSensor = transform.Find("GroundSensor").GetComponent<Sensor_Bandit>();
        m_playerHealth = GetComponent<PlayerHealth>();

        if (m_seSource == null)
            m_seSource = GetComponent<AudioSource>();
        if (m_seSource == null)
            m_seSource = gameObject.AddComponent<AudioSource>();

        if (m_seSource != null)
        {
            m_seSource.playOnAwake = false;
            m_seSource.loop = false;
            m_seSource.spatialBlend = 0f;
            m_seSource.volume = 1f; // 実際の音量は GameSettings.SEVolume で制御
        }

        // 重力スケールを適用
        if (m_body2d != null)
            m_body2d.gravityScale = m_gravityScale;

        // スポーン位置に移動
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

        // プレイヤーのサイズを設定（右向き）
        transform.localScale = new Vector3(-m_playerScale, m_playerScale, m_playerScale);

        // TypingCombatSystemのイベントを購読
        if (TypingCombatSystem.Instance != null)
        {
            TypingCombatSystem.Instance.SkillExecuted += OnSkillExecuted;
            // 自動移動中は入力無効、すぐ戦闘開始なら有効
            TypingCombatSystem.Instance.SetInputActive(!m_autoWalkOnStart);
        }

        EnemyHealth.EnemyDied += OnEnemyDied;
    }

    void OnDestroy()
    {
        if (TypingCombatSystem.Instance != null)
        {
            TypingCombatSystem.Instance.SkillExecuted -= OnSkillExecuted;
        }

        EnemyHealth.EnemyDied -= OnEnemyDied;
    }

    void OnEnemyDied()
    {
        if (TypingCombatSystem.Instance != null)
            TypingCombatSystem.Instance.SetInputActive(false);

        m_isAutoWalking = false;
        m_combatIdle = false;

        // 敵の消滅演出を待ってから移動開始
        StartCoroutine(BeginVictoryMoveAfterDelay());
    }

    IEnumerator BeginVictoryMoveAfterDelay()
    {
        m_body2d.linearVelocity = new Vector2(0f, m_body2d.linearVelocity.y);
        m_animator.SetInteger("AnimState", 0);

        if (m_victoryMoveDelay > 0f)
            yield return new WaitForSeconds(m_victoryMoveDelay);

        m_isVictoryMoving = true;
    }

    /// <summary>
    /// 技が発動された時の処理
    /// </summary>
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
        if (m_seSource == null)
            return;

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
	
	void Update () {
        if (!m_grounded && m_groundSensor.State()) {
            m_grounded = true;
            m_animator.SetBool("Grounded", m_grounded);
            if (!m_hasLanded && m_isAutoWalking)
                m_hasLanded = true;
        }

        if(m_grounded && !m_groundSensor.State()) {
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
        }

        // 勝利移動処理
        if (m_isVictoryMoving)
        {
            ProcessVictoryMove();
            return;
        }

        // 自動歩行処理
        if (m_isAutoWalking && !m_hasReachedTarget)
        {
            ProcessAutoWalk();
            return;
        }

        // 直接操作が無効の場合
        if (!m_enableDirectControl)
        {
            m_body2d.linearVelocity = new Vector2(0, m_body2d.linearVelocity.y);
            m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);

            if (m_combatIdle)
                m_animator.SetInteger("AnimState", 1);
            else
                m_animator.SetInteger("AnimState", 0);

            return;
        }

        // 以下は直接操作が有効な場合のみ
        float inputX = Input.GetAxis("Horizontal");

        if (inputX > 0)
            transform.localScale = new Vector3(-m_playerScale, m_playerScale, m_playerScale);
        else if (inputX < 0)
            transform.localScale = new Vector3(m_playerScale, m_playerScale, m_playerScale);

        m_body2d.linearVelocity = new Vector2(inputX * m_speed, m_body2d.linearVelocity.y);
        m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);

        // デバッグ用操作
        if (Input.GetKeyDown("e")) {
            if(!m_isDead)
                m_animator.SetTrigger("Death");
            else
                m_animator.SetTrigger("Recover");
            m_isDead = !m_isDead;
        }
        else if (Input.GetKeyDown("q"))
            m_animator.SetTrigger("Hurt");
        else if (Input.GetKeyDown("f"))
            m_combatIdle = !m_combatIdle;
        else if (Input.GetKeyDown("space") && m_grounded) {
            m_animator.SetTrigger("Jump");
            m_grounded = false;
            m_animator.SetBool("Grounded", m_grounded);
            m_body2d.linearVelocity = new Vector2(m_body2d.linearVelocity.x, m_jumpForce);
            m_groundSensor.Disable(0.2f);
        }
        else if (Mathf.Abs(inputX) > Mathf.Epsilon)
            m_animator.SetInteger("AnimState", 2);
        else if (m_combatIdle)
            m_animator.SetInteger("AnimState", 1);
        else
            m_animator.SetInteger("AnimState", 0);
    }

    /// <summary>
    /// 自動歩行処理
    /// </summary>
    void ProcessAutoWalk()
    {
        if (!m_hasLanded)
        {
            m_animator.SetInteger("AnimState", 0);
            m_animator.SetFloat("AirSpeed", m_body2d.linearVelocity.y);
            return;
        }

        if (transform.position.x >= m_targetX)
        {
            m_hasReachedTarget = true;
            m_isAutoWalking = false;
            m_body2d.linearVelocity = new Vector2(0, m_body2d.linearVelocity.y);
            m_animator.SetInteger("AnimState", 0); // 到達時はIdle
            m_combatIdle = false;

            // 到達後に入力解禁
            if (TypingCombatSystem.Instance != null)
                TypingCombatSystem.Instance.SetInputActive(true);

            return;
        }

        // 右方向に移動
        m_body2d.linearVelocity = new Vector2(m_speed, m_body2d.linearVelocity.y);
        transform.localScale = new Vector3(-m_playerScale, m_playerScale, m_playerScale);
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
        transform.localScale = new Vector3(-m_playerScale, m_playerScale, m_playerScale);
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
