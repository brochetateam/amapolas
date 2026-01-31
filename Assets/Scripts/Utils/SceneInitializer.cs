using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;

namespace Amapolas.Utils
{
    public class SceneInitializer : MonoBehaviour
    {
        [MenuItem("Amapolas/Setup Initial Scene")]
        public static void SetupScene()
        {
            RegisterTags();
            
            // 0. Setup UI Canvas FIRST so others can find it
            GameObject canvasGO = GameObject.Find("UICanvas");
            if (canvasGO == null)
            {
                canvasGO = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }

            // 1. Setup MediaPipe Bootstrap
            SetupMediaPipe();

            // 2. Create Managers
            GameObject managersGO = new GameObject("Systems");
            var detManager = managersGO.AddComponent<Managers.DetectionManager>();
            managersGO.AddComponent<Gameplay.GameplayManager>();
            var gui = managersGO.AddComponent<Managers.GameUI>();

            // 3. Create Camera Setup
            GameObject cam = GameObject.FindWithTag("MainCamera");
            if (cam == null)
            {
                cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 1.6f, 0);

            // 4. Create Knife Spawn Point
            GameObject spawnPoint = new GameObject("KnifeSpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 1.6f, 20f);
            
            var gameplay = managersGO.GetComponent<Gameplay.GameplayManager>();
            gameplay.knifeSpawnPoint = spawnPoint.transform;

            // 5. Create Video Preview (Visual Feedback) - Now Canvas exists!
            SetupCameraPreview(detManager);

            // 6. Create Knife Prefab (Rectangle)
            GameObject cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubePrefab.name = "KnifePlaceholder";
            cubePrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            var rb = cubePrefab.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            cubePrefab.GetComponent<BoxCollider>().isTrigger = true;
            gameplay.knifePrefab = cubePrefab;
            
            // 7. Create Player Hitbox
            GameObject playerHitbox = new GameObject("PlayerHitbox");
            playerHitbox.transform.position = new Vector3(0, 1.6f, 0.5f);
            var col = playerHitbox.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 2f, 0.5f);
            col.isTrigger = true;
            playerHitbox.tag = "Player";

            // 8. Support for Hand Shields
            CreateShield(new Vector3(-0.5f, 1.6f, 1f), "LeftHandShield");
            CreateShield(new Vector3(0.5f, 1.6f, 1f), "RightHandShield");

            // 9. Corridor Blocking Setup
            CreateCorridorBlocking();

            // 10. Status UI Setup
            GameObject panel = new GameObject("MessagePanel", typeof(Image));
            panel.transform.SetParent(canvasGO.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);
            
            GameObject textGO = new GameObject("StatusText", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(panel.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "INITIALIZING...";
            tmp.color = Color.red;

            gui.messagePanel = panel;
            gui.statusText = tmp;

            Debug.Log("Scene Setup Complete! UI and Managers ready.");
        }

        private static void RegisterTags()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            string[] neededTags = { "Shield", "Player" };
            foreach (string tag in neededTags)
            {
                bool exists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag) { exists = true; break; }
                }
                if (!exists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    Debug.Log($"[TagSetup] Tag '{tag}' added.");
                }
            }
            tagManager.ApplyModifiedProperties();
        }

        private static void CreateShield(Vector3 pos, string name)
        {
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = name;
            shield.transform.position = pos;
            shield.transform.localScale = Vector3.one * 0.3f;
            shield.tag = "Shield";
            shield.GetComponent<SphereCollider>().isTrigger = true;
        }

        private static void CreateCorridorBlocking()
        {
            GameObject root = new GameObject("CorridorContainer");
            root.AddComponent<Gameplay.InfiniteCorridor>();

            // Create a simple blocking prefab for the tile
            GameObject tile = new GameObject("CorridorTile_Blocking");
            
            // Floor
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(tile.transform);
            floor.transform.localScale = new Vector3(5, 0.1f, 10);
            
            // Walls
            CreatePrimitiveWall(tile.transform, "LeftWall", new Vector3(-2.5f, 2.5f, 0), new Vector3(0.1f, 5, 10));
            CreatePrimitiveWall(tile.transform, "RightWall", new Vector3(2.5f, 2.5f, 0), new Vector3(0.1f, 5, 10));
            CreatePrimitiveWall(tile.transform, "Ceiling", new Vector3(0, 5, 0), new Vector3(5, 0.1f, 10));

            tile.transform.position = new Vector3(0, 0, -100);
            var ic = root.GetComponent<Gameplay.InfiniteCorridor>();
            ic.corridorTilePrefab = tile;
        }

