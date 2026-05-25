using System.Collections.Generic;
using System.IO;
using Game.Balls;
using Game.Chain;
using Game.Core.Bootstrap;
using Game.Core.Pooling;
using Game.Level;
using Game.PathSystem;
using Game.Projectile;
using Game.Shooter;
using Game.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.EditorTools
{
    /// <summary>
    /// One-click scene builder.
    ///
    /// Why an editor script and not a hand-written .unity file?
    /// - Unity scene YAML uses cross-file GUID references that are very easy
    ///   to corrupt manually. A scaffolder gets compiled by the editor before
    ///   it runs, so the only failure mode is "this code is wrong" — not
    ///   "this YAML has an invalid GUID and Unity refuses to open the scene".
    /// - The script doubles as documentation: every assignment below is
    ///   exactly what you would do in the inspector by hand.
    ///
    /// Usage:
    ///   Tools → Game → Build Demo Scene
    ///   - Creates ScriptableObject assets under Assets/Game/Generated/
    ///   - Creates ball + projectile prefabs under Assets/Game/Prefabs/
    ///   - Opens a fresh scene and wires everything together
    ///   - Saves the scene as Assets/Game/Scenes/Demo.unity
    /// </summary>
    public static class SceneScaffolder
    {
        private const string GeneratedRoot = "Assets/Game/Generated";
        private const string PrefabsRoot   = "Assets/Game/Prefabs";
        private const string ScenePath     = "Assets/Game/Scenes/Demo.unity";

        [MenuItem("Tools/Game/Build Demo Scene")]
        public static void BuildDemoScene()
        {
            EnsureFolders();

            var standardShader = FindShader();

            // ---- Assets first: prefabs depend on materials, SOs depend on prefabs.
            var materials   = CreateBallMaterials(standardShader);
            var ballPrefab  = CreateBallViewPrefab(standardShader);
            var projPrefab  = CreateProjectilePrefab(standardShader);
            var defs        = CreateBallDefinitions(ballPrefab, materials);
            var database    = CreateBallDatabase(defs);
            var level       = CreateLevelDefinition();

            // ---- Scene: empty, then populated.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildHierarchy(scene, ballPrefab, projPrefab, database, level);

            // ---- Save.
            Directory.CreateDirectory("Assets/Game/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Demo Scene", "Built at " + ScenePath + ".\nOpen it and press Play.", "OK");
        }

        // -----------------------------------------------------------------
        // Folder + shader helpers
        // -----------------------------------------------------------------

        private static void EnsureFolders()
        {
            foreach (var p in new[]
            {
                "Assets/Game/Generated",
                "Assets/Game/Generated/Definitions",
                "Assets/Game/Generated/Materials",
                "Assets/Game/Prefabs",
                "Assets/Game/Scenes",
            })
            {
                if (!AssetDatabase.IsValidFolder(p))
                {
                    var parent = Path.GetDirectoryName(p)?.Replace('\\', '/');
                    var leaf   = Path.GetFileName(p);
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(leaf))
                        AssetDatabase.CreateFolder(parent, leaf);
                }
            }
        }

        // Pick whichever standard shader is available. URP first, built-in second.
        private static Shader FindShader()
        {
            return Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Standard")
                ?? Shader.Find("Hidden/InternalErrorShader");
        }

        // -----------------------------------------------------------------
        // Materials + ball-view prefab
        // -----------------------------------------------------------------

        private static Dictionary<BallColor, Material> CreateBallMaterials(Shader shader)
        {
            var palette = new Dictionary<BallColor, Color>
            {
                { BallColor.Red,    new Color(0.95f, 0.25f, 0.25f) },
                { BallColor.Green,  new Color(0.30f, 0.85f, 0.35f) },
                { BallColor.Blue,   new Color(0.25f, 0.55f, 0.95f) },
                { BallColor.Yellow, new Color(0.95f, 0.85f, 0.20f) },
                { BallColor.Purple, new Color(0.70f, 0.30f, 0.90f) },
            };

            var dict = new Dictionary<BallColor, Material>();
            foreach (var kv in palette)
            {
                var mat = new Material(shader) { name = $"Mat_{kv.Key}" };
                // URP uses _BaseColor; built-in uses _Color. Set both — extra
                // assignments are no-ops if the property doesn't exist.
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", kv.Value);
                if (mat.HasProperty("_Color"))     mat.SetColor("_Color", kv.Value);
                var path = $"{GeneratedRoot}/Materials/{mat.name}.mat";
                AssetDatabase.CreateAsset(mat, path);
                dict[kv.Key] = mat;
            }
            return dict;
        }

        private static GameObject CreateBallViewPrefab(Shader shader)
        {
            // Build the prefab source in the scene, save it as an asset, then destroy the scene copy.
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "BallView";
            go.transform.localScale = Vector3.one * 0.5f; // diameter ≈ 0.5 (matches ChainConfig.Default)

            var collider = go.GetComponent<SphereCollider>();
            collider.isTrigger = true; // chain balls are triggers for projectile detection

            // Attach the BallView component.
            var view = go.AddComponent<BallView>();

            // Wire the SerializeField on BallView via SerializedObject.
            var so = new SerializedObject(view);
            so.FindProperty("_renderer").objectReferenceValue   = go.GetComponent<MeshRenderer>();
            so.FindProperty("_visualRoot").objectReferenceValue = go.transform;
            so.ApplyModifiedProperties();

            // Default material so the prefab itself is visible in the inspector preview.
            var defaultMat = new Material(shader) { name = "Mat_BallPlaceholder" };
            go.GetComponent<MeshRenderer>().sharedMaterial = defaultMat;

            var path = $"{PrefabsRoot}/BallView.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        private static GameObject CreateProjectilePrefab(Shader shader)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Projectile";
            go.transform.localScale = Vector3.one * 0.45f;

            var collider = go.GetComponent<SphereCollider>();
            collider.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation          = RigidbodyInterpolation.Interpolate;

            var view  = go.AddComponent<BallView>();
            var projB = go.AddComponent<ProjectileBall>();

            var soView = new SerializedObject(view);
            soView.FindProperty("_renderer").objectReferenceValue   = go.GetComponent<MeshRenderer>();
            soView.FindProperty("_visualRoot").objectReferenceValue = go.transform;
            soView.ApplyModifiedProperties();

            var soProj = new SerializedObject(projB);
            soProj.FindProperty("_view").objectReferenceValue       = view;
            soProj.FindProperty("_rigidbody").objectReferenceValue  = rb;
            soProj.ApplyModifiedProperties();

            var mat = new Material(shader) { name = "Mat_Projectile" };
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

            var path = $"{PrefabsRoot}/Projectile.prefab";
            var asset = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return asset;
        }

        // -----------------------------------------------------------------
        // ScriptableObject definitions
        // -----------------------------------------------------------------

        private static Dictionary<BallColor, BallDefinition> CreateBallDefinitions(GameObject ballViewPrefab, Dictionary<BallColor, Material> mats)
        {
            var prefabView = ballViewPrefab.GetComponent<BallView>();
            var result = new Dictionary<BallColor, BallDefinition>();

            foreach (var color in new[] { BallColor.Red, BallColor.Green, BallColor.Blue, BallColor.Yellow, BallColor.Purple })
            {
                var def = ScriptableObject.CreateInstance<BallDefinition>();
                def.name = $"Ball_{color}";

                var so = new SerializedObject(def);
                so.FindProperty("_color").enumValueIndex = (int)color;
                var disp = mats.TryGetValue(color, out var m) ? m.color : Color.white;
                so.FindProperty("_displayColor").colorValue = disp;
                so.FindProperty("_viewPrefab").objectReferenceValue = prefabView;
                so.FindProperty("_scorePerBall").intValue = 10;
                so.ApplyModifiedProperties();

                AssetDatabase.CreateAsset(def, $"{GeneratedRoot}/Definitions/{def.name}.asset");
                result[color] = def;
            }
            return result;
        }

        private static BallDatabase CreateBallDatabase(Dictionary<BallColor, BallDefinition> defs)
        {
            var db = ScriptableObject.CreateInstance<BallDatabase>();
            db.name = "BallDatabase";
            var so = new SerializedObject(db);
            var arr = so.FindProperty("_definitions");
            arr.arraySize = defs.Count;
            int i = 0;
            foreach (var def in defs.Values)
                arr.GetArrayElementAtIndex(i++).objectReferenceValue = def;
            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(db, $"{GeneratedRoot}/BallDatabase.asset");
            return db;
        }

        private static LevelDefinition CreateLevelDefinition()
        {
            var level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.name = "Level_Demo";

            var so = new SerializedObject(level);
            so.FindProperty("_displayName").stringValue = "Demo Level";

            // ChainConfig defaults are fine; override forward speed for the demo.
            var cfg = so.FindProperty("_chainConfig");
            cfg.FindPropertyRelative("BallDiameter").floatValue    = 0.5f;
            cfg.FindPropertyRelative("ForwardSpeed").floatValue    = 1.0f;
            cfg.FindPropertyRelative("CollapseCatchUp").floatValue = 6.0f;
            cfg.FindPropertyRelative("MergeEpsilon").floatValue    = 0.001f;
            cfg.FindPropertyRelative("MinMatch").intValue          = 3;

            SetEnumList(so, "_availableColors", new[] { BallColor.Red, BallColor.Green, BallColor.Blue });

            // Pre-place a short rainbow so matches are obvious during the smoke test.
            SetEnumList(so, "_initialBalls", new[]
            {
                BallColor.Red, BallColor.Red, BallColor.Green, BallColor.Blue,
                BallColor.Green, BallColor.Blue, BallColor.Red, BallColor.Green,
            });

            // Drip-feed more balls so the chain keeps growing.
            var queue = new List<BallColor>(60);
            for (int i = 0; i < 60; i++)
                queue.Add(new[] { BallColor.Red, BallColor.Green, BallColor.Blue }[i % 3]);
            SetEnumList(so, "_spawnQueue", queue.ToArray());
            so.FindProperty("_spawnInterval").floatValue   = 0.6f;
            so.FindProperty("_scoreMultiplier").intValue   = 1;

            so.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(level, $"{GeneratedRoot}/Level_Demo.asset");
            return level;
        }

        private static void SetEnumList(SerializedObject so, string property, BallColor[] values)
        {
            var prop = so.FindProperty(property);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).enumValueIndex = (int)values[i];
        }

        // -----------------------------------------------------------------
        // Scene hierarchy
        // -----------------------------------------------------------------

        private static void BuildHierarchy(Scene scene, GameObject ballPrefab, GameObject projPrefab, BallDatabase db, LevelDefinition level)
        {
            // --- Camera ---------------------------------------------------
            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            var cam   = camGo.GetComponent<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 10f, 0f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // top-down
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.10f, 0.13f);
            camGo.tag = "MainCamera";

            // --- Light ----------------------------------------------------
            var lightGo = new GameObject("Directional Light", typeof(Light));
            var light = lightGo.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            lightGo.transform.rotation = Quaternion.Euler(45f, 30f, 0f);

            // --- Root containers -----------------------------------------
            var root        = new GameObject("GameRoot");
            var poolGo      = new GameObject("Pool", typeof(GameObjectPool));
            poolGo.transform.SetParent(root.transform, false);
            var pool        = poolGo.GetComponent<GameObjectPool>();

            var ballParent  = new GameObject("BallParent");
            ballParent.transform.SetParent(root.transform, false);

            // --- Path -----------------------------------------------------
            var pathGo = new GameObject("Path", typeof(WaypointPath));
            pathGo.transform.SetParent(root.transform, false);
            var path = pathGo.GetComponent<WaypointPath>();
            CreateSpiralWaypoints(pathGo.transform, 24, radiusStart: 5.5f, radiusEnd: 0.5f, turns: 1.25f);
            path.RebuildCache();

            // --- Chain controller ----------------------------------------
            var chainGo = new GameObject("Chain", typeof(ChainController));
            chainGo.transform.SetParent(root.transform, false);
            var chain = chainGo.GetComponent<ChainController>();
            {
                var so = new SerializedObject(chain);
                so.FindProperty("_path").objectReferenceValue         = path;
                so.FindProperty("_pool").objectReferenceValue         = pool;
                so.FindProperty("_ballDatabase").objectReferenceValue = db;
                so.FindProperty("_level").objectReferenceValue        = level;
                so.FindProperty("_ballParent").objectReferenceValue   = ballParent.transform;
                so.ApplyModifiedProperties();
            }

            // --- Shooter --------------------------------------------------
            var shooterGo = new GameObject("Shooter");
            shooterGo.transform.SetParent(root.transform, false);
            shooterGo.transform.position = new Vector3(0f, 0f, -6.5f);

            var inputGo = new GameObject("Input", typeof(MouseShooterInput));
            inputGo.transform.SetParent(shooterGo.transform, false);
            var input = inputGo.GetComponent<MouseShooterInput>();
            {
                var so = new SerializedObject(input);
                so.FindProperty("_camera").objectReferenceValue = cam;
                so.FindProperty("_trackY").floatValue           = 0f;
                so.ApplyModifiedProperties();
            }

            var muzzle = new GameObject("Muzzle");
            muzzle.transform.SetParent(shooterGo.transform, false);
            muzzle.transform.localPosition = new Vector3(0f, 0f, 0.4f);

            var currentPreview = PrefabUtility.InstantiatePrefab(ballPrefab) as GameObject;
            currentPreview.name = "CurrentPreview";
            currentPreview.transform.SetParent(shooterGo.transform, false);
            currentPreview.transform.localPosition = new Vector3(0f, 0f, 0f);

            var nextPreview = PrefabUtility.InstantiatePrefab(ballPrefab) as GameObject;
            nextPreview.name = "NextPreview";
            nextPreview.transform.SetParent(shooterGo.transform, false);
            nextPreview.transform.localPosition = new Vector3(-0.9f, 0f, -0.2f);
            nextPreview.transform.localScale = Vector3.one * 0.4f;

            var shooter = shooterGo.AddComponent<Shooter.Shooter>();
            {
                var so = new SerializedObject(shooter);
                so.FindProperty("_input").objectReferenceValue            = input;
                so.FindProperty("_muzzle").objectReferenceValue           = muzzle.transform;
                so.FindProperty("_currentPreview").objectReferenceValue   = currentPreview.GetComponent<BallView>();
                so.FindProperty("_nextPreview").objectReferenceValue      = nextPreview.GetComponent<BallView>();
                so.FindProperty("_pool").objectReferenceValue             = pool;
                so.FindProperty("_database").objectReferenceValue         = db;
                so.FindProperty("_level").objectReferenceValue            = level;
                so.FindProperty("_projectilePrefab").objectReferenceValue = projPrefab.GetComponent<ProjectileBall>();
                so.FindProperty("_chain").objectReferenceValue            = chain;
                so.FindProperty("_minX").floatValue            = -5f;
                so.FindProperty("_maxX").floatValue            =  5f;
                so.FindProperty("_projectileSpeed").floatValue = 16f;
                so.FindProperty("_fireCooldown").floatValue    = 0.18f;
                so.ApplyModifiedProperties();
            }

            // --- UI: Canvas + HUD + screens ------------------------------
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);

            // EventSystem for UI clicks (Pause buttons).
            new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
                                          typeof(UnityEngine.EventSystems.StandaloneInputModule));

            var hudGo = new GameObject("HUD", typeof(RectTransform));
            hudGo.transform.SetParent(canvasGo.transform, false);
            var scoreText  = CreateTmp(hudGo.transform, "Score",  "0",      new Vector2(20, -20), TextAlignmentOptions.TopLeft);
            var comboText  = CreateTmp(hudGo.transform, "Combo",  "",       new Vector2(20, -60), TextAlignmentOptions.TopLeft);
            var currentSw  = CreateSwatch(hudGo.transform, "CurrentSwatch", new Vector2(-120, 60));
            var nextSw     = CreateSwatch(hudGo.transform, "NextSwatch",    new Vector2(-60,  60), 0.7f);

            var hud = hudGo.AddComponent<HudView>();
            {
                var so = new SerializedObject(hud);
                so.FindProperty("_scoreLabel").objectReferenceValue = scoreText;
                so.FindProperty("_comboLabel").objectReferenceValue = comboText;
                so.FindProperty("_currentSwatch").objectReferenceValue = currentSw;
                so.FindProperty("_nextSwatch").objectReferenceValue    = nextSw;
                so.FindProperty("_database").objectReferenceValue      = db;
                so.ApplyModifiedProperties();
            }

            var pausePanel = CreatePanel(canvasGo.transform, "PauseScreen", "PAUSED", new Color(0, 0, 0, 0.65f));
            var winPanel   = CreatePanel(canvasGo.transform, "WinScreen",   "YOU WIN", new Color(0.05f, 0.4f, 0.05f, 0.8f));
            var losePanel  = CreatePanel(canvasGo.transform, "LoseScreen",  "GAME OVER", new Color(0.5f, 0.05f, 0.05f, 0.8f));

            var pauseScreen = pausePanel.AddComponent<PauseScreen>();
            var winScreen   = winPanel.AddComponent<WinScreen>();
            var loseScreen  = losePanel.AddComponent<LoseScreen>();
            new SerializedObject(pauseScreen).Apply(p => p.FindProperty("_root").objectReferenceValue = pausePanel);
            new SerializedObject(winScreen)  .Apply(p => p.FindProperty("_root").objectReferenceValue = winPanel);
            new SerializedObject(loseScreen) .Apply(p => p.FindProperty("_root").objectReferenceValue = losePanel);

            // --- Bootstrap (composition root) ----------------------------
            var bootGo = new GameObject("Bootstrap", typeof(GameBootstrap));
            bootGo.transform.SetParent(root.transform, false);
            var boot = bootGo.GetComponent<GameBootstrap>();
            {
                var so = new SerializedObject(boot);
                so.FindProperty("_pool").objectReferenceValue         = pool;
                so.FindProperty("_ballDatabase").objectReferenceValue = db;
                so.FindProperty("_level").objectReferenceValue        = level;
                so.FindProperty("_chain").objectReferenceValue        = chain;
                so.FindProperty("_shooter").objectReferenceValue      = shooter;
                so.FindProperty("_hud").objectReferenceValue          = hud;
                so.FindProperty("_pauseScreen").objectReferenceValue  = pauseScreen;
                so.FindProperty("_winScreen").objectReferenceValue    = winScreen;
                so.FindProperty("_loseScreen").objectReferenceValue   = loseScreen;

                // Prewarm two prefab buckets.
                var prefabs = so.FindProperty("_prewarmPrefabs");
                var counts  = so.FindProperty("_prewarmCounts");
                prefabs.arraySize = 2;
                counts.arraySize  = 2;
                prefabs.GetArrayElementAtIndex(0).objectReferenceValue = ballPrefab;
                counts.GetArrayElementAtIndex(0).intValue              = 32;
                prefabs.GetArrayElementAtIndex(1).objectReferenceValue = projPrefab;
                counts.GetArrayElementAtIndex(1).intValue              = 8;
                so.ApplyModifiedProperties();
            }
        }

        // -----------------------------------------------------------------
        // Path: programmatic spiral so balls travel along an obvious curve
        // -----------------------------------------------------------------

        private static void CreateSpiralWaypoints(Transform parent, int count, float radiusStart, float radiusEnd, float turns)
        {
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)(count - 1);
                float angle = t * turns * Mathf.PI * 2f;
                float r = Mathf.Lerp(radiusStart, radiusEnd, t);
                var pos = new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
                var wp = new GameObject($"WP_{i:00}");
                wp.transform.SetParent(parent, false);
                wp.transform.localPosition = pos;
            }
        }

        // -----------------------------------------------------------------
        // UI helpers
        // -----------------------------------------------------------------

        private static TMP_Text CreateTmp(Transform parent, string name, string text, Vector2 anchoredPos, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text     = text;
            tmp.fontSize = 42;
            tmp.alignment = align;
            tmp.color = Color.white;
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(600, 80);
            return tmp;
        }

        private static Image CreateSwatch(Transform parent, string name, Vector2 anchoredPos, float scale = 1f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(1, 1);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(80, 80) * scale;
            var img = go.GetComponent<Image>();
            img.color = Color.white;
            return img;
        }

        private static GameObject CreatePanel(Transform parent, string name, string label, Color bg)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = bg;

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 120;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            var trt = (RectTransform)textGo.transform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            go.SetActive(false); // hidden until the screen's Bind() reacts to the event
            return go;
        }
    }

    // Tiny ergonomic helper: chained SerializedObject edits without a temp var.
    internal static class SerializedObjectFluentExtensions
    {
        public static void Apply(this SerializedObject so, System.Action<SerializedObject> mutator)
        {
            mutator(so);
            so.ApplyModifiedProperties();
        }
    }
}
