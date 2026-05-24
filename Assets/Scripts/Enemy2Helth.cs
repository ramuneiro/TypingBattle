using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class Enemy2Helth : MonoBehaviour
{
    public static event System.Action EnemyDied;
    public static event System.Action EnemyDodged;

    [SerializeField] private int maxHealth = 1000;
    [SerializeField] private int currentHealth;

    [Header("HPバー")]
    [SerializeField] private Transform hpFillTransform;

    [Header("ダメージ表示")]
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float damageTextDuration = 0.5f;
    [SerializeField] private float damageTextRiseDistance = 0.3f;

    [Header("被弾タイミング")]
    [SerializeField] private float playerHitApplyDelay = 0.25f;

    [Header("回避")]
    [SerializeField, Range(0f, 1f)] private float dodgeChance = 0.25f;
    [SerializeField] private float dodgeInvincibleDuration = 0.3f;

    [Header("死亡演出")]
    [SerializeField] private string deathTriggerName = "Death";
    [SerializeField] private float deathAnimationLeadTime = 0.4f;
    [SerializeField] private float deathFadeDuration = 3.0f;
    [SerializeField, Range(0.1f, 1f)] private float deathAnimSpeed = 0.75f;

    [Header("シーン遷移")]
    [SerializeField] private string nextSceneName = "ClearScene";
    [SerializeField] private float sceneFadeDuration = 1.2f;

    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip deathSe;
    [SerializeField] private AudioClip finalBlowSe;

    [Header("BGMフェードアウト")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private float bgmFadeOutDuration = 3.0f;

    private Vector3 hpInitialScale;
    private RectTransform hpFillRect;

    private float damageTextTimer;
    private Vector3 damageTextBaseLocalPos;

    private bool isDead;
    private float invincibleTimer;
    private Animator animator;
    private SpriteRenderer[] spriteRenderers;

    void Start()
    {
        ApplyDifficultyHealth();
        currentHealth = maxHealth;

        animator = GetComponent<Animator>();
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (seSource == null)
            seSource = GetComponent<AudioSource>();
        if (seSource == null)
            seSource = gameObject.AddComponent<AudioSource>();

        if (seSource != null)
        {
            seSource.playOnAwake = false;
            seSource.loop = false;
            seSource.spatialBlend = 0f;
            seSource.volume = 1f;
        }

        if (hpFillTransform != null)
        {
            hpInitialScale = hpFillTransform.localScale;
            hpFillRect = hpFillTransform as RectTransform;
            if (hpFillRect != null)
            {
                Vector2 p = hpFillRect.pivot;
                p.x = 1f;
                hpFillRect.pivot = p;
            }
        }

        if (damageText == null)
            damageText = GetComponentInChildren<TMP_Text>(true);

        if (damageText != null)
        {
            damageTextBaseLocalPos = damageText.transform.localPosition;
            damageText.text = "";
            damageText.gameObject.SetActive(true);
            damageText.enabled = true;
        }

        UpdateHpBar();

        if (TypingCombatSystem.Instance != null)
            TypingCombatSystem.Instance.SkillExecuted += OnPlayerSkillExecuted;
    }

    void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;

        // 死亡後はUpdateを止める（AnimStateの上書き防止)
        if (isDead)
            return;

        UpdateDamageTextFade();
    }

    void OnDestroy()
    {
        if (TypingCombatSystem.Instance != null)
            TypingCombatSystem.Instance.SkillExecuted -= OnPlayerSkillExecuted;
    }

    void ApplyDifficultyHealth()
    {
        GameSettings.Load();

        switch (GameSettings.CurrentDifficulty)
        {
            case GameSettings.Difficulty.Easy:
                maxHealth = 500;
                break;
            case GameSettings.Difficulty.Hard:
                maxHealth = 2000;
                break;
            case GameSettings.Difficulty.Normal:
            default:
                maxHealth = 1000;
                break;
        }
    }

    void OnPlayerSkillExecuted(SkillData skill, int damage)
    {
        if (isDead)
            return;

        if (skill.skillType == SkillType.Attack || skill.skillType == SkillType.Magic)
        {
            if (TryDodge())
                return;

            StartCoroutine(ApplyDamageDelayed(damage));
        }
    }

    IEnumerator ApplyDamageDelayed(int damage)
    {
        float delay = Mathf.Max(0f, playerHitApplyDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 遅延中に死亡していたら無視
        if (isDead)
            yield break;

        TakeDamage(damage);
    }

    bool TryDodge()
    {
        if (invincibleTimer > 0f)
            return true;

        if (Random.value > dodgeChance)
            return false;

        invincibleTimer = dodgeInvincibleDuration;

        // 物理ジャンプはBandit2側で処理（イベント経由）
        EnemyDodged?.Invoke();

        return true;
    }

    public void TakeDamage(int damage)
    {
        if (isDead || invincibleTimer > 0f)
            return;

        int nextHealth = Mathf.Max(0, currentHealth - damage);
        bool isFinalBlow = nextHealth <= 0;

        currentHealth = nextHealth;
        UpdateHpBar();
        ShowDamageText(damage);

        if (isFinalBlow)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 死亡モーションをスロー再生（他のAnimatorパラメータより後に設定）
        if (animator != null)
        {
            animator.speed = Mathf.Max(0.1f, deathAnimSpeed);
            animator.SetInteger("AnimState", 0);
            animator.SetBool("Grounded", true);
            animator.ResetTrigger("Attack");
            animator.SetTrigger(deathTriggerName);
        }

        float seVol = Mathf.Clamp01(GameSettings.SEVolume);
        if (seVol <= 0.001f) seVol = 1f;

        if (finalBlowSe != null && seSource != null)
            seSource.PlayOneShot(finalBlowSe, seVol);
        else if (deathSe != null && seSource != null)
            seSource.PlayOneShot(deathSe, seVol);

        if (damageText != null)
            damageText.text = "";

        UpdateHpBar();

        // BGMフェードアウト開始
        StartCoroutine(FadeOutBGM());

        EnemyDied?.Invoke();
        StartCoroutine(FadeOutAndDisable());
    }

    IEnumerator FadeOutBGM()
    {
        // bgmSourceが未設定の場合はloop中のAudioSourceを自動検索
        if (bgmSource == null)
        {
            AudioSource[] allSources = Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
            foreach (var src in allSources)
            {
                if (src != seSource && src.isPlaying && src.loop)
                {
                    bgmSource = src;
                    break;
                }
            }
        }

        if (bgmSource == null)
            yield break;

        float startVolume = bgmSource.volume;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, bgmFadeOutDuration);

        while (elapsed < duration && bgmSource != null)
        {
            elapsed += Time.deltaTime;
            bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        if (bgmSource != null)
        {
            bgmSource.volume = 0f;
            bgmSource.Stop();
        }
    }

    IEnumerator FadeOutAndDisable()
    {
        // 死亡モーションが少し流れるまで待つ
        if (deathAnimationLeadTime > 0f)
            yield return new WaitForSeconds(deathAnimationLeadTime);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, deathFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);

            if (spriteRenderers != null)
            {
                for (int i = 0; i < spriteRenderers.Length; i++)
                {
                    var sr = spriteRenderers[i];
                    if (sr == null) continue;
                    Color c = sr.color;
                    c.a = alpha;
                    sr.color = c;
                }
            }

            yield return null;
        }

        // 速度を戻してスプライトを完全に非表示
        if (animator != null)
            animator.speed = 1f;

        if (spriteRenderers != null)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                    spriteRenderers[i].enabled = false;
            }
        }

        // スコアを確定（フェードアウト前に確実に保存）
        ScoreManager.FinalizeScore();
        PlayerPrefs.Save(); // WebGL向けに明示的に保存

        // フェードアウト用Canvas生成
        GameObject fadeCanvasObject = new GameObject("SceneFadeCanvas");
        Canvas canvas = fadeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        DontDestroyOnLoad(fadeCanvasObject);
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
        float fadeDuration = Mathf.Max(0.01f, sceneFadeDuration);
        group.alpha = 0f;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    void UpdateHpBar()
    {
        if (hpFillTransform == null)
            return;

        float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector3 scale = hpInitialScale;
        scale.x = hpInitialScale.x * ratio;
        hpFillTransform.localScale = scale;
    }

    void ShowDamageText(int damage)
    {
        if (damageText == null)
            return;

        damageText.gameObject.SetActive(true);
        damageText.enabled = true;
        damageText.transform.localPosition = damageTextBaseLocalPos;
        damageText.text = damage.ToString();

        Color c = Color.red;
        c.a = 1f;
        damageText.color = c;

        damageTextTimer = Mathf.Max(0.01f, damageTextDuration);
    }

    void UpdateDamageTextFade()
    {
        if (damageText == null)
            return;

        if (damageTextTimer > 0f)
        {
            damageTextTimer -= Time.deltaTime;
            float t = Mathf.Clamp01(damageTextTimer / Mathf.Max(0.01f, damageTextDuration));

            Color c = damageText.color;
            c.a = t;
            damageText.color = c;

            float riseT = 1f - t;
            damageText.transform.localPosition = damageTextBaseLocalPos + Vector3.up * (damageTextRiseDistance * riseT);

            if (damageTextTimer <= 0f)
            {
                damageText.text = "";
                damageText.transform.localPosition = damageTextBaseLocalPos;
            }
        }
    }

    public int GetCurrentHealth() => currentHealth;
    public int GetMaxHealth() => maxHealth;
    public float GetHealthPercentage() => (float)currentHealth / maxHealth;
}
