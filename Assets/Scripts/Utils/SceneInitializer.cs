using UnityEngine;
using UnityEditor;

namespace Amapolas.Utils
{
    public class SceneInitializer : MonoBehaviour
    {
        [MenuItem("Amapolas/Setup Initial Scene")]
        public static void SetupScene()
        {
            // 1. Create Managers
            GameObject managersGO = new GameObject("Systems");
            managersGO.AddComponent<Managers.DetectionManager>();
            managersGO.AddComponent<Gameplay.GameplayManager>();
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

            // 4. Create Knife Prefab (Rectangle)
            GameObject cubePrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubePrefab.name = "KnifePlaceholder";
            cubePrefab.transform.localScale = new Vector3(0.1f, 0.1f, 0.5f);
            var rb = cubePrefab.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            cubePrefab.GetComponent<BoxCollider>().isTrigger = true;
            
            // Save as asset if possible or just keep in scene
            gameplay.knifePrefab = cubePrefab;
            
            // 5. Create Player Hitbox
            GameObject playerHitbox = new GameObject("PlayerHitbox");
            playerHitbox.transform.position = new Vector3(0, 1.6f, 0.5f);
            var col = playerHitbox.AddComponent<BoxCollider>();
            col.size = new Vector3(1f, 2f, 0.5f);
            col.isTrigger = true;
            playerHitbox.tag = "Player";

            // 6. Support for Hand Shields
            GameObject leftHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftHand.name = "LeftHandShield";
            leftHand.transform.localScale = Vector3.one * 0.3f;
            leftHand.tag = "Shield";
            leftHand.GetComponent<SphereCollider>().isTrigger = true;
            
            GameObject rightHand = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightHand.name = "RightHandShield";
            rightHand.transform.localScale = Vector3.one * 0.3f;
            rightHand.tag = "Shield";
            rightHand.GetComponent<SphereCollider>().isTrigger = true;

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

            Debug.Log("Scene Setup Complete! Use 'F' to simulate face, then 'M' to start game.");
        }
    }
}
