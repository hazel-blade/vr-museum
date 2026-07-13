using System.Collections;
using UnityEngine;

public class SawCutter : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 0.7f;
    [SerializeField] private AudioSource cuttingSound;

    private bool hasCut;

    private void OnTriggerEnter(Collider other)
    {
        if (hasCut) return;

        if (other.CompareTag("CuttableWood"))
        {
            hasCut = true;
            StartCoroutine(CutWood(other.gameObject));
        }
    }

    private IEnumerator CutWood(GameObject wood)
    {
        if (cuttingSound != null)
        {
            cuttingSound.Play();
        }

        yield return new WaitForSeconds(destroyDelay);

        wood.SetActive(false);
    }
}