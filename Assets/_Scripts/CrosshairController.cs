using UnityEngine;
using UnityEngine.UI;

public class CrosshairController : MonoBehaviour
{
    public RectTransform top, bottom, left, right;

    [Header("Settings")]
    public float restingSize = 10f;     // The minimum gap
    public float spreadMultiplier = 500f; // How much the spread affects the UI
    public float speed = 10f;           // How fast the Crosshair snaps

    private float currentSize;

    public void UpdateCrosshair(float spreadIntensity)
    {
        float targetSize = restingSize + (spreadIntensity * spreadMultiplier);
        currentSize = Mathf.Lerp(currentSize, targetSize, Time.deltaTime * speed);
        ApplyCrosshairSize();
    }

    public void SnapCrosshair(float spreadIntensity)
    {
        currentSize = restingSize + (spreadIntensity * spreadMultiplier);
        ApplyCrosshairSize();
    }

    private void ApplyCrosshairSize()
    {
        top.localPosition = new Vector3(0, currentSize, 0);
        bottom.localPosition = new Vector3(0, -currentSize, 0);
        left.localPosition = new Vector3(-currentSize, 0, 0);
        right.localPosition = new Vector3(currentSize, 0, 0);
    }
}