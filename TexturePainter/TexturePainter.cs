using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;

public class TexturePainter : EditorWindow
{
    Texture2D targetTex;
    Mesh targetMesh;
    GameObject targetGameObject;
    RenderTexture targetRT;
    Material previewMat;
    Shader previewShader;
    Material paintMat;
    Shader paintShader;

    private static PreviewRenderUtility previewRenderUtility;

    private TexturePainterStage stage;
    private ObjectField targetTexField;
    private ToolbarToggle paintModeField;
    private IMGUIContainer viewField;
    private Texture viewImage;
    private Material quadMat;
    private float currentScale = 1f;
    private ToolbarToggle paintToggleField;
    private Texture2D[] brushesTex;
    private string[] brushName;
    private RaycastHit paintHit;

    [MenuItem("Tools/Texture Painter")]
    public static void InitWindow()
    {
        TexturePainter window = (TexturePainter)GetWindow(typeof(TexturePainter));

        //Init 2D View Window
        previewRenderUtility = new PreviewRenderUtility();
        var cam = previewRenderUtility.camera;
        cam.farClipPlane = 1000f;
        cam.clearFlags = CameraClearFlags.Nothing;
        cam.orthographic = true;
        cam.orthographicSize = 0.5f;
        cam.aspect = 1f;
        cam.transform.position = new Vector3(0f, 0f, -10f);

    }

    void OnEnable()
    {
        SetUp();

        var root = this.rootVisualElement;

        targetTexField = new ObjectField("Texture")
        {
            objectType = typeof(Texture2D)
        };
        targetTexField.RegisterValueChangedCallback((e)=>
        {
            targetTex = (Texture2D)e.newValue;
            RefreshTargetRT();
        });

        paintModeField = new ToolbarToggle()
        {
            text = "Paint Mode"
        };
        paintModeField.RegisterValueChangedCallback((e)=>
        {
            if(e.newValue)
            {
                EnablePaintMode();
            }
            else
            {
                DisablePaintMode();
            }
        });

        viewField = new IMGUIContainer(Update2DView)
        {
            style =
            {
                width = 512,
                height = 512
            }
        };

        root.Add(targetTexField);
        root.Add(paintModeField);
        root.Add(viewField);
    }

    void OnDisable()
    {
        DisablePaintMode();

        previewRenderUtility.Cleanup();
    }

    void OnSelectionChange()
    {
        RefreshTargetMesh();
    }

    public void OnInspectorUpdate()
    {
        viewField.MarkDirtyRepaint();
    }

    public void ResetPaintModeFieldValue(bool value)
    {
        paintModeField.value = value;
    }

    private void SetUp()
    {
        LoadBrushes();

        targetTex = Texture2D.whiteTexture;
        // targetGameObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        // if(targetGameObject.TryGetComponent<MeshFilter>(out MeshFilter filter))
        // {
        //     targetMesh = filter.sharedMesh;
        // }
        previewShader = Shader.Find("Tools/TexturePainter/3DViewPreview");
        previewMat = new Material(previewShader);
        paintShader = Shader.Find("Tools/TexturePainter/Paint");
        paintMat = new Material(paintShader);
        previewMat.SetTexture("_MainTex", targetRT);
        viewImage = Texture2D.whiteTexture;
        quadMat = new Material(Shader.Find("Unlit/Texture"));
    }

    private void RefreshTargetRT()
    {
        if(targetTex != null)
        {
            int targetTexWidth = targetTex.width;
            int targetTexHeight = targetTex.height;
            targetRT = new RenderTexture(targetTexWidth, targetTexHeight, 32, (RenderTextureFormat)targetTex.format);
            Graphics.Blit(targetTex, targetRT);
        }
    }

    private void RefreshTargetMesh()
    {
        if(Selection.activeGameObject == null)
        {
            Debug.Log("No Selection");
        }
        else
        {
            GameObject seleciton = Selection.activeGameObject;
            if(seleciton.TryGetComponent<MeshFilter>(out MeshFilter meshFilter))
            {
                targetGameObject = seleciton;
                if(targetMesh != meshFilter.sharedMesh)
                {
                    targetMesh = meshFilter.sharedMesh;
                }
            }
        }
    }

