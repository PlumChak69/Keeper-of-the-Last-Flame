using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Transform cam;               // przypnij Main Camera (dziecko gracza)
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 5.5f;
    public float gravity = -9.81f;

    float yVelocity;
    CharacterController cc;

    void Awake() => cc = GetComponent<CharacterController>();

    void Update()
    {
        // pod³o¿e + grawitacja
        if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;

        // wejœcie gracza
        float h = Input.GetAxisRaw("Horizontal");   // A/D
        float v = Input.GetAxisRaw("Vertical");     // W/S
        Vector3 input = new Vector3(h, 0f, v).normalized;

        // kierunek wzglêdem kamery
        Vector3 move = Vector3.zero;
        if (input.sqrMagnitude > 0.01f)
        {
            Vector3 forward = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : transform.forward;
            Vector3 right = cam ? cam.right : transform.right;
            move = (forward * input.z + right * input.x).normalized;
        }

        float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        Vector3 velocity = move * speed + Vector3.up * yVelocity;

        cc.Move(velocity * Time.deltaTime);
    }
}
