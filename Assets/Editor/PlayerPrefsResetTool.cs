#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PlayerPrefsResetTool
{
    [MenuItem("Tools/PlayerPrefsをリセット（パッケージ化前に実行）")]
    static void ResetAll()
    {
        if (!EditorUtility.DisplayDialog(
            "PlayerPrefs リセット",
            "スコア・累計ポイント・解放済みキーワード・設定をすべて削除します。\nよろしいですか？",
            "リセットする", "キャンセル"))
            return;

        PlayerPrefs.DeleteKey("Score_TotalPoints");
        PlayerPrefs.DeleteKey("Score_SessionChars");
        PlayerPrefs.DeleteKey("Score_LastScore");
        PlayerPrefs.DeleteKey("Keywords_Unlocked");
        PlayerPrefs.DeleteKey("GS_BGMVolume");
        PlayerPrefs.DeleteKey("GS_SEVolume");
        PlayerPrefs.DeleteKey("GS_Difficulty");
        PlayerPrefs.DeleteKey("LangManager_Language");
        PlayerPrefs.DeleteKey("BGMVolume");
        PlayerPrefs.DeleteKey("SEVolume");
        PlayerPrefs.DeleteKey("Difficulty");
        PlayerPrefs.DeleteKey("Language");
        PlayerPrefs.Save();

        Debug.Log("[PlayerPrefsResetTool] すべてのPlayerPrefsを削除しました。");
        EditorUtility.DisplayDialog("完了", "PlayerPrefsをリセットしました。", "OK");
    }
}
#endif
