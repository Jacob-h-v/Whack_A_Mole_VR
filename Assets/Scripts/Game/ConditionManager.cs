using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject pressureGauge;
    [SerializeField] Breakable breakableScript;
    [SerializeField] Material glassMaterial;

    [Header("Condition Overview")]
    [SerializeField] bool baselineCondition = false;
    [SerializeField] bool framingCondition = false;
    [SerializeField] bool lossAversionCondition = false;
    [SerializeField] bool anchoringCondition = false;
    [SerializeField] bool debugDevCondition = false;

    void Start()
    {
        if (baselineCondition)
        {
            SetBaselineCondition();
        }
        if(framingCondition)
        {
            SetFramingCondition();
        }
        if(lossAversionCondition)
        {
            SetLossAversionCondition();
        }
        if(anchoringCondition)
        {
            SetAnchoringCondition();
        }
        if(debugDevCondition)
        {
            SetDebugDevCondition();
        }
    }

    public void SetBaselineCondition() // Condition 0
    {
        baselineCondition = true;
        framingCondition = false;
        lossAversionCondition = false;
        anchoringCondition = false;
        debugDevCondition = false;
        UpdateConditionEnvironment(0);
    }

    public void SetFramingCondition() // Condition 1
    {
        baselineCondition = false;
        framingCondition = true;
        lossAversionCondition = false;
        anchoringCondition = false;
        debugDevCondition = false;
        UpdateConditionEnvironment(1);
    }

    public void SetLossAversionCondition() // Condition 2
    {
        baselineCondition = false;
        framingCondition = false;
        lossAversionCondition = true;
        anchoringCondition = false;
        debugDevCondition = false;
        UpdateConditionEnvironment(2);
    }

    public void SetAnchoringCondition() // Condition 3
    {
        baselineCondition = false;
        framingCondition = false;
        lossAversionCondition = false;
        anchoringCondition = true;
        debugDevCondition = false;
        UpdateConditionEnvironment(3);
    }

    public void SetDebugDevCondition() // Condition 4
    {
        baselineCondition = false;
        framingCondition = false;
        lossAversionCondition = false;
        anchoringCondition = false;
        debugDevCondition = true;
        UpdateConditionEnvironment(4);

    }

    private void UpdateConditionEnvironment(int condition)
    {
        switch (condition)
        {
            case 0: // Baseline Condition
                pressureGauge.SetActive(false);
                breakableScript.SetBreakageActive(false);
                break;
            case 1: // Framing Condition
                pressureGauge.SetActive(false);
                breakableScript.SetBreakageActive(false);
                glassMaterial.SetColor("_VialColor", new Color(231, 213, 66, 210));
                break;
            case 2: // Loss Aversion Condition
                pressureGauge.SetActive(false);
                breakableScript.SetBreakageActive(true);
                glassMaterial.SetColor("_VialColor", new Color(187, 226, 206, 210));
                break;
            case 3: // Anchoring Condition
                pressureGauge.SetActive(true);
                breakableScript.SetBreakageActive(false);
                glassMaterial.SetColor("_VialColor", new Color(187, 226, 206, 210));
                break;
            case 4: // Debug/Dev Condition
                pressureGauge.SetActive(true);
                breakableScript.SetBreakageActive(true);
                glassMaterial.SetColor("_VialColor", new Color(187, 226, 206, 210));
                break;
            default:
                pressureGauge.SetActive(true);
                breakableScript.SetBreakageActive(true);
                glassMaterial.SetColor("_VialColor", new Color(187, 226, 206, 210));
                break;
        }
    }
}
