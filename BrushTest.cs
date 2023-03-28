using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;

public class BrushTest : EditorWindow
{
    private static PreviewRenderUtility previewRenderUtility;

    public RenderTexture rt;
    public Material drawMat;
    public string shaderName = "Unlit/DrawShader";
    public Mesh mesh;
    public Texture result;


    [MenuItem("Tools/Brush")]
    public static void InitWindow()
    {
        BrushTest window = (BrushTest)GetWindow(typeof(BrushTest));

        previewRenderUtility = new PreviewRenderUtility();
        previewRenderUtility.camera.farClipPlane = 1000f;
        previewRenderUtility.camera.clearFlags = CameraClearFlags.Nothing;
        previewRenderUtility.camera.transform.position = new Vector3(0f, 0f, -10f);
    }

    public void OnEnable()
    {
        Init();

        var root = this.rootVisualElement;
        var button = new Button(()=>
        {
            OnBrush();
        })
        {
            text = "Brush"
        };

        var rtField = new ObjectField("RT")
        {
            objectType = typeof(RenderTexture)
        };
        rtField.RegisterValueChangedCallback((e)=>
        {
            rt = (RenderTexture)e.newValue;
        });

        var meshField = new ObjectField("Mesh")
        {
            objectType = typeof(Mesh)
        };
        meshField.RegisterValueChangedCallback((e)=>
        {
            mesh = (Mesh)e.newValue;
        });

        view = new IMGUIContainer(UpdateView)
        {
            style =
            {
                width = 512,
                height = 512
            }
        };

        var testLabel = new Label(result.ToString());

        root.Add(button);
        root.Add(rtField);
        root.Add(meshField);
        root.Add(view);
        root.Add(testLabel);
    }

    IMGUIContainer view;
    Vector3 viewCenter = Vector3.zero;

    public void OnInspectorUpdate()
    {
        view.MarkDirtyRepaint();
    }

    private void UpdateView()
    {
        if(mesh != null)
        {
            Rect rect = new Rect(0, 0, 512, 512);
            Camera cam = previewRenderUtility.camera;
            CameraControl(ref cam, rect);

            previewRenderUtility.BeginPreview(rect, GUIStyle.none);
            previewRenderUtility.DrawMesh(mesh, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one), drawMat, 0);
            previewRenderUtility.camera.Render();
            result = previewRenderUtility.EndPreview();
            GUI.Box(rect, result);
        }
    }

    private void Init()
    {
        rt = new RenderTexture(512, 512, 24, RenderTextureFormat.RFloat);
        drawMat = new Material(Shader.Find(shaderName));
        result = Texture2D.whiteTexture;
    }

    private void OnBrush()
    {
        Debug.Log("On Brush");
    }

    private void CameraControl(ref Camera cam, Rect rect)
    {
        Vector2 rotationDelta = Vector2.zero;
        Vector2 translateDelta = Vector2.zero;
        float distance = 0f;
        GetControlHandle(rect, ref rotationDelta, ref translateDelta, ref distance);
        
        var camTransform = cam.transform;
        camTransform.position += -camTransform.right * translateDelta.x + camTransform.up * translateDelta.y;
        viewCenter += -camTransform.right * translateDelta.x + camTransform.up * translateDelta.y;
        camTransform.position += Vector3.Normalize(camTransform.position - viewCenter) * distance;
        cam.transform.RotateAround(viewCenter, Vector3.up, rotationDelta.x);
        var camMat = cam.transform.localToWorldMatrix;
        cam.transform.RotateAround(viewCenter, new Vector3(camMat.m00, camMat.m10, camMat.m20), rotationDelta.y);
    }

    private void GetControlHandle(Rect rect, ref Vector2 rotationDelta, ref Vector2 translateDelta, ref float distance)
    {
        var controlID = GUIUtility.GetControlID("Control".GetHashCode(), FocusType.Passive);
        var e = Event.current;
        switch(e.GetTypeForControl(controlID))
        {
            case EventType.MouseDown:
                if(rect.Contains(e.mousePosition) && rect.width > 50f)
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
                }
                EditorGUIUtility.SetWantsMouseJumping(0);
                break;
            case EventType.MouseDrag:
                if(e.button == 1 && GUIUtility.hotControl == controlID)//right click and drag to rotation
                {
                    rotationDelta = e.delta;
                    e.Use();
                    GUI.changed = true;
                }
                if(e.button == 2 && GUIUtility.hotControl == controlID)// middle click and drag to translate
                {
                    translateDelta = e.delta * 0.01f;
                    e.Use();
                    GUI.changed = true;
                }
                break;
            case EventType.ScrollWheel:
                if(rect.Contains(e.mousePosition) && rect.width > 50f)//hover and scroll wheel to scale
                {
                    distance = e.delta.y * 0.1f;
                    e.Use();
                    GUI.changed = true;
                }
                break;
        }
    }

}
