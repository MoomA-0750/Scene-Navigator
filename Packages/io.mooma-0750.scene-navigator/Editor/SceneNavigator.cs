using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public class SceneNavigator : EditorWindow
{
    private Vector2 scrollPosition;
    private List<string> sceneNames = new List<string>();
    private List<string> scenePaths = new List<string>();
    private HashSet<string> favoriteScenes = new HashSet<string>();
    private string searchString = "";
    private bool showFavoritesOnly = false;
    private bool groupByFolder = true;
    private Dictionary<string, List<int>> folderGroups = new Dictionary<string, List<int>>();
    private Dictionary<string, bool> folderExpanded = new Dictionary<string, bool>();
    private Dictionary<string, Texture2D> thumbnailCache = new Dictionary<string, Texture2D>();
    private bool showThumbnails = true;
    private string thumbnailsDirectory = "Library/SceneNavigator/Thumbnails";
    private int thumbnailSize = 100;

    // スタイル
    private GUIStyle headerStyle;
    private GUIStyle sceneButtonStyle;
    private GUIStyle folderStyle;
    private GUIStyle thumbnailStyle;
    private GUIStyle settingsButtonStyle;

    private bool isCompactMode = false;
    private float compactModeThreshold = 300f;
    private float lastWindowWidth = 0f;

    // メニューに項目を追加
    [MenuItem("Tools/Scene Navigator/Scene Navigator")]
    public static void ShowWindow()
    {
        // 言語設定を読み込み
        SceneNavigatorLocalization.LoadLanguageSetting();
        
        var window = GetWindow<SceneNavigator>();
        window.titleContent = new GUIContent(SceneNavigatorLocalization.GetText("WindowTitle"));
        window.minSize = new Vector2(400, 400);
        window.LoadFavorites();
        window.LoadThumbnails();
        window.RefreshSceneList();
    }
    
    // メニューに設定画面を開くための項目を追加
    [MenuItem("Tools/Scene Navigator/Settings")]
    public static void ShowSettingsWindow()
    {
        // 言語設定を読み込み
        SceneNavigatorLocalization.LoadLanguageSetting();
        
        // 既存のSceneNavigatorウィンドウを探す
        SceneNavigator navigatorWindow = GetWindow<SceneNavigator>(false, "", false);
        
        if (navigatorWindow != null)
        {
            // 既存のウィンドウがある場合は、そのウィンドウから設定を開く
            SceneNavigatorSettings.ShowWindow(navigatorWindow);
        }
        else
        {
            // ウィンドウがない場合は、新しくウィンドウを作成して設定を開く
            var window = GetWindow<SceneNavigator>();
            window.titleContent = new GUIContent(SceneNavigatorLocalization.GetText("WindowTitle"));
            window.minSize = new Vector2(400, 400);
            window.LoadFavorites();
            window.LoadThumbnails();
            
            // 遅延呼び出しで設定ウィンドウを開く
            EditorApplication.delayCall += () => {
                SceneNavigatorSettings.ShowWindow(window);
            };
        }
    }

    private void OnEnable()
    {
        groupByFolder = true; // これで常にデフォルトでフォルダグループ化が有効になります
        LoadFavorites();
        LoadThumbnails();
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorApplication.update -= OnEditorUpdate;
    }

    private void InitStyles()
    {
        if (headerStyle == null)
        {
            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(5, 5, 5, 5);
        }

        if (sceneButtonStyle == null)
        {
            sceneButtonStyle = new GUIStyle(EditorStyles.miniButton);
            sceneButtonStyle.alignment = TextAnchor.MiddleLeft;
        }

        if (folderStyle == null)
        {
            folderStyle = new GUIStyle(EditorStyles.foldout);
            folderStyle.fontStyle = FontStyle.Bold;
        }

        if (thumbnailStyle == null)
        {
            thumbnailStyle = new GUIStyle();
            thumbnailStyle.margin = new RectOffset(5, 5, 5, 5);
            thumbnailStyle.padding = new RectOffset(5, 5, 5, 5);
            thumbnailStyle.border = new RectOffset(1, 1, 1, 1);
            thumbnailStyle.normal.background = MakeTexture(2, 2, new Color(0.3f, 0.3f, 0.3f, 0.3f));
        }
        
        if (settingsButtonStyle == null)
        {
            settingsButtonStyle = new GUIStyle(EditorStyles.miniButton);
            settingsButtonStyle.padding = new RectOffset(5, 5, 2, 2);
        }
    }
    
    // 設定変更時に呼び出されるメソッド
    public void OnSettingsChanged()
    {
        // ウィンドウタイトルを更新
        titleContent = new GUIContent(SceneNavigatorLocalization.GetText("WindowTitle"));
        
        // ウィンドウを再描画
        Repaint();
    }

    private void OnGUI()
    {
        InitStyles();

        // ウィンドウ幅に基づいてレイアウトモードを決定
        float currentWidth = position.width;
        if (currentWidth != lastWindowWidth)
        {
            lastWindowWidth = currentWidth;
            isCompactMode = currentWidth < compactModeThreshold;
            
            // ウィンドウ幅に応じてサムネイルサイズを調整
            if (!isCompactMode)
            {
                thumbnailSize = Mathf.Clamp(Mathf.FloorToInt(currentWidth * 0.4f), 100, 200);
            }
            else
            {
                thumbnailSize = Mathf.Clamp(Mathf.FloorToInt(currentWidth * 0.7f), 80, 150);
            }
        }

        // ヘッダー部分（タイトルと設定ボタン）
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(SceneNavigatorLocalization.GetText("WindowTitle"), headerStyle);
        GUILayout.FlexibleSpace();
        
        // 設定ボタン
        if (GUILayout.Button(SceneNavigatorLocalization.GetText("Settings"), settingsButtonStyle, GUILayout.Width(60)))
        {
            OpenSettingsPanel();
        }
        EditorGUILayout.EndHorizontal();
        
        EditorGUILayout.Space();
        
        // 上部のコントロールパネル
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        // 更新ボタン
        if (GUILayout.Button(SceneNavigatorLocalization.GetText("Refresh")))
        {
            RefreshSceneList();
        }

        // 検索フィールド
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(SceneNavigatorLocalization.GetText("Search"), GUILayout.Width(40));
        string newSearchString = EditorGUILayout.TextField(searchString);
        if (newSearchString != searchString)
        {
            searchString = newSearchString;
        }
        EditorGUILayout.EndHorizontal();

        // オプションコントロール - コンパクトモードでも表示を調整
        if (isCompactMode)
        {
            // コンパクトモード: 各オプションを単独の行に表示
            bool newShowFavoritesOnly = EditorGUILayout.Toggle(SceneNavigatorLocalization.GetText("ShowFavoritesOnly"), showFavoritesOnly);
            if (newShowFavoritesOnly != showFavoritesOnly)
            {
                showFavoritesOnly = newShowFavoritesOnly;
            }
            
            bool newGroupByFolder = EditorGUILayout.Toggle(SceneNavigatorLocalization.GetText("GroupByFolder"), groupByFolder);
            if (newGroupByFolder != groupByFolder)
            {
                groupByFolder = newGroupByFolder;
                if (groupByFolder)
                {
                    OrganizeByFolders();
                }
            }
            
            bool newShowThumbnails = EditorGUILayout.Toggle(SceneNavigatorLocalization.GetText("ShowThumbnails"), showThumbnails);
            if (newShowThumbnails != showThumbnails)
            {
                showThumbnails = newShowThumbnails;
            }
            
            if (showThumbnails)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("Size"), GUILayout.Width(40));
                int newSize = EditorGUILayout.IntSlider(thumbnailSize, 50, 200);
                if (newSize != thumbnailSize)
                {
                    thumbnailSize = newSize;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            // 通常モード: オプションを複数行に分けて表示
            // オプション行1
            EditorGUILayout.BeginHorizontal();
            bool newShowFavoritesOnly = EditorGUILayout.Toggle(SceneNavigatorLocalization.GetText("ShowFavoritesOnly"), showFavoritesOnly);
            if (newShowFavoritesOnly != showFavoritesOnly)
            {
                showFavoritesOnly = newShowFavoritesOnly;
            }
            
            bool newGroupByFolder = EditorGUILayout.Toggle(SceneNavigatorLocalization.GetText("GroupByFolder"), groupByFolder);
            if (newGroupByFolder != groupByFolder)
            {
                groupByFolder = newGroupByFolder;
                if (groupByFolder)
                {
                    OrganizeByFolders();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // オプション行2
            EditorGUILayout.BeginHorizontal();
            bool newShowThumbnails = EditorGUILayout.Toggle(SceneNavigatorLocalization.GetText("ShowThumbnails"), showThumbnails);
            if (newShowThumbnails != showThumbnails)
            {
                showThumbnails = newShowThumbnails;
            }
            
            if (showThumbnails)
            {
                EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("Size"), GUILayout.Width(40));
                int newSize = EditorGUILayout.IntSlider(thumbnailSize, 50, 200);
                if (newSize != thumbnailSize)
                {
                    thumbnailSize = newSize;
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        
        // すべてのサムネイルを再取得するボタン
        if (showThumbnails && GUILayout.Button(SceneNavigatorLocalization.GetText("RegenerateThumbnails")))
        {
            RegenerateThumbnails();
        }
        
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space();

        // スクロールビュー開始
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // シーンリストの表示
        if (sceneNames.Count == 0)
        {
            GUILayout.Label(SceneNavigatorLocalization.GetText("NoScenesFound"));
        }
        else
        {
            if (groupByFolder)
            {
                // フォルダでグループ化して表示
                DisplayScenesByFolder();
            }
            else
            {
                // 通常表示
                DisplaySceneList();
            }
        }

        EditorGUILayout.EndScrollView();
    }
    
    // 設定パネルを開く
    private void OpenSettingsPanel()
    {
        SceneNavigatorSettings.ShowWindow(this);
    }

    private void ShowThumbnail(string scenePath)
    {
        if (!thumbnailCache.ContainsKey(scenePath) || thumbnailCache[scenePath] == null)
        {
            // サムネイルが存在しない場合は空白のサムネイルを表示
            EditorGUILayout.LabelField(SceneNavigatorLocalization.GetText("NoThumbnail"), GUILayout.Height(thumbnailSize * 0.6f));
            return;
        }
        
        // ウィンドウ幅に合わせてサムネイルを表示
        float displayWidth = Mathf.Min(thumbnailSize, position.width - 50);
        float displayHeight = displayWidth * 0.6f; // 16:9のアスペクト比を維持
        
        // サムネイルの画像表示
        GUILayout.Box(thumbnailCache[scenePath], thumbnailStyle, 
            GUILayout.Width(displayWidth), 
            GUILayout.Height(displayHeight));
    }

    private void OrganizeByFolders()
    {
        folderGroups.Clear();
        
        for (int i = 0; i < scenePaths.Count; i++)
        {
            string path = scenePaths[i];
            string directory = Path.GetDirectoryName(path);
            
            if (directory == "Assets")
            {
                directory = SceneNavigatorLocalization.GetText("Root");
            }
            else
            {
                // "Assets/"を除去
                directory = directory.Substring(7);  
            }
            
            if (!folderGroups.ContainsKey(directory))
            {
                folderGroups[directory] = new List<int>();
            }
            
            folderGroups[directory].Add(i);
        }
    }

    private void RegenerateThumbnails()
    {
        // 確認ダイアログ
        if (!EditorUtility.DisplayDialog(
                SceneNavigatorLocalization.GetText("RegenerateThumbnailsTitle"),
                SceneNavigatorLocalization.GetText("RegenerateThumbnailsMessage"),
                SceneNavigatorLocalization.GetText("Yes"),
                SceneNavigatorLocalization.GetText("Cancel")))
        {
            return;
        }
        
        // 現在のシーンのパスを保存
        string currentScenePath = SceneManager.GetActiveScene().path;
        bool needToReopen = !string.IsNullOrEmpty(currentScenePath);
        
        // サムネイル情報を保持しながらディレクトリをクリア
        Dictionary<string, string> mappings = new Dictionary<string, string>();
        
        if (Directory.Exists(thumbnailsDirectory))
        {
            try
            {
                // 既存のマッピングを保存
                mappings = LoadThumbnailMappings();
                
                // サムネイル画像ファイルのみを削除（マッピングファイルは残す）
                string[] thumbnailFiles = Directory.GetFiles(thumbnailsDirectory, "*.png");
                foreach (string file in thumbnailFiles)
                {
                    File.Delete(file);
                }
                
                Debug.Log($"サムネイル画像をクリアしました。マッピング情報 ({mappings.Count} 件) は保持されています。");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"サムネイルディレクトリのクリア中にエラーが発生しました: {e.Message}");
                return;
            }
        }
        else
        {
            // ディレクトリが存在しない場合は作成
            Directory.CreateDirectory(thumbnailsDirectory);
        }
        
        // キャッシュをクリア
        foreach (var texture in thumbnailCache.Values)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }
        thumbnailCache.Clear();
        
        // 非同期で全シーンのサムネイルを生成するプロセスを開始
        EditorCoroutine.Start(RegenerateThumbnailsCoroutine(needToReopen, currentScenePath));
    }

    // 以下のメソッドはUIに関わる部分のみ多言語対応に修正
    
    private void DisplaySceneList()
    {
        for (int i = 0; i < sceneNames.Count; i++)
        {
            // 検索フィルタリング
            if (!string.IsNullOrEmpty(searchString) && 
                !sceneNames[i].ToLower().Contains(searchString.ToLower()))
            {
                continue;
            }

            // お気に入りのみ表示フィルタリング
            if (showFavoritesOnly && !favoriteScenes.Contains(scenePaths[i]))
            {
                continue;
            }

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (isCompactMode)
            {
                // コンパクトモード：お気に入りと名前を一行で表示
                EditorGUILayout.BeginHorizontal();
                
                // お気に入りトグル
                bool isFavorite = favoriteScenes.Contains(scenePaths[i]);
                bool newFavorite = EditorGUILayout.Toggle(isFavorite, GUILayout.Width(20));
                if (newFavorite != isFavorite)
                {
                    ToggleFavorite(scenePaths[i]);
                }
                
                // シーン名表示
                GUIContent nameContent = new GUIContent(sceneNames[i]);
                if (GUILayout.Button(nameContent, sceneButtonStyle, GUILayout.ExpandWidth(true)))
                {
                    OpenScene(scenePaths[i]);
                }
                
                EditorGUILayout.EndHorizontal();
                
                // アクションボタン：Openのみ表示（Updateボタンを削除）
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(SceneNavigatorLocalization.GetText("Open"), GUILayout.ExpandWidth(true)))
                {
                    OpenScene(scenePaths[i]);
                }
                EditorGUILayout.EndHorizontal();
            }
            else
            {
                // 通常モード
                EditorGUILayout.BeginHorizontal();
                
                // お気に入りトグル
                bool isFavorite = favoriteScenes.Contains(scenePaths[i]);
                bool newFavorite = EditorGUILayout.Toggle(isFavorite, GUILayout.Width(20));
                if (newFavorite != isFavorite)
                {
                    ToggleFavorite(scenePaths[i]);
                }
                
                // シーン名表示
                if (GUILayout.Button(sceneNames[i], sceneButtonStyle, GUILayout.ExpandWidth(true)))
                {
                    OpenScene(scenePaths[i]);
                }
                
                // Openボタンのみ表示（Updateボタンを削除）
                if (GUILayout.Button(SceneNavigatorLocalization.GetText("Open"), GUILayout.Width(60)))
                {
                    OpenScene(scenePaths[i]);
                }
                
                EditorGUILayout.EndHorizontal();
            }
            
            // サムネイル表示
            if (showThumbnails)
            {
                ShowThumbnail(scenePaths[i]);
            }
            
            EditorGUILayout.EndVertical();
        }
    }

    private void DisplayScenesByFolder()
    {
        foreach (var folder in folderGroups.Keys)
        {
            // 表示するコンテンツがなければスキップ
            if (!HasVisibleContent(folder))
            {
                continue;
            }

            // フォルダの初期展開状態
            if (!folderExpanded.ContainsKey(folder))
            {
                folderExpanded[folder] = true;
            }

            // フォルダのFoldout
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // フォルダ名を表示スペースに合わせて調整
            string displayFolderName = folder;
            if (isCompactMode && folder.Length > 20)
            {
                displayFolderName = folder.Substring(0, 17) + "...";
            }
            
            folderExpanded[folder] = EditorGUILayout.Foldout(folderExpanded[folder], displayFolderName, true, folderStyle);

            if (folderExpanded[folder])
            {
                foreach (int index in folderGroups[folder])
                {
                    // フィルタリング
                    if ((!string.IsNullOrEmpty(searchString) && 
                         !sceneNames[index].ToLower().Contains(searchString.ToLower())) ||
                        (showFavoritesOnly && !favoriteScenes.Contains(scenePaths[index])))
                    {
                        continue;
                    }

                    EditorGUILayout.BeginVertical();
                    
                    if (isCompactMode)
                    {
                        // コンパクトモードでの表示
                        // インデント付きのヘッダー行
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);
                        
                        // お気に入りトグル
                        bool isFavorite = favoriteScenes.Contains(scenePaths[index]);
                        bool newFavorite = EditorGUILayout.Toggle(isFavorite, GUILayout.Width(20));
                        if (newFavorite != isFavorite)
                        {
                            ToggleFavorite(scenePaths[index]);
                        }
                        
                        // シーン名表示
                        GUIContent nameContent = new GUIContent(sceneNames[index]);
                        if (GUILayout.Button(nameContent, sceneButtonStyle, GUILayout.ExpandWidth(true)))
                        {
                            OpenScene(scenePaths[index]);
                        }
                        
                        EditorGUILayout.EndHorizontal();
                        
                        // アクションボタン行
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(35); // 追加インデント
                        
                        // 開くボタン
                        if (GUILayout.Button(SceneNavigatorLocalization.GetText("Open"), GUILayout.ExpandWidth(true)))
                        {
                            OpenScene(scenePaths[index]);
                        }
                        
                        // サムネイル更新ボタン
                        if (showThumbnails)
                        {
                            if (GUILayout.Button(SceneNavigatorLocalization.GetText("Update"), GUILayout.ExpandWidth(true)))
                            {
                                CaptureSceneThumbnail(scenePaths[index]);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        // 通常モード - 従来のレイアウト
                        EditorGUILayout.BeginHorizontal();
                        
                        // インデント
                        GUILayout.Space(15);
                        
                        // お気に入りトグル
                        bool isFavorite = favoriteScenes.Contains(scenePaths[index]);
                        bool newFavorite = EditorGUILayout.Toggle(isFavorite, GUILayout.Width(20));
                        if (newFavorite != isFavorite)
                        {
                            ToggleFavorite(scenePaths[index]);
                        }
                        
                        // シーン名表示
                        if (GUILayout.Button(sceneNames[index], sceneButtonStyle, GUILayout.ExpandWidth(true)))
                        {
                            OpenScene(scenePaths[index]);
                        }
                        
                        // 開くボタン
                        if (GUILayout.Button(SceneNavigatorLocalization.GetText("Open"), GUILayout.Width(60)))
                        {
                            OpenScene(scenePaths[index]);
                        }
                        
                        // サムネイル更新ボタン
                        if (showThumbnails)
                        {
                            if (GUILayout.Button(SceneNavigatorLocalization.GetText("Update"), GUILayout.Width(50)))
                            {
                                CaptureSceneThumbnail(scenePaths[index]);
                            }
                        }
                        
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    // サムネイル表示
                    if (showThumbnails)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(35); // 追加インデント
                        ShowThumbnail(scenePaths[index]);
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }
            
            EditorGUILayout.EndVertical();
        }
    }

    private bool HasVisibleContent(string folder)
    {
        if (!folderGroups.ContainsKey(folder))
        {
            return false;
        }
        
        foreach (int index in folderGroups[folder])
        {
            if ((!string.IsNullOrEmpty(searchString) && 
                 !sceneNames[index].ToLower().Contains(searchString.ToLower())) ||
                (showFavoritesOnly && !favoriteScenes.Contains(scenePaths[index])))
            {
                continue;
            }
            
            return true;
        }
        
        return false;
    }

    private void ToggleFavorite(string scenePath)
    {
        if (favoriteScenes.Contains(scenePath))
        {
            favoriteScenes.Remove(scenePath);
        }
        else
        {
            favoriteScenes.Add(scenePath);
        }
        
        SaveFavorites();
    }

    private void SaveFavorites()
    {
        EditorPrefs.SetString("SceneNavigator_Favorites", string.Join("|", favoriteScenes));
    }

    private void LoadFavorites()
    {
        favoriteScenes.Clear();
        string savedFavorites = EditorPrefs.GetString("SceneNavigator_Favorites", "");
        
        if (!string.IsNullOrEmpty(savedFavorites))
        {
            string[] favorites = savedFavorites.Split('|');
            foreach (string favorite in favorites)
            {
                if (!string.IsNullOrEmpty(favorite))
                {
                    favoriteScenes.Add(favorite);
                }
            }
        }
    }

    private void CaptureSceneThumbnail(string scenePath)
    {
        string currentScenePath = SceneManager.GetActiveScene().path;
        
        // 現在開いているシーンと異なる場合、キャプチャできないのでスキップ
        if (currentScenePath != scenePath)
        {
            // 現在のシーンと違うシーンのサムネイルは取得できないことをデバッグログに表示
            Debug.Log($"サムネイルをキャプチャできるのは現在開いているシーンのみです: {scenePath}");
            return;
        }
        
        // メインカメラを探す
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("メインカメラが見つかりません。サムネイルを生成できません: " + scenePath);
            return;
        }
        
        // サムネイル保存用のディレクトリを作成
        if (!Directory.Exists(thumbnailsDirectory))
        {
            Directory.CreateDirectory(thumbnailsDirectory);
        }
        
        // シーンパスからハッシュコードを計算して一意のファイル名を生成
        string scenePathHash = scenePath.GetHashCode().ToString("X8");
        string fileName = $"thumbnail_{scenePathHash}.png";
        string thumbnailPath = Path.Combine(thumbnailsDirectory, fileName);
        
        // シーンパスとファイル名のマッピングを保存
        SaveThumbnailMapping(scenePath, fileName);
        
        // レンダーテクスチャを作成
        RenderTexture renderTexture = new RenderTexture(512, 288, 24); // 16:9のアスペクト比
        RenderTexture previousRenderTexture = mainCamera.targetTexture;
        
        mainCamera.targetTexture = renderTexture;
        mainCamera.Render();
        
        // レンダリング結果をテクスチャに保存
        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        texture.Apply();
        
        // 元の設定に戻す
        mainCamera.targetTexture = previousRenderTexture;
        RenderTexture.active = null;
        
        // ファイルに保存
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(thumbnailPath, bytes);
        
        // キャッシュに保存
        if (thumbnailCache.ContainsKey(scenePath))
        {
            DestroyImmediate(thumbnailCache[scenePath]);
        }
        thumbnailCache[scenePath] = texture;
        
        Debug.Log($"サムネイル保存完了: {thumbnailPath} for {scenePath}");
        
        // Unityエディタに変更を通知
        AssetDatabase.Refresh();
    }
    
    // シーンパスとサムネイルファイル名のマッピングを保存
    private void SaveThumbnailMapping(string scenePath, string fileName)
    {
        string mappingFilePath = Path.Combine(thumbnailsDirectory, "thumbnail_mapping.json");
        Dictionary<string, string> mappings = LoadThumbnailMappings();
        
        // マッピング情報を更新
        mappings[scenePath] = fileName;
        
        // JSON形式で保存
        string json = "{\n  \"mappings\": [\n";
        int count = 0;
        foreach (var pair in mappings)
        {
            if (count > 0)
                json += ",\n";
                
            json += "    {\n";
            json += $"      \"scenePath\": \"{EscapeJsonString(pair.Key)}\",\n";
            json += $"      \"fileName\": \"{EscapeJsonString(pair.Value)}\"\n";
            json += "    }";
            count++;
        }
        json += "\n  ]\n}";
        
        File.WriteAllText(mappingFilePath, json);
        
        // デバッグ
        Debug.Log($"マッピング保存完了: {mappingFilePath}, エントリ数: {mappings.Count}");
    }
    
    // JSON文字列のエスケープ処理
    private string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;
            
        return s.Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f");
    }
    
    // シーンパスとサムネイルファイル名のマッピングを読み込み
    private Dictionary<string, string> LoadThumbnailMappings()
    {
        string mappingFilePath = Path.Combine(thumbnailsDirectory, "thumbnail_mapping.json");
        Dictionary<string, string> mappings = new Dictionary<string, string>();
        
        if (File.Exists(mappingFilePath))
        {
            try
            {
                string jsonData = File.ReadAllText(mappingFilePath);
                
                // JSONデータからマッピング情報を手動でパース
                if (!string.IsNullOrEmpty(jsonData))
                {
                    ThumbnailMappingData data = JsonUtility.FromJson<ThumbnailMappingData>(jsonData);
                    if (data != null && data.mappings != null)
                    {
                        foreach (ThumbnailMapping mapping in data.mappings)
                        {
                            if (!string.IsNullOrEmpty(mapping.scenePath) && !string.IsNullOrEmpty(mapping.fileName))
                            {
                                // 既存のマッピングを上書き（重複防止）
                                mappings[mapping.scenePath] = mapping.fileName;
                                
                                // デバッグ: 各マッピングをログに出力
                                //Debug.Log($"マッピング読み込み: {mapping.scenePath} -> {mapping.fileName}");
                            }
                        }
                    }
                    
                    // デバッグ
                    Debug.Log($"マッピング読み込み完了: {mappingFilePath}, エントリ数: {mappings.Count}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"サムネイルマッピングの読み込み中にエラーが発生しました: {e.Message}");
            }
        }
        
        return mappings;
    }

    private void OpenScene(string scenePath)
    {
        // 変更があれば保存を促す
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            // シーンを開く
            EditorSceneManager.OpenScene(scenePath);
        }
    }

    // 定期的なチェックとサムネイルリロード
    private bool hasCheckedThumbnails = false;
    private void OnEditorUpdate()
    {
        // エディタの更新時に一度だけサムネイルをチェック
        if (!hasCheckedThumbnails)
        {
            hasCheckedThumbnails = true;
            if (thumbnailCache.Count == 0)
            {
                Debug.Log("サムネイルキャッシュが空のため再読み込みします");
                LoadThumbnails();
            }
        }
    }

    private void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        // シーンが開かれたときに自動的にサムネイルをキャプチャ
        string scenePath = scene.path;
        if (!string.IsNullOrEmpty(scenePath))
        {
            // 数フレーム待ってからサムネイルをキャプチャ（シーンのロードが完了するのを待つ）
            EditorApplication.delayCall += () => {
                CaptureSceneThumbnail(scenePath);
                
                // 他のシーンのサムネイルが消えている可能性があるので再度読み込む
                LoadThumbnailsWithoutClearing();
            };
        }
    }
    
    // サムネイルキャッシュをクリアせずに読み込み
    private void LoadThumbnailsWithoutClearing()
    {
        if (!Directory.Exists(thumbnailsDirectory))
        {
            Directory.CreateDirectory(thumbnailsDirectory);
            return;
        }
        
        // マッピング情報を読み込み
        Dictionary<string, string> mappings = LoadThumbnailMappings();
        int loadedCount = 0;
        
        // マッピング情報をもとにサムネイルをロード (既存キャッシュはそのまま)
        foreach (var entry in mappings)
        {
            string scenePath = entry.Key;
            
            // 既に読み込まれていて有効なら再読み込みしない
            if (thumbnailCache.ContainsKey(scenePath) && thumbnailCache[scenePath] != null)
            {
                continue;
            }
            
            string fileName = entry.Value;
            string thumbnailPath = Path.Combine(thumbnailsDirectory, fileName);
            
            if (File.Exists(thumbnailPath))
            {
                try
                {
                    // テクスチャをロード
                    byte[] fileData = File.ReadAllBytes(thumbnailPath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        thumbnailCache[scenePath] = texture;
                        loadedCount++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"サムネイルのロード中にエラーが発生しました: {thumbnailPath}, エラー: {e.Message}");
                }
            }
        }
        
        if (loadedCount > 0)
        {
            Debug.Log($"サムネイル再読み込み: 新たに{loadedCount}個のサムネイルをロードしました");
            Repaint(); // UI更新
        }
    }

    private void LoadThumbnails()
    {
        // 既存のテクスチャをクリーンアップ
        foreach (var texture in thumbnailCache.Values)
        {
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }
        thumbnailCache.Clear();
        
        if (!Directory.Exists(thumbnailsDirectory))
        {
            Directory.CreateDirectory(thumbnailsDirectory);
            return;
        }
        
        // マッピング情報を読み込み
        Dictionary<string, string> mappings = LoadThumbnailMappings();
        
        // デバッグ
        Debug.Log($"サムネイルロード開始: マッピング数 {mappings.Count}");
        
        // マッピング情報をもとにサムネイルをロード
        foreach (var entry in mappings)
        {
            string scenePath = entry.Key;
            string fileName = entry.Value;
            string thumbnailPath = Path.Combine(thumbnailsDirectory, fileName);
            
            if (File.Exists(thumbnailPath))
            {
                try
                {
                    // テクスチャをロード
                    byte[] fileData = File.ReadAllBytes(thumbnailPath);
                    Texture2D texture = new Texture2D(2, 2);
                    if (texture.LoadImage(fileData))
                    {
                        thumbnailCache[scenePath] = texture;
                        Debug.Log($"サムネイルをロードしました: {scenePath}");
                    }
                    else
                    {
                        Debug.LogWarning($"サムネイル画像の読み込みに失敗しました: {thumbnailPath}");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"サムネイルのロード中にエラーが発生しました: {thumbnailPath}, エラー: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"サムネイルファイルが見つかりません: {thumbnailPath} for {scenePath}");
            }
        }
        
        // 読み込みチェックフラグをリセット
        hasCheckedThumbnails = true;
        
        Debug.Log($"サムネイルロード完了: {thumbnailCache.Count}個のサムネイルをロードしました");
    }
    // Helper to create a solid-color Texture2D
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        var tex = new Texture2D(width, height);
        var pixels = Enumerable.Repeat(color, width * height).ToArray();
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // Populate sceneNames and scenePaths from all project scenes
    private void RefreshSceneList()
    {
        sceneNames.Clear();
        scenePaths.Clear();
        
        // 除外設定をEditorPrefsから取得
        string excludeFoldersSetting = EditorPrefs.GetString("SceneNavigator_ExcludedFolders", "");
        string excludeScenesSetting = EditorPrefs.GetString("SceneNavigator_ExcludedScenes", "");
        
        var excludedFolders = excludeFoldersSetting
                                .Split(',')
                                .Select(f => f.Trim())
                                .Where(f => !string.IsNullOrEmpty(f));
        var excludedScenes = excludeScenesSetting
                                .Split(',')
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s));
                                
        // 追加：包括設定をEditorPrefsから取得
        string includeFoldersSetting = EditorPrefs.GetString("SceneNavigator_IncludedFolders", "");
        string includeScenesSetting = EditorPrefs.GetString("SceneNavigator_IncludedScenes", "");
        
        var includedFolders = includeFoldersSetting
                                .Split(',')
                                .Select(f => f.Trim())
                                .Where(f => !string.IsNullOrEmpty(f));
        var includedScenes = includeScenesSetting
                                .Split(',')
                                .Select(s => s.Trim())
                                .Where(s => !string.IsNullOrEmpty(s));
        
        string[] guids = AssetDatabase.FindAssets("t:Scene");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            
            // 除外フォルダーに含まれるかチェック
            if (excludedFolders.Any(excl => path.Contains(excl)))
                continue;
            
            // 除外シーンとして指定されているかチェック
            string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);
            if (excludedScenes.Contains(sceneName))
                continue;
            
            // もし包括設定が指定されている場合、いずれかに一致しなければ除外
            if (includedFolders.Any() || includedScenes.Any())
            {
                bool matchesInclude = includedFolders.Any(inc => path.Contains(inc)) || includedScenes.Contains(sceneName);
                if (!matchesInclude)
                    continue;
            }
            
            scenePaths.Add(path);
            sceneNames.Add(sceneName);
        }
        if (groupByFolder)
            OrganizeByFolders();
    }

    // Coroutine to regenerate thumbnails for every scene
    private System.Collections.IEnumerator RegenerateThumbnailsCoroutine(bool needToReopen, string currentScenePath)
    {
        foreach (var scenePath in scenePaths)
        {
            if (needToReopen)
                EditorSceneManager.OpenScene(scenePath);
            CaptureSceneThumbnail(scenePath);
            yield return null;
        }
        if (needToReopen && !string.IsNullOrEmpty(currentScenePath))
        {
            EditorSceneManager.OpenScene(currentScenePath);
        }
    }
}