        private static void CreatePrimitiveWall(Transform parent, string name, Vector3 pos, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = pos;
            wall.transform.localScale = scale;
        }

        private static void SetupMediaPipe()
        {
            if (FindFirstObjectByType<Bootstrap>()) return;
            GameObject bootstrapGO = new GameObject("MediaPipeBootstrap");
            var bootstrap = bootstrapGO.AddComponent<Bootstrap>();
            
            AppSettings settings = AssetDatabase.LoadAssetAtPath<AppSettings>("Assets/MediaPipeUnity/Samples/Scenes/AppSettings.asset");
            if (settings != null)
            {
                var so = new SerializedObject(bootstrap);
                so.FindProperty("_appSettings").objectReferenceValue = settings;
                so.ApplyModifiedProperties();
            }
        }

        private static void SetupCameraPreview(Managers.DetectionManager detManager)
        {
            GameObject canvas = GameObject.Find("UICanvas");
            if (canvas == null)
            {
                Debug.LogError("UICanvas NOT FOUND in SetupCameraPreview!");
                return;
            }

            // 1. Container Panel
            GameObject container = new GameObject("Container Panel", typeof(RectTransform), typeof(Image));
            container.transform.SetParent(canvas.transform, false);
            var containerRT = container.GetComponent<RectTransform>();
            containerRT.anchorMin = new Vector2(1, 1);
            containerRT.anchorMax = new Vector2(1, 1);
            containerRT.pivot = new Vector2(1, 1);
            containerRT.anchoredPosition = new Vector2(-10, -10);
            containerRT.sizeDelta = new Vector2(340, 200);
            container.GetComponent<Image>().color = new Color(0, 0, 0, 0.5f);

            // 2. Body
            GameObject body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(container.transform, false);
            var bodyRT = body.GetComponent<RectTransform>();
            bodyRT.anchorMin = Vector2.zero;
            bodyRT.anchorMax = Vector2.one;
            bodyRT.sizeDelta = new Vector2(-20, -20);

            // 3. Annotable Screen
            GameObject screenGO = new GameObject("Annotable Screen", typeof(RectTransform), typeof(RawImage), typeof(Mediapipe.Unity.Screen));
            screenGO.transform.SetParent(body.transform, false);
            var screenRT = screenGO.GetComponent<RectTransform>();
            screenRT.anchorMin = Vector2.zero;
            screenRT.anchorMax = Vector2.one;
            screenRT.sizeDelta = Vector2.zero;
            
            var img = screenGO.GetComponent<RawImage>();
            img.color = Color.white;

            var screen = screenGO.GetComponent<Mediapipe.Unity.Screen>();
            var soScreen = new SerializedObject(screen);
            soScreen.FindProperty("_screen").objectReferenceValue = img;
            soScreen.ApplyModifiedProperties();

            // 4. Annotations
            GameObject faceAnnotationGO = new GameObject("Face Annotation", typeof(RectTransform), typeof(MultiFaceLandmarkListAnnotation), typeof(FaceLandmarkerResultAnnotationController));
            faceAnnotationGO.transform.SetParent(screenGO.transform, false);
            var faceAnnoRT = faceAnnotationGO.GetComponent<RectTransform>();
            faceAnnoRT.anchorMin = Vector2.zero;
            faceAnnoRT.anchorMax = Vector2.one;
            faceAnnoRT.sizeDelta = Vector2.zero;

            var multiAnno = faceAnnotationGO.GetComponent<MultiFaceLandmarkListAnnotation>();
            var annoController = faceAnnotationGO.GetComponent<FaceLandmarkerResultAnnotationController>();
            
            GameObject annoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.github.homuler.mediapipe/PackageResources/Prefabs/FaceLandmarkListWithIris Annotation.prefab");
            if (annoPrefab != null)
            {
                var soMulti = new SerializedObject(multiAnno);
                soMulti.FindProperty("_annotationPrefab").objectReferenceValue = annoPrefab;
                soMulti.ApplyModifiedProperties();
            }

            var soAnnoCtrl = new SerializedObject(annoController);
            soAnnoCtrl.FindProperty("_annotation").objectReferenceValue = multiAnno;
            soAnnoCtrl.ApplyModifiedProperties();

            var soDet = new SerializedObject(detManager);
            soDet.FindProperty("_screen").objectReferenceValue = screen;
            soDet.ApplyModifiedProperties();
        }
    }
}
