using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    public float speed;
    private Vector2 move;
    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
        
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        movePlayer();
    }

    public void movePlayer()
    {
        Vector3 movement = new Vector3(move.x, 0F , move.y);
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 0.15f);
        transform.Translate(movement * speed * Time.deltaTime, Space.World);
    }
}
