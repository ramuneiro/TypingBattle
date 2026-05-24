using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// キーワードの定義・解放状態の保存/読込を管理する静的クラス。
/// </summary>
public static class KeywordManager
{
    // 解放コスト
    public const int UnlockCost = 500;

    // PlayerPrefs キー（解放済みインデックスをカンマ区切りで保存）
    private const string KeyUnlocked = "Keywords_Unlocked";

    public enum KeywordType { Attack, Dodge, Defend, Heal }

    // キーワード定義（名前・種別をセットで管理）
    private static readonly (string ja, string en, KeywordType type)[] AllKeywordDefs =
    {
        // 攻撃（文字数順）
        ("きる",                     "cut",               KeywordType.Attack),
        ("こうげき",                 "strike",            KeywordType.Attack),
        ("きりつける",               "slash",             KeywordType.Attack),
        ("きりあげる",               "upper slash",       KeywordType.Attack),
        ("えくすかりばー",           "excalibur",         KeywordType.Attack),
        ("いのちをねらう",           "deadly aim",        KeywordType.Attack),
        ("きょうりょくなきり",       "power slash",       KeywordType.Attack),
        ("おいのちちょうだいいたす", "your life is mine", KeywordType.Attack),
        // 回避（文字数順）
        ("とぶ",       "fly",        KeywordType.Dodge),
        ("かいひ",     "evade",      KeywordType.Dodge),
        ("よける",     "dodge",      KeywordType.Dodge),
        ("じゃんぷ",   "jump back",  KeywordType.Dodge),
        ("かいひする", "side step",  KeywordType.Dodge),
        ("とびはねる", "leap aside", KeywordType.Dodge),
        // 回復（文字数順）
        ("ひーる",         "cure",       KeywordType.Heal),
        ("かいふく",       "recover",    KeywordType.Heal),
        ("たいりょくかいふく", "full restore", KeywordType.Heal),
    };

    // 解放済みインデックスのセット（実行時キャッシュ）
    private static HashSet<int> _unlockedSet = null;

    public static int TotalCount => AllKeywordDefs.Length;

    /// <summary>現在の言語に合ったキーワード一覧を返す</summary>
    public static string[] GetAllKeywords()
    {
        bool isEn = LangManager.Instance != null &&
                    LangManager.Instance.CurrentLanguageCode == "en";
        var result = new string[AllKeywordDefs.Length];
        for (int i = 0; i < AllKeywordDefs.Length; i++)
            result[i] = isEn ? AllKeywordDefs[i].en : AllKeywordDefs[i].ja;
        return result;
    }

    public static KeywordType GetKeywordType(int index)
    {
        if (index < 0 || index >= AllKeywordDefs.Length) return KeywordType.Attack;
        return AllKeywordDefs[index].type;
    }

    /// <summary>解放済みインデックスセットをロード（キャッシュ済みなら再利用）</summary>
    public static HashSet<int> LoadUnlocked()
    {
        if (_unlockedSet != null)
            return _unlockedSet;

        _unlockedSet = new HashSet<int>();
        string saved = PlayerPrefs.GetString(KeyUnlocked, "");

        if (string.IsNullOrEmpty(saved))
        {
            // 初回：きる(0)・とぶ(8)・ひーる(14) をデフォルト解放
            _unlockedSet.Add(0);  // きる
            _unlockedSet.Add(8);  // とぶ
            _unlockedSet.Add(14); // ひーる
            SaveUnlocked(_unlockedSet);
            return _unlockedSet;
        }

        foreach (var s in saved.Split(','))
        {
            if (int.TryParse(s.Trim(), out int idx))
                _unlockedSet.Add(idx);
        }
        return _unlockedSet;
    }

    /// <summary>指定インデックスが解放済みか</summary>
    public static bool IsUnlocked(int index) => LoadUnlocked().Contains(index);

    /// <summary>
    /// ランダムに未解放のキーワードを1つ解放する。
    /// ポイントが不足またはすべて解放済みなら false を返す。
    /// </summary>
    public static bool TryUnlockRandom(out string unlockedKeyword)
    {
        unlockedKeyword = null;

        // ポイントチェック
        if (ScoreManager.LoadTotalPoints() < UnlockCost)
            return false;

        // 未解放リストを作成
        var unlocked = LoadUnlocked();
        var locked = new List<int>();
        for (int i = 0; i < AllKeywordDefs.Length; i++)
            if (!unlocked.Contains(i)) locked.Add(i);

        if (locked.Count == 0)
            return false;
        // ポイント消費
        if (!ScoreManager.ConsumePoints(UnlockCost))
            return false;

        // ランダム解放
        int pick = locked[Random.Range(0, locked.Count)];
        unlocked.Add(pick);
        unlockedKeyword = AllKeywordDefs[pick].ja;

        // 保存
        SaveUnlocked(unlocked);
        return true;
    }

    /// <summary>解放済みセットをPlayerPrefsに保存</summary>
    private static void SaveUnlocked(HashSet<int> set)
    {
        _unlockedSet = set;
        PlayerPrefs.SetString(KeyUnlocked, string.Join(",", new List<int>(set)));
        PlayerPrefs.Save();
    }

    /// <summary>キャッシュをリセット（シーン遷移後などに呼ぶ）</summary>
    public static void ClearCache() => _unlockedSet = null;
}
