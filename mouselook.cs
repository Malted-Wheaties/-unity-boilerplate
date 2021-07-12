using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    // References
    [SerializeField] private Transform playerBodyRoot;
    
    // Player-set settings
    [SerializeField] private float xMouseSensitivity = 100f;
    [SerializeField] private float yMouseSensitivity = 100f;
    
    // Script settings
    [SerializeField] private float downClamp = -70f;
    [SerializeField] private float upClamp = 70f;
    
    // Private variables
    private float xRotation = 0f;

    
    // Start is called before the first frame update
    // Lock the mouse cursor to the game view when the game starts
    private void Start() => Cursor.lockState = CursorLockMode.Locked;
    
    // Update is called once per frame
    private void Update()
    {
        float mouseX = 0f;
        float mouseY = 0f;
        
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            mouseX = Input.GetAxis("Mouse X") * xMouseSensitivity * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * yMouseSensitivity * Time.deltaTime;
        }
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, downClamp, upClamp);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBodyRoot.Rotate(Vector3.up, mouseX);
    }
}
