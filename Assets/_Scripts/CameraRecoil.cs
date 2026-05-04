using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    [Header("Settings")]
    public float recoilAmount = 5f;
    public float snappiness = 10f;
    public float returnSpeed = 5f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    // This is what the movement script will read
    public Vector3 RecoilOffset => currentRotation;

    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.fixedDeltaTime);
    }

    public void TriggerRecoil()
    {
        // Add a vertical kick and a slight random horizontal tilt
        targetRotation += new Vector3(-recoilAmount, Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f), 0);
    }
}