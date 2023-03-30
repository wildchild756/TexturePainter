using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public class TexturePainterStage : PreviewSceneStage
{
    public Mesh mesh;
    public GameObject targetObj;
    public TexturePainter editorWindow;

    public TexturePainterStage(Mesh mesh)
    {
        this.mesh = mesh;
        scene = EditorSceneManager.NewPreviewScene();
        var sceneView = SceneView.lastActiveSceneView;
        OnFirstTimeOpenStageInSceneView(sceneView);
    }

    public void GoToStage(ref Material mat, ref RenderTexture targetRT)
    {
        StageUtility.GoToStage(this, true);
        targetObj = new GameObject(mesh.name);
        targetObj.transform.position = Vector3.zero;
        targetObj.transform.rotation = Quaternion.identity;
        var meshFilter = targetObj.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;
        targetObj.AddComponent<MeshCollider>();
        var meshRenderer = targetObj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = mat;
        meshRenderer.sharedMaterial.SetTexture("_MainTex", targetRT);
        StageUtility.PlaceGameObjectInCurrentStage(targetObj);
    }

    protected override GUIContent CreateHeaderContent()
    {
        return new GUIContent("TexturePainter:" + mesh.name);
    }

    protected override void OnCloseStage()
    {
        base.OnCloseStage();
        DestroyImmediate(targetObj);
        editorWindow.ResetPaintModeFieldValue(false);
    }
}
