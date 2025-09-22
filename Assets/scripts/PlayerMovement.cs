using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 7f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;

    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;
    private CharacterController characterController;

    private bool canMove = true;
    
    // Variáveis para guardar a velocidade original ao agachar
    private float originalWalkSpeed;
    private float originalRunSpeed;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // MODIFICAÇÃO: O cursor agora começa livre e visível.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Guarda as velocidades originais para restaurá-las depois de agachar
        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;
    }

    void Update()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpPower;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // MODIFICAÇÃO: Lógica de agachar melhorada para não resetar a velocidade toda hora
        if (Input.GetKey(KeyCode.R) && canMove)
        {
            characterController.height = crouchHeight;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            characterController.height = defaultHeight;
            walkSpeed = originalWalkSpeed;
            runSpeed = originalRunSpeed;
        }

        characterController.Move(moveDirection * Time.deltaTime);

        // MODIFICAÇÃO: Toda a lógica de câmera agora está condicionada ao botão direito do mouse.
        if (canMove)
        {
            // Verifica se o botão direito do mouse (1) está sendo pressionado
            if (Input.GetMouseButton(1))
            {
                // Trava e esconde o cursor ENQUANTO o botão estiver pressionado
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                // Executa a lógica de rotação da câmera
                rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
            }
            else
            {
                // Libera e mostra o cursor QUANDO o botão for solto
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}