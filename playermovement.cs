using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

[SuppressMessage("ReSharper", "SuggestVarOrType_BuiltInTypes")]
[SuppressMessage("ReSharper", "SuggestVarOrType_SimpleTypes")]
public class PlayerMovement : MonoBehaviour
{
    // Trauma inducer
    [Tooltip("Seconds to wait before triggering the explosion particles and the trauma effect")]
    public float Delay = 1;
    [Tooltip("Maximum stress the effect can inflict upon objects Range([0,1])")]
    public float MaximumStress = 0.6f;
    [Tooltip("Maximum distance in which objects are affected by this TraumaInducer")]
    public float Range = 45;
    
    // References
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform camera;
    [SerializeField] private AudioSource footstepSource;
    [SerializeField] private AudioClip[] footstepClips;
    
    // Developer settings
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float walkingBobbingSpeed = 14f;
    [SerializeField] private float bobbingAmount = 0.05f;
    [SerializeField] private float jumpHeight = 3f;
    [SerializeField] private float playerGravityMultiplier = 2f;
    [SerializeField] private float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    // Sneak
    private bool isSneaking = false;
    public float t = 0f;
    public Slider sneakSlider;
    public Volume sneakVolume;
    
    // Private variables
    private float x;
    private float z;
    
    private Vector3 velocity;
    private bool isGrounded;
    private float yGravity;
    private Vector3 savedMoveVector;
    
    // For head bob
    private float defaultPosY = 0f;
    private float timer = 0f;
    
    // For footsteps
    private float previousFrameCameraYPosition;
    private bool previousFrameWasGrounded;
    private bool goingDown;
    public float threshold = 0.5f;
    public TextMeshProUGUI downdisplay;


    // Start is called before the first frame update
    private void Start()
    {
        yGravity = Physics.gravity.y;
        defaultPosY = camera.localPosition.y;
        // controller = GetComponent<CharacterController>();
        
        previousFrameCameraYPosition = camera.localPosition.y;
    }

    // Update is called once per frame
    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;
        
        x = Input.GetAxis("Horizontal");
        z = Input.GetAxis("Vertical");

        Transform currentPlayerTransform = transform; // More efficient than accessing the transform component twice
        Vector3 move = (currentPlayerTransform.right * x) + (currentPlayerTransform.forward * z);
        
        
        // v = √(h * -2 * g)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * yGravity);
            savedMoveVector = move;
        }
        
        controller.Move((isGrounded ? move : savedMoveVector) * (walkSpeed * Time.deltaTime)); // More efficient multiplication order

        velocity.y += yGravity * playerGravityMultiplier * Time.deltaTime;

        controller.Move(velocity * Time.deltaTime); // Multiplying by delta again because time is squared in the equation Δy = ½g * t²

        
        isSneaking = Input.GetKey(KeyCode.LeftShift);

        //if (Input.GetKeyDown(KeyCode.LeftShift)) t = 0f;
        //if (Input.GetKeyUp(KeyCode.LeftShift)) t = 1f;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            t += 1.5f * Time.deltaTime;
            if (t >= 1f) t = 1f;
        }
        else
        {
            t -= 0.8f * Time.deltaTime;
            if (t <= 0f) t = 0f;
        }

        sneakSlider.value = t;

        ProcessSneak();
    }

    private void ProcessSneak() // Normal -> Sneak
    {
        // Set footstep volume
        footstepSource.volume = Mathf.Lerp(0.5f, 0.15f, t);
        
        // Set player walk speed
        walkSpeed = Mathf.Lerp(6f, 3f, t);
        
        // Set player head bob distance
        walkingBobbingSpeed = Mathf.Lerp(14f, 7f, t);
        
        // Lerp Player Y Scale
        var curPos = camera.parent.localPosition;
        camera.parent.localPosition = new Vector3(
            curPos.x,
            Mathf.Lerp(0.62f, 0f, t),
            curPos.y
            );
        
        // Lerp Camera FOV
        camera.gameObject.GetComponent<Camera>().fieldOfView = Mathf.Lerp(60, 40, t);
        
        // Lerp Post Processing
        sneakVolume.weight = Mathf.Lerp(0f, 1f, t);
    }

    private void LateUpdate()
    {
        CameraBob();
        Footsteps();
        CheckShake();
    }
    
    #region Camera Bob

    private void CameraBob()
    {
        bool playerMoving = Mathf.Abs(x) > 0.1f || Mathf.Abs(z) > 0.1f;
        if(playerMoving && isGrounded)
        {
            //Player is moving
            timer += Time.deltaTime * walkingBobbingSpeed;
            
            camera.localPosition = new Vector3(camera.localPosition.x, defaultPosY + Mathf.Sin(timer) * bobbingAmount, camera.localPosition.z);
        }
        else
        {
            //Idle
            timer = 0;
            camera.localPosition = new Vector3(camera.localPosition.x, Mathf.Lerp(camera.localPosition.y, defaultPosY, Time.deltaTime * walkingBobbingSpeed), camera.localPosition.z);
        }
    }
    
    #endregion
    
    #region Footsteps

    private void Footsteps()
    {
        goingDown = camera.localPosition.y < previousFrameCameraYPosition;
        downdisplay.text = goingDown ? "^" : "v";

        if (isGrounded)
        {
            if (goingDown)
            {
                if (camera.localPosition.y <= threshold && previousFrameCameraYPosition > threshold)
                {
                    PlayFootstepSound();
                }
            }

            if (!previousFrameWasGrounded)
            {
                PlayFootstepSound();
            }
        }

        previousFrameWasGrounded = isGrounded;
        previousFrameCameraYPosition = camera.localPosition.y;
    }

    private void PlayFootstepSound()
    {
        footstepSource.PlayOneShot(footstepClips[Random.Range(0, footstepClips.Length)]);
    }
    
    #endregion

    #region Camera Shake
    private void CheckShake()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            // camera.GetComponent<StressReceiver>().InduceStress(MaximumStress);
        }
    }
    
    #endregion
}

