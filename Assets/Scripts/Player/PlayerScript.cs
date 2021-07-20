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
    public GameObject PreviewBlockPrefab;

    private Vector3 playerVelocity;

    private CharacterController controller;
    private Camera playerCamera;
    private WorldGeneratorScript worldGenerator;
    private GameObject previewBlock;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        worldGenerator = FindObjectOfType<WorldGeneratorScript>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire2"))
        {
            ShowPreviewBlock();

            if (Input.GetButtonDown("Fire1"))
            {
                CreateBlock();
            }
        }
        else
        {
            RemovePreviewBlock();

            if (Input.GetButtonDown("Fire1"))
            {
                RemoveBlock();
            }
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

        var gravityValue = worldGenerator.IsDoneCreatingChunks ? -9.81f : 0;
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        transform.Rotate(0, Input.GetAxis("Mouse X"), 0);
        playerCamera.transform.Rotate(-Input.GetAxis("Mouse Y"), 0, 0);
    }

    private void ShowPreviewBlock()
    {
        var position = FindBlockPositionAtCursor(true, out _);
        if (position != null)
        {
            if (previewBlock == null)
            {
                previewBlock = GameObject.Instantiate(PreviewBlockPrefab);
            }
            previewBlock.transform.position = (Vector3Int)position;
        }
        else
        {
            RemovePreviewBlock();
        }
    }

    private void RemovePreviewBlock()
    {
        if (previewBlock != null)
        {
            Destroy(previewBlock);
            previewBlock = null;
        }
    }

    private void CreateBlock()
    {
        if (previewBlock != null)
        {
            worldGenerator.CreateBlock(Vector3Int.RoundToInt(previewBlock.transform.position));
        }
    }

    private void RemoveBlock()
    {
        var position = FindBlockPositionAtCursor(false, out var colliderHit);
        if (position != null)
        {
            var chunk = colliderHit.gameObject.GetComponent<ChunkScript>();
            chunk.SetBlock((Vector3Int)position, 0);
        }
    }

    private Vector3Int? FindBlockPositionAtCursor(bool outsideBlock, out Collider colliderHit)
    {
        var layerMask = 1 << 6; // ignore everything except layer 6
        if (Physics.Raycast(playerCamera.transform.position, playerCamera.transform.rotation * Vector3.forward, out var hitInfo, Mathf.Infinity, layerMask))
        {
            var normal = hitInfo.normal;
            var point = hitInfo.point;
            Vector3 correctedPoint;

            // decides which side of the face should be returned
            if (outsideBlock)
            {
                correctedPoint = point + normal / 2;
            }
            else
            {
                correctedPoint = point - normal / 2;
            }

            colliderHit = hitInfo.collider;
            return Vector3Int.RoundToInt(correctedPoint);
        }

        colliderHit = null;
        return null;
    }
}
