using UnityEngine;
using TMPro;
using System.Collections;

/// <summary>
/// 敵のHPを管理するクラス
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    public static event System.Action EnemyDied;

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("HPバー")]
    [SerializeField] private Transform hpFillTransform;

    [Header("ダメージ表示")]
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float damageTextDuration = 0.5f;
    [SerializeField] private float damageTextRiseDistance = 0.3f;

    [Header("死亡演出")]
    [SerializeField] private string deathTriggerName = "Death";
    [SerializeField] private float deathAnimationLeadTime = 0.4f;
    [SerializeField] private float deathFadeDuration = 3.0f;

    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip deathSe;

    private Vector3 hpInitialScale;
    private RectTransform hpFillRect;

    private float damageTextTimer;
    private Vector3 damageTextBaseLocalPos;

    private bool isDead = false;
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
            seSource.volume = 1f; // 実際の音量は GameSettings.SEVolume で制御
        }

        if (hpFillTransform != null)
        {
            hpInitialScale = hpFillTransform.localScale;
            hpFillRect = hpFillTransform as RectTransform;

            // UIバーの場合: 右を固定して減らす（右→左）
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
        {
            TypingCombatSystem.Instance.SkillExecuted += OnPlayerSkillExecuted;
        }
    }

    void Update()
    {
        UpdateDamageTextFade();
    }

    void ApplyDifficultyHealth()
    {
        GameSettings.Load();

        switch (GameSettings.CurrentDifficulty)
        {
            case GameSettings.Difficulty.Easy:
                maxHealth = 150;
                break;
            case GameSettings.Difficulty.Hard:
                maxHealth = 900;
                break;
            case GameSettings.Difficulty.Normal:
            default:
                maxHealth = 300;
                break;
        }
    }

    void OnDestroy()
    {
        if (TypingCombatSystem.Instance != null)
        {
            TypingCombatSystem.Instance.SkillExecuted -= OnPlayerSkillExecuted;
        }
    }

    void OnPlayerSkillExecuted(SkillData skill, int damage)
    {
        if (isDead)
            return;

        if (skill.skillType == SkillType.Attack || skill.skillType == SkillType.Magic)
        {
            TakeDamage(damage);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHpBar();
        ShowDamageText(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 死亡アニメーション再生
        if (animator != null)
            animator.SetTrigger(deathTriggerName);

        float seVol = Mathf.Clamp01(GameSettings.SEVolume);
        if (seVol <= 0.001f) seVol = 1f;

        if (deathSe != null && seSource != null)
            seSource.PlayOneShot(deathSe, seVol);

        // ダメージ表示は消す
        if (damageText != null)
            damageText.text = "";

        // HPバーは空に
        UpdateHpBar();

        EnemyDied?.Invoke();
        StartCoroutine(FadeOutAndDisable());
    }

    IEnumerator FadeOutAndDisable()
    {
        // 倒れモーションを先に見せる
        if (deathAnimationLeadTime > 0f)
            yield return new WaitForSeconds(deathAnimationLeadTime);

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, deathFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float alpha = 1f - t;

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

        gameObject.SetActive(false);
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

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public float GetHealthPercentage()
    {
        return (float)currentHealth / maxHealth;
    }
}
