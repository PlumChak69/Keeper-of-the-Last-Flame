using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementIso : MonoBehaviour
{
    public IsometricCamera isoCam;    // przeci¹gnij komponent IsometricCamera
    public float walkSpeed = 4.0f;
    public float sprintSpeed = 6.0f;
    public float gravity = -9.81f;

    float yVel;
    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!isoCam) isoCam = FindFirstObjectByType<IsometricCamera>();
    }

    void Update()
    {
        // grawitacja
        if (cc.isGrounded && yVel < 0f) yVel = -2f;
        yVel += gravity * Time.deltaTime;

        // wejœcie WASD
        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        Vector3 move = Vector3.zero;
        if (input.sqrMagnitude > 0.001f)
        {
            // oœ kamery na p³aszczyŸnie (izometryczny forward/right)
            Vector3 fwd = isoCam ? isoCam.GetCameraForwardOnPlane() : Vector3.forward;
            Vector3 right = isoCam ? isoCam.GetCameraRightOnPlane() : Vector3.right;
            move = (right * input.x + fwd * input.z).normalized;
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector3 vel = move * speed + Vector3.up * yVel;
        cc.Move(vel * Time.deltaTime);

        // opcjonalnie: obróæ model gracza w kierunku ruchu (tylko yaw)
        if (move.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(move, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, look, 16f * Time.deltaTime);
        }
    }
}
