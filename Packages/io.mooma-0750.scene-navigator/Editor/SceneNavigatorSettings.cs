using UnityEngine;
using UnityEditor;

/// <summary>
/// シーンナビゲーターの設定パネル
/// </summary>
public class SceneNavigatorSettings : EditorWindow
{
    private SceneNavigatorLocalization.Language selectedLanguage;
    private Vector2 scrollPosition;
    private SceneNavigator parentWindow;

    // 除外設定用のフィールド
    private string excludeFolders;
    private string excludeScenes;

    // 追加：包括設定用のフィールド
    private string includeFolders;
    private string includeScenes;

    // 設定保存用のキー
    private const string ExcludeFoldersKey = "SceneNavigator_ExcludedFolders";
    private const string ExcludeScenesKey = "SceneNavigator_ExcludedScenes";
    private const string IncludeFoldersKey = "SceneNavigator_IncludedFolders";
    private const string IncludeScenesKey = "SceneNavigator_IncludedScenes";
    
    // ウィンドウを表示
    public static void ShowWindow(SceneNavigator parent)
    {
        var window = GetWindow<SceneNavigatorSettings>(true, SceneNavigatorLocalization.GetText("SettingsPanelTitle"), true);
        window.minSize = new Vector2(300, 200);
        window.maxSize = new Vector2(400, 350);
        window.parentWindow = parent;
        
        window.selectedLanguage = SceneNavigatorLocalization.GetCurrentLanguage();
        window.excludeFolders = EditorPrefs.GetString(ExcludeFoldersKey, "");
        window.excludeScenes = EditorPrefs.GetString(ExcludeScenesKey, "");
        window.includeFolders = EditorPrefs.GetString(IncludeFoldersKey, "");
        window.includeScenes = EditorPrefs.GetString(IncludeScenesKey, "");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("SettingsPanelTitle"), EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("LanguageLabel"), GUILayout.Width(100));
        SceneNavigatorLocalization.Language newLanguage = (SceneNavigatorLocalization.Language)EditorGUILayout.EnumPopup(selectedLanguage);
        if (newLanguage != selectedLanguage)
        {
            selectedLanguage = newLanguage;
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("EnglishLanguage") + " / " + SceneNavigatorLocalization.GetText("JapaneseLanguage"));
        EditorGUILayout.Space(20);

        // 除外設定のUI
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("ExcludeSettingsTitle"), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(SceneNavigatorLocalization.GetText("ExcludeSettingsHelp"), MessageType.Info);
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("ExcludedFolders"), GUILayout.Width(100));
        excludeFolders = EditorGUILayout.TextField(excludeFolders);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("ExcludedScenes"), GUILayout.Width(100));
        excludeScenes = EditorGUILayout.TextField(excludeScenes);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(20);

        // 包括設定のUI
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("IncludeSettingsTitle"), EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(SceneNavigatorLocalization.GetText("IncludeSettingsHelp"), MessageType.Info);
        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("IncludedFolders"), GUILayout.Width(100));
        includeFolders = EditorGUILayout.TextField(includeFolders);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("IncludedScenes"), GUILayout.Width(100));
        includeScenes = EditorGUILayout.TextField(includeScenes);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(20);
        
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(SceneNavigatorLocalization.GetText("Apply"), GUILayout.Width(100)))
        {
            ApplySettings();
        }
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(SceneNavigatorLocalization.GetText("Close"), GUILayout.Width(100)))
        {
            Close();
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndScrollView();
    }
    
    private void ApplySettings()
    {
        SceneNavigatorLocalization.SaveLanguageSetting(selectedLanguage);
        EditorPrefs.SetString(ExcludeFoldersKey, excludeFolders);
        EditorPrefs.SetString(ExcludeScenesKey, excludeScenes);
        EditorPrefs.SetString(IncludeFoldersKey, includeFolders);
        EditorPrefs.SetString(IncludeScenesKey, includeScenes);
        if (parentWindow != null)
        {
            parentWindow.OnSettingsChanged();
        }
        titleContent = new GUIContent(SceneNavigatorLocalization.GetText("SettingsPanelTitle"));
    }
}
