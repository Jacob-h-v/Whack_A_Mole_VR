using System.Collections;
using System.Collections.Generic;
using System.Drawing.Text;
using UnityEngine;


public class Breakable : MonoBehaviour
{
    [SerializeField] GameObject brokenObject;
    [SerializeField] Material glassMaterial;
    [SerializeField] private int objectIntactness = 100;
    [SerializeField] EMGPointer emgPointer;
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

        InteractiveMole interactiveMole = GetComponent<InteractiveMole>();
        if (interactiveMole != null)
        {
            interactiveMole.FailPop();
        }
        else
        {
            Destroy(gameObject); //or disable it, if we need it to not be destroyed.
        }
        
        // Call any logger functions we need here.
    }

    public void UpdateGraspStatus(float graspStatus)
    {
        switch (graspStatus)
        {
            case >60f and <=100f:
                adjustmentPerTick = -25;
                break;
            case >=20f and <=60f:
                adjustmentPerTick = 25;
                break;
            case <20f:
                adjustmentPerTick = -10;
                break;
            case <=0 or >100f:
                adjustmentPerTick = 0;
                break;
            default:
                adjustmentPerTick = 0;
                break;
        }
    }

    private void UpdatePartialBreakage()
    {
        glassMaterial.SetFloat("_CrackedAmount", (100.0f-objectIntactness)/100.0f);
    }

    private void UpdateAuraShader()
    {
        // Update the aura shader here based on grasp status
    }

    private IEnumerator IntactnessAdjustmentLoop()
    {
        while (objectIntactness > 0)
        {
            if (emgPointer == null)
            {
                emgPointer = FindObjectOfType<EMGPointer>();
            }
            float mvcPercent = emgPointer != null ? emgPointer.GetCurrentMvcPercent() : 0f;
            UpdateGraspStatus(mvcPercent);

            // Adjust the intactness based on the current grasp adjustment amount
            objectIntactness += adjustmentPerTick;
            objectIntactness = Mathf.Clamp(objectIntactness, 0, 100);
            UpdatePartialBreakage();
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
