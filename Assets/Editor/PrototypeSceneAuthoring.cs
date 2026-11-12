using System.IO;
using System.Linq;
using SpaceRunner.Combat;
using SpaceRunner.Demo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SpaceRunner.Editor
{
    // One-time editor authoring. Every result is saved as an ordinary scene object or asset.
    public static class PrototypeSceneAuthoring
    {
        private const string ScenePath = "Assets/_Scenes/WatchPrototype.unity";
        private const string Materials = "Assets/Graphics/Materials/Prototype";
        private const string Meshes = "Assets/Graphics/Meshes";
        private const float Apothem = 14;
        private const float Length = 160;
        private const float StartZ = -8;

        [MenuItem("Space Runner/Author Octagonal Prototype (Once)")]
        public static void AuthorScene()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                scene = EditorSceneManager.OpenScene(ScenePath);
            }
            if (Object.FindFirstObjectByType<WatchPrototypeBootstrap>() is not { } bootstrap)
                throw new System.InvalidOperationException("Watch Prototype bootstrap is missing.");
            if (bootstrap.transform.Find("Octagonal Tunnel") != null)
            {
                Debug.Log("The authored tunnel already exists. Edit its scene objects directly; existing placement is preserved.");
                return;
            }
            Directory.CreateDirectory(Materials); Directory.CreateDirectory(Meshes);
            AssetDatabase.Refresh();
            CreateTunnel(bootstrap.transform);
            var player = Instance<FirstPersonController>("Assets/Prefabs/Player.prefab", bootstrap.transform, "Player", new Vector3(0, 2, -5));
            player.playerCamera.nearClipPlane = 0.03f;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player.playerCamera);
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "Main Camera" && root.GetComponent<Camera>() != null) Object.DestroyImmediate(root);

            var watch = Instance<WatchSystem>("Assets/Prefabs/Watch/WatchSystem.prefab", bootstrap.transform, "Watch System", Vector3.zero);
            Bind(watch, "player", player);
            var layout = Child("Enemy Layout - Move or Duplicate These", bootstrap.transform, Vector3.zero);
            var positions = new[] { new Vector3(-4, 0, 7), new Vector3(3.8f, 0, 42), new Vector3(-3.8f, 0, 78), new Vector3(3.8f, 0, 120) };
            for (int i = 0; i < positions.Length; i++)
                Instance<TriggerTower>("Assets/Prefabs/Combat/TriggerTower.prefab", layout, "Tower " + (i + 1).ToString("00"), positions[i]);
            var melee = Instance<MeleeEnemy>("Assets/Prefabs/Combat/MeleeEnemy.prefab", layout, "Melee 01", new Vector3(4, 0, 17));
            melee.GetComponent<NavMeshAgent>().enabled = false;
            PrefabUtility.RecordPrefabInstancePropertyModifications(melee.GetComponent<NavMeshAgent>());

            var combatObject = Child("Combat System", bootstrap.transform, Vector3.zero).gameObject;
            var combat = combatObject.AddComponent<CombatSystem>();
            var navigation = combatObject.AddComponent<GroundNavigationRegion>();
            var navFields = new SerializedObject(navigation);
            navFields.FindProperty("center").vector3Value = new Vector3(0, Apothem, StartZ + Length * 0.5f);
            navFields.FindProperty("size").vector3Value = new Vector3(34, 34, Length + 4);
            navFields.ApplyModifiedPropertiesWithoutUndo();
            Bind(combat, "navigationRegion", navigation);
            Bind(combat, "sceneEnemyLayout", layout);
            Bind(combat, "hudPrefab", AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/HealthHUD.prefab").GetComponent<HealthHudView>());
            Bind(bootstrap, "scenePlayer", player);
            Bind(bootstrap, "watchSystem", watch);
            Bind(bootstrap, "combatSystem", combat);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.16f, 0.18f, 0.21f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.007f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.58f, 0.62f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("Authored 160m octagonal tunnel, editable walls, player and enemy prefab layout. No runtime level generation remains.");
        }

        private static void CreateTunnel(Transform parent)
        {
            var tunnel = Child("Octagonal Tunnel", parent, Vector3.zero);
            var panels = Child("Eight Inner Surfaces", tunnel, Vector3.zero);
            var ribs = Child("Support Rings", tunnel, Vector3.zero);
            var walls = Child("Interior Walls - Move These", tunnel, Vector3.zero);
            var lights = Child("Lighting and Guide Strips", tunnel, Vector3.zero);
            var wallMaterial = Material("Tunnel Wall", new Color(0.38f, 0.41f, 0.46f));
            var alternateMaterial = Material("Tunnel Alternate", new Color(0.28f, 0.31f, 0.36f));
            var floorMaterial = Material("Tunnel Floor", new Color(0.25f, 0.28f, 0.32f));
            var ribMaterial = Material("Tunnel Rib", new Color(0.48f, 0.5f, 0.53f));
            var guideMaterial = Material("Tunnel Guide", new Color(0.65f, 0.73f, 0.78f), true);
            var obstacleMaterial = Material("Tunnel Obstacles", new Color(0.4f, 0.36f, 0.3f));
            var beaconMaterial = Material("Pause Marker", new Color(0.72f, 0.49f, 0.23f), true);
            float width = 2 * Apothem * Mathf.Tan(Mathf.PI / 8);
            var center = new Vector3(0, Apothem, StartZ + Length * 0.5f);
            string[] names = { "Ceiling", "Upper Right", "Right Wall", "Lower Right", "Floor", "Lower Left", "Left Wall", "Upper Left" };
            for (int i = 0; i < 8; i++)
            {
                var rotation = Quaternion.Euler(0, 0, -i * 45);
                var outward = rotation * Vector3.up;
                var material = i == 4 ? floorMaterial : i % 2 == 0 ? wallMaterial : alternateMaterial;
                var panel = Cube(names[i], panels, center + outward * (Apothem + 0.2f), new Vector3(width + 0.4f, 0.4f, Length), material, 6);
                panel.transform.localRotation = rotation;
                for (int ring = 0; ring <= 8; ring++)
                {
                    var ringCenter = new Vector3(0, Apothem, StartZ + ring * 20);
                    var rib = Cube("Ring " + ring.ToString("00") + " - " + names[i], ribs, ringCenter + outward * (Apothem - 0.055f),
                        new Vector3(width, 0.11f, 0.18f), ribMaterial);
                    rib.transform.localRotation = rotation;
                }
            }
            var capMesh = EndCapMesh();
            for (int i = 0; i < 2; i++)
            {
                var cap = Child(i == 0 ? "Entry End Cap" : "Far End Cap", tunnel, new Vector3(0, Apothem, StartZ + i * Length)).gameObject;
                cap.layer = 7;
                cap.transform.localRotation = Quaternion.Euler(0, i * 180, 0);
                cap.AddComponent<MeshFilter>().sharedMesh = capMesh;
                cap.AddComponent<MeshRenderer>().sharedMaterial = alternateMaterial;
                cap.AddComponent<MeshCollider>().sharedMesh = capMesh;
            }
            for (int side = -1; side <= 1; side += 2)
                Cube("Floor Guide " + side, lights, new Vector3(side * 4.7f, 0.015f, center.z), new Vector3(0.06f, 0.02f, Length), guideMaterial);
            for (int i = 0; i < 8; i++)
            {
                var lamp = Child("Tunnel Light " + (i + 1).ToString("00"), lights, new Vector3(0, 7, i * 20)).gameObject.AddComponent<Light>();
                lamp.type = LightType.Point; lamp.range = 28; lamp.intensity = 5;
                lamp.color = new Color(0.85f, 0.9f, 1); lamp.shadows = LightShadows.None;
            }
            Cube("Wall 01", walls, new Vector3(-3, 2.5f, 30), new Vector3(6, 5, 0.8f), obstacleMaterial, 7);
            Cube("Wall 02", walls, new Vector3(3, 3, 55), new Vector3(6, 6, 0.8f), obstacleMaterial, 7);
            Cube("Wall 03", walls, new Vector3(-3, 3, 88), new Vector3(6, 6, 0.8f), obstacleMaterial, 7);
            Cube("Wall 04", walls, new Vector3(3, 2.5f, 132), new Vector3(6, 5, 0.8f), obstacleMaterial, 7);
            Cube("Beacon Pedestal", walls, new Vector3(0, 0.5f, 9), new Vector3(2, 1, 2), obstacleMaterial, 7);
            Cube("Rotating Beacon", tunnel, new Vector3(0, 2, 9), Vector3.one * 1.1f, beaconMaterial).AddComponent<ScaledRotation>();
        }
        private static Mesh EndCapMesh()
        {
            const string path = Meshes + "/Octagonal End Cap.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh != null) return mesh;
            mesh = new Mesh { name = "Octagonal End Cap" };
            var vertices = new Vector3[9]; var uv = new Vector2[9]; var triangles = new int[24];
            float radius = Apothem / Mathf.Cos(Mathf.PI / 8);
            uv[0] = Vector2.one * 0.5f;
            for (int i = 0; i < 8; i++)
            {
                float angle = (22.5f + i * 45) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                uv[i + 1] = new Vector2(vertices[i + 1].x, vertices[i + 1].y) / (radius * 2) + Vector2.one * 0.5f;
                triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % 8 + 1;
            }
            mesh.vertices = vertices; mesh.uv = uv; mesh.triangles = triangles;
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }
        private static Transform Child(string name, Transform parent, Vector3 position)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false); child.localPosition = position;
            return child;
        }
        private static GameObject Cube(string name, Transform parent, Vector3 position, Vector3 scale, Material material, int layer = 0)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.layer = layer;
            obj.transform.SetParent(parent, false); obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = material;
            if (layer == 0) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static Material Material(string name, Color color, bool unlit = false)
        {
            string path = Materials + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }
        private static T Instance<T>(string path, Transform parent, string name, Vector3 position) where T : Component
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
            instance.name = name; instance.transform.localPosition = position;
            return instance.GetComponent<T>();
        }
        private static void Bind(Object target, string name, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(name).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
