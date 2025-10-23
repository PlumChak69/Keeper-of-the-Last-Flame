using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class Billboard : MonoBehaviour
{
    public Camera cam;

    void LateUpdate()
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;

        // patrz w kierunku kamery, z poprawn¹ "gór¹"
        var forward = cam.transform.rotation * Vector3.forward;
        var up = cam.transform.rotation * Vector3.up;
        transform.LookAt(transform.position + forward, up);
    }
}