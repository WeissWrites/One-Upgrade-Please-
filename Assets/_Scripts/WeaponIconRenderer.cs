using UnityEngine;
using System.Collections;

public class WeaponIconRenderer : MonoBehaviour
{
    public static WeaponIconRenderer Instance { get; private set; }

    [Header("Icon Cameras (one per slot, positioned off-screen)")]
    public Camera[] iconCameras = new Camera[3];

    [Header("RenderTextures (256x256, one per slot)")]
    public RenderTexture[] renderTextures = new RenderTexture[3];

    [Header("White-texture display prefabs (13, same order as weaponSettings)")]
    public GameObject[] iconPrefabs;

    [System.Serializable]
    public class WeaponIconOverride
    {
        public string label;

        [Header("Normal")]
        public Vector3 positionOffset = Vector3.zero;
        public Vector3 rotation = new Vector3(0f, 90f, 0f);
        public float scale = 1f;

        [Header("Akimbo")]
        public Vector3 akimboPositionOffset = Vector3.zero;
        public Vector3 akimboRotation = new Vector3(0f, 90f, 0f);
        public float akimboScale = 1f;
    }

    [Header("Per-Weapon Overrides (13 entries, same order as Icon Prefabs)")]
    public WeaponIconOverride[] overrides = new WeaponIconOverride[13];

    private GameObject[] activeModels = new GameObject[3];

    void Awake()
    {
        Instance = this;
        for (int i = 0; i < iconCameras.Length; i++)
            if (iconCameras[i] != null && i < renderTextures.Length && renderTextures[i] != null)
                iconCameras[i].targetTexture = renderTextures[i];
    }

    public void SetSlot(int slot, int weaponIndex, int rarity)
    {
        if (slot < 0 || slot >= activeModels.Length) return;
        ClearSlot(slot);
        if (iconPrefabs == null || weaponIndex < 0 || weaponIndex >= iconPrefabs.Length) return;
        if (iconPrefabs[weaponIndex] == null || iconCameras[slot] == null) return;
        StartCoroutine(SpawnIcon(slot, weaponIndex, rarity));
    }

    private IEnumerator SpawnIcon(int slot, int weaponIndex, int rarity)
    {
        Camera cam = iconCameras[slot];
        WeaponIconOverride ov = GetOverride(weaponIndex);

        // Spawn hidden at camera forward so bounds are valid after one frame
        GameObject model = Instantiate(iconPrefabs[weaponIndex],
            cam.transform.position + cam.transform.forward * 1.5f,
            Quaternion.Euler(ov.rotation));

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            r.enabled = false;

        Weapon w = model.GetComponent<Weapon>();
        if (w != null) { w.SetRarity(rarity); w.enabled = false; }

        yield return null;

        if (activeModels[slot] != null && activeModels[slot] != model) { Destroy(model); yield break; }

        // Pick normal vs akimbo layout
        bool akimbo = w != null && w.isAkimbo;
        Vector3 posOffset = akimbo ? ov.akimboPositionOffset : ov.positionOffset;
        Vector3 rot       = akimbo ? ov.akimboRotation       : ov.rotation;
        float   scl       = akimbo ? ov.akimboScale           : ov.scale;

        model.transform.rotation = Quaternion.Euler(rot);
        model.transform.localScale = Vector3.one * scl;
        model.transform.position = cam.transform.position + cam.transform.forward * 1.5f + posOffset;

        foreach (Renderer r in model.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        // Auto-fit FOV to frame the weapon
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            float dist = Vector3.Distance(cam.transform.position, bounds.center);
            float halfSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (dist > 0.001f && halfSize > 0.001f)
                cam.fieldOfView = 2f * Mathf.Atan(halfSize / dist) * Mathf.Rad2Deg * 1.15f;
        }

        activeModels[slot] = model;
    }

    WeaponIconOverride GetOverride(int index)
    {
        if (overrides != null && index >= 0 && index < overrides.Length && overrides[index] != null)
            return overrides[index];
        return new WeaponIconOverride();
    }

    public void ClearSlot(int slot)
    {
        if (slot < 0 || slot >= activeModels.Length) return;
        if (activeModels[slot] != null) Destroy(activeModels[slot]);
        activeModels[slot] = null;
    }

    public RenderTexture[] GetTextures() => renderTextures;
}
