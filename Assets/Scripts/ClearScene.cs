using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// ClearSceneに配置して使う。
/// タイトルへ戻るボタンを制御する。
/// </summary>
public class ClearScene : MonoBehaviour
{
    [Header("ボタン")]
    [SerializeField] private Button toTitleButton;

    [Header("シーン名")]
    [SerializeField] private string titleSceneName = "TitleScene";

    void Start()
    {
        if (toTitleButton != null)
        {
            toTitleButton.onClick.RemoveAllListeners();
            toTitleButton.onClick.AddListener(GoToTitle);
        }
    }

    void GoToTitle()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(titleSceneName);
    }
}
