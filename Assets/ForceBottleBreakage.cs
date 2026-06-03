using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceBottleBreakage : MonoBehaviour
{
    private bool forceBreakage = false;

    void Awake()
    {
        // Ensure the bottle is not broken at the start
        forceBreakage = false;
    }

    public void ForceBreakage(bool value)
    {
        forceBreakage = value;
        Debug.Log("[BottleBreaker] Force breakage set to: " + forceBreakage);
    }

    public bool IsForceBreakageEnabled()
    {
        Debug.Log("[BottleBreaker] Breakage Script checked 'force breakage' state and got: " + forceBreakage);
        return forceBreakage;
    }
}
