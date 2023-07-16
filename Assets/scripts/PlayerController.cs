using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public float speed;
    private Vector2 move;
    private PlayerAnimator animator;
    [SerializeField] private Camera camera;

    public bool CanMove { get; internal set; }

    void Start()
    {
        animator = GetComponent<PlayerAnimator>();
    }
    public void SetMove(Vector2 move)
    {
        this.move = move;
        
    }

    void Update()
    {
        MovePlayer();
    }

    public void MovePlayer()
    {
        if (!CanMove) return;

        Vector3 movement = new Vector3(move.x, 0F , move.y);

        var forward = camera.transform.forward;
        forward.y = 0;
        forward.Normalize();
        var right = camera.transform.right;
        right.y = 0;
        right.Normalize();


        //if (movement != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement.x * right + movement.z * forward), 0.55f);
        if (movement != Vector3.zero) transform.rotation = Quaternion.LookRotation(movement.x * right + movement.z * forward);
        transform.Translate((movement.x  * right + movement.z * forward) * Time.deltaTime * speed, Space.World);

        //transform.Translate(movement * speed * Time.deltaTime, Space.World);
        animator.SetSpeed(movement);
    }
}
