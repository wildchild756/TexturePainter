using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.SceneManagement;

public class TexturePainter : EditorWindow
{
    Texture2D targetTex;
    Mesh targetMesh;
    RenderTexture targetRT;
    Material previewMat;
    Shader previewShader;

    private static PreviewRenderUtility previewRenderUtility;

    private ObjectField targetTexField;
    private ToolbarToggle paintModeField;
    private IMGUIContainer viewField;
    private Texture viewImage;
    private Material quadMat;
    private float currentScale = 1f;
    private TexturePainterStage stage;
    private ToolbarToggle paintToggleField;

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
            Refresh();
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
        targetTex = Texture2D.whiteTexture;
        targetMesh = GetPrimitiveMesh(PrimitiveType.Cube);
        previewShader = Shader.Find("Tools/TexturePainter/3DViewPreview");
        previewMat = new Material(previewShader);
        previewMat.SetTexture("_MainTex", targetRT);
        viewImage = Texture2D.whiteTexture;
        quadMat = new Material(Shader.Find("Unlit/Texture"));
    }

    private void Refresh()
    {
        if(targetTex != null)
        {
            int targetTexWidth = targetTex.width;
            int targetTexHeight = targetTex.height;
            targetRT = new RenderTexture(targetTexWidth, targetTexHeight, 32, (RenderTextureFormat)targetTex.format);
            Graphics.Blit(targetTex, targetRT);
        }
    }

    private void Update2DView()
    {
        quadMat.SetTexture("_MainTex", targetRT);
        Mesh quad = GetPrimitiveMesh(PrimitiveType.Quad);
        Rect rect = new Rect(0, 0, 512, 512);
        Camera cam = previewRenderUtility.camera;
        CameraControl(ref cam, rect, ref currentScale);
        previewRenderUtility.BeginPreview(rect, GUIStyle.none);
        previewRenderUtility.DrawMesh(quad, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(currentScale, currentScale, currentScale)), quadMat, 0);
        previewRenderUtility.camera.Render();
        viewImage = previewRenderUtility.EndPreview();
        GUI.DrawTexture(rect, viewImage);
        
    }

    private void EnablePaintMode()
    {
        if(Selection.activeGameObject == null)
        {
            Debug.Log("No Selection");
        }
        else
        {
            GameObject seleciton = Selection.activeGameObject;
            MeshFilter meshFilter;
            if(seleciton.TryGetComponent<MeshFilter>(out meshFilter))
            {
                targetMesh = meshFilter.sharedMesh;
            }

        }
        stage = new TexturePainterStage(targetMesh);
        stage.editorWindow = this;
        stage.GoToStage(ref previewMat, ref targetRT);
        
        DrawSceneGUI();
    }

    private void DisablePaintMode()
    {
        CleanSceneGUI();
        
        StageUtility.GoBackToPreviousStage();

    }

    private void DrawSceneGUI()
    {
        SceneView sceneview = SceneView.lastActiveSceneView;
        var root = sceneview.rootVisualElement;
        
        paintToggleField = new ToolbarToggle()
        {
            text = "Paint"
        };
        paintToggleField.RegisterValueChangedCallback((e)=>
        {
            if(e.newValue)
            {
                StartPaint();
            }
            else
            {
                EndPaint();
            }
        });

        root.Add(paintToggleField);
    }

    private void CleanSceneGUI()
    {
        SceneView sceneview = SceneView.lastActiveSceneView;
        var root = sceneview.rootVisualElement;

        root.Remove(paintToggleField);
    }

    private void CameraControl(ref Camera cam, Rect rect, ref float scale)
    {
        Vector2 panningDelta = Vector2.zero;
        Vector2 paintPos = Vector2.negativeInfinity;
        GetControlHandle(rect, ref panningDelta, ref paintPos, ref scale);
        var camTransform = cam.transform;
        camTransform.position += -camTransform.right * panningDelta.x + camTransform.up * panningDelta.y;
    }

    private void GetControlHandle(Rect rect, ref Vector2 panningDelta, ref Vector2 paintPos, ref float scale)
    {
        var controlID = GUIUtility.GetControlID("TexturePainter 2DView Control".GetHashCode(), FocusType.Passive);
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

    private Mesh GetPrimitiveMesh(PrimitiveType type)
    {
        GameObject gameObject = GameObject.CreatePrimitive(type);
        Mesh mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
        GameObject.DestroyImmediate(gameObject);

        return mesh;
    }

    private void StartPaint()
    {
        Debug.Log("Start Paint");
    }

    private void EndPaint()
    {
        Debug.Log("End Paint");
    }
}
