using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

public class FixAquariumGameScene
{
    [MenuItem("Tools/Fix Aquarium Game References")]
    public static void FixReferences()
    {
        string scenePath = "Assets/Scenes/AquariumGame.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        
        var controller = Object.FindObjectOfType<AquariumGameController>();
        if (controller != null)
        {
            // Find textures
            string marineGuid = "a1b2c3d4e5f60001a1b2c3d4e5f60001"; // FishSprite_Clownfish.png
            string freshGuid = "a1b2c3d4e5f60002a1b2c3d4e5f60002"; // FishSprite_Guppy.png
            
            string bgMarineGuid = "168c55e8a76b7b3469285e669c08046e"; // marineBackground
            string bgFreshGuid = "c6fbaeb6c818fa34e80be403355632c2"; // freshwaterBackground
            
            // Find prefabs
            string marinePrefabGuid = "7c609d199e833e64e80264c75b43688d"; // marine_clownfish.prefab
            string freshPrefabGuid = "89c7d56787878bd45a97b081a69b5b43"; // freshWater_guppy.prefab
            
            controller.marineFishTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(marineGuid));
            controller.freshwaterFishTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(freshGuid));
            
            controller.marineFishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(marinePrefabGuid));
            controller.freshwaterFishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(freshPrefabGuid));
            
            controller.marineBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(bgMarineGuid));
            controller.freshwaterBackground = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath(bgFreshGuid));
            
            EditorUtility.SetDirty(controller);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Successfully fixed references in AquariumGame scene!");
        }
        else
        {
            Debug.LogError("Controller not found in scene!");
        }
    }
}
