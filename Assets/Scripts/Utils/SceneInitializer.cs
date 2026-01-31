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
            
            // 0. Setup MediaPipe Bootstrap
            SetupMediaPipe();

            // 1. Create Managers
            GameObject managersGO = new GameObject("Systems");
            managersGO.AddComponent<Managers.DetectionManager>();
            managersGO.AddComponent<Gameplay.GameplayManager>();
            managersGO.AddComponent<Gameplay.HandBlockingManager>();
            managersGO.AddComponent<Managers.GameUI>();

            // 2. Create Camera Setup
            GameObject cam = GameObject.FindWithTag("MainCamera");
            if (cam == null)
            {
                cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
            }
            cam.transform.position = new Vector3(0, 1.6f, 0);

            // 3. Create Knife Spawn Point
            GameObject spawnPoint = new GameObject("KnifeSpawnPoint");
            spawnPoint.transform.position = new Vector3(0, 1.6f, 20f);
            
            var gameplay = managersGO.GetComponent<Gameplay.GameplayManager>();
            gameplay.knifeSpawnPoint = spawnPoint.transform;

            // 4. Create Video Preview (Visual Feedback)
            SetupCameraPreview(managersGO.GetComponent<Managers.DetectionManager>());

            // 5. Create Knife Prefab (Rectangle)
            GameObject cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubePrefab.name = "KnifePlaceholder";
            cubePrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            var rb = cubePrefab.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            cubePrefab.GetComponent<BoxCollider>().isTrigger = true;
            cubePrefab.tag = "Knife";
            cubePrefab.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            
            // Save as asset if possible or just keep in scene
            gameplay.knifePrefab = cubePrefab;
            
            // 5. Create Player Hitbox
            GameObject playerHitbox = new GameObject("PlayerHitbox");
            playerHitbox.transform.position = new Vector3(0, 1.6f, 0.5f);
            var col = playerHitbox.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 2f, 0.5f);
            col.isTrigger = true;
            playerHitbox.tag = "Player";

            // 5b. Create Amapola Prefab (Pink Sphere)
            GameObject amapolaPlaceholder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            amapolaPlaceholder.name = "AmapolaPlaceholder";
            amapolaPlaceholder.transform.localScale = Vector3.one * 0.3f;
            amapolaPlaceholder.GetComponent<Renderer>().sharedMaterial.color = Color.green;
            amapolaPlaceholder.GetComponent<SphereCollider>().isTrigger = true;
            amapolaPlaceholder.tag = "Untagged"; // It's not a knife, but projectile handles it
            gameplay.amapolaPrefab = amapolaPlaceholder;

            // 6. Support for Hand Shields
            GameObject shieldTemplate = CreateShield(new Vector3(0, -10, 0), "Shield_Template");
            var blockingManager = managersGO.GetComponent<Gameplay.HandBlockingManager>();
            blockingManager.shieldPrefab = shieldTemplate;
            
            // Note: We don't need static shields anymore as they are spawned by manager

            // 7. Corridor Blocking Setup
            CreateCorridorBlocking();

            // 7. UI Setup (Minimal)
            GameObject canvasGO = new GameObject("UICanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            
            GameObject panel = new GameObject("MessagePanel", typeof(Image));
            panel.transform.SetParent(canvasGO.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);
            
            GameObject textGO = new GameObject("StatusText", typeof(TextMeshProUGUI));
            textGO.transform.SetParent(panel.transform, false);
            var tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.text = "INITIALIZING...";
            tmp.color = Color.red;

            var gui = managersGO.GetComponent<Managers.GameUI>();
            gui.messagePanel = panel;
            gui.statusText = tmp;

            // 7b. UI Health Slider
            GameObject sliderGO = new GameObject("HealthSlider", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(canvasGO.transform, false);
            var sliderRT = sliderGO.GetComponent<RectTransform>();
            sliderRT.anchorMin = new Vector2(0.5f, 0);
            sliderRT.anchorMax = new Vector2(0.5f, 0);
            sliderRT.pivot = new Vector2(0.5f, 0);
            sliderRT.anchoredPosition = new Vector2(0, 50);
            sliderRT.sizeDelta = new Vector2(300, 20);

            var slider = sliderGO.GetComponent<Slider>();
            
            // Create background and fill for the slider (simplified)
            GameObject bg = new GameObject("Background", typeof(Image));
            bg.transform.SetParent(sliderGO.transform, false);
            bg.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 0);
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bg.GetComponent<RectTransform>().anchorMax = Vector2.one;

            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGO.transform, false);
            fillArea.GetComponent<RectTransform>().sizeDelta = new Vector2(-10, -4);
            fillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            fillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;

            GameObject fill = new GameObject("Fill", typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = Color.green;
            fill.GetComponent<RectTransform>().sizeDelta = Vector2.zero;

            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.targetGraphic = fill.GetComponent<Image>();
            
            gui.healthSlider = slider;

            // 8. Setup Feedback Overlays
            gui.hitOverlay = CreateOverlay(canvasGO.transform, "HitOverlay", new Color(1, 0, 0, 0));
            gui.blockOverlay = CreateOverlay(canvasGO.transform, "BlockOverlay", new Color(0, 1, 0, 0));

            Debug.Log("Scene Setup Complete! Use 'F' to simulate face, then 'M' to start game.");
        }

        private static Image CreateOverlay(Transform parent, string name, Color color)
        {
            GameObject overlayGO = new GameObject(name, typeof(Image));
            overlayGO.transform.SetParent(parent, false);
            var rect = overlayGO.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            var img = overlayGO.GetComponent<Image>();
            img.color = color;
            overlayGO.SetActive(false);
            return img;
        }

        private static void RegisterTags()
        {
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");

            string[] neededTags = { "Shield", "Player", "Knife" };
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

        private static GameObject CreateShield(Vector3 pos, string name)
        {
            GameObject shield = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shield.name = name;
            shield.transform.position = pos;
            shield.transform.localScale = Vector3.one * 0.4f;
            shield.tag = "Shield";
            shield.GetComponent<SphereCollider>().isTrigger = true;
            shield.AddComponent<Gameplay.HandShield>();
            
            // Add a visual material if possible, or just color it
            var renderer = shield.GetComponent<Renderer>();
            renderer.sharedMaterial.color = new Color(0, 0.8f, 1f, 0.5f);
            
            return shield;
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
            
            // Left Wall
            GameObject leftWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftWall.name = "LeftWall";
            leftWall.transform.SetParent(tile.transform);
            leftWall.transform.position = new Vector3(-2.5f, 2.5f, 0);
            leftWall.transform.localScale = new Vector3(0.1f, 5, 10);

            // Right Wall
            GameObject rightWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightWall.name = "RightWall";
            rightWall.transform.SetParent(tile.transform);
            rightWall.transform.position = new Vector3(2.5f, 2.5f, 0);
            rightWall.transform.localScale = new Vector3(0.1f, 5, 10);

            // Ceiling
            GameObject ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(tile.transform);
            ceiling.transform.position = new Vector3(0, 5, 0);
            ceiling.transform.localScale = new Vector3(5, 0.1f, 10);

            tile.transform.position = new Vector3(0, 0, -100); // Hide template
            var ic = root.GetComponent<Gameplay.InfiniteCorridor>();
            ic.corridorTilePrefab = tile;
        }

        private static void SetupMediaPipe()
        {
            GameObject bootstrapGO = new GameObject("MediaPipeBootstrap");
            var bootstrap = bootstrapGO.AddComponent<Bootstrap>();
            
            // Load AppSettings from the path found earlier
            AppSettings settings = AssetDatabase.LoadAssetAtPath<AppSettings>("Assets/MediaPipeUnity/Samples/Scenes/AppSettings.asset");
            if (settings != null)
            {
                // Assign via SerializedObject to private field if necessary, or just rely on manual link if possible
                var so = new SerializedObject(bootstrap);
                so.FindProperty("_appSettings").objectReferenceValue = settings;
                so.ApplyModifiedProperties();
            }
        }

        private static void SetupCameraPreview(Managers.DetectionManager detManager)
        {
            GameObject canvas = GameObject.Find("UICanvas");
            if (canvas == null) return;

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

            // 2. Body (Container for Screen)
            GameObject body = new GameObject("Body", typeof(RectTransform));
            body.transform.SetParent(container.transform, false);
            var bodyRT = body.GetComponent<RectTransform>();
            bodyRT.anchorMin = Vector2.zero;
            bodyRT.anchorMax = Vector2.one;
            bodyRT.sizeDelta = new Vector2(-20, -20); // Padding

            // 3. Annotable Screen (The image + Annotation Controllers)
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
            
            // Link Prefab to MultiAnnotation (private field _annotationPrefab in ListAnnotation)
            GameObject annoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.github.homuler.mediapipe/PackageResources/Prefabs/FaceLandmarkListWithIris Annotation.prefab");
            if (annoPrefab != null)
            {
                var soMulti = new SerializedObject(multiAnno);
                soMulti.FindProperty("_annotationPrefab").objectReferenceValue = annoPrefab;
                soMulti.ApplyModifiedProperties();
            }

            // Link Annotation to Controller (both private fields)
            var soAnnoCtrl = new SerializedObject(annoController);
            soAnnoCtrl.FindProperty("_annotation").objectReferenceValue = multiAnno;
            soAnnoCtrl.ApplyModifiedProperties();

            // 5. Hand Annotations
            GameObject handAnnotationGO = new GameObject("Hand Annotation", typeof(RectTransform), typeof(MultiHandLandmarkListAnnotation), typeof(HandLandmarkerResultAnnotationController));
            handAnnotationGO.transform.SetParent(screenGO.transform, false);
            var handAnnoRT = handAnnotationGO.GetComponent<RectTransform>();
            handAnnoRT.anchorMin = Vector2.zero;
            handAnnoRT.anchorMax = Vector2.one;
            handAnnoRT.sizeDelta = Vector2.zero;

            var handMultiAnno = handAnnotationGO.GetComponent<MultiHandLandmarkListAnnotation>();
            var handAnnoController = handAnnotationGO.GetComponent<HandLandmarkerResultAnnotationController>();

            GameObject handAnnoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.github.homuler.mediapipe/PackageResources/Prefabs/Multi HandLandmarkList Annotation.prefab");
            if (handAnnoPrefab != null)
            {
                var soHandMulti = new SerializedObject(handMultiAnno);
                soHandMulti.FindProperty("_annotationPrefab").objectReferenceValue = handAnnoPrefab;
                soHandMulti.ApplyModifiedProperties();
            }

            var soHandAnnoCtrl = new SerializedObject(handAnnoController);
            soHandAnnoCtrl.FindProperty("_annotation").objectReferenceValue = handMultiAnno;
            soHandAnnoCtrl.ApplyModifiedProperties();

            // Assign to detection manager
            var soDet = new SerializedObject(detManager);
            soDet.FindProperty("_screen").objectReferenceValue = screen;
            soDet.ApplyModifiedProperties();
            
            // Note: We'll need a way in DetectionManager to find or reference annoController
            // For now, we'll let it find it in Start/Awake
        }
    }
}
