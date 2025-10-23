using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public Transform playerBody;    // kapsu³a gracza (root z CharacterController)
    public float sensitivity = 120f;
    public float minPitch = -80f;
    public float maxPitch = 80f;

    float xRot = 0f; // pitch

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        xRot -= mouseY;
        xRot = Mathf.Clamp(xRot, minPitch, maxPitch);

        transform.localRotation = Quaternion.Euler(xRot, 0f, 0f); // kamera pitch
        playerBody.Rotate(Vector3.up * mouseX);                   // obrót yaw gracza
    }
}
