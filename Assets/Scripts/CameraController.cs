using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class CameraController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] float horizontalSensitivity = 100f;
    [SerializeField] float verticalSensitivity = 100f;

    [Header("Invert Controls")]
    [SerializeField] bool invertHorizontal = false;
    [SerializeField] bool invertVertical = false;
    
    public Transform orientation;
    public Transform camHolder;

    private float xRotation = 0f;
    private float yRotation = 0f;
    
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        // Get input from mouse
        float mouseX = Input.GetAxis("Mouse X") * horizontalSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * verticalSensitivity * Time.deltaTime;

        // Get vertical rotation and clamp it
        xRotation -= mouseY * (invertVertical ? -1 : 1);
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Get horizontal rotation
        yRotation += mouseX * (invertHorizontal ? -1 : 1);

        // Rotate the camera holder vertically and horizontally
        camHolder.rotation = Quaternion.Euler(xRotation, yRotation, 0f);

        // Rotate the player orientation horizontally
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    public void ChangeFOV(float value, float animationTime = 0.25f)
    {
        GetComponent<Camera>().DOFieldOfView(value, animationTime);
    }

    public void Tilt(float xTilt = 0, float yTilt = 0, float zTilt = 0, float animationTime = 0.25f)
    {
        transform.DOLocalRotate(new Vector3(xTilt, yTilt, zTilt), animationTime);
    }
}
