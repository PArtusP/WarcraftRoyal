using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // Start is called before the first frame update
    public Transform target;
    public float smoothTime = 0.5f;
    public Vector3 offset;
    private Vector3 velocity = Vector3.zero;
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if(target != null)
        {
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
        
    }
}
