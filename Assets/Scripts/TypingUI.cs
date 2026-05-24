using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// タイピングUIの表示と管理を行うクラス
/// </summary>
public class TypingUI : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TMP_InputField inputField;  // 入力用のInputField（推奨）
    [SerializeField] private TMP_Text inputDisplay;  // 入力表示用のText（InputFieldがない場合）
    [SerializeField] private TMP_Text skillListDisplay;  // 技リスト表示
    [SerializeField] private TMP_Text feedbackDisplay;  // フィードバック表示
    [SerializeField] private GameObject typingPanel;  // タイピングパネル全体

    [Header("効果音（オプション）")]
    [SerializeField] private AudioSource correctSound;  // 正解時の効果音
    [SerializeField] private AudioSource incorrectSound;  // 不正解時の効果音

    private float feedbackTimer = 0f;  // フィードバック表示時間
    private string romajiBuffer = "";  // ローマ字変換用のバッファ
    private string m_lastAcceptedText = "";
    private bool m_internalTextUpdate = false;

    // ローマ字→ひらがな変換テーブル
    private Dictionary<string, string> romajiTable = new Dictionary<string, string>();

    void Start()
    {
        // ローマ字変換テーブルを初期化
        InitializeRomajiTable();

        // TypingCombatSystemのイベントを購読
        if (TypingCombatSystem.Instance != null)
        {
            TypingCombatSystem.Instance.InputChanged += OnInputChanged;
            TypingCombatSystem.Instance.SkillExecuted += OnSkillExecuted;
            TypingCombatSystem.Instance.InputFailed += OnInputFailed;
        }

        // LangManagerのイベントを購読して言語変更時にIMEを設定
        if (LangManager.Instance != null)
        {
            LangManager.LanguageChanged += OnLanguageChanged;
            // 初期設定
            SetupIMEForCurrentLanguage();
        }

        // InputFieldの設定
        if (inputField != null)
        {
            inputField.onValueChanged.AddListener(OnInputFieldChanged);
            inputField.onSubmit.AddListener(OnInputFieldSubmit);
            SelectInputField();
            m_lastAcceptedText = inputField.text;
        }

        // 技リストを更新
        UpdateSkillList();
        
        // パネルを表示
        if (typingPanel != null)
            typingPanel.SetActive(true);
    }

    /// <summary>
    /// ローマ字→ひらがな変換テーブルの初期化
    /// </summary>
    void InitializeRomajiTable()
    {
        romajiTable.Clear();

        // 基本的なひらがな
        romajiTable["a"] = "あ"; romajiTable["i"] = "い"; romajiTable["u"] = "う"; romajiTable["e"] = "え"; romajiTable["o"] = "お";
        romajiTable["ka"] = "か"; romajiTable["ki"] = "き"; romajiTable["ku"] = "く"; romajiTable["ke"] = "け"; romajiTable["ko"] = "こ";
        romajiTable["sa"] = "さ"; romajiTable["si"] = "し"; romajiTable["shi"] = "し"; romajiTable["su"] = "す"; romajiTable["se"] = "せ"; romajiTable["so"] = "そ";
        romajiTable["ta"] = "た"; romajiTable["ti"] = "ち"; romajiTable["chi"] = "ち"; romajiTable["tu"] = "つ"; romajiTable["tsu"] = "つ"; romajiTable["te"] = "て"; romajiTable["to"] = "と";
        romajiTable["na"] = "な"; romajiTable["ni"] = "に"; romajiTable["nu"] = "ぬ"; romajiTable["ne"] = "ね"; romajiTable["no"] = "の";
        romajiTable["ha"] = "は"; romajiTable["hi"] = "ひ"; romajiTable["hu"] = "ふ"; romajiTable["fu"] = "ふ"; romajiTable["he"] = "へ"; romajiTable["ho"] = "ほ";
        romajiTable["ma"] = "ま"; romajiTable["mi"] = "み"; romajiTable["mu"] = "む"; romajiTable["me"] = "め"; romajiTable["mo"] = "も";
        romajiTable["ya"] = "や"; romajiTable["yu"] = "ゆ"; romajiTable["yo"] = "よ";
        romajiTable["ra"] = "ら"; romajiTable["ri"] = "り"; romajiTable["ru"] = "る"; romajiTable["re"] = "れ"; romajiTable["ro"] = "ろ";
        romajiTable["wa"] = "わ"; romajiTable["wo"] = "を";

        // 濁音
        romajiTable["ga"] = "が"; romajiTable["gi"] = "ぎ"; romajiTable["gu"] = "ぐ"; romajiTable["ge"] = "げ"; romajiTable["go"] = "ご";
        romajiTable["za"] = "ざ"; romajiTable["zi"] = "じ"; romajiTable["ji"] = "じ"; romajiTable["zu"] = "ず"; romajiTable["ze"] = "ぜ"; romajiTable["zo"] = "ぞ";
        romajiTable["da"] = "だ"; romajiTable["di"] = "ぢ"; romajiTable["du"] = "づ"; romajiTable["de"] = "で"; romajiTable["do"] = "ど";
        romajiTable["ba"] = "ば"; romajiTable["bi"] = "び"; romajiTable["bu"] = "ぶ"; romajiTable["be"] = "べ"; romajiTable["bo"] = "ぼ";
        romajiTable["pa"] = "ぱ"; romajiTable["pi"] = "ぴ"; romajiTable["pu"] = "ぷ"; romajiTable["pe"] = "ぺ"; romajiTable["po"] = "ぽ";

        // 拗音
        romajiTable["kya"] = "きゃ"; romajiTable["kyu"] = "きゅ"; romajiTable["kyo"] = "きょ";
        romajiTable["sya"] = "しゃ"; romajiTable["sha"] = "しゃ"; romajiTable["syu"] = "しゅ"; romajiTable["shu"] = "しゅ"; romajiTable["syo"] = "しょ"; romajiTable["sho"] = "しょ";
        romajiTable["tya"] = "ちゃ"; romajiTable["cha"] = "ちゃ"; romajiTable["tyu"] = "ちゅ"; romajiTable["chu"] = "ちゅ"; romajiTable["tyo"] = "ちょ"; romajiTable["cho"] = "ちょ";
        romajiTable["nya"] = "にゃ"; romajiTable["nyu"] = "にゅ"; romajiTable["nyo"] = "にょ";
        romajiTable["hya"] = "ひゃ"; romajiTable["hyu"] = "ひゅ"; romajiTable["hyo"] = "ひょ";
        romajiTable["mya"] = "みゃ"; romajiTable["myu"] = "みゅ"; romajiTable["myo"] = "みょ";
        romajiTable["rya"] = "りゃ"; romajiTable["ryu"] = "りゅ"; romajiTable["ryo"] = "りょ";
        romajiTable["gya"] = "ぎゃ"; romajiTable["gyu"] = "ぎゅ"; romajiTable["gyo"] = "ぎょ";
        romajiTable["zya"] = "じゃ"; romajiTable["ja"] = "じゃ"; romajiTable["jya"] = "じゃ";
        romajiTable["zyu"] = "じゅ"; romajiTable["ju"] = "じゅ"; romajiTable["jyu"] = "じゅ";
        romajiTable["zyo"] = "じょ"; romajiTable["jo"] = "じょ"; romajiTable["jyo"] = "じょ";
        romajiTable["bya"] = "びゃ"; romajiTable["byu"] = "びゅ"; romajiTable["byo"] = "びょ";
        romajiTable["pya"] = "ぴゃ"; romajiTable["pyu"] = "ぴゅ"; romajiTable["pyo"] = "ぴょ";
    }

    void Update()
    {
        // タイピング入力が有効な時だけ InputField のフォーカスを維持
        bool allowAutoFocus = TypingCombatSystem.Instance != null
                              && TypingCombatSystem.Instance.IsInputActive;

        if (allowAutoFocus && inputField != null && EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            // いまUI操作中（スライダー/ボタン/ドロップダウンなど）ならフォーカスを奪わない
            bool isInteractingOtherUi = selected != null && selected != inputField.gameObject;

            // 何も選択されていない時だけ入力欄へ戻す
            if (!isInteractingOtherUi && selected != inputField.gameObject)
            {
                SelectInputField();
            }

            // Ctrl+V 貼り付けを無効化（入力欄選択時のみ）
            if (selected == inputField.gameObject && IsPasteShortcutPressed())
            {
                SetInputFieldTextSafely(m_lastAcceptedText);
            }
        }

        // フィードバック表示のタイマー処理
        if (feedbackTimer > 0)
        {
            feedbackTimer -= Time.deltaTime;
            if (feedbackTimer <= 0 && feedbackDisplay != null)
            {
                feedbackDisplay.text = "";
            }
        }
    }

    void OnDestroy()
    {
        // イベントの購読解除
        if (TypingCombatSystem.Instance != null)
        {
            TypingCombatSystem.Instance.InputChanged -= OnInputChanged;
            TypingCombatSystem.Instance.SkillExecuted -= OnSkillExecuted;
            TypingCombatSystem.Instance.InputFailed -= OnInputFailed;
        }

        if (LangManager.Instance != null)
        {
            LangManager.LanguageChanged -= OnLanguageChanged;
        }
    }

    /// <summary>
    /// 言語が変更された時の処理
    /// </summary>
    void OnLanguageChanged(string languageCode)
    {
        SetupIMEForCurrentLanguage();
        UpdateSkillList();
        romajiBuffer = "";  // ローマ字バッファをクリア
    }

    /// <summary>
    /// 現在の言語設定に応じてIMEを設定
    /// </summary>
    void SetupIMEForCurrentLanguage()
    {
        if (inputField == null || LangManager.Instance == null)
            return;

        Input.imeCompositionMode = IMECompositionMode.Off;
        inputField.lineType = TMP_InputField.LineType.SingleLine;

        // contentType の変更は onValueChanged を誤発火させるため、
        // inputType と characterValidation を直接設定する
        inputField.inputType = TMP_InputField.InputType.Standard;
        inputField.characterValidation = TMP_InputField.CharacterValidation.None;

        // テキストクリアは内部フラグを立ててから行い、誤検知を防ぐ
        m_internalTextUpdate = true;
        inputField.text = "";
        m_internalTextUpdate = false;

        romajiBuffer = "";
        m_lastAcceptedText = "";

        SelectInputField();
    }

    /// <summary>
    /// InputFieldを選択状態にする
    /// </summary>
    void SelectInputField()
    {
        if (inputField != null)
        {
            inputField.Select();
            inputField.ActivateInputField();
        }
    }

    /// <summary>
    /// InputFieldの値が変更された時の処理
    /// </summary>
    void OnInputFieldChanged(string value)
    {
        if (LangManager.Instance == null || inputField == null)
            return;

        if (m_internalTextUpdate)
            return;

        // 貼り付けを無効化（ショートカット or 一度に複数文字増加）
        if (IsPasteShortcutPressed() || IsSuspiciousBulkInsert(value))
        {
            SetInputFieldTextSafely(m_lastAcceptedText);
            inputField.caretPosition = inputField.text.Length;
            return;
        }

        // 日本語モードの場合、ローマ字をひらがなに変換
        if (LangManager.Instance.CurrentLanguageCode == "ja")
        {
            ProcessRomajiInput(value);
        }
        else
        {
            // 英語モードの場合、全角文字を半角に変換
            string converted = ConvertToHalfWidth(value);
            if (converted != value)
            {
                int cursorPos = inputField.caretPosition;
                SetInputFieldTextSafely(converted);
                inputField.caretPosition = Mathf.Clamp(cursorPos, 0, inputField.text.Length);
            }
        }

        m_lastAcceptedText = inputField.text;
    }

    // ローマ字入力を処理してひらがなに変換
    void ProcessRomajiInput(string input)
    {
        if (inputField == null)
            return;

        string lowerInput = input.ToLower();
        string converted = ConvertRomajiToHiragana(lowerInput, out string remaining);

        if (converted + remaining != input)
        {
            SetInputFieldTextSafely(converted + remaining);
            inputField.caretPosition = inputField.text.Length;
        }

        m_lastAcceptedText = inputField.text;
    }

    /// <summary>
    /// InputFieldでEnterが押された時の処理
    /// </summary>
    void OnInputFieldSubmit(string value)
    {
        if (inputField == null || TypingCombatSystem.Instance == null)
            return;

        // 入力無効中は何もしない（移動中など）
        if (!TypingCombatSystem.Instance.IsInputActive)
        {
            SetInputFieldTextSafely("");
            romajiBuffer = "";
            m_lastAcceptedText = "";
            SelectInputField();
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            SelectInputField();
            return;
        }

        if (LangManager.Instance != null && LangManager.Instance.CurrentLanguageCode == "ja")
        {
            string converted = ConvertRomajiToHiragana(value.ToLower(), out string remaining);

            // Enter時のみ単独nをんとして確定
            if (remaining == "n")
                remaining = "ん";

            string finalText = converted + remaining;
            TypingCombatSystem.Instance.ExecuteSkillByName(finalText);
        }
        else
        {
            TypingCombatSystem.Instance.ExecuteSkillByName(value);
        }

        SetInputFieldTextSafely("");
        romajiBuffer = "";
        m_lastAcceptedText = "";
        TypingCombatSystem.Instance.ResetInput();
        SelectInputField();
    }

    void SetInputFieldTextSafely(string text)
    {
        if (inputField == null)
            return;

        m_internalTextUpdate = true;
        inputField.text = text;
        m_internalTextUpdate = false;
    }

    bool IsPasteShortcutPressed()
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        bool cmd = Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);
        bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        bool ctrlV = ctrl && Input.GetKey(KeyCode.V);
        bool cmdV = cmd && Input.GetKey(KeyCode.V);
        bool shiftInsert = shift && Input.GetKey(KeyCode.Insert);

        return ctrlV || cmdV || shiftInsert;
    }

    bool IsSuspiciousBulkInsert(string current)
    {
        // 1フレーム相当で2文字以上増えたら貼り付け扱い
        int diff = current.Length - m_lastAcceptedText.Length;
        return diff > 1;
    }

  // 母音かどうか判定
    bool IsVowel(char c)
    {
        return c == 'a' || c == 'i' || c == 'u' || c == 'e' || c == 'o';
    }

    bool IsAsciiLower(char c)
    {
        return c >= 'a' && c <= 'z';
    }

    bool IsConsonant(char c)
    {
        return IsAsciiLower(c) && !IsVowel(c);
    }

   // 全角英数字を半角に変換
    string ConvertToHalfWidth(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        char[] chars = input.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            // 全角英数字を半角に変換
            if (chars[i] >= 'Ａ' && chars[i] <= 'Ｚ')
                chars[i] = (char)(chars[i] - 'Ａ' + 'A');
            else if (chars[i] >= 'ａ' && chars[i] <= 'ｚ')
                chars[i] = (char)(chars[i] - 'ａ' + 'a');
            else if (chars[i] >= '０' && chars[i] <= '９')
                chars[i] = (char)(chars[i] - '０' + '0');
            else if (chars[i] == '　')  // 全角スペース
                chars[i] = ' ';  // 半角スペース
        }
        return new string(chars);
    }

    // 入力が変更された時
    void OnInputChanged(string input)
    {
        // InputFieldがある場合
        if (inputField != null && inputField.text != input)
        {
            inputField.text = input;
        }
        // InputFieldがない場合はTextに表示
        else if (inputDisplay != null)
        {
            inputDisplay.text = input;
        }
    }

   // 技が成功した時
    void OnSkillExecuted(SkillData skill, int damage)
    {
        if (feedbackDisplay != null)
        {
            string skillName = skill.GetLocalizedName();
            feedbackDisplay.text = $"{skillName}! {damage} ダメージ!";
            feedbackDisplay.color = Color.green;
            feedbackTimer = 2.0f;
        }

        if (correctSound != null)
        {
            float seVol = Mathf.Clamp01(GameSettings.SEVolume);
            if (seVol <= 0.001f) seVol = 1f;
            correctSound.volume = seVol;
            correctSound.Play();
        }

        romajiBuffer = "";
        SelectInputField();
    }

    // 入力失敗した時
    void OnInputFailed(string input)
    {
        if (feedbackDisplay != null)
        {
            feedbackDisplay.text = "入力ミス!";
            feedbackDisplay.color = Color.red;
            feedbackTimer = 1.5f;
        }

        if (incorrectSound != null)
        {
            float seVol = Mathf.Clamp01(GameSettings.SEVolume);
            if (seVol <= 0.001f) seVol = 1f;
            incorrectSound.volume = seVol;
            incorrectSound.Play();
        }

        romajiBuffer = "";
        SelectInputField();
    }

    // 技リストを更新
    void UpdateSkillList()
    {
        if (skillListDisplay == null || TypingCombatSystem.Instance == null)
            return;

        var skills = TypingCombatSystem.Instance.GetAvailableSkills();
        string listText = "使える技:\n";
        
        foreach (var skill in skills)
        {
            string skillName = skill.GetLocalizedName();
            int damage = skill.GetDamage();
            listText += $"・{skillName} ({damage})\n";
        }

        skillListDisplay.text = listText;
    }

    // UIを更新（言語変更時などに呼び出す）
    public void RefreshUI()
    {
        SetupIMEForCurrentLanguage();
        UpdateSkillList();
    }

    // ローマ字をひらがなに変換
    string ConvertRomajiToHiragana(string romaji, out string remaining)
    {
        string result = "";
        string temp = romaji;
        remaining = "";

        while (temp.Length > 0)
        {
            // ひらがな等はそのまま通す
            if (!IsAsciiLower(temp[0]))
            {
                // ハイフンは長音符「ー」に変換
                if (temp[0] == '-')
                    result += "ー";
                else
                    result += temp[0];
                temp = temp.Substring(1);
                continue;
            }

            bool matched = false;

            // 3文字→2文字→1文字でマッチ
            for (int len = Mathf.Min(3, temp.Length); len >= 1; len--)
            {
                string sub = temp.Substring(0, len);

                bool allAsciiLower = true;
                for (int i = 0; i < sub.Length; i++)
                {
                    if (!IsAsciiLower(sub[i]))
                    {
                        allAsciiLower = false;
                        break;
                    }
                }

                if (!allAsciiLower)
                    continue;

                if (romajiTable.TryGetValue(sub, out string hira))
                {
                    result += hira;
                    temp = temp.Substring(len);
                    matched = true;
                    break;
                }
            }

            if (!matched)
            {
                // n の特殊処理
                // 次が母音(a/i/u/e/o)なら保留、それ以外なら『ん』確定
                if (temp[0] == 'n')
                {
                    if (temp.Length == 1)
                    {
                        remaining = temp;
                        break;
                    }

                    char next = temp[1];
                    if (next == 'n' || !IsVowel(next))
                    {
                        result += "ん";
                        temp = temp.Substring(1);
                    }
                    else
                    {
                        remaining = temp;
                        break;
                    }
                }
                // 促音（っ）: 同じ子音が続く場合（n除外）
                else if (temp.Length > 1 && temp[0] == temp[1] && IsConsonant(temp[0]) && temp[0] != 'n')
                {
                    result += "っ";
                    temp = temp.Substring(1);
                }
                else
                {
                    remaining = temp;
                    break;
                }
            }
        }

        return result;
    }
}
