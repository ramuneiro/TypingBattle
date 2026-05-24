using System;

// ‹Z‚Ìƒf[ƒ^‚ğ•Û‚·‚éƒNƒ‰ƒX
[Serializable]
public class SkillData
{
    public string japaneseName;      // “ú–{Œê‚Ì‹Z–¼
    public string englishName;       // ‰pŒê‚Ì‹Z–¼
    public SkillType skillType;      // ‹Z‚Ìí—Ş
    public float damageMultiplier = 1.0f;  // ƒ_ƒ[ƒW”{—¦

    // Œ»İ‚ÌŒ¾Œêİ’è‚É‰‚¶‚½‹Z–¼‚ğæ“¾
    public string GetLocalizedName()
    {
        if (LangManager.Instance != null && LangManager.Instance.CurrentLanguageCode == "en")
            return englishName;
        return japaneseName;
    }

    //•¶š” ~ 10 ~ ”{—¦
    public int GetDamage()
    {
        string name = GetLocalizedName();
        int characterCount = name.Length;
        return (int)(characterCount * 10 * damageMultiplier);
    }
}

// ‹Z‚Ìí—Ş

public enum SkillType
{
    Attack,  // UŒ‚‹Z
    Dodge,   // ‰ñ”ğ‹Z
    Magic,   // –‚–@‹Z
    Heal     // ‰ñ•œ‹Z
}
