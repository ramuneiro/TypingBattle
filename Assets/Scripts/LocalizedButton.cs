using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

// ボタンに直接アタッチして、キー一つで子の Text / TextMeshProUGUI を更新するコンポーネント
[RequireComponent(typeof(Button))]
public class LocalizedButton : MonoBehaviour
{
    // インスペクタで設定するキー（例: "StartButton"）
    public string key;

    // 子にある UnityEngine.UI.Text を使う場合
    Text _uiText;

    // TextMeshPro があればリフレクションで扱う（コンパイル時の依存を避ける）
    Component _tmpComponent;
    PropertyInfo _tmpTextProperty;

    void Awake()
    {
        // Unity UI の Text を探す
        _uiText = GetComponentInChildren<Text>(true);

        // TextMeshProUGUI が存在すればリフレクションで取得
        var tmpType = Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro");
        if (tmpType != null)
        {
            _tmpComponent = (Component)GetComponentInChildren(tmpType, true);
            if (_tmpComponent != null)
            {
                _tmpTextProperty = tmpType.GetProperty("text");
            }
        }
    }

    void OnEnable()
    {
        LangManager.LanguageChanged += OnLanguageChanged;
        UpdateText();
    }

    void OnDisable()
    {
        LangManager.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged(string _)
    {
        UpdateText();
    }

    void UpdateText()
    {
        var useKey = string.IsNullOrEmpty(key) ? gameObject.name : key;
        if (string.IsNullOrEmpty(useKey)) return;

        var lm = LangManager.Instance ?? UnityEngine.Object.FindFirstObjectByType<LangManager>();

        string localized = useKey;

        if (lm != null)
        {
            localized = lm.GetText(useKey);

            // キーがそのまま返ってきた（未翻訳）場合のみ、サフィックス除去して再検索
            if (localized == useKey)
            {
                var alt = TryStripSuffix(useKey);
                if (!string.IsNullOrEmpty(alt) && !string.Equals(alt, useKey, StringComparison.Ordinal))
                {
                    var altVal = lm.GetText(alt);
                    if (altVal != alt)
                        localized = altVal;
                }
            }
        }
        else
        {
            var alt = TryStripSuffix(useKey);
            if (!string.IsNullOrEmpty(alt) && !string.Equals(alt, useKey, StringComparison.Ordinal))
                localized = alt;
        }

        if (_uiText != null)
        {
            _uiText.text = localized;
            return;
        }

        if (_tmpComponent != null && _tmpTextProperty != null)
        {
            _tmpTextProperty.SetValue(_tmpComponent, localized);
        }
    }

    // 一般的なサフィックスを削ってキーを推測する
    string TryStripSuffix(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        var suffixes = new[] { "Button", "Btn", "_Button", "-Button" };
        foreach (var suf in suffixes)
        {
            if (s.EndsWith(suf, StringComparison.OrdinalIgnoreCase))
            {
                return s.Substring(0, s.Length - suf.Length);
            }
        }
        return s;
    }
}
