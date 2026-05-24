using UnityEngine;

/// <summary>
/// ステージをまたいで攻撃文字数を累積し、クリア時に難易度倍率を適用してスコアを確定・保存する。
/// ポイントは解放システムにも使用するため PlayerPrefs に永続保存する。
/// </summary>
public static class ScoreManager
{
    // 難易度倍率
    private static readonly float[] DifficultyMultiplier =
    {
        0.5f,  // Easy
        1.0f,  // Normal
        3.0f,  // Hard
    };

    // 1文字あたりのベースポイント
    private const int PointsPerChar = 100;

    // PlayerPrefs キー
    private const string KeyTotalPoints  = "Score_TotalPoints";
    private const string KeySessionChars = "Score_SessionChars";
    private const string KeyLastScore    = "Score_LastScore";

    /// <summary>現在セッション（ゲーム開始?クリア）の攻撃文字数合計</summary>
    public static int SessionAttackChars { get; private set; }

    /// <summary>今回クリアで確定したスコア（難易度倍率適用済み）</summary>
    public static int LastScore { get; private set; }

    /// <summary>累計保存ポイント（解放システム用）</summary>
    public static int TotalSavedPoints { get; private set; }

    /// <summary>ゲーム開始時にセッション文字数をリセットする</summary>
    public static void StartNewSession()
    {
        SessionAttackChars = 0;
        LastScore = 0;
        // セッション途中データも念のためクリア
        PlayerPrefs.SetInt(KeySessionChars, 0);
        PlayerPrefs.Save();
    }

    /// <summary>攻撃技が成功するたびに文字数を加算する</summary>
    public static void AddAttackChars(int charCount)
    {
        if (charCount <= 0) return;
        SessionAttackChars += charCount;
        // 途中セーブ（強制終了対策）
        PlayerPrefs.SetInt(KeySessionChars, SessionAttackChars);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// クリア時に呼ぶ。難易度倍率を掛けてスコアを確定し、累計ポイントに加算して保存する。
    /// </summary>
    /// <returns>確定したスコア</returns>
    public static int FinalizeScore()
    {
        GameSettings.Load();

        float multiplier = DifficultyMultiplier[(int)GameSettings.CurrentDifficulty];
        int rawScore = SessionAttackChars * PointsPerChar;
        LastScore = Mathf.RoundToInt(rawScore * multiplier);

        // 累計ポイントに加算して保存
        TotalSavedPoints = PlayerPrefs.GetInt(KeyTotalPoints, 0) + LastScore;
        PlayerPrefs.SetInt(KeyTotalPoints, TotalSavedPoints);
        PlayerPrefs.SetInt(KeyLastScore, LastScore);
        PlayerPrefs.SetInt(KeySessionChars, 0);
        PlayerPrefs.Save();

        SessionAttackChars = 0;
        return LastScore;
    }

    /// <summary>累計ポイントをロードして返す（解放システム等から呼ぶ）</summary>
    public static int LoadTotalPoints()
    {
        TotalSavedPoints = PlayerPrefs.GetInt(KeyTotalPoints, 0);
        return TotalSavedPoints;
    }

    /// <summary>直前のクリアスコアをPlayerPrefsからロードして返す</summary>
    public static int LoadLastScore()
    {
        LastScore = PlayerPrefs.GetInt(KeyLastScore, 0);
        return LastScore;
    }

    /// <summary>累計ポイントを指定量消費する（解放システム用）</summary>
    /// <returns>消費できた場合 true</returns>
    public static bool ConsumePoints(int amount)
    {
        LoadTotalPoints();
        if (TotalSavedPoints < amount) return false;
        TotalSavedPoints -= amount;
        PlayerPrefs.SetInt(KeyTotalPoints, TotalSavedPoints);
        PlayerPrefs.Save();
        return true;
    }
}
