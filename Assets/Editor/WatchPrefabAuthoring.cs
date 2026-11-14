using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpaceRunner.Editor
{
    // One-time authoring utility. Existing assets are never regenerated or overwritten.
    // Runtime code only loads the saved prefabs and their artist-editable Image/Text components.
    public static class WatchPrefabAuthoring
    {
        private const string Root = "Assets/Prefabs";
        private const string Art = "Assets/Graphics/Sprites";
        private static Font font;
        private static readonly Color Background = new(0.17f, 0.17f, 0.17f);
        private static readonly Color Ink = new(0.10f, 0.10f, 0.10f);
        private static readonly Color Paper = new(0.75f, 0.75f, 0.75f);

        [MenuItem("Space Runner/Create Missing Watch Prefabs")]
        public static void CreateMissingAssets()
        {
            Directory.CreateDirectory(Root + "/Watch");
            Directory.CreateDirectory(Root + "/UI");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            CreateRingSprite();
            CreateWatchMenu();
            CreateScreenUI();
            CreateArm();
            CreateRig();
            CreateGravityMenu();
            CreateSystem();
            AssetDatabase.SaveAssets();
            Debug.Log("Watch prefabs ready. Edit these assets directly; runtime never rebuilds their UI.");
        }

        private static string AssetPath(string name)
        {
            string folder = name == "WatchMenu" || name == "ScreenUI" || name == "GravityMenu" ? "UI" : "Watch";
            return Root + "/" + folder + "/" + name + ".prefab";
        }
        private static bool Exists(string name) => File.Exists(AssetPath(name));
        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, AssetPath(name));
            Object.DestroyImmediate(root);
        }
        private static void Bind(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            var field = serialized.FindProperty(property);
            if (field == null) throw new System.InvalidOperationException(target.GetType().Name + "." + property);
            field.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * 0.5f;
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static Image Panel(string name, Transform parent, Vector2 position, Vector2 size, Color tint)
        {
            var image = Rect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = tint; image.raycastTarget = false;
            return image;
        }
        private static Text Label(string name, Transform parent, string value, Vector2 position, Vector2 size, int fontSize = 24, Color? color = null)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = fontSize;
            text.color = color ?? Color.white; text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            return text;
        }
        private static Button Button(string name, Transform parent, string label, Vector2 position, Vector2 size)
        {
            var image = Panel(name, parent, position, size, Paper); image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f); colors.pressedColor = new Color(0.65f, 0.65f, 0.65f); button.colors = colors;
            Label("Label", image.transform, label, Vector2.zero, size - new Vector2(24, 8), 26, Ink);
            return button;
        }
        private static Slider Slider(string name, Transform parent, Vector2 position, float min, float max)
        {
            var rect = Rect(name, parent, position, new Vector2(330, 44));
            var slider = rect.gameObject.AddComponent<Slider>();
            var track = Panel("Track", rect, Vector2.zero, new Vector2(330, 5), new Color(0.35f, 0.35f, 0.35f)); track.raycastTarget = true;
            var fillArea = Rect("Fill Area", rect, Vector2.zero, new Vector2(310, 5));
            var fill = Panel("Fill", fillArea, Vector2.zero, Vector2.zero, Paper);
            var handleArea = Rect("Handle Area", rect, Vector2.zero, new Vector2(310, 26));
            var handle = Panel("Handle", handleArea, Vector2.zero, new Vector2(16, 0), Color.white); handle.raycastTarget = true;
            slider.fillRect = fill.rectTransform; slider.handleRect = handle.rectTransform; slider.targetGraphic = handle;
            slider.minValue = min; slider.maxValue = max; slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }
        private static Canvas Canvas(string name, Vector2 size, bool overlay = false)
        {
            var rect = Rect(name, null, Vector2.zero, size);
            var canvas = rect.gameObject.AddComponent<Canvas>();
            canvas.renderMode = overlay ? RenderMode.ScreenSpaceOverlay : RenderMode.WorldSpace;
            rect.gameObject.AddComponent<GraphicRaycaster>();
            if (overlay)
            {
                var scaler = rect.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = size; scaler.matchWidthOrHeight = 0.5f;
            }
            return canvas;
        }
        private static void CreateRingSprite()
        {
            string path = Art + "/DialOutline.png";
            if (File.Exists(path)) return;
            var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
            var pixels = new Color32[512 * 512];
            for (int y = 0; y < 512; y++) for (int x = 0; x < 512; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(255.5f, 255.5f));
                float alpha = Mathf.Clamp01(3 - Mathf.Abs(distance - 250));
                pixels[y * 512 + x] = new Color32(255, 255, 255, (byte)(alpha * 255));
            }
            texture.SetPixels32(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite; importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        private static void CreateWatchMenu()
        {
            if (Exists("WatchMenu")) return;
            var canvas = Canvas("Watch Menu", new Vector2(1280, 800));
            var root = canvas.transform;
            canvas.gameObject.AddComponent<CanvasGroup>();
            var view = canvas.gameObject.AddComponent<WatchMenuView>();
            Panel("Background", root, Vector2.zero, new Vector2(1280, 800), Background);
            var ring = Panel("Dial Outline", root, Vector2.zero, new Vector2(620, 620), Paper);
            ring.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/DialOutline.png");
            var title = Label("Page Title", root, "RUN TIME", new Vector2(0, 170), new Vector2(450, 50), 32);
            var status = Label("Status", root, "PAUSED", new Vector2(0, -160), new Vector2(360, 40), 25);
            var home = Rect("Home", root, Vector2.zero, new Vector2(500, 270));
            var time = Label("Run Time", home, "00:00.00", new Vector2(0, 20), new Vector2(490, 110), 80);
            var resume = Button("Resume", root, "RESUME", new Vector2(-455, 130), new Vector2(270, 112));
            var settingsButton = Button("Open settings", root, "SETTINGS", new Vector2(-455, -130), new Vector2(270, 112));
            var end = Button("End Run", root, "END RUN\n<size=18>RETURN TO MAIN MENU</size>", new Vector2(455, 130), new Vector2(270, 112));
            var quit = Button("Quit Game", root, "QUIT GAME", new Vector2(455, -130), new Vector2(270, 112));
            var back = Button("Back", root, "BACK", new Vector2(-455, -300), new Vector2(270, 68));
            Label("Close Hint", root, "R  CLOSE WATCH     /     ESC  BACK", new Vector2(0, -360), new Vector2(700, 32), 18);
            var pages = Rect("Pages", root, Vector2.zero, new Vector2(400, 270));
            var settingsRoot = Rect("Settings", pages, Vector2.zero, new Vector2(400, 270));
            var settings = settingsRoot.gameObject.AddComponent<SettingsPage>();
            Label("Volume Label", settingsRoot, "MASTER VOLUME", new Vector2(-30, 90), new Vector2(250, 30), 20);
            var volumeText = Label("Volume Value", settingsRoot, "80%", new Vector2(145, 90), new Vector2(70, 30), 20);
            var volume = Slider("Master Volume", settingsRoot, new Vector2(0, 45), 0, 1);
            Label("Sensitivity Label", settingsRoot, "LOOK SENSITIVITY", new Vector2(-30, -30), new Vector2(250, 30), 20);
            var sensitivityText = Label("Sensitivity Value", settingsRoot, "1.0x", new Vector2(145, -30), new Vector2(70, 30), 20);
            var sensitivity = Slider("Look Sensitivity", settingsRoot, new Vector2(0, -75), 0.03f, 0.4f);
            Bind(settings, "volume", volume); Bind(settings, "sensitivity", sensitivity);
            Bind(settings, "volumeValue", volumeText); Bind(settings, "sensitivityValue", sensitivityText);
            var confirmation = Rect("Confirmation", root, Vector2.zero, new Vector2(1280, 800));
            var blocker = Panel("Input Blocker", confirmation, Vector2.zero, new Vector2(1280, 800), new Color(0, 0, 0, 0.7f)); blocker.raycastTarget = true;
            Panel("Dialog Background", confirmation, Vector2.zero, new Vector2(590, 350), Background);
            var confirmTitle = Label("Confirm Title", confirmation, "END THIS RUN?", new Vector2(0, 100), new Vector2(550, 50), 30);
            var confirmBody = Label("Confirm Body", confirmation, "Return to the main menu.", new Vector2(0, 20), new Vector2(530, 70), 22);
            var cancel = Button("Cancel", confirmation, "CANCEL", new Vector2(-135, -95), new Vector2(230, 75));
            var confirm = Button("Confirm", confirmation, "CONFIRM", new Vector2(135, -95), new Vector2(230, 75));
            Bind(view, "title", title); Bind(view, "timer", time); Bind(view, "status", status); Bind(view, "home", home.gameObject);
            Bind(view, "pageHost", pages); Bind(view, "settingsPage", settings);
            Bind(view, "resumeButton", resume); Bind(view, "endRunButton", end); Bind(view, "quitButton", quit); Bind(view, "backButton", back);
            Bind(view, "confirmation", confirmation.gameObject); Bind(view, "confirmTitle", confirmTitle); Bind(view, "confirmBody", confirmBody);
            Bind(view, "cancelButton", cancel); Bind(view, "confirmButton", confirm);
            var serialized = new SerializedObject(view); var entries = serialized.FindProperty("pages"); entries.arraySize = 1;
            var entry = entries.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("id").stringValue = "settings"; entry.FindPropertyRelative("title").stringValue = "SETTINGS";
            entry.FindPropertyRelative("content").objectReferenceValue = settingsRoot.gameObject;
            entry.FindPropertyRelative("openButton").objectReferenceValue = settingsButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            settingsRoot.gameObject.SetActive(false); confirmation.gameObject.SetActive(false); back.gameObject.SetActive(false);
            Save(canvas.gameObject, "WatchMenu");
        }
        private static void CreateScreenUI()
        {
            if (Exists("ScreenUI")) return;
            var canvas = Canvas("Main Menu and HUD", new Vector2(1600, 900), true); canvas.sortingOrder = 20;
            var view = canvas.gameObject.AddComponent<MainMenuView>();
            var menu = Rect("Main Menu", canvas.transform, Vector2.zero, new Vector2(1600, 900));
            var shade = Panel("Background", menu, Vector2.zero, new Vector2(6000, 6000), new Color(0.08f, 0.08f, 0.08f, 0.97f)); shade.raycastTarget = true;
            var menuTitle = Label("Title", menu, "SPACE RUNNER", new Vector2(0, 220), new Vector2(800, 80), 54);
            var start = Button("Start Run", menu, "START RUN", new Vector2(0, 60), new Vector2(480, 80));
            var quit = Button("Quit Game", menu, "QUIT GAME", new Vector2(0, -50), new Vector2(480, 80));
            var last = Label("Last Run", menu, "", new Vector2(0, -145), new Vector2(700, 40), 22);
            Label("Controls", menu, "WASD  MOVE  /  SPACE  JUMP\nR  WATCH  /  HOLD TAB  GRAVITY\nKEEP MOVING FORWARD TO SPRINT", new Vector2(0, -260), new Vector2(850, 120), 22);
            var hud = Rect("HUD", canvas.transform, Vector2.zero, new Vector2(1600, 900));
            var hudGroup = hud.gameObject.AddComponent<CanvasGroup>(); hudGroup.blocksRaycasts = false;
            Label("Crosshair", hud, "+", Vector2.zero, new Vector2(25, 25), 20);
            Label("Watch Hint", hud, "R  WATCH     /     HOLD TAB  GRAVITY", new Vector2(0, -380), new Vector2(800, 40), 22);
            var modal = Rect("Quit Confirmation", canvas.transform, Vector2.zero, new Vector2(1600, 900));
            var hit = Panel("Shade", modal, Vector2.zero, new Vector2(6000, 6000), new Color(0, 0, 0, 0.8f)); hit.raycastTarget = true;
            Panel("Dialog", modal, Vector2.zero, new Vector2(780, 340), Background);
            Label("Title", modal, "QUIT GAME?", new Vector2(0, 80), new Vector2(650, 70), 40);
            var cancel = Button("Cancel", modal, "CANCEL", new Vector2(-170, -70), new Vector2(300, 80));
            var confirm = Button("Confirm Quit", modal, "QUIT GAME", new Vector2(170, -70), new Vector2(300, 80));
            Bind(view, "menuRoot", menu.gameObject); Bind(view, "hudRoot", hud.gameObject); Bind(view, "confirmation", modal.gameObject);
            Bind(view, "startButton", start); Bind(view, "quitButton", quit); Bind(view, "cancelButton", cancel); Bind(view, "confirmButton", confirm);
            Bind(view, "lastRun", last); Bind(view, "hud", hudGroup);
            Bind(view, "title", menuTitle);
            modal.gameObject.SetActive(false); hud.gameObject.SetActive(false);
            Save(canvas.gameObject, "ScreenUI");
        }
        private static Transform Cube(string name, Transform parent, Vector3 position, Vector3 size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.SetParent(parent, false);
            cube.transform.localPosition = position; cube.transform.localScale = size;
            Object.DestroyImmediate(cube.GetComponent<Collider>());
            return cube.transform;
        }
        private static void CreateArm()
        {
            if (Exists("PlaceholderArm")) return;
            var arm = new GameObject("Arm Presentation");
            var presentation = arm.AddComponent<PlaceholderWatchPresentation>(); Bind(presentation, "armRoot", arm.transform);
            Cube("Forearm", arm.transform, new Vector3(-0.13f, -0.015f, 0), new Vector3(0.46f, 0.12f, 0.13f));
            Cube("Glove", arm.transform, new Vector3(0.17f, -0.012f, 0), new Vector3(0.15f, 0.09f, 0.12f));
            var socket = new GameObject("WatchSocket").transform; socket.SetParent(arm.transform, false);
            Cube("Watch Strap", socket, Vector3.zero, new Vector3(0.115f, 0.135f, 0.145f));
            Cube("Watch Body", socket, new Vector3(0, 0.07f, 0), new Vector3(0.27f, 0.035f, 0.18f));
            var face = new GameObject("FaceSocket").transform; face.SetParent(socket, false);
            face.localPosition = new Vector3(0, 0.094f, 0); face.localRotation = Quaternion.Euler(90, 0, 0);
            Save(arm, "PlaceholderArm");
        }
        private static void CreateRig()
        {
            if (Exists("WatchRig")) return;
            var root = new GameObject("Watch Rig"); var rig = root.AddComponent<WatchRig>();
            var arm = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath("PlaceholderArm")), root.transform);
            var faceSocket = arm.transform.Find("WatchSocket/FaceSocket");
            var menu = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath("WatchMenu")), faceSocket);
            menu.transform.localScale = Vector3.one * 0.0002f;
            var camera = new GameObject("Watch Camera", typeof(Camera)).GetComponent<Camera>(); camera.transform.SetParent(root.transform, false);
            camera.nearClipPlane = 0.005f; camera.farClipPlane = 20; camera.fieldOfView = 60; camera.cullingMask = 1 << 30;
            camera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Overlay;
            menu.GetComponent<Canvas>().worldCamera = camera;
            var focus = root.AddComponent<WatchFaceZoom>();
            Bind(focus, "watchCamera", camera); Bind(focus, "face", menu.GetComponent<RectTransform>());
            Bind(focus, "backdrop", menu.transform.Find("Background")); Bind(focus, "group", menu.GetComponent<CanvasGroup>());
            var serialized = new SerializedObject(focus); var visuals = serialized.FindProperty("armVisuals");
            var renderers = arm.GetComponentsInChildren<Renderer>(); visuals.arraySize = renderers.Length;
            for (int i = 0; i < renderers.Length; i++) visuals.GetArrayElementAtIndex(i).objectReferenceValue = renderers[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Bind(rig, "presentation", arm.GetComponent<WatchPresentation>()); Bind(rig, "menu", menu.GetComponent<WatchMenuView>()); Bind(rig, "focus", focus);
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
            Save(root, "WatchRig");
        }
        private static void CreateGravityMenu()
        {
            if (Exists("GravityMenu")) return;
            var canvas = Canvas("Gravity Menu", new Vector2(1600, 900), true); canvas.sortingOrder = 5;
            canvas.gameObject.AddComponent<CanvasGroup>();
            var menu = canvas.gameObject.AddComponent<GravityChangerUI>();
            var manager = canvas.gameObject.AddComponent<GravityManager>();
            var binding = canvas.gameObject.AddComponent<GravityMenuBinding>();
            var border = Panel("Selection Border", canvas.transform, Vector2.zero, new Vector2(420, 420), Paper);
            border.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/DialOutline.png");
            var serialized = new SerializedObject(menu); var entries = serialized.FindProperty("gravityImages"); entries.arraySize = 4;
            string[] labels = { "DOWN", "LEFT", "UP", "RIGHT" };
            for (int i = 0; i < 4; i++)
            {
                var image = Panel("Direction " + i, canvas.transform, Vector2.zero, new Vector2(400, 400), new Color(0.65f, 0.65f, 0.65f, 0.7f));
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + "/DialOutline.png");
                image.type = Image.Type.Filled; image.fillMethod = Image.FillMethod.Radial360;
                image.fillOrigin = 0; image.fillClockwise = true; image.fillAmount = 0.25f;
                image.transform.localRotation = Quaternion.Euler(0, 0, 45f - 90 * i);
                float angle = (270 - 90 * i) * Mathf.Deg2Rad;
                Label("Direction Label " + i, canvas.transform, labels[i], new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 145,
                    new Vector2(130, 25), 18);
                entries.GetArrayElementAtIndex(i).objectReferenceValue = image;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var cursor = Panel("Selection Cursor", canvas.transform, Vector2.zero, new Vector2(12, 12), Color.white);
            Label("Hint", canvas.transform, "HOLD TAB / CHOOSE DIRECTION / RELEASE", new Vector2(0, -270), new Vector2(900, 40), 22);
            Bind(menu, "cursor", cursor); Bind(menu, "cursorMoveBorder", border);
            Bind(manager, "gravityChangerUI", menu); Bind(binding, "menu", menu); Bind(binding, "manager", manager);
            Save(canvas.gameObject, "GravityMenu");
        }
        private static void CreateSystem()
        {
            if (Exists("WatchSystem")) return;
            var root = new GameObject("Watch System"); var system = root.AddComponent<WatchSystem>();
            Bind(system, "watchRigPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath("WatchRig")).GetComponent<WatchRig>());
            Bind(system, "screenUIPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath("ScreenUI")).GetComponent<MainMenuView>());
            Bind(system, "gravityMenuPrefab", AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath("GravityMenu")).GetComponent<GravityMenuBinding>());
            Save(root, "WatchSystem");
        }
    }
}
