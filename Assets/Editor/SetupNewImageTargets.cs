using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System;

public class SetupNewImageTargets
{
    [MenuItem("Tools/Setup 3 New Fish Targets")]
    public static void SetupTargets()
    {
        string scenePath = "Assets/Scenes/MainScene.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find components dynamically across all loaded assemblies (Vuforia assembly names vary by version)
        System.Type typeImageTarget = FindTypeInAssemblies("Vuforia.ImageTargetBehaviour");
        System.Type typeObserver = FindTypeInAssemblies("Vuforia.DefaultObserverEventHandler");
        if (typeObserver == null)
        {
            typeObserver = FindTypeInAssemblies("Vuforia.DefaultTrackableEventHandler");
        }

        if (typeImageTarget == null)
        {
            Debug.LogError("Vuforia ImageTargetBehaviour type not found in any loaded assemblies! Is Vuforia Engine installed in the project?");
            return;
        }

        // Define our 3 new fish
        // 1. Emperor Angelfish
        string angelPrefabPath = "Assets/Mikhail Nesterov/Emperor Angelfish/Prefab/EmperorAngelfish_swim1.prefab";
        string angelTexturePath = "Assets/FishSprite_Angelfish.png";
        
        // 2. Surgeonfish
        string surgeonPrefabPath = "Assets/Prefabs/Fish/Surgeonfish.prefab";
        string surgeonTexturePath = "Assets/FishSprite_BlueTang.png";
        
        // 3. Clownfish 1
        string clown1PrefabPath = "Assets/Prefabs/Fish/Clownfish 1.prefab";
        string clown1TexturePath = "Assets/FishSprite_Pufferfish.png"; // Pufferfish texture acts as image target for Clownfish 1

        SetupFishTarget("ImageTarget_Angelfish", "emperor_angelfish", angelPrefabPath, angelTexturePath, "EmperorAngelfish_swim1", typeImageTarget, typeObserver);
        SetupFishTarget("ImageTarget_Surgeonfish", "surgeonfish", surgeonPrefabPath, surgeonTexturePath, "Surgeonfish", typeImageTarget, typeObserver);
        SetupFishTarget("ImageTarget_Clownfish1", "clownfish_1", clown1PrefabPath, clown1TexturePath, "Clownfish 1", typeImageTarget, typeObserver);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Successfully setup 3 new Fish Image Targets and children!");
    }

    private static void SetupFishTarget(string targetObjName, string trackableName, string prefabPath, string texturePath, string childGameObjectName, Type itType, Type obsType)
    {
        // 1. Find or create the ImageTarget GameObject
        GameObject targetObj = GameObject.Find(targetObjName);
        if (targetObj == null)
        {
            targetObj = new GameObject(targetObjName);
            Undo.RegisterCreatedObjectUndo(targetObj, "Create " + targetObjName);
        }

        // Add MeshFilter and MeshRenderer to prevent Vuforia Editor errors (it expects a Renderer on the ImageTarget GameObject)
        MeshFilter mf = targetObj.GetComponent<MeshFilter>();
        if (mf == null)
        {
            mf = targetObj.AddComponent<MeshFilter>();
        }
        if (mf.sharedMesh == null)
        {
            GameObject tempQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            mf.sharedMesh = tempQuad.GetComponent<MeshFilter>().sharedMesh;
            GameObject.DestroyImmediate(tempQuad);
        }
        MeshRenderer mr = targetObj.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = targetObj.AddComponent<MeshRenderer>();
        }
        mr.enabled = false; // Always disable the MeshRenderer of the ImageTarget itself to prevent rendering the white quad

        // 2. Add or configure ImageTargetBehaviour
        Component itBehaviour = targetObj.GetComponent(itType);
        if (itBehaviour == null)
        {
            itBehaviour = targetObj.AddComponent(itType);
        }

        // Use SerializedObject to modify private/serialized fields of Vuforia script safely
        SerializedObject so = new SerializedObject(itBehaviour);
        so.Update();
        
