using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MysteryBox))]
// REMOVED CanEditMultipleObjects to force Unity to only look at ONE at a time
public class MysteryBoxEditor : Editor
{
    private int testIndex = 0;
    private int testRarity = 0;

    public override void OnInspectorGUI()
    {
        // Force the inspector to ONLY talk to the object you clicked on
        MysteryBox box = target as MysteryBox;
        if (box == null) return;

        // Draw the fields. 
        // If changing these still affects others, then your objects are still linked in Unity's memory.
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("--- Preview Tool ---", EditorStyles.boldLabel);

        testIndex = EditorGUILayout.IntField("Weapon Index", testIndex);
        testRarity = EditorGUILayout.IntSlider("Rarity", testRarity, 0, 3);

        if (GUILayout.Button("Show Preview"))
        {
            ShowPreview(box);
        }

        if (GUILayout.Button("Clear Preview"))
        {
            ClearPreview(box);
        }

        // Final safety: ensures Unity doesn't try to 'broadcast' this change
        if (GUI.changed)
        {
            EditorUtility.SetDirty(box);
        }
    }

    private void ShowPreview(MysteryBox box)
    {
        ClearPreview(box);

        if (box.weaponSettings == null || testIndex >= box.weaponSettings.Count) return;

        var settings = box.weaponSettings[testIndex];
        if (settings == null || settings.prefab == null) return;

        // Instantiate specifically as a child of THIS box's spawn point
        GameObject preview = (GameObject)Instantiate(settings.prefab, box.weaponSpawnPoint);
        preview.name = "DEBUG_PREVIEW";

        // Hide it from the "scene save" so it doesn't get stuck there
        preview.hideFlags = HideFlags.DontSave;

        Weapon w = preview.GetComponent<Weapon>();
        if (w != null) w.SetRarity(testRarity);

        bool isAkimbo = (w != null && w.isAkimbo);

        if (isAkimbo)
        {
            preview.transform.localPosition = settings.akimboOffset;
            preview.transform.localRotation = Quaternion.Euler(settings.akimboRotation);
            preview.transform.localScale = Vector3.one * (settings.akimboScale <= 0 ? 1f : settings.akimboScale);
        }
        else
        {
            preview.transform.localPosition = settings.offset;
            preview.transform.localRotation = Quaternion.Euler(settings.rotation);
            preview.transform.localScale = Vector3.one * (settings.scale <= 0 ? 1f : settings.scale);
        }
    }

    private void ClearPreview(MysteryBox box)
    {
        if (box.weaponSpawnPoint == null) return;

        // Use a loop that only targets children of THIS specific spawn point
        for (int i = box.weaponSpawnPoint.childCount - 1; i >= 0; i--)
        {
            Transform child = box.weaponSpawnPoint.GetChild(i);
            if (child.name == "DEBUG_PREVIEW")
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }
}