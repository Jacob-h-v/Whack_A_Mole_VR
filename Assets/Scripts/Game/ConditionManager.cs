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
    [SerializeField] private GameObject pressureGauge;
    [SerializeField] private EMGPointer emgPointerRight; // Drag Right Controller's EMGPointer script here

    [Header("Condition Overview")]
    [SerializeField] ConditionType condition;

    private bool conditionManagerBreakageEnabled = false; //inspector?


    [Header("Condition Canvas")]
    [SerializeField] private GameObject LossAversionCanvas;
    [SerializeField] private GameObject AnchoringCanvas;
    [SerializeField] private GameObject FramingCanvas;
    [SerializeField] public Transform canvasSpawnPoint;
    

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
        SpawnConditionCanvas(FramingCanvas);
    }

    public void SetLossAversionCondition() // Condition 2, Loss Aversion
    {
        UpdateConditionEnvironment(2);
        SpawnConditionCanvas(LossAversionCanvas);
    }

    public void SetAnchoringCondition() // Condition 3, Anchoring
    {
        UpdateConditionEnvironment(3);
        SpawnConditionCanvas(AnchoringCanvas);
    }

    public void SetDebugDevCondition() // Condition 4, Debug/Dev
    {
        UpdateConditionEnvironment(4);

    }

    private void UpdateConditionEnvironment(int condition)
    {
        // 1. Dynamically find the Pressure Gauge in the active scene, even if it is disabled.
        PressureGaugeAnimator gaugeScript = FindObjectOfType<PressureGaugeAnimator>(true);
        if (gaugeScript != null)
        {
            pressureGauge = gaugeScript.gameObject;
        }
        else
        {
            Debug.LogWarning("ConditionManager: Could not find the PressureGauge in the active scene!");
        }

        // 2. Apply the condition logic
        switch (condition)
        {
            case 0: // Baseline Condition
                if (pressureGauge != null) pressureGauge.SetActive(false);
                if (emgPointerRight != null) emgPointerRight.SetDwellTime(9.5f); // Fast dwell time for Baseline
                conditionManagerBreakageEnabled = false;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            case 1: // Framing Condition
                if (pressureGauge != null) pressureGauge.SetActive(false);
                if (emgPointerRight != null) emgPointerRight.SetDwellTime(9.5f); // Restore normal dwell time
                conditionManagerBreakageEnabled = false;
                materialColor = new Color(231f/255f, 213f/255f, 66f/255f, 210f/255f);
                break;
            case 2: // Loss Aversion Condition
                if (pressureGauge != null) pressureGauge.SetActive(false);
                if (emgPointerRight != null) emgPointerRight.SetDwellTime(9.5f);
                conditionManagerBreakageEnabled = true;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            case 3: // Anchoring Condition
                if (pressureGauge != null) pressureGauge.SetActive(true);
                if (emgPointerRight != null) emgPointerRight.SetDwellTime(9.5f);
                conditionManagerBreakageEnabled = false;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            case 4: // Debug/Dev Condition
                if (pressureGauge != null) pressureGauge.SetActive(true);
                if (emgPointerRight != null) emgPointerRight.SetDwellTime(9.5f);
                conditionManagerBreakageEnabled = true;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
            default:
                if (pressureGauge != null) pressureGauge.SetActive(true);
                if (emgPointerRight != null) emgPointerRight.SetDwellTime(9.5f);
                conditionManagerBreakageEnabled = true;
                materialColor = new Color(187f/255f, 226f/255f, 206f/255f, 210f/255f);
                break;
        }
    }

    private void SpawnConditionCanvas(GameObject prefab)
    {
        if (prefab == null) return;
        
        if (canvasSpawnPoint == null) 
        {
            Debug.LogError("ConditionManager: Canvas Spawn Point is not assigned!");
            return;
        }

        // 1. Instantiate as a child of the canvasSpawnPoint
        GameObject spawnedCanvas = Instantiate(prefab, canvasSpawnPoint);

        // 2. Force the transform to absolutely zero out any prefab offsets
        RectTransform rectTransform = spawnedCanvas.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            // Optionally lock scale to (1,1,1) if your prefabs scale inconsistently:
            // rectTransform.localScale = Vector3.one; 
        }
        else
        {
            // Fallback for non-UI objects, just in case
            spawnedCanvas.transform.localPosition = Vector3.zero;
            spawnedCanvas.transform.localRotation = Quaternion.identity;
        }

        // 3. Get the Canvas component and assign the VR Camera
        Canvas canvas = spawnedCanvas.GetComponent<Canvas>();
        if (canvas != null)
        {
            Camera vrCamera = Camera.main; 

            if (vrCamera != null)
            {
                canvas.worldCamera = vrCamera;
            }
            else
            {
                Debug.LogError("ConditionManager: Could not find a camera tagged 'MainCamera'!");
            }
        }
    }
}
