using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PreviewWindow : SceneView
{
    public UnityEngine.Object selectedObj;
    public Scene sceneLoaded;
 
    [MenuItem("Assets/Preview/Preview Asset")]
    public static void ShowWindow()
    {
        if (Selection.objects.Length > 1)
        {
            Debug.LogError("Your selection must include a single object.");
            return;
        }
        else if (Selection.objects.Length <= 0)
        {
            Debug.LogError("No object selected to preview.");
            return;
        }
 
        if (Selection.activeGameObject == null)
        {
            Debug.LogError("No Game Objects selected, only GameObjects/Prefabs are supported now");
            return;
        }
 
        // Create the window
        PreviewWindow window = CreateWindow<PreviewWindow>("Preview");
 
        // Get the object you're selecting in the Unity Editor
        window.selectedObj = Selection.activeObject;
        window.titleContent = window.GetName();
 
        // Load a new preview scene
        scene = EditorSceneManager.NewPreviewScene();
     
        window.sceneLoaded = scene;
        window.sceneLoaded.name = window.name;
        window.customScene = window.sceneLoaded;
 
        window.drawGizmos = false;
 
        window.SetupScene();
 
        window.Repaint();
    }
 
    private static Scene scene;
 
    public override void OnEnable()
    {
        base.OnEnable();
 
        // Set title name
        titleContent = GetName();
    }
 
    public override void OnDisable()
    {
        base.OnDisable();
    }
 
    private new void OnDestroy()
    {
        base.OnDestroy();
    }
 
    void SetupScene()
    {
        // Create lighting
        GameObject lightingObj = new GameObject("Lighting");
        lightingObj.transform.eulerAngles = new Vector3(50, -30, 0);
        lightingObj.AddComponent<Light>().type = UnityEngine.LightType.Directional;
 
        // Create the object we're selecting
        GameObject obj = GameObject.Instantiate(selectedObj as GameObject);
 
        // Move the objects to the preview scene
        EditorSceneManager.MoveGameObjectToScene(obj, customScene);
        EditorSceneManager.MoveGameObjectToScene(lightingObj, customScene);
 
        Selection.activeObject = obj;
 
        // Zoom the scene view into the new object
        FrameSelected();
    }
 
    private GUIContent GetName()
    {
        if (selectedObj == null)
            return new GUIContent("NuLL");
 
        // Setup the title GUI Content (Image, Text, Tooltip options) for the window
        GUIContent titleContent = new GUIContent(selectedObj.name);
        if (selectedObj is GameObject)
        {
            titleContent.image = EditorGUIUtility.IconContent("GameObject Icon").image;
        }
        else if (selectedObj is SceneAsset)
        {
            titleContent.image = EditorGUIUtility.IconContent("SceneAsset Icon").image;
        }
 
        return titleContent;
    }
 
    new void OnGUI()
    {
        base.OnGUI();
    }
}
