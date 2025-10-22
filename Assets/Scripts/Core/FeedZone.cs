using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FeedZone : MonoBehaviour
{
    public FlameEnergy flame;
    public float feedAmount = 10f;
    public KeyCode feedKey = KeyCode.E;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(feedKey))
        {
            flame.FeedFlame(feedAmount);
        }
    }
}
