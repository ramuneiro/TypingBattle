using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class Gamepause : MonoBehaviour
{
    [Header("Pause Root")]
    [SerializeField] private GameObject pausePanel;

    [Header("Main Buttons")]
    [SerializeField] private Button titleButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button keywordButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button backButton;

    [Header("Sub Panels")]
    [SerializeField] private GameObject keywordPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private KeywordPanel keywordPanelController;

    [Header("Sub Back Buttons (optional)")]
    [SerializeField] private Button keywordBackButton;
    [SerializeField] private Button optionsBackButton;

    [Header("Options UI")]
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider seSlider;
    [SerializeField] private TMP_Dropdown languageDropdown;

    [Header("Scene Names")]
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string restartSceneName = ""; // 空なら現在シーンを再読込

    private bool m_isPaused;
    private bool m_wasTypingInputActive;

    void Start()
    {
        GameSettings.Load();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (keywordPanel != null) keywordPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (titleButton != null)   { titleButton.onClick.RemoveAllListeners();   titleButton.onClick.AddListener(GoToTitle); }
        if (restartButton != null) { restartButton.onClick.RemoveAllListeners(); restartButton.onClick.AddListener(RestartScene); }
        if (keywordButton != null) { keywordButton.onClick.RemoveAllListeners(); keywordButton.onClick.AddListener(ShowKeyword); }
        if (optionsButton != null) { optionsButton.onClick.RemoveAllListeners(); optionsButton.onClick.AddListener(ShowOptions); }
        if (backButton != null)    { backButton.onClick.RemoveAllListeners();    backButton.onClick.AddListener(ResumeGame); }

        if (keywordBackButton != null) { keywordBackButton.onClick.RemoveAllListeners(); keywordBackButton.onClick.AddListener(CloseKeyword); }
        if (optionsBackButton != null) { optionsBackButton.onClick.RemoveAllListeners(); optionsBackButton.onClick.AddListener(CloseOptions); }

        // キーワードパネルを手前に表示するためCanvasを設定
        SetupKeywordPanelCanvas();

        LangManager.LanguageChanged += OnLanguageChanged;
        SetupOptionsUI();
    }

    void SetupKeywordPanelCanvas()
    {
        if (keywordPanel == null) return;
        var canvas = keywordPanel.GetComponent<Canvas>();
        if (canvas == null) canvas = keywordPanel.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 5;
        var raycaster = keywordPanel.GetComponent<UnityEngine.UI.GraphicRaycaster>();
        if (raycaster == null) keywordPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (m_isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void SetupOptionsUI()
    {
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.wholeNumbers = false;
            bgmSlider.onValueChanged.RemoveAllListeners();
            bgmSlider.interactable = true;
            bgmSlider.SetValueWithoutNotify(Mathf.Clamp01(GameSettings.BGMVolume));
            bgmSlider.onValueChanged.AddListener(v => GameSettings.SetBGMVolume(v));
        }

        if (seSlider != null)
        {
            seSlider.minValue = 0f;
            seSlider.maxValue = 1f;
            seSlider.wholeNumbers = false;
            seSlider.onValueChanged.RemoveAllListeners();
            seSlider.interactable = true;
            seSlider.SetValueWithoutNotify(Mathf.Clamp01(GameSettings.SEVolume));
            seSlider.onValueChanged.AddListener(v => GameSettings.SetSEVolume(v));
        }

        if (languageDropdown != null)
        {
            languageDropdown.onValueChanged.RemoveAllListeners();
            languageDropdown.ClearOptions();

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
            {
                idx = codes.IndexOf(LangManager.Instance.CurrentLanguageCode);
                if (idx < 0) idx = 0;
            }
            languageDropdown.value = idx;

            languageDropdown.onValueChanged.AddListener(i =>
            {
                if (i < 0 || i >= codes.Count) return;
                if (LangManager.Instance != null)
                    LangManager.Instance.SetLanguage(codes[i]);
            });
        }
    }

    void PauseGame()
    {
        m_isPaused = true;
        Time.timeScale = 0f;

        if (TypingCombatSystem.Instance != null)
        {
            m_wasTypingInputActive = TypingCombatSystem.Instance.IsInputActive;
            TypingCombatSystem.Instance.SetInputActive(false);
        }

        if (pausePanel != null) pausePanel.SetActive(true);
        if (keywordPanel != null) keywordPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    public void ResumeGame()
    {
        m_isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null) pausePanel.SetActive(false);
        if (keywordPanel != null) keywordPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (TypingCombatSystem.Instance != null)
            TypingCombatSystem.Instance.SetInputActive(m_wasTypingInputActive);

        var typingUI = FindObjectOfType<TypingUI>();
        if (typingUI != null)
            typingUI.RefreshUI();
    }

    void ShowKeyword()
    {
        if (keywordPanel != null) keywordPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);

        if (keywordPanelController != null)
            keywordPanelController.RefreshAll();
        else if (keywordPanel != null)
            keywordPanel.GetComponentInChildren<KeywordPanel>()?.RefreshAll();
    }

    void CloseKeyword()
    {
        // キーワードパネルを閉じてポーズメニューに戻る（ゲームには戻らない）
        if (keywordPanel != null) keywordPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    void ShowOptions()
    {
        if (optionsPanel != null) optionsPanel.SetActive(true);
        if (keywordPanel != null) keywordPanel.SetActive(false);
    }

    void CloseOptions()
    {
        // 設定パネルを閉じてポーズメニューに戻る（ゲームには戻らない）
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    void HideSubPanels()
    {
        if (keywordPanel != null) keywordPanel.SetActive(false);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }

    void RestartScene()
    {
        Time.timeScale = 1f;
        string scene = string.IsNullOrEmpty(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;
        SceneManager.LoadScene(scene);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        LangManager.LanguageChanged -= OnLanguageChanged;
    }

    void OnLanguageChanged(string _)
    {
        // キーワードパネルが開いていたら即時更新
        if (keywordPanel != null && keywordPanel.activeSelf)
        {
            if (keywordPanelController != null)
                keywordPanelController.RefreshAll();
            else
                keywordPanel.GetComponentInChildren<KeywordPanel>()?.RefreshAll();
        }

        // ゲーム中の技リストも更新
        var typingUI = FindObjectOfType<TypingUI>();
        if (typingUI != null)
            typingUI.RefreshUI();
    }
}
