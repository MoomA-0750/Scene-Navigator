using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// シーンナビゲーターの多言語対応用クラス
/// </summary>
public static class SceneNavigatorLocalization
{
    public enum Language
    {
        English,
        Japanese
    }
    
    // 現在の言語
    private static Language currentLanguage = Language.English; // デフォルトは英語
    
    // 言語設定保存用のキー
    private const string LanguagePrefsKey = "SceneNavigator_Language";
    
    // 言語設定を読み込み
    public static void LoadLanguageSetting()
    {
        int langIndex = EditorPrefs.GetInt(LanguagePrefsKey, (int)Language.English);
        currentLanguage = (Language)langIndex;
    }
    
    // 言語設定を保存
    public static void SaveLanguageSetting(Language language)
    {
        currentLanguage = language;
        EditorPrefs.SetInt(LanguagePrefsKey, (int)language);
    }
    
    // 現在の言語を取得
    public static Language GetCurrentLanguage()
    {
        return currentLanguage;
    }
    
    // 翻訳用辞書
    private static readonly Dictionary<string, Dictionary<Language, string>> translations = new Dictionary<string, Dictionary<Language, string>>
    {
        // ウィンドウタイトル
        {"WindowTitle", new Dictionary<Language, string> {
            {Language.English, "Scene Navigator"},
            {Language.Japanese, "シーンナビゲーター"}
        }},
        
        // ボタンとラベル
        {"Refresh", new Dictionary<Language, string> {
            {Language.English, "Refresh Scene List"},
            {Language.Japanese, "シーンリスト更新"}
        }},
        {"Search", new Dictionary<Language, string> {
            {Language.English, "Search:"},
            {Language.Japanese, "検索:"}
        }},
        {"ShowFavoritesOnly", new Dictionary<Language, string> {
            {Language.English, "Show Favorites Only"},
            {Language.Japanese, "お気に入りのみ表示"}
        }},
        {"GroupByFolder", new Dictionary<Language, string> {
            {Language.English, "Group by Folder"},
            {Language.Japanese, "フォルダでグループ化"}
        }},
        {"ShowThumbnails", new Dictionary<Language, string> {
            {Language.English, "Show Thumbnails"},
            {Language.Japanese, "サムネイル表示"}
        }},
        {"Size", new Dictionary<Language, string> {
            {Language.English, "Size:"},
            {Language.Japanese, "サイズ:"}
        }},
        {"RegenerateThumbnails", new Dictionary<Language, string> {
            {Language.English, "Regenerate All Thumbnails"},
            {Language.Japanese, "すべてのサムネイルを再取得"}
        }},
        {"Open", new Dictionary<Language, string> {
            {Language.English, "Open"},
            {Language.Japanese, "開く"}
        }},
        {"Update", new Dictionary<Language, string> {
            {Language.English, "Update"},
            {Language.Japanese, "更新"}
        }},
        {"Settings", new Dictionary<Language, string> {
            {Language.English, "Settings"},
            {Language.Japanese, "設定"}
        }},
        {"NoThumbnail", new Dictionary<Language, string> {
            {Language.English, "(No Thumbnail)"},
            {Language.Japanese, "(サムネイルなし)"}
        }},
        {"NoScenesFound", new Dictionary<Language, string> {
            {Language.English, "No scenes found."},
            {Language.Japanese, "シーンが見つかりません。"}
        }},
        {"Root", new Dictionary<Language, string> {
            {Language.English, "Root"},
            {Language.Japanese, "ルート"}
        }},
        
        // ダイアログとメッセージ
        {"RegenerateThumbnailsTitle", new Dictionary<Language, string> {
            {Language.English, "Regenerate Thumbnails"},
            {Language.Japanese, "サムネイル再生成"}
        }},
        {"RegenerateThumbnailsMessage", new Dictionary<Language, string> {
            {Language.English, "This will open all scenes in your project to generate thumbnails.\n\nDo you want to proceed?"},
            {Language.Japanese, "すべてのシーンのサムネイルを再生成するには、各シーンを順番に開く必要があります。\n\n実行しますか？"}
        }},
        {"Yes", new Dictionary<Language, string> {
            {Language.English, "Yes"},
            {Language.Japanese, "はい"}
        }},
        {"Cancel", new Dictionary<Language, string> {
            {Language.English, "Cancel"},
            {Language.Japanese, "キャンセル"}
        }},
        
        // 設定パネル
        {"SettingsPanelTitle", new Dictionary<Language, string> {
            {Language.English, "Scene Navigator Settings"},
            {Language.Japanese, "シーンナビゲーター設定"}
        }},
        {"LanguageLabel", new Dictionary<Language, string> {
            {Language.English, "Language:"},
            {Language.Japanese, "言語:"}
        }},
        {"ExcludeSettingsTitle", new Dictionary<Language, string> {
            {Language.English, "Exclude Settings"},
            {Language.Japanese, "除外設定"}
        }},
        {"ExcludeSettingsHelp", new Dictionary<Language, string> {
            {Language.English, "Specify folders and scenes to exclude, separated by commas.\nExample: FolderA,FolderB"},
            {Language.Japanese, "除外したいフォルダーおよびシーンをカンマ区切りで指定してください。\n例：FolderA,FolderB"}
        }},
        {"ExcludedFolders", new Dictionary<Language, string> {
            {Language.English, "Exclude Folders:"},
            {Language.Japanese, "除外フォルダー:"}
        }},
        {"ExcludedScenes", new Dictionary<Language, string> {
            {Language.English, "Exclude Scenes:"},
            {Language.Japanese, "除外シーン:"}
        }},
        {"IncludeSettingsTitle", new Dictionary<Language, string> {
            {Language.English, "Include Settings"},
            {Language.Japanese, "包括設定"}
        }},
        {"IncludeSettingsHelp", new Dictionary<Language, string> {
            {Language.English, "Specify folders and scenes to include, separated by commas.\nExample: FolderC,FolderD\n*Leave blank to include all."},
            {Language.Japanese, "表示対象とするフォルダーおよびシーンをカンマ区切りで指定してください。\n例：FolderC,FolderD\n※何も指定しない場合は全件対象となります。"}
        }},
        {"IncludedFolders", new Dictionary<Language, string> {
            {Language.English, "Include Folders:"},
            {Language.Japanese, "包括フォルダー:"}
        }},
        {"IncludedScenes", new Dictionary<Language, string> {
            {Language.English, "Include Scenes:"},
            {Language.Japanese, "包括シーン:"}
        }}
    };
    
    // テキストを取得
    public static string GetText(string key)
    {
        if (translations.TryGetValue(key, out Dictionary<Language, string> langDict))
        {
            if (langDict.TryGetValue(currentLanguage, out string text))
            {
                return text;
            }
        }
        
        // キーが見つからない場合はキーをそのまま返す
        return key;
    }
}
