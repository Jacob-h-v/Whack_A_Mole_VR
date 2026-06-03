using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForceBottleBreakage : MonoBehaviour
{
    private bool forceBreakage = false;

    public void ForceBreakage(bool value)
    {
        forceBreakage = value;
    }

    public bool IsForceBreakageEnabled()
    {
        return forceBreakage;
    }
}
