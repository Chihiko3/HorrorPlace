using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;
using Cursor = UnityEngine.Cursor;

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
    [SerializeField] private bool useFootsteps = true;
    [SerializeField] private bool useStamina = true;

    [Header("Controls")]
    // KeyCode is natually an enum, so good.
    // Notice that you can change them in the inspector.
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
    [SerializeField] private KeyCode jumpKey = KeyCode.Space;
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode zoomKey = KeyCode.LeftControl; // Won't be used in this game cuz the shader doesn't fit.
    [SerializeField] private KeyCode interactKey = KeyCode.Mouse1;
    [SerializeField] private KeyCode resizeKey = KeyCode.Mouse0;

    [Header("Movement Parameters")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintSpeed = 5.0f;
    [SerializeField] private float crouchSpeed = 2.0f;
    [SerializeField] private float slopeSpeed = 8f;

    [Header("Stamina Parameters")]
    [SerializeField] private float maxStamina = 100;
    [SerializeField] private float staminaUseMultiplier = 5;
    [SerializeField] private float staminaUseMultiplierCrouch = 2.5f;
    [SerializeField] private float timeBeforeStaminaRegenStarts = 5;
    [SerializeField] private float staminaValueIncrement = 2;
    [SerializeField] private float staminaTimeIncrement = 0.1f;
    private float currentStamina;
    private Coroutine regeneratingStamina;
    public static Action<float> OnStaminaChange;


    [Header("Look Parameters")]
    [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
    [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
    [SerializeField, Range(1, 180)] private float upperLookLimit = 80.0f;
    [SerializeField, Range(1, 180)] private float lowerLookLimit = 80.0f;

    [Header("Health Parameters")]
    [SerializeField] private float maxHealth = 100;
    [SerializeField] private float timeBeforeRegenStarts = 3;
    [SerializeField] private float healthValueIncrement = 1;
    [SerializeField] private float healthTimeIncrement = 0.1f;
    private float currentHealth;
    private Coroutine regenerateingHealth;
    // below are about dmg actions
    public static Action<float> OnTakeDamage;
    public static Action<float> OnDamage;
    public static Action<float> OnHeal;

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

    [Header("Footstep Parameters")]
    [SerializeField] private float baseStepSpeed = 0.5f;
    [SerializeField] private float crouchStepMultipler = 1.5f;
    [SerializeField] private float sprintStepMultipler = 0.6f;
    // set to default so we won't get warnning in console.
    [SerializeField] private AudioSource footstepAudioSource = default;
    [SerializeField] private AudioClip[] concreteClips = default;
    [SerializeField] private AudioClip[] metalClips = default;
    [SerializeField] private AudioClip[] grassClips = default;
    private float footstepTimer = 0;
    private float GetCurrentOffset => isCrouching ? baseStepSpeed * crouchStepMultipler : IsSprinting ? baseStepSpeed * sprintStepMultipler : baseStepSpeed;


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

    // need simplify
    [Header("Resize")]
    [SerializeField] private Transform target; // The target object we picked up for scaling
    [SerializeField] private LayerMask ignoreMaskDuringResizing;
    [SerializeField] float offsetFactor; // The offset amount for positioning the object so it doesn't clip into walls

    private float originalDistance; // The original distance between the player camera and the target
    private float originalScale; // The original scale of the target objects prior to being resized
    private Vector3 targetScale; // The scale we want our object to be set to each frame


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

    // used in door scripts
    public static FPC instance;

    private void OnEnable()
    {
        // += in here means "call or subscribe"
        OnTakeDamage += ApplyDamage;
    }

    private void OnDisable()
    {
        OnTakeDamage -= ApplyDamage;
    }

    void Awake()
    {
        // used in door scripts;
        instance = this;
        playerCamera = GetComponentInChildren<Camera>();
        characterController = GetComponent<CharacterController>();
        defaultYPos = playerCamera.transform.localPosition.y;
        defaultFOV = playerCamera.fieldOfView;
        currentHealth = maxHealth;
        currentStamina = maxStamina;

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

            if (useFootsteps)
            {
                Handle_Footsteps();
            }

            if (canInteract)
            {
                HandleInteractionCheck();
                HandleInteractionInput();
                HandleResizeInput();
                HandleResize();
            }

            if (useStamina)
            {
                HandleStamina();
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
        // we do not need to rotate the camera.
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

    private void HandleStamina()
    {
        if (IsSprinting && currentInput != Vector2.zero)
        {
            // this "if" is used to stop regen stamina at the time player just in sprint so avoid some strange situations.
            if (regeneratingStamina != null)
            {
                StopCoroutine(regeneratingStamina);
                regeneratingStamina = null;
            }

            // I personally added this to differenciate those 2 situations.
            if (isCrouching)
            {
                currentStamina -= staminaUseMultiplierCrouch * Time.deltaTime;
            }
            else
            {
                currentStamina -= staminaUseMultiplier * Time.deltaTime;
            }


            if (currentStamina < 0)
            {
                currentStamina = 0;
            }

            OnStaminaChange?.Invoke(currentStamina);

            if (currentStamina <= 0)
            {
                canSprint = false;
            }
        }

        if (!IsSprinting && currentStamina < maxStamina && regeneratingStamina == null)
        {
            regeneratingStamina = StartCoroutine(RegenerateStamina());
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
        // used to handle focus or not
    {
        // if we haven't focus on anything before
        if (Physics.Raycast(playerCamera.ViewportPointToRay(interactionRayPoint), out RaycastHit hit, interactionDistance))
        {
            // if we watch something that is layer 9, and we haven't watched any interactable thing before or we are watching a different interactable thing, then we get the current component.
            if (hit.collider.gameObject.layer == 9 && (currentInteractable == null || hit.collider.gameObject.GetInstanceID() != currentInteractable.GetInstanceID()))
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

    private void HandleResizeInput()
    {
        // Check for right mouse click
        if (Input.GetKeyDown(resizeKey))
        {
            // If we do not currently have a target, we catch it
            if (target == null)
            {
                // Fire a raycast with the layer mask that only hits potential targets

                if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, Mathf.Infinity, interactionLayer))
                {
                    // Set our target variable to be the Transform object we hit with our raycast
                    target = hit.transform;

                    // Disable physics for the object
                    target.GetComponent<Rigidbody>().isKinematic = true;

                    // Calculate the distance between the camera and the object
                    originalDistance = Vector3.Distance(playerCamera.transform.position, target.position);

                    // Save the original scale of the object into our originalScale Vector3 variabble
                    originalScale = target.localScale.x;

                    // Set our target scale to be the same as the original for the time being
                    targetScale = target.localScale;
                }
            }
            // If we DO have a target, we release it
            else
            {
                // Reactivate physics for the target object
                target.GetComponent<Rigidbody>().isKinematic = false;

                // Set our target variable to null
                target = null;
            }
        }
    }

    private void HandleResize()
    {
        // If our target is null
        if (target == null)
        {
            // Return from this method, nothing to do here
            return;
        }

        // Cast a ray forward from the camera position, ignore the layer that is used to acquire targets
        // so we don't hit the attached target with our ray

        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, Mathf.Infinity, ignoreMaskDuringResizing))
        {
            // Set the new position of the target by getting the hit point and moving it back a bit
            // depending on the scale and offset factor
            target.position = hit.point - playerCamera.transform.forward * offsetFactor * targetScale.x;

            // Calculate the current distance between the camera and the target object
            float currentDistance = Vector3.Distance(playerCamera.transform.position, target.position);

            // Calculate the ratio between the current distance and the original distance
            float s = currentDistance / originalDistance;

            // Set the scale Vector3 variable to be the ratio of the distances
            targetScale.x = targetScale.y = targetScale.z = s;

            // Set the scale for the target objectm, multiplied by the original scale
            target.localScale = targetScale * originalScale;
        }
    }

    private void Handle_Footsteps()
    {
        if (!characterController.isGrounded)
        {
            return;
        }
        if (currentInput == Vector2.zero)
        {
            return;
        }

        footstepTimer -= Time.deltaTime;

        if (footstepTimer <= 0)
        {
            if (Physics.Raycast(playerCamera.transform.position, Vector3.down, out RaycastHit hit, 3f))
            {
                switch (hit.collider.tag)
                {
                    case "Footsteps/Concrete":
                        footstepAudioSource.PlayOneShot(concreteClips[Random.Range(0, concreteClips.Length - 1)]);
                        break;
                    case "Footsteps/METAL":
                        footstepAudioSource.PlayOneShot(metalClips[Random.Range(0, metalClips.Length - 1)]);
                        break;
                    case "Footsteps/GRASS":
                        footstepAudioSource.PlayOneShot(grassClips[Random.Range(0, grassClips.Length - 1)]);
                        break;
                    default:
                        footstepAudioSource.PlayOneShot(concreteClips[Random.Range(0, concreteClips.Length - 1)]);
                        break;
                }
            }
            // actually we just select a positive number related to footstep = =
            footstepTimer = GetCurrentOffset;
        }
    }

    private void ApplyDamage(float dmg)
    {
        currentHealth -= dmg;

        // the ?.Invoke() make this code more safe because even if there is no one listen to OnDamage, there should be no error now.
        // and the data in the () will be sent to the receiver, so you dont need to code other things to get it.
        OnDamage?.Invoke(currentHealth);

        if (currentHealth <= 0)
        {
            KillPlayer();
        }
        
        else if (regenerateingHealth != null)
        
        {
            StopCoroutine(regenerateingHealth);

            regenerateingHealth = StartCoroutine(RegeneratingHealth());
        }
    }

    // apply when currentHealth = 0;
    private void KillPlayer()
    {
        currentHealth = 0;

        if (regenerateingHealth != null)
        {
            StopCoroutine(regenerateingHealth);
        }

        SceneManager.LoadScene("GameField", LoadSceneMode.Single);
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

        // added it later to fix a problem that if you repeatedly falling off heights your velocity.y will be huge to make you fall instantly.
        if (characterController.velocity.y < -1 && characterController.isGrounded)
        {
            moveDirection.y = 0;
        }

        // if you are falling
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

    private IEnumerator RegeneratingHealth()
    {
        yield return new WaitForSeconds(timeBeforeRegenStarts);
        WaitForSeconds timeTowait = new WaitForSeconds(healthTimeIncrement);

        while(currentHealth < maxHealth)
        {
            currentHealth += healthValueIncrement;

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            OnHeal?.Invoke(currentHealth);
            yield return timeTowait;
        }

        regenerateingHealth = null;
    }

    // Coroutine to generate stamina
    private IEnumerator RegenerateStamina()
    {
        yield return new WaitForSeconds(timeBeforeStaminaRegenStarts);
        WaitForSeconds timeToWait = new WaitForSeconds(staminaTimeIncrement);

        while (currentStamina < maxStamina)
        {
            if (currentStamina > 0)
            {
                canSprint = true;
            }

            currentStamina += staminaValueIncrement;

            if (currentStamina > maxStamina)
            {
                currentStamina = maxStamina;
            }

            OnStaminaChange?.Invoke(currentStamina);

            yield return timeToWait;
        }

        regeneratingStamina = null;
    }

}
