using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class TypingCombatSystem : MonoBehaviour
{
    public static TypingCombatSystem Instance { get; private set; }

    private List<SkillData> availableSkills = new List<SkillData>();
    private string currentInput = "";
    private bool isInputActive = false;

    public bool IsInputActive => isInputActive;

    public delegate void OnSkillExecuted(SkillData skill, int damage);
    public event OnSkillExecuted SkillExecuted;

    public delegate void OnInputFailed(string input);
    public event OnInputFailed InputFailed;

    public delegate void OnInputChanged(string input);
    public event OnInputChanged InputChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // ステージ1開始時のみセッションをリセット（ステージ2は引き継ぐ）
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "Game1Scene")
            ScoreManager.StartNewSession();

        InitializeSkills();
    }

    void InitializeSkills()
    {
        availableSkills.Clear();

        // 攻撃技（文字数順）
        availableSkills.Add(new SkillData { japaneseName = "きる",                     englishName = "cut",               skillType = SkillType.Attack, damageMultiplier = 1.2f });
        availableSkills.Add(new SkillData { japaneseName = "こうげき",                 englishName = "strike",            skillType = SkillType.Attack, damageMultiplier = 1.0f });
        availableSkills.Add(new SkillData { japaneseName = "きりつける",               englishName = "slash",             skillType = SkillType.Attack, damageMultiplier = 1.4f });
        availableSkills.Add(new SkillData { japaneseName = "きりあげる",               englishName = "upper slash",       skillType = SkillType.Attack, damageMultiplier = 1.3f });
        availableSkills.Add(new SkillData { japaneseName = "えくすかりばー",           englishName = "excalibur",         skillType = SkillType.Attack, damageMultiplier = 2.0f });
        availableSkills.Add(new SkillData { japaneseName = "いのちをねらう",           englishName = "deadly aim",        skillType = SkillType.Attack, damageMultiplier = 2.2f });
        availableSkills.Add(new SkillData { japaneseName = "きょうりょくなきり",       englishName = "power slash",       skillType = SkillType.Attack, damageMultiplier = 1.5f });
        availableSkills.Add(new SkillData { japaneseName = "おいのちちょうだいいたす", englishName = "your life is mine", skillType = SkillType.Attack, damageMultiplier = 1.8f });

        // 回避技（文字数順）
        availableSkills.Add(new SkillData { japaneseName = "とぶ",       englishName = "fly",        skillType = SkillType.Dodge, damageMultiplier = 0.8f });
        availableSkills.Add(new SkillData { japaneseName = "かいひ",     englishName = "evade",      skillType = SkillType.Dodge, damageMultiplier = 1.0f });
        availableSkills.Add(new SkillData { japaneseName = "よける",     englishName = "dodge",      skillType = SkillType.Dodge, damageMultiplier = 1.2f });
        availableSkills.Add(new SkillData { japaneseName = "じゃんぷ",   englishName = "jump back",  skillType = SkillType.Dodge, damageMultiplier = 1.6f });
        availableSkills.Add(new SkillData { japaneseName = "かいひする", englishName = "side step",  skillType = SkillType.Dodge, damageMultiplier = 1.4f });
        availableSkills.Add(new SkillData { japaneseName = "とびはねる", englishName = "leap aside", skillType = SkillType.Dodge, damageMultiplier = 0.9f });

        // 回復技（文字数順）
        availableSkills.Add(new SkillData { japaneseName = "ひーる",           englishName = "cure",         skillType = SkillType.Heal, damageMultiplier = 1.0f });
        availableSkills.Add(new SkillData { japaneseName = "かいふく",         englishName = "recover",      skillType = SkillType.Heal, damageMultiplier = 1.0f });
        availableSkills.Add(new SkillData { japaneseName = "たいりょくかいふく", englishName = "full restore", skillType = SkillType.Heal, damageMultiplier = 1.0f });
    }

    void Update()
    {
        if (!isInputActive)
            return;

        foreach (char c in Input.inputString)
        {
            if (c == '\b')
            {
                if (currentInput.Length > 0)
                {
                    currentInput = currentInput.Substring(0, currentInput.Length - 1);
                    InputChanged?.Invoke(currentInput);
                }
            }
            else if (c == '\n' || c == '\r')
            {
                ProcessInput();
            }
            else
            {
                currentInput += c;
                InputChanged?.Invoke(currentInput);
            }
        }
    }

    void ProcessInput()
    {
        if (string.IsNullOrEmpty(currentInput))
            return;

        SkillData matchedSkill = availableSkills.FirstOrDefault(skill =>
            skill.GetLocalizedName().Equals(currentInput, System.StringComparison.OrdinalIgnoreCase));

        if (matchedSkill != null)
        {
            int damage = matchedSkill.GetDamage();

            // 攻撃・魔法技のみスコア加算
            if (matchedSkill.skillType == SkillType.Attack || matchedSkill.skillType == SkillType.Magic)
                ScoreManager.AddAttackChars(matchedSkill.GetLocalizedName().Length);

            SkillExecuted?.Invoke(matchedSkill, damage);
        }
        else
        {
            InputFailed?.Invoke(currentInput);
        }

        currentInput = "";
        InputChanged?.Invoke(currentInput);
    }

    public void SetInputActive(bool active)
    {
        isInputActive = active;
        if (!active)
        {
            currentInput = "";
            InputChanged?.Invoke(currentInput);
        }
    }

    public string GetCurrentInput()
    {
        return currentInput;
    }

    public List<SkillData> GetAvailableSkills()
    {
        return new List<SkillData>(availableSkills);
    }

    public void AddSkill(SkillData skill)
    {
        if (!availableSkills.Contains(skill))
        {
            availableSkills.Add(skill);
        }
    }

    public void RemoveSkill(SkillData skill)
    {
        availableSkills.Remove(skill);
    }

    public void ExecuteSkillByName(string skillName)
    {
        if (!isInputActive)
            return;

        if (string.IsNullOrEmpty(skillName))
            return;

        SkillData matchedSkill = availableSkills.FirstOrDefault(skill =>
            skill.GetLocalizedName().Equals(skillName, System.StringComparison.OrdinalIgnoreCase));

        if (matchedSkill != null)
        {
            int damage = matchedSkill.GetDamage();

            // 攻撃・魔法技のみスコア加算
            if (matchedSkill.skillType == SkillType.Attack || matchedSkill.skillType == SkillType.Magic)
                ScoreManager.AddAttackChars(matchedSkill.GetLocalizedName().Length);

            SkillExecuted?.Invoke(matchedSkill, damage);
        }
        else
        {
            InputFailed?.Invoke(skillName);
        }
    }

    public void ResetInput()
    {
        currentInput = "";
        InputChanged?.Invoke(currentInput);
    }

    // 回避の無敵時間（文字数ベース）
    public float GetDodgeInvincibleDuration(SkillData skill)
    {
        if (skill == null || skill.skillType != SkillType.Dodge)
            return 0f;

        string name = skill.GetLocalizedName();
        int len = string.IsNullOrEmpty(name) ? 0 : name.Length;

        // 基本0.30秒 + 1文字あたり0.10秒（上限2.0秒）※従来の2倍
        return Mathf.Min(2.0f, 0.30f + (len * 0.10f));
    }

    // 回復量を計算する（1文字あたり7）
    public int GetHealAmount(SkillData skill)
    {
        if (skill == null || skill.skillType != SkillType.Heal)
            return 0;

        string name = skill.GetLocalizedName();
        int len = string.IsNullOrEmpty(name) ? 0 : name.Length;
        return len * 7;
    }
}
