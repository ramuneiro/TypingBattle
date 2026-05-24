using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("HPバー")]
    [SerializeField] private Transform hpFillTransform;

    [Header("ダメージ表示")]
    [SerializeField] private TMP_Text damageText;
    [SerializeField] private float damageTextDuration = 0.5f;
    [SerializeField] private float damageTextRiseDistance = 0.3f;

    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip hitSe;
    [SerializeField] private AudioClip deathSe;

    [Header("死亡")]
    [SerializeField] private string deathTriggerName = "Death";
    [SerializeField] private float deathAnimDuration = 1.5f;  // デスアニメーション待機時間
    [SerializeField] private float fadeDuration = 1.0f;        // フェードアウト時間
    [SerializeField] private string gameOverSceneName = "GameOverScene";

    public static event System.Action PlayerDied;

    private float invincibleTimer;
    private Vector3 hpInitialScale;
    private RectTransform hpFillRect;
    private float damageTextTimer;
    private Vector3 damageTextBaseLocalPos;
    private bool isDead;
    private Animator animator;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();

        if (seSource == null) seSource = GetComponent<AudioSource>();
        if (seSource == null) seSource = gameObject.AddComponent<AudioSource>();

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
    }

    void Update()
    {
        if (invincibleTimer > 0f)
            invincibleTimer -= Time.deltaTime;

        UpdateDamageTextFade();
    }

    public void SetInvincible(float duration)
    {
        if (duration > invincibleTimer)
            invincibleTimer = duration;
    }

    public bool IsInvincible() => invincibleTimer > 0f;

    public bool TakeDamage(int damage)
    {
        if (isDead || IsInvincible()) return false;

        currentHealth = Mathf.Max(0, currentHealth - damage);
        UpdateHpBar();
        ShowDamageText(damage);

        float seVol = Mathf.Clamp01(GameSettings.SEVolume);
        if (seVol <= 0.001f) seVol = 1f;

        if (hitSe != null && seSource != null)
            seSource.PlayOneShot(hitSe, seVol);

        if (currentHealth <= 0)
        {
            isDead = true;

            if (animator != null)
                animator.SetTrigger(deathTriggerName);

            if (deathSe != null && seSource != null)
                seSource.PlayOneShot(deathSe, seVol);

            // 入力を止める
            if (TypingCombatSystem.Instance != null)
                TypingCombatSystem.Instance.SetInputActive(false);

            PlayerDied?.Invoke();

            StartCoroutine(DeathSequence());
        }

        return true;
    }

    IEnumerator DeathSequence()
    {
        // デスアニメーション再生を待つ
        yield return new WaitForSeconds(deathAnimDuration);

        // フェードアウト用Canvas生成
        GameObject fadeObj = new GameObject("DeathFadeCanvas");
        Canvas canvas = fadeObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        CanvasGroup group = fadeObj.AddComponent<CanvasGroup>();

        GameObject imgObj = new GameObject("FadeImage");
        imgObj.transform.SetParent(fadeObj.transform, false);
        Image img = imgObj.AddComponent<Image>();
        img.color = Color.black;
        RectTransform rt = imgObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        group.alpha = 0f;
        float t = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (t < duration)
        {
            t += Time.deltaTime;
            group.alpha = Mathf.Clamp01(t / duration);
            yield return null;
        }

        SceneManager.LoadScene(gameOverSceneName);
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0) return;
        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        UpdateHpBar();
        ShowHealText(amount);
    }

    public int GetCurrentHealth() => currentHealth;

    void UpdateHpBar()
    {
        if (hpFillTransform == null) return;

        float ratio = Mathf.Clamp01((float)currentHealth / maxHealth);
        Vector3 scale = hpInitialScale;
        scale.x = hpInitialScale.x * ratio;
        hpFillTransform.localScale = scale;
    }

    void ShowDamageText(int damage)
    {
        if (damageText == null) return;
        damageText.gameObject.SetActive(true);
        damageText.enabled = true;
        damageText.transform.localPosition = damageTextBaseLocalPos;
        damageText.text = damage.ToString();
        Color c = Color.red; c.a = 1f;
        damageText.color = c;
        damageTextTimer = Mathf.Max(0.01f, damageTextDuration);
    }

    void ShowHealText(int amount)
    {
        if (damageText == null) return;
        damageText.gameObject.SetActive(true);
        damageText.enabled = true;
        damageText.transform.localPosition = damageTextBaseLocalPos;
        damageText.text = "+" + amount.ToString();
        Color c = Color.green; c.a = 1f;
        damageText.color = c;
        damageTextTimer = Mathf.Max(0.01f, damageTextDuration);
    }

    void UpdateDamageTextFade()
    {
        if (damageText == null || damageTextTimer <= 0f) return;

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
