using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

[RequireComponent(typeof (CharacterController))]
public class PlayerScript : MonoBehaviour
{
    public float PlayerSpeed = 10;
    public float JumpSpeed = 8;
    public GameObject PreviewBlockPrefab;
    public WorldGeneratorScript WorldGenerator;
    public GameObject[] BlockTypePanels;
    public RectTransform BlockDestroyProgressPanel;

    private Vector3 playerVelocity;
    private CharacterController controller;
    private Camera playerCamera;
    private GameObject previewBlock;
    private float defaultBlockTypePanelHeight;
    private byte chosenBlockType;
    private Vector3Int? blockToDestroy;
    private DateTime blockDestroyStartTime;
    private TimeSpan blockDestroyDuration;
    private float BlockDestroyProgress => Mathf.Min(1, (float)((DateTime.Now - blockDestroyStartTime).TotalMilliseconds / blockDestroyDuration.TotalMilliseconds));
    private float defaultBlockDestroyProgressPanelWidth;

    public float CameraRotationX
    {
        get
        {
            return playerCamera.transform.rotation.eulerAngles.x;
        }
        set
        {
            var angles = playerCamera.transform.rotation.eulerAngles;
            angles.x = value;
            playerCamera.transform.rotation = Quaternion.Euler(angles);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;

        playerCamera.farClipPlane = (WorldGenerator.ChunkViewDistance - 2) * WorldGenerator.ChunkSettings.ChunkSize;

        foreach(var (color, panel) in WorldGenerator.ChunkSettings.BlockColors.Zip(BlockTypePanels, (color, panel) => (color, panel)))
        {
            panel.GetComponent<Image>().color = color;
        }

        defaultBlockDestroyProgressPanelWidth = BlockDestroyProgressPanel.rect.width;

        defaultBlockTypePanelHeight = BlockTypePanels[0].GetComponent<RectTransform>().rect.height;
        ChooseBlockType(1);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButton("Fire2"))
        {
            StopDestroyingBlock();
            ShowPreviewBlock();

            if (Input.GetButtonDown("Fire1"))
            {
                CreateBlock();
            }
        }
        else
        {
            RemovePreviewBlock();

            if (Input.GetButton("Fire1"))
            {
                ProgressDestroyingBlock();
            }
            else
            {
                StopDestroyingBlock();
            }
        }

        for (int i = 0; i < BlockTypePanels.Length; i++)
        {
            if (Input.GetButtonDown($"Select{i + 1}"))
            {
                ChooseBlockType((byte)(i + 1));
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

        var gravityValue = WorldGenerator.IsDoneCreatingChunks ? -9.81f : 0;
        playerVelocity.y += gravityValue * Time.deltaTime;
        controller.Move(playerVelocity * Time.deltaTime);

        transform.Rotate(0, Input.GetAxis("Mouse X"), 0);
        playerCamera.transform.Rotate(-Input.GetAxis("Mouse Y"), 0, 0);
    }

    private void ChooseBlockType(byte type)
    {
        var selectedPanelSizeDifference = 40;
        var rect = BlockTypePanels[type - 1].GetComponent<RectTransform>();
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, defaultBlockTypePanelHeight + selectedPanelSizeDifference);
        foreach (GameObject panel in BlockTypePanels.Except(new[] { BlockTypePanels[type - 1] }))
        {
            panel.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, defaultBlockTypePanelHeight);
        }
        chosenBlockType = type;
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
            WorldGenerator.CreateBlock(Vector3Int.RoundToInt(previewBlock.transform.position), chosenBlockType);
        }
    }

    private void ProgressDestroyingBlock()
    {
        var position = FindBlockPositionAtCursor(false, out var colliderHit);
        
        if (position != null)
        {
            var chunk = colliderHit.gameObject.GetComponent<ChunkScript>();
            if (position == blockToDestroy)
            {
                if (BlockDestroyProgress >= 1)
                {
                    chunk.SetBlock((Vector3Int)position, 0);
                    StopDestroyingBlock();
                }
                else
                {
                    BlockDestroyProgressPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, defaultBlockDestroyProgressPanelWidth * BlockDestroyProgress);
                }
            }
            else
            {
                blockToDestroy = position;
                blockDestroyStartTime = DateTime.Now;
                blockDestroyDuration = chunk.GetBlockDestroyDuration((Vector3Int)position);
            }
        }
        else
        {
            StopDestroyingBlock();
        }
    }

    private void StopDestroyingBlock()
    {
        blockToDestroy = null;
        BlockDestroyProgressPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 0);
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
