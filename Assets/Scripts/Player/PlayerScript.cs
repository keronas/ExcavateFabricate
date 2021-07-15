using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[RequireComponent(typeof (CharacterController))]
public class PlayerScript : MonoBehaviour
{
    public float PlayerSpeed = 10;
    public float JumpSpeed = 8;

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
        if (Input.GetButton("Fire1"))
        {
            RemoveBlock();
        }

        var groundedPlayer = controller.isGrounded;
        if (groundedPlayer && playerVelocity.y < 0)
        {
            playerVelocity.y = 0f;
        }
        if (Input.GetButton("Jump") && groundedPlayer)
        {
            playerVelocity.y = JumpSpeed;
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

    private void RemoveBlock()
    {
        var layerMask = 1 << 6; // ignore everything except layer 6
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.rotation * Vector3.forward, out var hitInfo, Mathf.Infinity, layerMask))
        {
            var normal = hitInfo.normal;
            var point = hitInfo.point;

            var correctedPoint = point - normal / 2; // moves point from face of the block to the middle of it, to decide which side of face is the correct block 
            var blockPosition = Vector3Int.RoundToInt(correctedPoint);

            var chunk = hitInfo.collider.gameObject.GetComponent<ChunkScript>();
            chunk.RemoveBlock(blockPosition);
        }
    }
}
