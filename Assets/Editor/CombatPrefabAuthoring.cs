using System.IO;
using SpaceRunner.Combat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SpaceRunner.Editor
{
    // Create once, then edit the saved prefabs in Unity. Existing artwork is preserved.
    public static class CombatPrefabAuthoring
    {
        private const string Root = "Assets/Prefabs/Combat";
        private const string Art = "Assets/Graphics/Materials";

        [MenuItem("Space Runner/Create Missing Combat Prefabs")]
        public static void CreateMissingAssets()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/Prefabs/UI");
            Directory.CreateDirectory(Art);
            AssetDatabase.Refresh();
            CreateProjectile(); CreateTower(); CreateMelee(); CreateHud(); CreateSystem();
            BindMenuTitle();
            AssetDatabase.SaveAssets();
            Debug.Log("Combat prefabs ready: health, tower, straight projectiles and ground melee navigation.");
        }
        private static string AssetPath(string name) => (name == "HealthHUD" ? "Assets/Prefabs/UI" : Root) + "/" + name + ".prefab";
        private static bool Exists(string name) => File.Exists(AssetPath(name));
        private static T Load<T>(string name) where T : Component => AssetDatabase.LoadAssetAtPath<GameObject>(AssetPath(name)).GetComponent<T>();
        private static void Save(GameObject root, string name)
        {
            PrefabUtility.SaveAsPrefabAsset(root, AssetPath(name));
            Object.DestroyImmediate(root);
        }
        private static void Bind(Object target, string property, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(property).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static Material Material(string name, Color color)
        {
            string path = Art + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static Transform Child(string name, Transform parent, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position;
            return child;
        }
        private static void Shape(string name, Transform parent, PrimitiveType shape, Vector3 position, Vector3 scale, Material material)
        {
            var obj = GameObject.CreatePrimitive(shape);
            obj.name = name; obj.layer = parent.gameObject.layer;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            Object.DestroyImmediate(obj.GetComponent<Collider>());
            obj.GetComponent<Renderer>().sharedMaterial = material;
        }
        private static void CreateProjectile()
        {
            if (Exists("TowerProjectile")) return;
            var root = new GameObject("Tower Projectile") { layer = 2 };
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var collider = root.AddComponent<SphereCollider>(); collider.radius = 0.12f; collider.isTrigger = true;
            root.AddComponent<TowerProjectile>();
            Shape("Visual - Replace Mesh", root.transform, PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.24f,
                Material("Projectile", new Color(1, 0.65f, 0.16f)));
            Save(root, "TowerProjectile");
        }
        private static void CreateTower()
        {
            if (Exists("TriggerTower")) return;
            var root = new GameObject("Trigger Tower");
            var tower = root.AddComponent<TriggerTower>();
            var collider = root.AddComponent<BoxCollider>(); collider.center = new Vector3(0, 0.85f, 0); collider.size = new Vector3(1, 1.7f, 1);
            var obstacle = root.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box;
            obstacle.center = collider.center; obstacle.size = collider.size; obstacle.carving = true;
            var visual = Child("Visuals - Replace Art", root.transform, Vector3.zero);
            var material = Material("Tower", new Color(0.65f, 0.24f, 0.18f));
            Shape("Base", visual, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(1, 1.2f, 1), material);
            var pivot = Child("Aim Pivot", root.transform, new Vector3(0, 1.5f, 0));
            Shape("Head - Replace Art", pivot, PrimitiveType.Cube, Vector3.zero, new Vector3(0.8f, 0.6f, 0.8f), material);
            Shape("Barrel - Replace Art", pivot, PrimitiveType.Cube, new Vector3(0, 0, 0.5f), new Vector3(0.2f, 0.2f, 0.6f), material);
            var muzzle = Child("Muzzle", pivot, new Vector3(0, 0, 0.85f));
            Bind(tower, "aimPivot", pivot); Bind(tower, "muzzle", muzzle); Bind(tower, "projectilePrefab", Load<TowerProjectile>("TowerProjectile"));
            Save(root, "TriggerTower");
        }
        private static void CreateMelee()
        {
            if (Exists("MeleeEnemy")) return;
            var root = new GameObject("Melee Enemy");
            var body = root.AddComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            var collider = root.AddComponent<CapsuleCollider>(); collider.center = Vector3.up; collider.radius = 0.45f; collider.height = 2;
            var agent = root.AddComponent<NavMeshAgent>(); agent.radius = 0.45f; agent.height = 2; agent.speed = 1.8f;
            agent.enabled = false; // Initialized after the level's navigation data is ready.
            agent.acceleration = 6; agent.angularSpeed = 180; agent.stoppingDistance = 0.05f;
            var melee = root.AddComponent<MeleeEnemy>();
            var attack = Child("Contact Kill Volume", root.transform, Vector3.up);
            var contact = attack.gameObject.AddComponent<CapsuleCollider>(); contact.radius = 0.6f; contact.height = 2; contact.isTrigger = true;
            Bind(melee, "attackOrigin", attack);
            Shape("Visual - Replace Art", root.transform, PrimitiveType.Capsule, Vector3.up, new Vector3(0.9f, 1, 0.9f),
                Material("Melee", new Color(0.46f, 0.35f, 0.65f)));
            Save(root, "MeleeEnemy");
        }
        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static void CreateHud()
        {
            if (Exists("HealthHUD")) return;
            var root = Rect("Health HUD", null, Vector2.zero, new Vector2(1600, 900));
            var canvas = root.gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var scaler = root.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            var group = root.gameObject.AddComponent<CanvasGroup>(); group.interactable = group.blocksRaycasts = false; group.alpha = 0;
            var panel = Rect("Health", root, new Vector2(40, 60), new Vector2(270, 60)); panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.zero;
            var text = Rect("Label", panel, new Vector2(0, 14), new Vector2(270, 34)).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = 24; text.text = "HEALTH  100 / 100";
            text.alignment = TextAnchor.MiddleLeft; text.raycastTarget = false;
            var barRect = Rect("Bar", panel, new Vector2(0, -18), new Vector2(270, 12));
            var background = barRect.gameObject.AddComponent<Image>(); background.color = new Color(0.15f, 0.15f, 0.15f); background.raycastTarget = false;
            var fill = Rect("Fill - Replace Sprite", barRect, Vector2.zero, Vector2.zero).gameObject.AddComponent<Image>();
            fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = Vector2.one;
            fill.color = new Color(0.8f, 0.3f, 0.25f); fill.raycastTarget = false;
            var bar = barRect.gameObject.AddComponent<Slider>(); bar.fillRect = fill.rectTransform;
            bar.minValue = 0; bar.maxValue = 100; bar.value = 100; bar.interactable = false; bar.navigation = new Navigation { mode = Navigation.Mode.None };
            var view = root.gameObject.AddComponent<HealthHudView>();
            Bind(view, "label", text); Bind(view, "bar", bar); Bind(view, "group", group);
            Save(root.gameObject, "HealthHUD");
        }
        private static void CreateSystem()
        {
            if (Exists("CombatSystem")) return;
            var root = new GameObject("Combat System");
            var system = root.AddComponent<CombatSystem>();
            var navigation = root.AddComponent<GroundNavigationRegion>();
            Bind(system, "navigationRegion", navigation); Bind(system, "hudPrefab", Load<HealthHudView>("HealthHUD"));
            var towerSpawn = Child("Tower Spawn", root.transform, new Vector3(-4, 0, 7));
            var meleeSpawn = Child("Melee Spawn", root.transform, new Vector3(4, 0, 17));
            var serialized = new SerializedObject(system);
            var spawns = serialized.FindProperty("spawns"); spawns.arraySize = 2;
            spawns.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue = Load<TriggerTower>("TriggerTower");
            spawns.GetArrayElementAtIndex(0).FindPropertyRelative("point").objectReferenceValue = towerSpawn;
            spawns.GetArrayElementAtIndex(1).FindPropertyRelative("prefab").objectReferenceValue = Load<MeleeEnemy>("MeleeEnemy");
            spawns.GetArrayElementAtIndex(1).FindPropertyRelative("point").objectReferenceValue = meleeSpawn;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Save(root, "CombatSystem");
        }
        private static void BindMenuTitle()
        {
            const string path = "Assets/Prefabs/UI/ScreenUI.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var menu = root.GetComponent<MainMenuView>();
                var serialized = new SerializedObject(menu);
                if (serialized.FindProperty("title").objectReferenceValue != null) return;
                var menuRoot = (GameObject)serialized.FindProperty("menuRoot").objectReferenceValue;
                Bind(menu, "title", menuRoot.transform.Find("Title").GetComponent<Text>());
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
