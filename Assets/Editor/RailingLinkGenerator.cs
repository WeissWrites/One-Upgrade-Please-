using UnityEditor;
using UnityEngine;
using Unity.AI.Navigation;

// Window: Tools → Railing Link Generator
// Place one NavMeshLink anywhere in the scene to use as a template (sets width, area, etc.)
// Define the 4 corners of your railing's inner edge and click Generate.
public class RailingLinkGenerator : EditorWindow
{
    private Transform cornerNW;
    private Transform cornerNE;
    private Transform cornerSE;
    private Transform cornerSW;
    private float linkSpacing = 2f;
    private float dropBelow   = 0.1f; // how far below the railing edge the end point sits
    private float endYOffset  = -5f;  // Y offset of the link end point (on the floor)
    private float linkWidth   = 1.5f;
    private GameObject parentObject;

    [MenuItem("Tools/Railing Link Generator")]
    static void Open() => GetWindow<RailingLinkGenerator>("Railing Links");

    void OnGUI()
    {
        GUILayout.Label("Railing Inner Edge Corners", EditorStyles.boldLabel);
        GUILayout.Label("Assign 4 empty GameObjects at each inner corner of the railing.");

        cornerNW = (Transform)EditorGUILayout.ObjectField("Corner NW", cornerNW, typeof(Transform), true);
        cornerNE = (Transform)EditorGUILayout.ObjectField("Corner NE", cornerNE, typeof(Transform), true);
        cornerSE = (Transform)EditorGUILayout.ObjectField("Corner SE", cornerSE, typeof(Transform), true);
        cornerSW = (Transform)EditorGUILayout.ObjectField("Corner SW", cornerSW, typeof(Transform), true);

        EditorGUILayout.Space();
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        linkSpacing = EditorGUILayout.FloatField("Link Spacing (units)", linkSpacing);
        endYOffset  = EditorGUILayout.FloatField("End Point Y Offset (floor height)", endYOffset);
        linkWidth   = EditorGUILayout.FloatField("Link Width", linkWidth);

        EditorGUILayout.Space();
        parentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);

        EditorGUILayout.Space();

        bool cornersSet = cornerNW && cornerNE && cornerSE && cornerSW;
        GUI.enabled = cornersSet;

        if (GUILayout.Button("Generate Links"))
            GenerateLinks();

        if (GUILayout.Button("Clear Generated Links") && parentObject != null)
            ClearLinks();

        GUI.enabled = true;

        if (!cornersSet)
            EditorGUILayout.HelpBox("Assign all 4 corner Transforms to generate links.", MessageType.Info);
    }

    void GenerateLinks()
    {
        // Create a parent if not assigned
        if (parentObject == null)
        {
            parentObject = new GameObject("Railing NavMeshLinks");
            Undo.RegisterCreatedObjectUndo(parentObject, "Create Railing Links Parent");
        }

        Vector3[] corners = { cornerNW.position, cornerNE.position, cornerSE.position, cornerSW.position };

        // Walk each side: NW→NE, NE→SE, SE→SW, SW→NW
        for (int i = 0; i < 4; i++)
        {
            Vector3 from = corners[i];
            Vector3 to   = corners[(i + 1) % 4];
            SpawnLinksAlongEdge(from, to);
        }

        EditorUtility.SetDirty(parentObject);
    }

    void SpawnLinksAlongEdge(Vector3 from, Vector3 to)
    {
        float totalLength = Vector3.Distance(from, to);
        int   count       = Mathf.Max(1, Mathf.RoundToInt(totalLength / linkSpacing));

        for (int i = 0; i <= count; i++)
        {
            float t = (float)i / count;
            Vector3 pos = Vector3.Lerp(from, to, t);

            GameObject go = new GameObject("RailingLink");
            Undo.RegisterCreatedObjectUndo(go, "Railing Link");
            go.transform.SetParent(parentObject.transform);
            go.transform.position = pos;

            NavMeshLink link     = go.AddComponent<NavMeshLink>();
            link.startPoint      = Vector3.zero;
            link.endPoint        = new Vector3(0f, endYOffset, 0f);
            link.width           = linkWidth;
            link.bidirectional   = false; // one-way drop only
            link.autoUpdate      = true;
        }
    }

    void ClearLinks()
    {
        Undo.RegisterFullObjectHierarchyUndo(parentObject, "Clear Railing Links");
        while (parentObject.transform.childCount > 0)
            DestroyImmediate(parentObject.transform.GetChild(0).gameObject);
    }
}
