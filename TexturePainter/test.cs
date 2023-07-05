using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class test : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        string[] path = AssetDatabase.FindAssets("t:script test");
        var thisScriptPath = AssetDatabase.GUIDToAssetPath(path[0]);
        Debug.Log(thisScriptPath);
        Debug.Log(thisScriptPath.Replace("test.cs", "Brushes"));
        var GUIDs = AssetDatabase.FindAssets("t:texture2D", new[]{thisScriptPath.Replace("test.cs", "Brushes")});
        Debug.Log(GUIDs);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
