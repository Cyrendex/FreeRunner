using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerControllerOld : MonoBehaviour
{
    [SerializeField] float speed = 12f;
    [SerializeField] float gravityMultiplier = 1.0f;
    [SerializeField] LayerMask groundMask;
    [SerializeField] float jumpForce = 2f;
    private CharacterController controller;

    private Transform groundCheck;
    private float groundDistance = 0.4f;
    
    private float gravity = -9.81f;
    private Vector3 velocity;
    private bool isGrounded;

    // Start is called before the first frame update
    void Start()
    {
        controller = gameObject.GetComponent<CharacterController>();
        groundCheck = transform.Find("GroundCheck");
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        if(isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Slightly negative in case groundcheck triggers before contact.
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 direction = transform.right * x + transform.forward * z;
        controller.Move(speed * Time.deltaTime * direction);

        // Check jump
        if(Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpForce * -2f * (gravity * gravityMultiplier));
            Debug.Log("Jump!");
        }

        velocity.y += gravityMultiplier * gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
