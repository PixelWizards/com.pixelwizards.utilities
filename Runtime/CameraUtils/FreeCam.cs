using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// A simple free camera to be added to a Unity game object.
/// 
/// Keys:
///	wasd / arrows	- movement
///	q/e 			- up/down (local space)
///	r/f 			- up/down (world space)
///	pageup/pagedown	- up/down (world space)
///	hold shift		- enable fast movement mode
///	right mouse  	- enable free look
///	mouse			- free look / rotation
///     
/// </summary>
public class FreeCam : MonoBehaviour
{
    /// <summary>
    /// Normal speed of camera movement.
    /// </summary>
    public float movementSpeed = 10f;

    /// <summary>
    /// Speed of camera movement when shift is held down,
    /// </summary>
    public float fastMovementSpeed = 100f;

    /// <summary>
    /// Sensitivity for free look.
    /// </summary>
    public float freeLookSensitivity = 3f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel.
    /// </summary>
    public float zoomSensitivity = 10f;

    /// <summary>
    /// Amount to zoom the camera when using the mouse wheel (fast mode).
    /// </summary>
    public float fastZoomSensitivity = 50f;

    /// <summary>
    /// Set to true when free looking (on right mouse button).
    /// </summary>
    private bool looking = false;

    private bool fastMode = false;
    private float desiredSpeed;
    private Vector3 right;
    private Vector3 up;
    private Vector3 forward;
    private Vector3 currentPosition;
    private Vector3 currentRotation;
    private float rotx, roty;
    private float desiredZoom;

    void Update()
    {
        fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        desiredSpeed = fastMode ? this.fastMovementSpeed : this.movementSpeed;
        right = transform.right;
        up = transform.up;
        forward = transform.forward;
        currentRotation = transform.localEulerAngles;
        
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            currentPosition = currentPosition + (-right * desiredSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            currentPosition = currentPosition + (right * desiredSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            currentPosition = currentPosition + (forward * desiredSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            currentPosition = currentPosition + (-forward * desiredSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.Q))
        {
            currentPosition = currentPosition + (-up * desiredSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.E))
        {
            currentPosition = currentPosition + (up * desiredSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.PageUp))
        {
            currentPosition = currentPosition + (Vector3.up * movementSpeed * Time.deltaTime);
        }

        if (Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.PageDown))
        {
            currentPosition = currentPosition + (-Vector3.up * movementSpeed * Time.deltaTime);
        }


        if (looking)
        {
            rotx = currentRotation.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            roty = currentRotation.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
            currentRotation = new Vector3(roty, rotx, 0f);
        }

        float axis = Input.GetAxis("Mouse ScrollWheel");
        if (axis != 0)
        {
            desiredZoom = fastMode ? this.fastZoomSensitivity : this.zoomSensitivity;
            currentPosition = currentPosition + transform.forward * axis * desiredZoom;
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLooking();
        }
    }

    private void FixedUpdate()
    {
        transform.position = currentPosition;
        transform.localEulerAngles = currentRotation;
    }

    void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    /// Enable free looking.
    /// </summary>
    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Disable free looking.
    /// </summary>
    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}