// エディタコルーチンのシンプルな実装
public static class EditorCoroutine
{
    public static EditorCoroutineInstance Start(System.Collections.IEnumerator routine)
    {
        EditorCoroutineInstance coroutine = new EditorCoroutineInstance(routine);
        coroutine.Start();
        return coroutine;
    }
    
    public class EditorCoroutineInstance
    {
        private System.Collections.IEnumerator routine;
        
        public EditorCoroutineInstance(System.Collections.IEnumerator routine)
        {
            this.routine = routine;
        }
        
        public void Start()
        {
            EditorApplication.update += Update;
        }
        
        public void Stop()
        {
            EditorApplication.update -= Update;
        }
        
        private void Update()
        {
            if (!routine.MoveNext())
            {
                Stop();
            }
        }
    }
}

// JsonUtilityでシリアライズするためのクラス
[System.Serializable]
public class ThumbnailMapping
{
    public string scenePath;
    public string fileName;
    
    public ThumbnailMapping(string scenePath, string fileName)
    {
        this.scenePath = scenePath;
        this.fileName = fileName;
    }
}

[System.Serializable]
public class ThumbnailMappingData
{
    public List<ThumbnailMapping> mappings = new List<ThumbnailMapping>();
    
    public ThumbnailMappingData(Dictionary<string, string> mappingDict)
    {
        foreach (var entry in mappingDict)
        {
            mappings.Add(new ThumbnailMapping(entry.Key, entry.Value));
        }
    }
    
    public ThumbnailMappingData() { }
}
