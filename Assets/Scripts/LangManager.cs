using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LangManager : MonoBehaviour
{
    public enum Language
    {
        Japanese,
        English
    }

    // シングルトン
    public static LangManager Instance { get; private set; }

    // 言語コード（例: "ja", "en"）。言語追加を簡単にするため、これを主な識別子として使用します。
    public string CurrentLanguageCode { get; private set; } = "ja";

    // 言語が変更されたときに発行されるイベント。言語コードを渡します。
    public static event Action<string> LanguageChanged;

    const string PlayerPrefKey = "LangManager_Language";

    // 外部ファイルを用意しない場合に使用される組み込みマップ（フォールバック）。
    Dictionary<string, Dictionary<string, string>> _builtin = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
    {
        { "ja", new Dictionary<string,string>{{"StartButton","スタート"}, {"OptionsButton","設定"}, {"ExitButton","終了"}, {"TypeWord_1","切る"}, {"TypeWord_2","まもる"}} },
        { "en", new Dictionary<string,string>{{"StartButton","Start"}, {"OptionsButton","Options"}, {"ExitButton","Exit"}, {"TypeWord_1","slash"}, {"TypeWord_2","gard"}} },
    };

    // Resources/Langs から読み込まれる外部言語。キー: 言語コード、値: (キー->テキスト)
    Dictionary<string, Dictionary<string, string>> _external = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

    // 外部言語ファイルが読み込まれたかを示します
    bool _hasExternal = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadExternalLangFiles();

        string saved = PlayerPrefs.GetString(PlayerPrefKey, "");
        if (!string.IsNullOrEmpty(saved))
        {
            CurrentLanguageCode = saved;
        }
        else if (PlayerPrefs.HasKey(PlayerPrefKey))
        {
            try
            {
                int v = PlayerPrefs.GetInt(PlayerPrefKey);
                CurrentLanguageCode = MapEnumToCode((Language)v);
            }
            catch
            {
                CurrentLanguageCode = "ja";
            }
        }

        // Start() で通知するため Awake では発火しない
    }

    void Start()
    {
        // 全ての OnEnable が登録された後に通知する
        LanguageChanged?.Invoke(CurrentLanguageCode);
    }

    void LoadExternalLangFiles()
    {
        _external.Clear();

        // Resources/Langs/ 以下のファイルを想定します（例: "ja.txt", "en.txt"）。
        // ファイル形式: "key=value" の行。先頭が '#' の行や空行は無視されます。
        var assets = Resources.LoadAll<TextAsset>("Lang");
        foreach (var asset in assets)
        {
            if (string.IsNullOrWhiteSpace(asset.name)) continue;
            var code = asset.name; // ファイル名（拡張子なし）を言語コードとして使用
            var map = ParseLangFile(asset.text);
            if (map.Count > 0)
            {
                _external[code] = map;
            }
        }

        _hasExternal = _external.Count > 0;
    }

    Dictionary<string, string> ParseLangFile(string text)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(text)) return result;

        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            if (line.StartsWith("#")) continue; // コメント行は無視

            var idx = line.IndexOf('=');
            if (idx <= 0) continue;
            var key = line.Substring(0, idx).Trim();
            var value = line.Substring(idx + 1).Trim();
            if (string.IsNullOrEmpty(key)) continue;
            result[key] = value;
        }

        return result;
    }

    // 言語をコードで設定（例: "ja", "en"）
    public void SetLanguage(string code)
    {
        if (string.IsNullOrEmpty(code)) return;
        if (string.Equals(CurrentLanguageCode, code, StringComparison.OrdinalIgnoreCase)) return;
        CurrentLanguageCode = code;
        PlayerPrefs.SetString(PlayerPrefKey, CurrentLanguageCode);
        PlayerPrefs.Save();
        LanguageChanged?.Invoke(CurrentLanguageCode);
    }

    // enum を使った互換メソッド
    public void SetLanguage(Language lang)
    {
        SetLanguage(MapEnumToCode(lang));
    }

    // 日本語と英語をトグル（従来の挙動を維持）
    public void ToggleLanguage()
    {
        if (string.Equals(CurrentLanguageCode, "ja", StringComparison.OrdinalIgnoreCase))
            SetLanguage("en");
        else
            SetLanguage("ja");
    }

    string MapEnumToCode(Language lang)
    {
        return lang switch
        {
            Language.Japanese => "ja",
            Language.English => "en",
            _ => "ja",
        };
    }

    // キーに対応する翻訳を取得。見つからなければキーを返す。
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return "";

        // 外部ファイルがある場合はそちらを優先
        if (_hasExternal)
        {
            if (_external.TryGetValue(CurrentLanguageCode, out var map) && map.TryGetValue(key, out var val))
                return val;

            // フォールバック: 地域部分を除いたコードなどで再検索
            var shortCode = CurrentLanguageCode.Split('_')[0];
            if (_external.TryGetValue(shortCode, out map) && map.TryGetValue(key, out val))
                return val;
        }

        // 組み込み辞書へフォールバック
        if (_builtin.TryGetValue(CurrentLanguageCode, out var bmap) && bmap.TryGetValue(key, out var bval))
            return bval;

        var shortCode2 = CurrentLanguageCode.Split('_')[0];
        if (_builtin.TryGetValue(shortCode2, out bmap) && bmap.TryGetValue(key, out bval))
            return bval;

        return key; // 最終フォールバック
    }

    // ???p?\?????R?[?h??????????i?O?????????O???A?????Αg?????j
    public IEnumerable<string> GetAvailableLanguages()
    {
        if (_hasExternal) return _external.Keys;
        return _builtin.Keys;
    }

    /// <summary>
    /// 言語ファイル内の Lang= キーから表示名を取得する。
    /// 例: ja.txt に Lang=日本語 があれば "日本語" を返す。
    /// なければ言語コード（"ja" など）をそのまま返す。
    /// </summary>
    public string GetLangDisplayName(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;

        // 外部ファイルから Lang= キーを探す
        if (_external.TryGetValue(code, out var map) && map.TryGetValue("Lang", out var val))
            return val;

        var shortCode = code.Split('_')[0];
        if (_external.TryGetValue(shortCode, out map) && map.TryGetValue("Lang", out val))
            return val;

        // 組み込みフォールバック
        if (_builtin.TryGetValue(code, out var bmap) && bmap.TryGetValue("Lang", out val))
            return val;

        return code;
    }
}
