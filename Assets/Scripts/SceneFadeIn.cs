using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// シーン開始時に黒画面からフェードインし、スコアを表示するコンポーネント。
/// ClearSceneに配置して使う。scoreText/totalPointsTextが未アサインでも
/// 自動で画面中央にスコアを生成して表示する。
/// </summary>
public class SceneFadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.0f;

    [Header("スコア表示（アサイン済みのTMP_Textがあればそちらを使う）")]
    [SerializeField] private bool showScore = true;         // オフにするとスコア非表示（GameOverScene用）
    [SerializeField] private TMP_Text scoreText;
    [SerializeField] private TMP_Text totalPointsText;

    void Start()
    {
        // KeyLastScore から直接読む（静的変数に依存しない）
        int lastScore   = PlayerPrefs.GetInt("Score_LastScore", 0);
        int totalPoints = ScoreManager.LoadTotalPoints();

        if (showScore)
        {
            if (scoreText != null)
                scoreText.text = $"{lastScore:N0} pt";

            if (totalPointsText != null)
                totalPointsText.text = $"{totalPoints:N0} pt";

            // アサインがない場合は自動生成して表示
            if (scoreText == null && totalPointsText == null)
                StartCoroutine(ShowAutoScore(lastScore, totalPoints));
        }

        StartCoroutine(FadeIn());
    }

    IEnumerator ShowAutoScore(int lastScore, int totalPoints)
    {
        // フェードイン完了まで待つ
        yield return new WaitForSeconds(fadeDuration + 0.1f);

        // Canvas生成
        GameObject canvasObj = new GameObject("ScoreCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 今回のスコアのみ表示
        CreateAutoText(canvasObj.transform, $"SCORE\n{lastScore:N0} pt", new Vector2(0, 0), 48);
    }

    void CreateAutoText(Transform parent, string text, Vector2 anchoredPos, float fontSize = 48)
    {
        GameObject obj = new GameObject("AutoScoreText");
        obj.transform.SetParent(parent, false);
        TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600, 120);
        rt.anchoredPosition = anchoredPos;
    }

    IEnumerator FadeIn()
    {
        // 前シーンからDontDestroyOnLoadで持ち込まれた暗転Canvasを除去
        GameObject old = GameObject.Find("SceneFadeCanvas");
        if (old != null)
            Destroy(old);

        // フェードイン用Canvas生成
        GameObject fadeCanvasObject = new GameObject("SceneFadeCanvas");
        Canvas canvas = fadeCanvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        CanvasGroup group = fadeCanvasObject.AddComponent<CanvasGroup>();

        GameObject imageObj = new GameObject("FadeImage");
        imageObj.transform.SetParent(fadeCanvasObject.transform, false);
        Image image = imageObj.AddComponent<Image>();
        image.color = Color.black;
        RectTransform rt = imageObj.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        group.alpha = 1f;

        // WebGLでは最初のフレームでTime.deltaTimeが0になることがあるため1フレーム待つ
        yield return null;

        float t = 0f;
        float duration = Mathf.Max(0.01f, fadeDuration);

        while (t < duration)
        {
            // WebGL対策: deltaTimeが0でも最低1ms進める
            t += Mathf.Max(Time.deltaTime, 0.001f);
            group.alpha = 1f - Mathf.Clamp01(t / duration);
            yield return null;
        }

        group.alpha = 0f;
        Destroy(fadeCanvasObject);
    }
}
