using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatchCameraRotation : MonoBehaviour
{
    [SerializeField] bool matchHorizontal;
    [SerializeField] bool matchVertical;
    public new Transform transform;
    public Transform cameraRotation;
    
    private void Start() {
        // transform = gameObject.GetComponent<Transform>();
    }
    void Update()
    {
        if(matchHorizontal && matchVertical)
        {
            transform.rotation.Set(cameraRotation.rotation.x, cameraRotation.rotation.y, cameraRotation.rotation.z, cameraRotation.rotation.w);
        }
        else if(matchVertical)
        {
            transform.rotation.Set(cameraRotation.rotation.x, transform.rotation.y, cameraRotation.rotation.z, transform.rotation.w);
        }
        else if(matchHorizontal)
        {
            transform.rotation.Set(transform.rotation.x, cameraRotation.rotation.y, transform.rotation.z, transform.rotation.w);
        }
    }
}
