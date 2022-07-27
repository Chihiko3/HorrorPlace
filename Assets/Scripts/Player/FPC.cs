using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPC : MonoBehaviour
{
    public bool CanMove { get; private set; } = true;

    // here we introduce a bool to check whether we are in sprinting.
    // the syntax means return true if the part after => is true.
    private bool IsSprinting => canSprint && Input.GetKey(sprintKey);

    // unlike the IsSprinting check, we do not need to check canJump because if player cannot jump, all code about jump won't run at all.
    // and we will check it in the apply method below.
    private bool ShouldJump => Input.GetKeyDown(jumpKey) && characterController.isGrounded;

    private bool ShouldCrouch => Input.GetKeyDown(crouchKey) && !duringCrouchAnimation && characterController.isGrounded;

    [Header("Functional Options")]
    [SerializeField] private bool canSprint = true;
    [SerializeField] private bool canJump = true;
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private bool canUseHeadbob = true;
    [SerializeField] private bool willSlideOnSlopes = true;
    [SerializeField] private bool canZoom = true;
    [SerializeField] private bool canInteract = true;

    [Header("Controls")]
    // KeyCode is natually an enum, so good.
    // Notice that you can change them in the inspector.
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode zoomKey = KeyCode.LeftControl; // Won't be used in this game cuz the shader doesn't fit.
    [SerializeField] private KeyCode interactKey = KeyCode.Mouse0;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Jumping Parameters")]
    [SerializeField] private float jumpForce = 8.0f;
    [SerializeField] private float gravity = 30.0f;

    [Header("Crouch Parameters")]
    [SerializeField] private float crouchHeight = 0.5f;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float timeToCrouch = 0.25f;
    [SerializeField] private Vector3 crouchingCenter = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 standingCenter = new Vector3(0, 0, 0);
    private bool isCrouching;
    private bool duringCrouchAnimation;

    [Header("Headbob Parameters")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float sprintBobSpeed = 18f;
    [SerializeField] private float sprintBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 8f;
    [SerializeField] private float crouchAmount = 0.025f;
    private float defaultYPos = 0;
    private float timer;

    [Header("Zoom Parameters")]
    [SerializeField] private float timeToZoom = 0.3f;
    [SerializeField] private float zoomFOV = 30f;
    private float defaultFOV;
    private Coroutine zoomRoutine;


    // SLIDING PARAMETERS BELOW

    private Vector3 hitpointNormal;
    private bool isSliding
    {
        get
        {   // Raycast here to get the information of what we hit
            if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
            {
                // get the normal vector at the point where we slide, so we can get the angle by compare the normal and the worldy vertical direction
                hitpointNormal = slopeHit.normal;
                // so if the angle is bigger than the slopeLimit we set in inspector, we are sliding actually
                // notice here we just in calculation part, not in apply part. should apply it in the apply method.
                return Vector3.Angle(hitpointNormal, Vector3.up) > characterController.slopeLimit;
                
            }
            // if player is falling
            else
            {
                return false;
            }
        }
    }

    [Header("Interaction")]
    [SerializeField] private Vector3 interactionRayPoint = default;
    [SerializeField] private float interactionDistance = default;
    [SerializeField] private LayerMask interactionLayer = default;

    // So we can call the abstract script
    private InteractableTemplate currentInteractable;


    private Camera playerCamera;
    private CharacterController characterController;

    private Vector3 moveDirection;
    private Vector2 currentInput;

    private float rotationX = 0;

    void Awake()
    {
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        defaultFOV = playerCamera.fieldOfView;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (CanMove)
        {
            HandleMovementInput();
            HandleMouseLook();

            if (canJump)
            {
                HandleJump();
            }

            if (canCrouch)
            {
                HandleCrouch();
            }

            if (canUseHeadbob)
            {
                HandleHeadbob();
            }

            if (canZoom)
            {
                HandleZoom();
            }

            if (canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
            }

            ApplyFinalMovements();
        }
    }

    // this method not actually apply those movements, pure calculation in this method.
    private void HandleMovementInput()
    {
        // currentInput stores values of current input from keyboard, but not used at this time.
        // the syntax here means "checking whether we are in sprinting, if yes using sprintSpeed if not using walkSpeed".
        currentInput = new Vector2((isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

        // ?moveDirection is a Vector3, so moveDirection.y is the direction away from the ground, we should keep it as same as before,
        // ?so we store it here and restore it after we did the worldly action.
        float moveDirectionY = moveDirection.y;

        // make the movement worldly, notice the moveDirection is a Vector3. So when we handle the horizontal movings, we should use Vector3.right.
        moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);

        // ?restore the moveDirection.y here so we won't float in the sky or fly I guess.
        moveDirection.y = moveDirectionY;
    }

    private void HandleMouseLook()
    {
        // vertical mouse look, the reason why we call it rotationX is that the axis is X axis, but the data that is changing is y.
        rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;

        // limit the rotation "angle", the reason why using -upperLookLimit is due to Unity itself,
        // the upper part above the horizontal line is belong to negative zone.
        rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);

        // now apply the rotationX data to real transform in a smoothy way.
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);

        // horizontal mouse look is much easier than vertical one, we just make it smoothy by adding Quaternion.
        // notice that we are directly changing the character's rotation here cuz this is a first person view
        // so if we do not need the headbob or something like that, we do not need to rotate the camera.
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0);
    }

    private void HandleJump()
    {
        if (ShouldJump && !isSliding /* = = I added !isSliding here, so player cannot jump when sliding*/ )
        {
            moveDirection.y = jumpForce;
        }
    }
    private void HandleCrouch()
    {
        // should notice that when ShouldCrouch == true, we are actually pressing the crouch button!
        // so the Coroutine starts when we are pressing the crouch button instead of holding the button.
        if (ShouldCrouch)
        {
            StartCoroutine(CrouchStand());
        }
    }

    private void HandleHeadbob()
    {
        if (!characterController.isGrounded)
        {
            return;
        }

        // remove direction factor from doing headbob
        if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
        {
            timer += Time.deltaTime * (isCrouching ? crouchBobSpeed  : IsSprinting ? sprintBobSpeed : walkBobSpeed);
            
            // Sin can give a headbob on vertical axis. if you wanna have a bob on horizontal axis, you can add the long equation on x.
            // like what I did in the comment line
            playerCamera.transform.localPosition = new Vector3(
                playerCamera.transform.localPosition.x /* +(Mathf.Sin(timer) * (isCrouching ? crouchAmount : IsSprinting ? sprintBobAmount : walkBobAmount))/50f */, 
                defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchAmount : IsSprinting ? sprintBobAmount : walkBobAmount),
                playerCamera.transform.localPosition.z);
        }
    }

    private void HandleZoom()
    {
        if (Input.GetKeyDown(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }
            zoomRoutine = StartCoroutine(ToggleZoom(true));
        }

        if (Input.GetKeyUp(zoomKey))
        {
            if (zoomRoutine != null)
            {
                StopCoroutine(zoomRoutine);
                zoomRoutine = null;
            }
            zoomRoutine = StartCoroutine(ToggleZoom(false));
        }
    }

    private void HandleInteractionCheck()
    {
        // if we haven't focus on anything before
        if (Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            // if we watch something that is layer 9, and we haven't watched any interactable thing before or we are watching a different interactable thing, then we get the current component.
            if (hit.collider.gameObject.layer ==9 && (currentInteractable == null || hit.collider.gameObject.GetInstanceID() != currentInteractable.GetInstanceID()))
            {
                hit.collider.TryGetComponent(out currentInteractable);

                if (currentInteractable)
                {
                    currentInteractable.OnFocus();
                }
            }
        }
        // we have focused on something before but now we do not focus on it, we call it lose focus.
        else if (currentInteractable)
        {
            currentInteractable.OnLoseFocus();
            currentInteractable = null;
        }
    }

    private void HandleInteractionInput()
    {
        if (Input.GetKeyDown(interactKey) && currentInteractable != null && Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance, interactionLayer))
        {
            currentInteractable.OnInteract();
        }
    }

    // apply those calculations that we did in HandleMovementInput().
    // the reason why we split them into two methods is that we are using CharacterController component,
    // by doing this, we can handle it more clearly.
    private void ApplyFinalMovements()
    {
        // so is.Grounded is a very convinient way in CharacterController Component
        if (!characterController.isGrounded)
        {
            // gravity should drag our character back to ground
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (willSlideOnSlopes && isSliding)
        {
            moveDirection += new Vector3(hitpointNormal.x, -hitpointNormal.y, hitpointNormal.z) * slopeSpeed;
        }

        // move the character with data in other methods
        // and this is also a convinent way that CharacterController Component provides us.
        characterController.Move(moveDirection * Time.deltaTime);
    }

    // so this Coroutine did both crouch and stand functions, so it looks a little confused
    private IEnumerator CrouchStand()
    {
        // if we are crouching and wanna stand up, we should first make sure that we got enough room for it.
        if (isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
        {
            yield break;
        }

        duringCrouchAnimation = true;

        float timeElapsed = 0;
        // if we are crouching, of course, our target height when we press the botton is standing height
        // and if we are standing, our target height is crouch height
        float targetHeight = isCrouching ? standingHeight : crouchHeight;
        float currenHeight = characterController.height;
        // same as the height code logic
        Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
        Vector3 currentCenter = characterController.center;

        // this while loop is actually setting the time period for us to complete the crouch or stand action
        while(timeElapsed < timeToCrouch)
        {
            characterController.height = Mathf.Lerp(currenHeight, targetHeight, timeElapsed/timeToCrouch);
            characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed/timeToCrouch);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        characterController.height = targetHeight;
        characterController.center = targetCenter;

        isCrouching = !isCrouching;

        duringCrouchAnimation = false;
    }

    private IEnumerator ToggleZoom(bool isEnter)
    {
        float targetFOV = isEnter ? zoomFOV : defaultFOV;
        float startingFOV = playerCamera.fieldOfView;
        float timeElasped = 0;

        while(timeElasped < timeToZoom)
        {
            playerCamera.fieldOfView = Mathf.Lerp(startingFOV, targetFOV, timeElasped / timeToZoom);
            timeElasped += Time.deltaTime;
            yield return null;
        }

        playerCamera.fieldOfView = targetFOV;
        zoomRoutine = null;
    }
}
