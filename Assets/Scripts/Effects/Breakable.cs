using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEngine;


public class Breakable : MonoBehaviour
{
    [SerializeField] GameObject brokenObject;
    private int objectIntactness = 100;
    private int adjustmentPerTick = 0;
    private Coroutine adjustmentCoroutine;

    void Awake()
    {
        objectIntactness = 100;
        UpdateGraspStatus("NoGrasp");
        adjustmentCoroutine = StartCoroutine(IntactnessAdjustmentLoop());
    }

    public void BreakObject()
    {
        Instantiate(brokenObject, transform.position, transform.rotation);
        Destroy(gameObject); //or disable it, if we need it to not be destroyed.

        // Call any logger functions we need here.
    }

    public void UpdateGraspStatus(string graspStatus)
    {
        switch (graspStatus?.Trim())
        {
            case "TightGrasp":
                adjustmentPerTick = -25;
                break;
            case "IdealGrasp":
                adjustmentPerTick = 25;
                break;
            case "LooseGrasp":
                adjustmentPerTick = -10;
                break;
            case "NoGrasp":
                adjustmentPerTick = -10;
                break;
            default:
                adjustmentPerTick = -10;
                break;
        }
        UpdatePartialBreakage();
        UpdateAuraShader();
    }

    private void UpdatePartialBreakage()
    {
        // Update shader here for partial breakage visual
    }

    private void UpdateAuraShader()
    {
        // Update the aura shader here based on grasp status
    }

    private IEnumerator IntactnessAdjustmentLoop()
    {
        while (objectIntactness > 0)
        {
            // Adjust the intactness based on the current grasp adjustment amount
            objectIntactness += adjustmentPerTick;

            // Check if the object should break
            if (objectIntactness <= 0)
            {
                BreakObject();
                yield break; // Exit the coroutine after breaking the object
            }

            yield return new WaitForSeconds(1.0f); // Adjust the frequency of intactness updates as needed
        }
    }

    void OnDestroy()
    {
        if (adjustmentCoroutine != null) // Should be unnecessary, but just in case..
        {
            StopCoroutine(adjustmentCoroutine);
        }
    }
}
