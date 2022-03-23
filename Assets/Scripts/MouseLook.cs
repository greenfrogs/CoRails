using UnityEngine;

public class MouseLook : MonoBehaviour {
    public float mouseSensitivty = 100f;
    public Transform playerBody;

    private float xRotation;

    private void Start() {
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivty * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivty * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }
}