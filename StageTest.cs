using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public class StageTest : PreviewSceneStage
{
    [MenuItem("stages/test")]
    static void CreateWindow()
    {
        var inst = CreateInstance<StageTest>();
        inst.scene = EditorSceneManager.NewPreviewScene();
        var sceneView = SceneView.lastActiveSceneView;
        inst.OnFirstTimeOpenStageInSceneView(sceneView);
        StageUtility.GoToStage(inst, true);
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sceneView.camera.transform.position = new Vector3(10f, 0f, 0f);
        sceneView.camera.transform.rotation = Quaternion.identity;
        StageUtility.PlaceGameObjectInCurrentStage(cube);
    }

    protected override GUIContent CreateHeaderContent()
    {
        return new GUIContent("Test");
    }
 
    protected override void OnCloseStage()
    {
        base.OnCloseStage();
        Debug.Log("Bye Stage");
    }
}

public class CustomSceneView : SceneView
{
    // public static StageTest stageTest;

    private static Scene scene;

    private Scene sceneLoaded;

    // [MenuItem("stages/test")]
    static void CreateWindow()
    {
        CustomSceneView window = (CustomSceneView)GetWindow(typeof(CustomSceneView));

        scene = EditorSceneManager.NewPreviewScene();
        window.sceneLoaded = scene;
        window.customScene = window.sceneLoaded;

        window.drawGizmos = false;

        window.SetupScene();
 
        window.Repaint();
    }

    protected override void OnSceneGUI()
    {
        base.OnSceneGUI();
        GUILayout.Button("Custom Scene View Button");
        // Debug.Log(camera);
    }

    public override void OnEnable()
    {
        base.OnEnable();
    }
 
    public override void OnDisable()
    {
        base.OnDisable();
    }
 
    private new void OnDestroy()
    {
        base.OnDestroy();
    }

    private void SetupScene()
    {
        GameObject lightingObj = new GameObject("Lighting");
        lightingObj.transform.eulerAngles = new Vector3(0, -30, 0);
        lightingObj.AddComponent<Light>().type = UnityEngine.LightType.Directional;
 
        GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
 
        // EditorSceneManager.MoveGameObjectToScene(obj, customScene);
        EditorSceneManager.MoveGameObjectToScene(lightingObj, customScene);
    }
}
