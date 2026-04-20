using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 15f;
    public float gravity = -40f;
    public float jumpHeight = 2.5f;

    [Header("Dash Settings")]
    public float dashDistance = 10f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 3f;
    private float nextDashTime = 0f;
    private bool isDashing;

    [Header("Look Settings")]
    public float mouseSensitivity = 2f;
    public Transform playerCamera;
    private float xRotation = 0f;
    private float sensitivityDivisor = 1f;

    public void SetSensitivityDivisor(float divisor) => sensitivityDivisor = Mathf.Max(1f, divisor);

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {

        HandleRotation();
        if (isDashing) return;

        HandleMovement();
        HandleJumpAndGravity();

        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= nextDashTime)
        {
            StartCoroutine(Dash());
            nextDashTime = Time.time + dashCooldown;
        }
    }

    void HandleRotation()
    {
        float mouseX = Input.GetAxis("Mouse X") * (mouseSensitivity / sensitivityDivisor * 100) * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * (mouseSensitivity / sensitivityDivisor * 100) * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * walkSpeed * Time.deltaTime);
    }

    void HandleJumpAndGravity()
    {
        if (Input.GetButton("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    System.Collections.IEnumerator Dash()
    {
        isDashing = true;

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = transform.forward * z + transform.right * x;
        if (moveDir == Vector3.zero) moveDir = transform.forward;

        Vector3 fixedDashDir = moveDir.normalized;

        float startTime = Time.time;

        while (Time.time < startTime + dashDuration)
        {
            controller.Move(fixedDashDir * (dashDistance / dashDuration) * Time.deltaTime);
            yield return null;
        }

        isDashing = false;
    }
}