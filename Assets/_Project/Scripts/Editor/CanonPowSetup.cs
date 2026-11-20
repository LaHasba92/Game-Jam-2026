using System;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// À lancer dans Game, en mode édition. Les modifications de la scène restent sous Undo.
public static class CanonPowSetup
{
    private const string GameplayPath = "Assets/_Project/Prefabs/Gameplay.prefab";
    private const string DataFolder = "Assets/_Project/Data/Launchers";
    private static readonly Color Ink = new Color(0.08f, 0.13f, 0.20f, 0.94f);
    private static readonly Color Accent = new Color(0.16f, 0.41f, 0.57f, 1f);

    [MenuItem("CanonPOW/Installer la partie dans la scène ouverte")]
    private static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded ||
            PrefabStageUtility.GetCurrentPrefabStage() != null)
        {
            Debug.LogWarning("CanonPOW : ouvre la scène Game en mode édition.");
            return;
        }

        if (Find<RunHud>(scene) != null)
        {
            Debug.Log("CanonPOW : la scène contient déjà un HUD de partie.");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GameplayPath);
        if (prefab == null)
        {
            Debug.LogError($"CanonPOW : prefab introuvable ({GameplayPath}).");
            return;
        }

        LauncherController launcher = Find<LauncherController>(scene);
        ProjectileController projectile = Find<ProjectileController>(scene);
        WallController wall = Find<WallController>(scene);
        bool hasAnyGameplay = launcher != null || projectile != null || wall != null || Find<LevelManager>(scene) != null ||
                              Find<WorldStreamer>(scene) != null;
        if (hasAnyGameplay && (launcher == null || projectile == null || wall == null ||
                               Find<LevelManager>(scene) == null || Find<WorldStreamer>(scene) == null))
        {
            Debug.LogError("CanonPOW : installation partielle détectée. Vérifie les composants avant de relancer ce menu.");
            return;
        }

        const string action = "Installer la partie CanonPOW";
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(action);

        if (!hasAnyGameplay)
        {
            GameObject gameplay = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            Undo.RegisterCreatedObjectUndo(gameplay, action);
            launcher = gameplay.GetComponentInChildren<LauncherController>(true);
            projectile = gameplay.GetComponentInChildren<ProjectileController>(true);
            wall = gameplay.GetComponentInChildren<WallController>(true);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name != "--- Greybox ---") continue;
                Undo.RecordObject(root, action);
                root.SetActive(false);
            }
        }

        GameManager game = Find<GameManager>(scene);
        if (game == null) game = Undo.AddComponent<GameManager>(Create("GameManager"));
        var gameSettings = new SerializedObject(game);
        gameSettings.FindProperty("keyboardDebug").boolValue = false;
        gameSettings.ApplyModifiedProperties();

        Camera camera = Find<CameraDirector>(scene)?.GetComponent<Camera>() ?? Find<Camera>(scene);
        if (camera == null)
        {
            GameObject view = Create("Main Camera");
            view.tag = "MainCamera";
            view.transform.position = new Vector3(-8f, 5f, 0f);
            camera = Undo.AddComponent<Camera>(view);
        }
        if (Find<AudioListener>(scene) == null) Undo.AddComponent<AudioListener>(camera.gameObject);
        CameraDirector cameraDirector = camera.GetComponent<CameraDirector>() ?? Undo.AddComponent<CameraDirector>(camera.gameObject);
        Wire(cameraDirector, "projectile", projectile, "wall", wall);

        GameObject systems = Create("CanonPOW");
        RunUpgrades upgrades = Undo.AddComponent<RunUpgrades>(systems);
        SetArray(upgrades, "upgrades", LoadUpgrades());
        SetArray(upgrades, "launchers", CreateLaunchers());
        Wire(launcher, "upgrades", upgrades, null, null);
        Wire(projectile, "upgrades", upgrades, null, null);

        FeedbackManager feedback = Find<FeedbackManager>(scene) ?? Undo.AddComponent<FeedbackManager>(systems);
        Wire(feedback, "projectile", projectile, null, null);
        if (Find<SfxManager>(scene) == null) Undo.AddComponent<SfxManager>(Create("SfxManager"));

        EventSystem events = Find<EventSystem>(scene) ?? Undo.AddComponent<EventSystem>(Create("EventSystem"));
        StandaloneInputModule legacy = events.GetComponent<StandaloneInputModule>();
        if (legacy != null)
        {
            Undo.RecordObject(legacy, action);
            legacy.enabled = false;
        }
        if (events.GetComponent<InputSystemUIInputModule>() == null)
            Undo.AddComponent<InputSystemUIInputModule>(events.gameObject);

        BuildHud(game, launcher, projectile, upgrades);
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("CanonPOW : partie installée. Vérifie la scène et enregistre-la dans Unity.");
    }

    private static void BuildHud(GameManager game, LauncherController launcher, ProjectileController projectile, RunUpgrades upgrades)
    {
        GameObject root = Create("HUD CanonPOW", null, typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;
        RunHud hud = Undo.AddComponent<RunHud>(root);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        RectTransform parent = (RectTransform)root.transform;
        Text wallet = TextAt(parent, "Portefeuille", font, "", 44, 0.50f, 0.95f, 850, 90);
        RectTransform home = Panel(parent, "Accueil");
        TextAt(home, "Titre", font, "Canon POW", 96, 0.5f, 0.72f, 940, 170);
        ButtonAt(home, font, "TIRER", 0.5f, 0.36f, 620, 120, hud.Play);
        ButtonAt(home, font, "BOUTIQUE", 0.5f, 0.27f, 620, 120, hud.OpenShop);

        RectTransform aiming = Panel(parent, "Visée");
        Text aimStats = TextAt(aiming, "Consigne de visée", font, "", 42, 0.5f, 0.87f, 1010, 100);
        Image gauge = Bar(aiming, "Jauge de puissance", 0.09f, 0.50f, 44, 470, Image.OriginVertical.Bottom);

        RectTransform flying = Panel(parent, "Vol");
        Text flightStats = TextAt(flying, "Statistiques", font, "", 42, 0.5f, 0.85f, 980, 130);
        Text impact = TextAt(flying, "Impact", font, "", 100, 0.5f, 0.65f, 950, 150);
        Image record = Bar(flying, "Progression record", 0.5f, 0.80f, 730, 20, null);
        Button skip = ButtonAt(flying, font, "PASSER", 0.5f, 0.10f, 520, 104, hud.Skip);
        skip.gameObject.SetActive(false);
        ButtonAt(flying, font, "PAUSE", 0.85f, 0.95f, 185, 86, hud.Pause);

        RectTransform result = Panel(parent, "Résultat", true);
        TextAt(result, "Titre", font, "Résultat", 76, 0.5f, 0.76f, 940, 140);
        Text resultStats = TextAt(result, "Score", font, "", 54, 0.5f, 0.61f, 980, 270);
        Button doubleReward = ButtonAt(result, font, "×2 avec une pub", 0.5f, 0.44f, 700, 108, hud.DoubleReward);
        Button revive = ButtonAt(result, font, "Relancer avec une pub", 0.5f, 0.36f, 700, 108, hud.Revive);
        ButtonAt(result, font, "REJOUER", 0.5f, 0.27f, 700, 108, hud.Play);
        ButtonAt(result, font, "ACCUEIL", 0.5f, 0.18f, 700, 108, hud.GoHome);

        RectTransform shop = Panel(parent, "Boutique", true);
        TextAt(shop, "Titre", font, "Boutique", 62, 0.5f, 0.91f, 800, 100);
        Button[] upgradeButtons = new Button[5];
        Button[] launcherButtons = new Button[5];
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            upgradeButtons[i] = ButtonAt(shop, font, $"Amélioration {i + 1}", 0.5f, 0.80f - i * 0.066f,
                890, 95, null);
            UnityEventTools.AddIntPersistentListener(upgradeButtons[i].onClick, hud.BuyUpgrade, index);
            launcherButtons[i] = ButtonAt(shop, font, $"Lanceur {i + 1}", 0.5f, 0.43f - i * 0.066f,
                890, 95, null);
            UnityEventTools.AddIntPersistentListener(launcherButtons[i].onClick, hud.SelectLauncher, index);
        }
        Button freeUpgrade = ButtonAt(shop, font, "Amélioration gratuite", 0.5f, 0.865f, 890, 95, hud.FreeUpgrade);
        ButtonAt(shop, font, "RETOUR", 0.5f, 0.075f, 680, 100, hud.CloseShop);

        RectTransform pause = Panel(parent, "Pause", true);
        TextAt(pause, "Titre", font, "Pause", 85, 0.5f, 0.62f, 900, 160);
        ButtonAt(pause, font, "REPRENDRE", 0.5f, 0.47f, 700, 120, hud.Resume);

        var settings = new SerializedObject(hud);
        settings.FindProperty("game").objectReferenceValue = game;
        settings.FindProperty("launcher").objectReferenceValue = launcher;
        settings.FindProperty("projectile").objectReferenceValue = projectile;
        settings.FindProperty("upgrades").objectReferenceValue = upgrades;
        foreach (RectTransform panel in new[] { home, aiming, flying, result, shop, pause })
            panel.gameObject.SetActive(panel == home);
        settings.FindProperty("home").objectReferenceValue = home.gameObject;
        settings.FindProperty("aiming").objectReferenceValue = aiming.gameObject;
        settings.FindProperty("flying").objectReferenceValue = flying.gameObject;
        settings.FindProperty("result").objectReferenceValue = result.gameObject;
        settings.FindProperty("shop").objectReferenceValue = shop.gameObject;
        settings.FindProperty("pause").objectReferenceValue = pause.gameObject;
        settings.FindProperty("wallet").objectReferenceValue = wallet;
        settings.FindProperty("flightStats").objectReferenceValue = flightStats;
        settings.FindProperty("aimStats").objectReferenceValue = aimStats;
        settings.FindProperty("resultStats").objectReferenceValue = resultStats;
        settings.FindProperty("impact").objectReferenceValue = impact;
        settings.FindProperty("gaugeFill").objectReferenceValue = gauge;
        settings.FindProperty("recordFill").objectReferenceValue = record;
        settings.FindProperty("skip").objectReferenceValue = skip;
        settings.FindProperty("doubleReward").objectReferenceValue = doubleReward;
        settings.FindProperty("revive").objectReferenceValue = revive;
        settings.FindProperty("freeUpgrade").objectReferenceValue = freeUpgrade;
        settings.ApplyModifiedProperties();
        SetArray(hud, "upgradeButtons", upgradeButtons);
        SetArray(hud, "launcherButtons", launcherButtons);
        wallet.transform.SetAsLastSibling();
        Selection.activeGameObject = root;
    }

    private static RectTransform Panel(RectTransform parent, string name, bool overlay = false)
    {
        GameObject obj = Create(name, parent, typeof(RectTransform));
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        if (overlay) obj.AddComponent<Image>().color = Ink;
        return rect;
    }

    private static RectTransform Place(GameObject obj, float x, float y, float width, float height)
    {
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(x, y);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        return rect;
    }

    private static Text TextAt(RectTransform parent, string name, Font font, string value, int size,
        float x, float y, float width, float height)
    {
        GameObject obj = Create(name, parent, typeof(RectTransform), typeof(Text));
        Place(obj, x, y, width, height);
        Text label = obj.GetComponent<Text>();
        label.text = value;
        label.font = font;
        label.fontSize = size;
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 24;
        label.resizeTextMaxSize = size;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = Ink;
        outline.effectDistance = new Vector2(2f, -2f);
        return label;
    }

    private static Button ButtonAt(RectTransform parent, Font font, string name,
        float x, float y, float width, float height, UnityAction action)
    {
        GameObject obj = Create(name, parent, typeof(RectTransform), typeof(Image), typeof(Button), typeof(SfxButton));
        RectTransform rect = Place(obj, x, y, width, height);
        Image background = obj.GetComponent<Image>();
        background.color = Accent;
        Button button = obj.GetComponent<Button>();
        button.targetGraphic = background;
        TextAt(rect, "Libellé", font, name, 44, 0.5f, 0.5f, width - 30f, height);
        if (action != null) UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    private static Image Bar(RectTransform parent, string name, float x, float y, float width, float height,
        Image.OriginVertical? vertical)
    {
        GameObject background = Create(name, parent, typeof(RectTransform), typeof(Image));
        RectTransform rect = Place(background, x, y, width, height);
        background.GetComponent<Image>().color = Ink;
        background.GetComponent<Image>().raycastTarget = false;
        Image fill = Create("Remplissage", rect, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        // Sans sprite, Image rend un quad plein et ignore son fillAmount.
        fill.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        fill.rectTransform.anchorMin = Vector2.zero;
        fill.rectTransform.anchorMax = Vector2.one;
        fill.rectTransform.offsetMin = new Vector2(3, 3);
        fill.rectTransform.offsetMax = new Vector2(-3, -3);
        fill.type = Image.Type.Filled;
        fill.fillMethod = vertical.HasValue ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
        fill.fillOrigin = vertical.HasValue ? (int)vertical.Value : (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        fill.color = new Color(1f, 0.76f, 0.18f);
        fill.raycastTarget = false;
        return fill;
    }

    [MenuItem("CanonPOW/Créer les fiches de lanceurs")]
    private static void CreateLauncherAssets() => CreateLaunchers();

    private static LauncherDefinition[] CreateLaunchers()
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "Launchers");

        return new[]
        {
            GetLauncher("Fronde", "Fronde en bois", 0, 0, 1f),
            GetLauncher("LancePierre", "Lance-pierre géant", 400, 2, 1.3f),
            GetLauncher("Baliste", "Baliste", 1500, 4, 1.7f),
            GetLauncher("Catapulte", "Catapulte", 5000, 6, 2.2f),
            GetLauncher("Canon", "Canon", 15000, 8, 3f)
        };
    }

    private static LauncherDefinition GetLauncher(string file, string label, int price, int wall, float power)
    {
        string path = $"{DataFolder}/{file}.asset";
        LauncherDefinition data = AssetDatabase.LoadAssetAtPath<LauncherDefinition>(path);
        if (data != null) return data;
        data = ScriptableObject.CreateInstance<LauncherDefinition>();
        data.displayName = label;
        data.price = price;
        data.requiredWall = wall;
        data.powerMultiplier = power;
        AssetDatabase.CreateAsset(data, path);
        return data;
    }

    [MenuItem("CanonPOW/Appliquer les distances du GDD à la scène")]
    private static void UseGddTrack()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || !scene.IsValid() || !scene.isLoaded ||
            PrefabStageUtility.GetCurrentPrefabStage() != null) return;

        LevelManager levels = Find<LevelManager>(scene);
        ProjectileController projectile = Find<ProjectileController>(scene);
        WallController wall = Find<WallController>(scene);
        if (levels == null || projectile == null || wall == null)
        {
            Debug.LogWarning("CanonPOW : installe la partie avant d'appliquer les distances du GDD.");
            return;
        }

        const string folder = "Assets/_Project/Data/NiveauxGDD";
        if (!AssetDatabase.IsValidFolder(folder))
            AssetDatabase.CreateFolder("Assets/_Project/Data", "NiveauxGDD");

        float[] distances = { 50, 70, 100, 140, 190, 270, 380, 530, 740, 1000 };
        LevelDefinition[] definitions = new LevelDefinition[distances.Length];
        for (int i = 0; i < definitions.Length; i++)
        {
            string path = $"{folder}/Level_{i + 1:00}.asset";
            LevelDefinition data = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
            if (data == null)
            {
                LevelDefinition original = AssetDatabase.LoadAssetAtPath<LevelDefinition>($"Assets/Data/Levels/Level_{i + 1:00}.asset");
                data = original != null ? UnityEngine.Object.Instantiate(original) : ScriptableObject.CreateInstance<LevelDefinition>();
                data.levelNumber = i + 1;
                data.wallDistance = distances[i];
                AssetDatabase.CreateAsset(data, path);
            }
            definitions[i] = data;
        }

        SetArray(levels, "walls", definitions);
        SetFloat(levels, "distanceGrowth", 1.4f);
        SetFloat(wall, "corridorHeight", 8f);
        SetFloat(projectile, "physics.rollBrake", 4f);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static void SetFloat(Component component, string field, float value)
    {
        using var settings = new SerializedObject(component);
        settings.FindProperty(field).floatValue = value;
        settings.ApplyModifiedProperties();
    }

    private static UpgradeDefinition[] LoadUpgrades() => new[]
    {
        AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/Power.asset"),
        AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/Aerodynamics.asset"),
        AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/Momentum.asset"),
        AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/Money.asset"),
        AssetDatabase.LoadAssetAtPath<UpgradeDefinition>("Assets/Bounce.asset")
    };

    private static GameObject Create(string name, Transform parent = null, params Type[] components)
    {
        GameObject obj = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(obj, "Créer " + name);
        if (parent != null)
        {
            Undo.SetTransformParent(obj.transform, parent, "Rattacher " + name);
            obj.transform.localScale = Vector3.one;
        }
        return obj;
    }

    private static T Find<T>(Scene scene) where T : Component
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T found = root.GetComponentInChildren<T>();
            if (found != null) return found;
        }
        return null;
    }

    private static void Wire(Component component, string field, UnityEngine.Object value,
        string secondField, UnityEngine.Object secondValue)
    {
        var settings = new SerializedObject(component);
        settings.FindProperty(field).objectReferenceValue = value;
        if (secondField != null) settings.FindProperty(secondField).objectReferenceValue = secondValue;
        settings.ApplyModifiedProperties();
    }

    private static void SetArray(Component component, string field, UnityEngine.Object[] values)
    {
        var settings = new SerializedObject(component);
        SerializedProperty property = settings.FindProperty(field);
        property.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        settings.ApplyModifiedProperties();
    }

    [MenuItem("CanonPOW/Vérifier le projet")]
    private static void ValidateScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded) return;
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                foreach (Component component in child.GetComponents<Component>())
                {
                    if (component == null)
                    {
                        Debug.LogWarning($"Script manquant sur {child.name}.", child);
                        count++;
                        continue;
                    }
                    using var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType != SerializedPropertyType.ObjectReference ||
                            property.objectReferenceValue != null) continue;
                        bool missing = property.objectReferenceInstanceIDValue != 0;
                        if (!missing && !(component is MonoBehaviour)) continue;
                        Debug.LogWarning($"{child.name} / {component.GetType().Name} / {property.propertyPath} " +
                            (missing ? "référence cassée" : "champ vide (peut être facultatif)"), child);
                        count++;
                    }
                }
            }
        }
        Debug.Log($"CanonPOW : {count} signalement(s) dans {scene.name}.");
    }
}