    private void Update2DView()
    {
        quadMat.SetTexture("_MainTex", targetRT);
        Rect rect = new Rect(0, 0, 512, 512);
        Mesh quad = GetPrimitiveMesh(PrimitiveType.Quad);
        Camera cam = previewRenderUtility.camera;
        InteractiveControl(ref cam, rect, ref currentScale);
        previewRenderUtility.BeginPreview(rect, GUIStyle.none);
        previewRenderUtility.DrawMesh(quad, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(currentScale, currentScale, currentScale)), quadMat, 0);
        previewRenderUtility.camera.Render();
        viewImage = previewRenderUtility.EndPreview();
        GUI.DrawTexture(rect, viewImage);
        
    }

    private void EnablePaintMode()
    {
        if(targetGameObject != null && targetMesh != null)
        {
            stage = ScriptableObject.CreateInstance<TexturePainterStage>();
            stage.mesh = targetMesh;
            stage.editorWindow = this;
            stage.GoToStage(ref previewMat, ref targetRT);
        }
        else
        {
            Debug.Log("No Selection");
        }

        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void DisablePaintMode()
    {
        if(stage != null)
        {
            if(stage.isInStage)
            {
                StageUtility.GoBackToPreviousStage();
            }
        }

        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        
        Get3DViewControlHandle();
    }

    private void CleanSceneGUI()
    {
        SceneView sceneview = SceneView.lastActiveSceneView;
        var root = sceneview.rootVisualElement;

        root.Remove(paintToggleField);
    }

    private void InteractiveControl(ref Camera cam, Rect rect, ref float scale)
    {
        Vector2 panningDelta = Vector2.zero;
        Vector2 paintPos = Vector2.negativeInfinity;
        Get2DViewControlHandle(rect, ref panningDelta, ref paintPos, ref scale);
        var camTransform = cam.transform;
        camTransform.position += -camTransform.right * panningDelta.x + camTransform.up * panningDelta.y;
    }

    private void Get2DViewControlHandle(Rect rect, ref Vector2 panningDelta, ref Vector2 paintPos, ref float scale)
    {
        var controlID = GUIUtility.GetControlID("TexturePainter2DViewControl".GetHashCode(), FocusType.Passive);
        var e = Event.current;
        switch(e.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if(rect.Contains(e.mousePosition))
                {
                    GUIUtility.hotControl = controlID;
                    e.Use();
                    EditorGUIUtility.SetWantsMouseJumping(1);
                }
                break;
            case EventType.MouseUp:
                if(GUIUtility.hotControl == controlID)
                {
                    GUIUtility.hotControl = 0;
                    EditorGUIUtility.SetWantsMouseJumping(0);
                }
                break;
            case EventType.MouseDrag:
                if(GUIUtility.hotControl == controlID)
                {
                    if(e.button == 0)//left click and drag to paint
                    {
                        //paint
                    }
                    if(e.button == 1)//right click and drag to panning
                    {
                        panningDelta = e.delta * 0.001f;
                    }
                }
                break;
            case EventType.ScrollWheel://scroll wheel to scale
                if(rect.Contains(e.mousePosition))
                {
                    scale /= Mathf.Pow(2f, e.delta.y * 0.05f);
                }
            break;
        }
    }

    private void Get3DViewControlHandle()
    {
        Event e = Event.current;
        if(e.button == 0 && e.type == EventType.MouseDrag)
        {
            Debug.Log("3DViewPaint");
            Camera cam = SceneView.lastActiveSceneView.camera;
            Vector3 mouseScreenPos = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mouseScreenPos);
            Physics.Raycast(ray, out paintHit, 1000f);
            if(paintHit.transform.gameObject == targetGameObject)
            {
                Vector3 hitNormal = paintHit.normal;
                Vector3 hitPoint = paintHit.point;
                CommandBuffer cb = new CommandBuffer();
                cb.DrawMesh(targetMesh, Matrix4x4.identity, paintMat, 0, 0);
                
            }
        }
    }

    private Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        GameObject.DestroyImmediate(gameObject);

        return mesh;
    }

    private void LoadBrushes()
    {
        string[] path = AssetDatabase.FindAssets("t:script TexturePainter");
        var thisScriptPath = AssetDatabase.GUIDToAssetPath(path[0]);
        Debug.Log(thisScriptPath);
        Debug.Log(thisScriptPath.Replace("TexturePainter.cs", "Brushes"));
        var GUIDs = AssetDatabase.FindAssets("t:texture2D", new[]{thisScriptPath.Replace("/TexturePainter.cs", "/")});
        Debug.Log(GUIDs);
        ArrayList brushList = new ArrayList();
        brushesTex = new Texture2D[GUIDs.Length];
        Debug.Log(brushesTex[0]);
        brushName = new string[GUIDs.Length];
        for(int i = 0; i < GUIDs.Length; i++)
        {
            string brushTexPath = AssetDatabase.GUIDToAssetPath(GUIDs[i]);
            string assetPath = GetAssetPath(brushTexPath);
            Texture2D brush = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if(brush != null)
            {
                if(brush.width != brush.height && brush.width != 64)
                {
                    Debug.Log("Brush " + brush.name + " 的大小请改为 64！");
                }
                else
                {
                    brushList.Add(brush);
                    brushName[i] = brush.ToString();

                    TextureImporter ti = (TextureImporter)TextureImporter.GetAtPath(AssetDatabase.GetAssetPath(brush));
                    ti.isReadable = true;
                    AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(brush));
                }
            }
        }
        brushesTex = brushList.ToArray(typeof(Texture2D)) as Texture2D[];
    }

    private string GetAssetPath(string path)
    {
        int idx = path.IndexOf("Assets");
        string assetPath = path.Substring(idx, path.Length - idx);
        return assetPath;
    }

}
