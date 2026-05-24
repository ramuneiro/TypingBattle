using System;
using UnityEngine;

// ゲーム全体で共有する設定を保持・永続化する静的クラス
public static class GameSettings
{
    public enum Difficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    const string KeyDifficulty = "GS_Difficulty";
    const string KeyBgm = "GS_BGMVolume";
    const string KeySe = "GS_SEVolume";
    const string KeyLanguage = "LangManager_Language"; // LangManager と同じキーを使う

    public static Difficulty CurrentDifficulty { get; private set; } = Difficulty.Normal;
    public static float BGMVolume { get; private set; } = 0.5f; // 0..1
    public static float SEVolume { get; private set; } = 0.5f; // 0..1

    // 設定をロード（起動時やシーン切替時に呼ぶ）
    public static void Load()
    {
        if (PlayerPrefs.HasKey(KeyDifficulty))
            CurrentDifficulty = (Difficulty)PlayerPrefs.GetInt(KeyDifficulty);
        else
            CurrentDifficulty = Difficulty.Normal;

        float bgmRaw = PlayerPrefs.GetFloat(KeyBgm, 0.5f);
        float seRaw = PlayerPrefs.GetFloat(KeySe, 0.5f);

        BGMVolume = NormalizeLoadedVolume(bgmRaw);
        SEVolume = NormalizeLoadedVolume(seRaw);

        // 補正後の値を保存して次回以降も安定化
        PlayerPrefs.SetFloat(KeyBgm, BGMVolume);
        PlayerPrefs.SetFloat(KeySe, SEVolume);
        PlayerPrefs.Save();
    }

    static float NormalizeLoadedVolume(float v)
    {
        // 旧設定で 0..0.2 レンジのまま保存されていた値を 0..1 に補正
        if (v > 0f && v <= 0.2f)
            return Mathf.Clamp01(v * 5f);

        return Mathf.Clamp01(v);
    }

    public static void SetDifficulty(Difficulty d)
    {
        CurrentDifficulty = d;
        PlayerPrefs.SetInt(KeyDifficulty, (int)d);
        PlayerPrefs.Save();
    }

    public static void SetBGMVolume(float v)
    {
        BGMVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeyBgm, BGMVolume);
        PlayerPrefs.Save();
    }

    public static void SetSEVolume(float v)
    {
        SEVolume = Mathf.Clamp01(v);
        PlayerPrefs.SetFloat(KeySe, SEVolume);
        PlayerPrefs.Save();
    }
}
