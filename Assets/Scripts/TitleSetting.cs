using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TitleSetting : MonoBehaviour
{
    public Button startButton;
    public Button keywordButton;
    public Button optionsButton;
    public Button exitButton;

    public Slider bgmSlider;
    public Slider seSlider;
    public TMP_Dropdown languageDropdown;

    public GameObject optionsPanel;
    public Button optionsBackButton;

    public GameObject difficultyPanel;
    public Button difficultyBackButton;

    public Button easyButton;
    public Button normalButton;
    public Button hardButton;

    public GameObject mainMenuButtonsPanel;

    public GameObject keywordPanel;
    public Button keywordBackButton;
    public KeywordPanel keywordPanelController;

    void Start()
    {
        GameSettings.Load();

        // Inspectorでアサインされていない場合はシーン上から自動検索（非アクティブも含む）
        if (optionsPanel == null)    optionsPanel    = FindInactiveByName("OptionPanel") ?? FindInactiveByName("OptionsPanel");
        if (difficultyPanel == null) difficultyPanel = FindInactiveByName("DifficultyPanel") ?? FindInactiveByName("StartPanel");
        if (keywordPanel == null)    keywordPanel    = FindInactiveByName("KeywordPanel");

        // mainMenuButtonsPanel が未設定なら startButton の親を使う
        if (mainMenuButtonsPanel == null && startButton != null)
        {
            Transform parent = startButton.transform.parent;
            if (parent != null && parent.GetComponentsInChildren<Button>(true).Length > 1)
                mainMenuButtonsPanel = parent.gameObject;
        }
        // それでも null なら名前で検索
        if (mainMenuButtonsPanel == null)
            mainMenuButtonsPanel = FindInactiveByName("GameManager")
                ?? FindInactiveByName("MainMenuButtonsPanel")
                ?? FindInactiveByName("MainMenu");

        Debug.Log($"[TitleSetting] mainMenuButtonsPanel resolved to: {(mainMenuButtonsPanel != null ? mainMenuButtonsPanel.name : "null")}");

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f; bgmSlider.maxValue = 1f;
            bgmSlider.wholeNumbers = false;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.interactable = true;
            bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(GameSettings.BGMVolume));
            bgmSlider.onValueChanged.AddListener(v => GameSettings.SetBGMVolume(v));
        }
        if (seSlider != null)
        {
            seSlider.minValue = 0f; seSlider.maxValue = 1f;
            seSlider.wholeNumbers = false;
            seSlider.onValueChanged.RemoveAllListeners();
            seSlider.interactable = true;
            seSlider.SetValueWithoutNotify(Mathf.Clamp01(GameSettings.SEVolume));
            seSlider.onValueChanged.AddListener(v => GameSettings.SetSEVolume(v));
        }

        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();

            if (LangManager.Instance == null)
                UnityEngine.Object.FindFirstObjectByType<LangManager>();

            var codes = new System.Collections.Generic.List<string>();
            if (LangManager.Instance != null)
                codes = new System.Collections.Generic.List<string>(LangManager.Instance.GetAvailableLanguages());
            if (codes == null || codes.Count == 0)
                codes = new System.Collections.Generic.List<string> { "ja", "en" };
            codes.RemoveAll(s => string.IsNullOrWhiteSpace(s));

            var display = new System.Collections.Generic.List<string>();
            foreach (var c in codes)
            {
                string label = LangManager.Instance != null
                    ? LangManager.Instance.GetLangDisplayName(c)
                    : c;
                display.Add(label);
            }
            languageDropdown.AddOptions(display);

            int idx = 0;
            if (LangManager.Instance != null)
                idx = codes.IndexOf(LangManager.Instance.CurrentLanguageCode);
            languageDropdown.value = idx >= 0 ? idx : 0;
            languageDropdown.onValueChanged.AddListener(i => {
                if (LangManager.Instance != null) LangManager.Instance.SetLanguage(codes[i]);
            });
        }

        if (optionsPanel != null)    optionsPanel.SetActive(false);
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        if (keywordPanel != null)    keywordPanel.SetActive(false);

        Debug.Log($"[TitleSetting] optionsPanel={optionsPanel}, difficultyPanel={difficultyPanel}, keywordPanel={keywordPanel}, mainMenuButtonsPanel={mainMenuButtonsPanel}");
        Debug.Log($"[TitleSetting] startButton={startButton}, optionsButton={optionsButton}, keywordButton={keywordButton}");

        if (startButton != null)          startButton.onClick.AddListener(ShowDifficulty);
        if (optionsButton != null)        optionsButton.onClick.AddListener(ShowOptions);
        if (optionsBackButton != null)    optionsBackButton.onClick.AddListener(HideOptions);
        if (difficultyBackButton != null) difficultyBackButton.onClick.AddListener(HideDifficulty);
        if (exitButton != null)           exitButton.onClick.AddListener(QuitGame);
        if (easyButton != null)           easyButton.onClick.AddListener(() => SelectDifficultyAndStart(GameSettings.Difficulty.Easy));
        if (normalButton != null)         normalButton.onClick.AddListener(() => SelectDifficultyAndStart(GameSettings.Difficulty.Normal));
        if (hardButton != null)           hardButton.onClick.AddListener(() => SelectDifficultyAndStart(GameSettings.Difficulty.Hard));
        if (keywordButton != null)        keywordButton.onClick.AddListener(ShowKeyword);
        if (keywordBackButton != null)    keywordBackButton.onClick.AddListener(HideKeyword);

        // 起動時にメインメニューを確実に表示する
        ShowMainMenuButtons();
    }

    void Update()
    {
        if (optionsPanel != null && optionsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            HideOptions();
        if (difficultyPanel != null && difficultyPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            HideDifficulty();
        if (keywordPanel != null && keywordPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            HideKeyword();
    }

    // パネルを確実に表示する（親が非アクティブ・CanvasGroup封鎖でも対応）
    static void ShowPanel(GameObject panel)
    {
        if (panel == null) return;
        Transform t = panel.transform.parent;
        while (t != null)
        {
            if (!t.gameObject.activeSelf)
                t.gameObject.SetActive(true);
            var cg = t.GetComponent<CanvasGroup>();
            if (cg != null) { cg.alpha = 1f; cg.interactable = true; cg.blocksRaycasts = true; }
            t = t.parent;
        }
        panel.SetActive(true);
        var panelCg = panel.GetComponent<CanvasGroup>();
        if (panelCg != null) { panelCg.alpha = 1f; panelCg.interactable = true; panelCg.blocksRaycasts = true; }
    }

    public void ShowOptions()
    {
        if (optionsPanel == null)
            optionsPanel = FindInactiveByName("OptionPanel");
        if (optionsPanel == null)
            optionsPanel = FindInactiveByName("OptionsPanel");
        if (optionsPanel == null)
        {
            Debug.LogError("[TitleSetting] optionsPanel is NULL");
            return;
        }
        ShowPanel(optionsPanel);
        HideMainMenuButtons();
    }

    public void HideOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(false);
        ShowMainMenuButtons();
    }

    public void ShowDifficulty()
    {
        if (difficultyPanel == null)
            difficultyPanel = FindInactiveByName("DifficultyPanel") ?? FindInactiveByName("StartPanel");
        if (difficultyPanel == null)
        {
            Debug.LogError("[TitleSetting] difficultyPanel is NULL");
            return;
        }
        ShowPanel(difficultyPanel);
        HideMainMenuButtons();
    }

    public void HideDifficulty()
    {
        if (difficultyPanel != null) difficultyPanel.SetActive(false);
        ShowMainMenuButtons();
    }

    public void ShowKeyword()
    {
        if (keywordPanel == null)
            keywordPanel = FindInactiveByName("KeywordPanel");
        if (keywordPanel == null) return;
        ShowPanel(keywordPanel);
        HideMainMenuButtons();

        if (keywordPanelController != null)
            keywordPanelController.RefreshAll();
        else
            keywordPanel.GetComponentInChildren<KeywordPanel>()?.RefreshAll();
    }

    public void HideKeyword()
    {
        if (keywordPanel != null) keywordPanel.SetActive(false);
        ShowMainMenuButtons();
    }

    void HideMainMenuButtons()
    {
        if (mainMenuButtonsPanel != null) { mainMenuButtonsPanel.SetActive(false); return; }
        if (startButton != null)   startButton.gameObject.SetActive(false);
        if (keywordButton != null) keywordButton.gameObject.SetActive(false);
        if (optionsButton != null) optionsButton.gameObject.SetActive(false);
        if (exitButton != null)    exitButton.gameObject.SetActive(false);
    }

    void ShowMainMenuButtons()
    {
        if (mainMenuButtonsPanel != null) { mainMenuButtonsPanel.SetActive(true); return; }
        if (startButton != null)   startButton.gameObject.SetActive(true);
        if (keywordButton != null) keywordButton.gameObject.SetActive(true);
        if (optionsButton != null) optionsButton.gameObject.SetActive(true);
        if (exitButton != null)    exitButton.gameObject.SetActive(true);
    }

    public void SelectDifficultyAndStart(GameSettings.Difficulty difficulty)
    {
        GameSettings.SetDifficulty(difficulty);
        SceneManager.LoadScene("Game1Scene");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // GameObject.Findは非アクティブを見つけられないため独自実装
    // Canvasの子を再帰的に探す（WebGL互換）
    static GameObject FindInactiveByName(string name)
    {
        // まずCanvasを全て取得して子を探す
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var canvas in canvases)
        {
            var found = FindInChildren(canvas.transform, name);
            if (found != null) return found;
        }
        // Canvasに無ければシーン全体から
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (go.scene.isLoaded && go.hideFlags == HideFlags.None && go.name == name)
                return go;
        }
        return null;
    }

    static GameObject FindInChildren(Transform parent, string name)
    {
        if (parent.name == name) return parent.gameObject;
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindInChildren(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }
}
