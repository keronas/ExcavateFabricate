using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public float PlayerSpeed;

    private Vector3 playerVelocity;
    private CharacterController controller;
    private Camera playerCamera;

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        var groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }

        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        move = transform.TransformVector(move);
        controller.Move(move * Time.deltaTime * PlayerSpeed);

        var gravityValue = -9.81f;
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        transform.Rotate(0, Input.GetAxis("Mouse X"), 0);
        playerCamera.transform.Rotate(-Input.GetAxis("Mouse Y"), 0, 0);
    }
}
