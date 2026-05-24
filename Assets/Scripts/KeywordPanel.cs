using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// キーワードパネルのUI制御。
/// ポーズ画面・タイトル画面のキーワードパネルに配置して使う。
/// </summary>
public class KeywordPanel : MonoBehaviour
{
    [Header("キーワード一覧")]
    [SerializeField] private Transform keywordListParent;   // キーワード行を並べる親Transform
    [SerializeField] private GameObject keywordRowPrefab;   // TMP_Textひとつを持つPrefab（なければ動的生成）

    [Header("ポイント表示（右下）")]
    [SerializeField] private TMP_Text pointsText;           // 累計ポイント表示

    [Header("解放ボタン")]
    [SerializeField] private Button unlockButton;           // 解放ボタン
    [SerializeField] private TMP_Text unlockButtonText;     // ボタンのラベル
    [SerializeField] private TMP_Text unlockResultText;     // 解放結果メッセージ
    [SerializeField] private float resultDisplayDuration = 2.5f;

    private float _resultTimer;

    void OnEnable()
    {
        RefreshAll();

        if (unlockButton != null)
        {
            unlockButton.onClick.RemoveAllListeners();
            unlockButton.onClick.AddListener(OnUnlockClicked);
        }
    }

    void Update()
    {
        if (_resultTimer > 0f)
        {
            _resultTimer -= Time.unscaledDeltaTime;   // ポーズ中（timeScale=0）でも動く
            if (_resultTimer <= 0f && unlockResultText != null)
                unlockResultText.text = "";
        }
    }

    /// <summary>パネル全体を再描画する</summary>
    public void RefreshAll()
    {
        RefreshPoints();
        RefreshKeywordList();
        RefreshUnlockButton();
        if (unlockResultText != null)
            unlockResultText.text = "";
    }

    // ─── ポイント表示 ───────────────────────────────────────
    void RefreshPoints()
    {
        if (pointsText == null) return;
        int pts = ScoreManager.LoadTotalPoints();
        pointsText.text = $"{pts:N0}pt";
    }

    // ─── キーワード一覧 ─────────────────────────────────────
    void RefreshKeywordList()
    {
        if (keywordListParent == null) return;

        // 既存の行を全消去
        foreach (Transform child in keywordListParent)
            Destroy(child.gameObject);

        // VerticalLayoutGroup を自動セット（なければ追加）
        var vlg = keywordListParent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = keywordListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(8, 8, 24, 4);

        // ContentSizeFitter を自動セット（なければ追加）
        var csf = keywordListParent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = keywordListParent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        string[] keywords = KeywordManager.GetAllKeywords();
        for (int i = 0; i < keywords.Length; i++)
        {
            bool unlocked = KeywordManager.IsUnlocked(i);
            string display = unlocked ? keywords[i] : "???";
            Color col = unlocked ? GetTypeColor(KeywordManager.GetKeywordType(i)) : Color.gray;
            GameObject row = CreateRow(display, col);
            row.transform.SetParent(keywordListParent, false);
        }
    }

    // 種別ごとの色を返す
    Color GetTypeColor(KeywordManager.KeywordType type)
    {
        switch (type)
        {
            case KeywordManager.KeywordType.Attack: return new Color(1.0f, 0.35f, 0.35f); // 赤系
            case KeywordManager.KeywordType.Dodge:  return new Color(0.4f, 0.85f, 1.0f);  // 水色系
            case KeywordManager.KeywordType.Heal:   return new Color(0.4f, 1.0f, 0.5f);   // 緑系
            default:                                return Color.white;                    // 防御など
        }
    }

    GameObject CreateRow(string text, Color color)
    {
        GameObject go;

        if (keywordRowPrefab != null)
        {
            go = Instantiate(keywordRowPrefab);
            var tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.color = color;
            }
        }
        else
        {
            go = new GameObject("KeywordRow");

            // RectTransformに固定の高さを設定
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, 36);

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36;
            le.flexibleWidth = 1;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.color = color;
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.enableWordWrapping = false;
        }

        return go;
    }

    // ─── 解放ボタン ─────────────────────────────────────────
    void RefreshUnlockButton()
    {
        if (unlockButton == null) return;

        int pts = ScoreManager.LoadTotalPoints();
        bool canUnlock = pts >= KeywordManager.UnlockCost && HasLocked();
        unlockButton.interactable = canUnlock;

        if (unlockButtonText != null)
            unlockButtonText.text = $"解放（{KeywordManager.UnlockCost}pt）";
    }

    bool HasLocked()
    {
        string[] keywords = KeywordManager.GetAllKeywords();
        for (int i = 0; i < keywords.Length; i++)
        {
            if (!KeywordManager.IsUnlocked(i))
                return true;
        }
        return false;
    }

    void OnUnlockClicked()
    {
        if (KeywordManager.TryUnlockRandom(out string keyword))
        {
            RefreshAll();
            ShowResult($"「{keyword}」を解放！");
        }
        else
        {
            int pts = ScoreManager.LoadTotalPoints();
            if (pts < KeywordManager.UnlockCost)
                ShowResult($"ポイントが足りない（{pts}/{KeywordManager.UnlockCost}）");
            else
                ShowResult("解放できるキーワードがありません");
        }
    }

    void ShowResult(string msg)
    {
        if (unlockResultText != null)
        {
            unlockResultText.text = msg;
            _resultTimer = resultDisplayDuration;
        }
    }
}
