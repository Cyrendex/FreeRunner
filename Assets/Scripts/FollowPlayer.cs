using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public Transform target; // Must assign manually from UI

    void Update()
    {
        transform.position = target.position;        
    }
}
