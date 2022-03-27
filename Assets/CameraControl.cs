using System;
using UnityEngine;

/// <summary>
/// A class for controlling the movement of the camera.
/// </summary>
public class CameraControl : MonoBehaviour
{
	public EditorController editorController;
	public float mouseSensitivity = 100f;

	private float _yRotation;

    void Start()
    {
	    Cursor.lockState = CursorLockMode.Locked;
    }
    
    /// <summary>
    /// Look at a certain point.
    /// </summary>
    public void LookAt(Vector3 point) {
		transform.LookAt(point);
    }

    void Update() {
		// don't move when shift is pressed in edit mode (holds turn)
		if (Input.GetKey(KeyCode.LeftShift) && editorController.currentMode == Mode.Holding) return;
		
		float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
		float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
		
		// turn up/down relative to self
		transform.Rotate(Vector3.right * -mouseY, Space.Self);
		
		// rotate left/right around relative to world
		transform.Rotate(Vector3.up * mouseX, Space.World);
		
		// clamp +- 90
		// please don't ask, I don't know
		if (Math.Abs(transform.localRotation.eulerAngles.z - 180.0) < 0.1)
		{
			var angle = Vector3.Angle(transform.forward, Vector3.down);

			if (angle > 90)
				angle = -(180 - angle);
			
			transform.Rotate(Vector3.right * -angle, Space.Self);
		}
    }
}