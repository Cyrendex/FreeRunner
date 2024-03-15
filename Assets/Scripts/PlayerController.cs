using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode sprintKey;
    public KeyCode crouchKey;

    [Header("Movement")]
    private float maxSpeed;
    [SerializeField] float walkSpeed;
    [SerializeField] float sprintSpeed;
    [SerializeField] float crouchSpeed;
    [SerializeField] float slideSpeed;
    [SerializeField] float wallRunSpeed;
    [SerializeField] float groundDrag;
    [SerializeField] float speedIncreaseMultiplier;
    [SerializeField] float slopeIncreaseMultiplier;

    private float desiredSpeed;
    private float lastDesiredSpeed;

    [Header("Jumping")]
    [SerializeField] float jumpForce;
    [SerializeField] float jumpCooldown;
    [SerializeField] float airMultiplier;
    private bool jumpReady;

    [Header("Crouching")]
    [SerializeField] float crouchScale;
    private float standScale;

    [Header("Wall Running")]
    [SerializeField] bool wallGravity;
    [SerializeField] float gravityCounterForce;
    [SerializeField] LayerMask wallMask;
    [SerializeField] float wallRunForce;
    [SerializeField] float maxWallRunTime;
    [SerializeField] float wallDistance;
    [SerializeField] float wallJumpForce;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool isWallLeft;
    private bool isWallRight;
    private float wallRunTimer;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float checkRadius;
    [SerializeField] LayerMask groundMask;
    private bool isGrounded;

    [Header("Slope Handling")]
    [SerializeField] float playerHeight;
    [SerializeField] float maxSlopeAngle;
    [SerializeField] float slideForce;
    private RaycastHit slopeHit;


    [Header("Animation Settings")]
    [SerializeField] float wallRunZTilt;
    [SerializeField] float sprintFOV;
    [SerializeField] float regularFOV;
    [SerializeField] float slideXTilt;
    [SerializeField] float slideFOV;
    [SerializeField] float wallRunFOV;
    private Animator animator;

    [Header("Miscellaneous")]
    public TMP_Text speedText;
    public TMP_Text stateText;

    public Transform orientation;
    public CameraController playerCam;

    private float horizontalInput;
    private float verticalInput;
    private Vector3 moveDirection;

    private Rigidbody rb;
    private PlayerState state;
    private PlayerState lastState;
    public Transform hitbox;

    private enum PlayerState
    {
        walking,
        sprinting,
        crouching,
        sliding,
        wallrunning,
        midair

    }
    
    private void Start() 
    {
        jumpReady = true;
        lastState = PlayerState.midair; // Null exception prevention

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;

        animator = GetComponent<Animator>();

        desiredSpeed = walkSpeed;
        standScale = hitbox.localScale.y;
    }

    private void Update() 
    {
        // Check collision with ground around player's feet
        isGrounded = Physics.CheckBox(groundCheck.position, new Vector3(checkRadius, 0.1f, checkRadius), Quaternion.Euler(0,0,0), groundMask);
        
        CheckWall();
        PlayerInput();
        LimitSpeed();
        StateHandler();
        
        UpdateText();
        // Apply drag if grounded
        if(isGrounded)
        {
            rb.drag = groundDrag;
            animator.SetBool("isGrounded", true);
        }      
        else
        {
            rb.drag = 0f;
            animator.SetBool("isGrounded", false);
        }
    }

    private void FixedUpdate() 
    {
        MovePlayer();
    }

    private void AnimationHandler()
    {
        if(lastState != state && state == PlayerState.wallrunning) // Wallrun enter
        {
            playerCam.ChangeFOV(wallRunFOV);
            animator.SetBool("isWallrunning", true);
            if(isWallLeft)
            {
                playerCam.Tilt(zTilt: -wallRunZTilt);
                animator.SetTrigger("wallrunningLeft");
            }
            else
            {
                playerCam.Tilt(zTilt: wallRunZTilt);
                animator.SetTrigger("wallrunningRight");
            }
        }
        else if(lastState != state && lastState == PlayerState.wallrunning) // Wallrun exit
        {
            animator.SetBool("isWallrunning", false);
            playerCam.ChangeFOV(regularFOV);
            playerCam.Tilt(zTilt: 0.0f);
        }
        else if(lastState != state && state == PlayerState.sprinting) // Sprint enter
        {
            animator.SetBool("isRunning", true);
            playerCam.ChangeFOV(sprintFOV);
        }
        else if(lastState != state && lastState == PlayerState.sprinting) // Sprint exit
        {
            animator.SetBool("isRunning", false);
            playerCam.ChangeFOV(regularFOV);
        }
        else if(lastState != state && state == PlayerState.sliding) // Slide enter
        {
            animator.SetBool("isSliding", true);
            playerCam.ChangeFOV(slideFOV);
            playerCam.Tilt(xTilt: slideXTilt);
        }
        else if(lastState != state && lastState == PlayerState.sliding) // Slide exit
        {
            animator.SetBool("isSliding", false);
            playerCam.ChangeFOV(regularFOV);
            playerCam.Tilt(xTilt: 0);
        }
        

    }
    private void StateHandler() 
    {
        if(verticalInput != 0 || horizontalInput != 0)
        {
            animator.SetBool("isMoving", true);
            Debug.Log("Moving");
        }
        else
        {
            animator.SetBool("isMoving", false);
            Debug.Log("Not moving");
        }

        if(!isGrounded && (isWallLeft || isWallRight) && verticalInput > 0) // Player is wallrunning
        {
            state = PlayerState.wallrunning;
            desiredSpeed = wallRunSpeed;
        }
        else if(OnSlope() && Input.GetKey(crouchKey) && rb.velocity.y < 0.01f) // Player is sliding down
        {
            state = PlayerState.sliding;
            desiredSpeed = slideSpeed;
        }
        else if(isGrounded && Input.GetKey(crouchKey)) // Player is crouching
        {
            state = PlayerState.crouching;
            desiredSpeed = crouchSpeed;
        }
        else if(isGrounded && Input.GetKey(sprintKey)) // Player is sprinting
        {
            state = PlayerState.sprinting;
            desiredSpeed = sprintSpeed;
        }
        else if(isGrounded) // Player is walking
        {
            state = PlayerState.walking;
            desiredSpeed = walkSpeed;
        }
        else // Player is in the air
        {
            state = PlayerState.midair;
        }

        // Animation handler... THIS HAS TO BE BETWEEN STATE HANDLING AND LAST STATE ASSIGNING
        AnimationHandler();

        // Lerp the speed if the change is too drastic (unless player is stationary)
        if(Mathf.Abs(desiredSpeed - lastDesiredSpeed) > 4f && maxSpeed != 0)
        {
            StopCoroutine(SmoothMomentumShift());
            StartCoroutine(SmoothMomentumShift());
        }
        else // If the change is small, do it without lerping.
        {
            maxSpeed = desiredSpeed;
        }
        lastState = state;
        lastDesiredSpeed = desiredSpeed;
    }
    private void PlayerInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        // Jump check
        if(Input.GetButton("Jump") && jumpReady)
        {   
            if(isGrounded)
            {
                jumpReady = false;
                Jump();
            }
                
            else if(state == PlayerState.wallrunning)
            {
                jumpReady = false;
                WallJump();
            }            

            // Apply cooldown to jump
            if(!jumpReady)
                Invoke(nameof(ResetJump), jumpCooldown);
        }
        
        // Crouch check
        if(Input.GetKeyDown(crouchKey))
        {   
            bool wasGrounded = isGrounded;
            animator.SetBool("isCrouching", true);
            hitbox.localScale = new Vector3(hitbox.localScale.x, crouchScale, hitbox.localScale.z); // Shrink player hitbox

            if(wasGrounded)
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse); // Plant player to the ground if they were grounded before crouching
        }

        // Un-crouch check
        if(Input.GetKeyUp(crouchKey))
        {
            animator.SetBool("isCrouching", false);
            hitbox.localScale = new Vector3(hitbox.localScale.x, standScale, hitbox.localScale.z); // Un-shrink player hitbox
        }
    }

    private void MovePlayer()
    {
        // Calculate the movement direction
        moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;

        // Apply force depending on the state
        if(state == PlayerState.wallrunning)
        {
            WallRun();
        }
        else if(state == PlayerState.sliding)
        {
            if(!isGrounded)
                rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);
            Slide();
        }
        else if(OnSlope() && jumpReady)
        {
            rb.AddForce(20f * maxSpeed  * GetSlopeMoveDirection(), ForceMode.Force);

            if (rb.velocity.y > 0)
                rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }     
        else if(isGrounded) // On the ground
            rb.AddForce(10f * maxSpeed * moveDirection.normalized, ForceMode.Force);
        else if(!isGrounded) // In the air
            rb.AddForce(10f * airMultiplier * maxSpeed * moveDirection.normalized, ForceMode.Force);

        if(!(state == PlayerState.wallrunning))
            rb.useGravity = !OnSlope();
    }

    private void LimitSpeed()
    {
        // Get current x,z velocity as a new Vector3
        Vector3 currentVelocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        // Limit player velocity if it exceeds movespeed on a slope
        if(OnSlope())
        {
            // Making sure slope movement is also normalized
            if(rb.velocity.magnitude > maxSpeed)
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                // Bigger speed penalty for climbing higher slopes
                rb.velocity = maxSlopeAngle / (angle+90.0f) * maxSpeed  * rb.velocity.normalized;
            }
        }
        else
        {
            // Limit player velocity if it exceeds movespeed on the ground.
            if(currentVelocity.magnitude > maxSpeed)
            {
                // Get the normalized vector and amplify it with max speed.
                Vector3 newVelocity = currentVelocity.normalized * maxSpeed;
                rb.velocity = new Vector3(newVelocity.x, rb.velocity.y, newVelocity.z);
            }
        }
        
    }

    private void Jump()
    {
        // Reset y velocity
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        // Call animation trigger
        animator.SetTrigger("jump");
        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        jumpReady = true;
    }

    private bool OnSlope()
    {
        if(Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * 0.5f + 0.45f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(moveDirection, slopeHit.normal).normalized;
    }

    private void Slide()
    {
        rb.AddForce(GetSlopeMoveDirection() * slideForce, ForceMode.Force);
    }

    private void WallRun()
    {
        rb.useGravity = wallGravity;

        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        if((orientation.forward - wallForward).magnitude > (orientation.forward + wallForward).magnitude)
            wallForward = -wallForward;
        
        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);

        // Stick the player to the wall unless attempting to leave
        if(!(isWallLeft && horizontalInput > 0) && !(isWallRight && horizontalInput < 0) && jumpReady)
            rb.AddForce(-wallNormal * 100, ForceMode.Force);

        // Slow wall fall when using gravity
        if(wallGravity)
            rb.AddForce(transform.up * gravityCounterForce, ForceMode.Force);
    }
    private IEnumerator SmoothMomentumShift()
    {
        // Lerp movement speed to desired value
        float time = 0;
        float difference = Mathf.Abs(desiredSpeed - maxSpeed);
        float startValue = maxSpeed;

        while(time < difference)
        {
            maxSpeed = Mathf.Lerp(startValue, desiredSpeed, time / difference);
            if(OnSlope()) 
            {
                float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
                float steepness = 1 + (angle/90f);

                time += Time.deltaTime * speedIncreaseMultiplier * slopeIncreaseMultiplier * steepness;
            }
            else
                time += Time.deltaTime * speedIncreaseMultiplier;
            
            yield return null;
        }

        
        maxSpeed = desiredSpeed;
    }

    private void CheckWall()
    {
        isWallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallDistance, wallMask);
        isWallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallDistance, wallMask);
    }

    private void WallJump()
    {
        Vector3 wallNormal = isWallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 jumpForce = transform.up * wallJumpForce + 2.2f * wallJumpForce * wallNormal;

        // Reset y velocity and jump
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(jumpForce, ForceMode.Impulse);
    }
    private void UpdateText()
    {
        speedText.text = "Speed: " + rb.velocity.magnitude;
        stateText.text = "" + state;
    }
}
