using UnityEngine;

public class CameraRecoil : MonoBehaviour
{
    public static CameraRecoil Instance { get; private set; }

    [Header("Weapon Recoil")]
    public float recoilAmount = 5f;
    public float snappiness = 10f;
    public float returnSpeed = 5f;

    [Header("Hit Shake")]
    public float hitShakeAmount = 0.05f;
    public float hitShakeSnappiness = 15f;
    public float hitShakeReturnSpeed = 8f;

    private Vector3 currentRotation;
    private Vector3 targetRotation;

    private Vector3 currentShake;
    private Vector3 targetShake;
    private Vector3 initialLocalPos;

    public Vector3 RecoilOffset => currentRotation;

    void Awake()
    {
        Instance = this;
        initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        targetRotation = Vector3.Lerp(targetRotation, Vector3.zero, returnSpeed * Time.deltaTime);
        currentRotation = Vector3.Slerp(currentRotation, targetRotation, snappiness * Time.deltaTime);

        targetShake = Vector3.Lerp(targetShake, Vector3.zero, hitShakeReturnSpeed * Time.deltaTime);
        currentShake = Vector3.Slerp(currentShake, targetShake, hitShakeSnappiness * Time.deltaTime);

        transform.localPosition = initialLocalPos + currentShake;
    }

    public void TriggerRecoil()
    {
        targetRotation += new Vector3(-recoilAmount, Random.Range(-recoilAmount * 0.5f, recoilAmount * 0.5f), 0);
    }

    public void TriggerHitShake()
    {
        targetShake += new Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            0f) * hitShakeAmount;
    }
}