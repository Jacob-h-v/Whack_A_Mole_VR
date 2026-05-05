using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionManager : MonoBehaviour
{
    private enum ConditionType
    {
        Baseline,
        Framing,
        LossAversion,
        Anchoring,
        Debug
    }

    private Color materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);

    [Header("References")]
    [SerializeField] GameObject pressureGauge;

    [Header("Condition Overview")]
    [SerializeField] ConditionType condition;

    private bool conditionManagerBreakageEnabled = false; //inspector?


    [Header("Condition Canvas")]
    [SerializeField] private GameObject LossAversionCanvas;
    [SerializeField] private GameObject AnchoringCanvas;
    [SerializeField] private GameObject FramingCanvas;


    public bool GetBreakageEnableState()
    {
        bool breakageState = conditionManagerBreakageEnabled;
        return breakageState;
    }

    public Color GetMaterialColor()
    {
        Color color = materialColor;
        return color;
    }

    void Start()
    {
        UpdateConditionEnvironment((int)condition);
    }

    public void SetBaselineCondition() // Condition 0, Baseline
    {
        UpdateConditionEnvironment(0);
    }

    public void SetFramingCondition() // Condition 1, Framing
    {
        UpdateConditionEnvironment(1);
        Instantiate(FramingCanvas);
    }

    public void SetLossAversionCondition() // Condition 2, Loss Aversion
    {
        UpdateConditionEnvironment(2);
        Instantiate(LossAversionCanvas);
    }

    public void SetAnchoringCondition() // Condition 3, Anchoring
    {
        UpdateConditionEnvironment(3);
        Instantiate(AnchoringCanvas);
    }

    public void SetDebugDevCondition() // Condition 4, Debug/Dev
    {
        UpdateConditionEnvironment(4);

    }

    private void UpdateConditionEnvironment(int condition)
    {
        switch (condition)
        {
            case 0: // Baseline Condition
                pressureGauge.SetActive(false);
                conditionManagerBreakageEnabled = false;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            case 1: // Framing Condition
                pressureGauge.SetActive(false);
                conditionManagerBreakageEnabled = false;
                materialColor = new Color(231f/255f, 213f/255f, 66f/255f, 210f/255f);
                break;
            case 2: // Loss Aversion Condition
                pressureGauge.SetActive(false);
                conditionManagerBreakageEnabled = true;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            case 3: // Anchoring Condition
                pressureGauge.SetActive(true);
                conditionManagerBreakageEnabled = false;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            case 4: // Debug/Dev Condition
                pressureGauge.SetActive(true);
                conditionManagerBreakageEnabled = true;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            default:
                pressureGauge.SetActive(true);
                conditionManagerBreakageEnabled = true;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
        }
    }
}
