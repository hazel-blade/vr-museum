using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(CharacterController))]
public class XRCharacterControllerFix : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The XR Origin or Camera Offset transform")]
    [SerializeField] private Transform xrOrigin;
    [Tooltip("The Main Camera or Head transform")]
    [SerializeField] private Transform playerHead;

    [Header("Collider Settings")]
    [SerializeField] private float minHeight = 1f;
    [SerializeField] private float maxHeight = 2.5f;
    [SerializeField] private float additionalHeightAdjustment = 0.2f;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        
        if (xrOrigin == null)
        {
            xrOrigin = transform; // Assuming this is on the XR Origin root
        }
    }

    private void FixedUpdate()
    {
        if (playerHead == null || characterController == null)
            return;

        UpdateCharacterController();
    }

    private void UpdateCharacterController()
    {
        // Calculate the height of the headset relative to the XR Origin
        float headHeight = playerHead.localPosition.y;
        
        // Clamp the height to prevent the collider from becoming too small or too tall
        float targetHeight = Mathf.Clamp(headHeight + additionalHeightAdjustment, minHeight, maxHeight);
        
        characterController.height = targetHeight;

        // Calculate the center of the character controller
        // The center should be exactly half of the height, offset by the headset's local X and Z
        Vector3 newCenter = new Vector3(
            playerHead.localPosition.x,
            targetHeight / 2f,
            playerHead.localPosition.z
        );

        characterController.center = newCenter;
    }
}