        SerializedProperty propTrackableName = so.FindProperty("mTrackableName");
        if (propTrackableName != null) propTrackableName.stringValue = trackableName;

        SerializedProperty propType = so.FindProperty("mImageTargetType");
        if (propType != null) propType.intValue = 3; // Instant target type

        SerializedProperty propInit = so.FindProperty("mInitializedInEditor");
        if (propInit != null) propInit.boolValue = true;

        // Load the texture
        Texture2D targetTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (targetTex != null)
        {
            SerializedProperty propTexture = so.FindProperty("mRuntimeTexture");
            if (propTexture != null) propTexture.objectReferenceValue = targetTex;

            float aspect = (float)targetTex.width / targetTex.height;
            SerializedProperty propAspect = so.FindProperty("mAspectRatio");
            if (propAspect != null) propAspect.floatValue = aspect;

            SerializedProperty propWidth = so.FindProperty("mWidth");
            if (propWidth != null) propWidth.floatValue = aspect * 0.25f;

            SerializedProperty propHeight = so.FindProperty("mHeight");
            if (propHeight != null) propHeight.floatValue = 0.25f;
        }
        else
        {
            Debug.LogWarning("Target texture not found at: " + texturePath);
        }

        so.ApplyModifiedProperties();

        // 3. Add or configure DefaultObserverEventHandler
        if (obsType != null)
        {
            Component observer = targetObj.GetComponent(obsType);
            if (observer == null)
            {
                targetObj.AddComponent(obsType);
            }
        }

        // 4. Handle child fish model prefab
        // Remove existing child objects first if any
        for (int i = targetObj.transform.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(targetObj.transform.GetChild(i).gameObject);
        }

        // Load and instantiate prefab
        GameObject fishPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (fishPrefab != null)
        {
            GameObject fishInstance = (GameObject)PrefabUtility.InstantiatePrefab(fishPrefab, targetObj.transform);
            fishInstance.name = childGameObjectName; // CRITICAL: must match exact name expected by CacheFishRoots
            fishInstance.transform.localPosition = new Vector3(0, 0.05f, 0);
            fishInstance.transform.localRotation = Quaternion.identity;
            fishInstance.transform.localScale = Vector3.one * 3.5f; // reasonable scale for AR aquarium
            
            // Register for undo
            Undo.RegisterCreatedObjectUndo(fishInstance, "Instantiate " + childGameObjectName);
            Debug.Log("Created Target " + targetObjName + " with child fish: " + childGameObjectName);
        }
        else
        {
            Debug.LogError("Fish prefab not found at path: " + prefabPath);
        }
    }

    [MenuItem("Tools/Fix Existing Image Targets")]
    public static void FixExistingTargets()
    {
        System.Type typeImageTarget = FindTypeInAssemblies("Vuforia.ImageTargetBehaviour");
        System.Type typeObserver = FindTypeInAssemblies("Vuforia.DefaultObserverEventHandler");
        if (typeObserver == null)
        {
            typeObserver = FindTypeInAssemblies("Vuforia.DefaultTrackableEventHandler");
        }

        if (typeImageTarget == null || typeObserver == null)
        {
            Debug.LogError("Vuforia types not found!");
            return;
        }

        var targets = GameObject.FindObjectsOfType(typeImageTarget);
        int fixedCount = 0;
        foreach (var target in targets)
        {
            GameObject targetObj = ((MonoBehaviour)target).gameObject;
            Component observer = targetObj.GetComponent(typeObserver);
            if (observer == null)
            {
                targetObj.AddComponent(typeObserver);
                fixedCount++;
                Debug.Log("Added " + typeObserver.Name + " to " + targetObj.name);
            }
        }

        Debug.Log("Successfully verified/fixed " + targets.Length + " targets. Added missing event handlers to " + fixedCount + " of them.");
    }

    private static Type FindTypeInAssemblies(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = assembly.GetType(typeName);
            if (t != null)
            {
                return t;
            }
        }
        return null;
    }
}
