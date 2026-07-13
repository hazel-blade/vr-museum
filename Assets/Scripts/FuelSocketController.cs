using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class FuelSocketController : MonoBehaviour
{
    [Header("Required Item")]
    [SerializeField] private string requiredTag = "FuelCan";

    [Header("Objects To Activate")]
    [SerializeField] private GameObject[] objectsToActivate;

    [Header("Lights To Enable")]
    [SerializeField] private Light[] lightsToEnable;

    [Header("Optional Audio")]
    [SerializeField] private AudioSource activationSound;

    private bool isActivated;

    private void Start()
{
    foreach (GameObject target in objectsToActivate)
    {
        if (target != null)
            target.SetActive(false);
    }

    foreach (Light targetLight in lightsToEnable)
    {
        if (targetLight != null)
            targetLight.enabled = false;
    }
}

    public void OnFuelInserted(SelectEnterEventArgs args)
    {
        if (isActivated)
            return;

        GameObject insertedObject =
            args.interactableObject.transform.gameObject;

        if (!insertedObject.CompareTag(requiredTag))
        {
            Debug.Log(
                $"Wrong item inserted: {insertedObject.name}"
            );

            return;
        }

        ActivateGenerator();
    }

    private void ActivateGenerator()
    {
        isActivated = true;

        foreach (GameObject target in objectsToActivate)
        {
            if (target != null)
                target.SetActive(true);
        }

        foreach (Light targetLight in lightsToEnable)
        {
            if (targetLight != null)
                targetLight.enabled = true;
        }

        if (activationSound != null)
            activationSound.Play();

        Debug.Log("Fuel inserted. Generator activated.");
    }
}