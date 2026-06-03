using System.Collections;
using System.Data.SqlTypes;
using UnityEngine;


public class Breakable : MonoBehaviour
{
    private ConditionManager conditionManager;

    [Header("References")]
    [SerializeField] private GameObject brokenObject;
    [SerializeField] private Material glassMaterial;
    [SerializeField] private GameObject auraGood;
    [SerializeField] private GameObject auraBadLow;
    [SerializeField] private GameObject auraBadHigh;
    [SerializeField] private EMGPointer emgPointer;
    [SerializeField] private AudioClip crackingSound;
    

    [Header("DebugIndicators")]
    [SerializeField] private int objectIntactness = 100;

    [Header("Settings")]
    [SerializeField] private bool enableGraspStatusUpdates = false;
    [SerializeField] private bool enableAuras = false;
    [SerializeField] private bool enableWobbles = true;
    [SerializeField] [Range(0f, 2f)] private float crackVolume = 1f;
    [SerializeField] [Range(0.1f, 2.0f)] private float wobbleDelay = 0.5f;
    [SerializeField] [Range(1f, 100f)] private float maxWobble = 10f;
    [SerializeField] [Range(1f, 10f)] private float wobbleSpeedMin = 2f;
    [SerializeField] [Range(1f, 10f)] private float wobbleSpeedMax = 5f;
    [SerializeField] [Range(0.1f, 10f)] private float wobbleLerpSpeed = 5f;
    [SerializeField] [Range(0.1f, 50f)] private float minWobble = 1.0f;
    [SerializeField] [Range(0f, 2f)] private float spawnGracePeriod = 0.3f;

    [Header("Ramp Settings")]
    public float rampSpeed = 1.5f;          // exponential growth rate
    public float minRampMultiplier = 0.5f; // starting strength
    public float maxRampMultiplier = 2.5f;  // max scaling
    public int maxDeltaPerTick = 50;        // spike cap

    private Quaternion baseObjectAngle;
    private bool wobbleActive = false;
    private float wobbleIntensity = 0f;
    private float wobbleSpeed;
    private int adjustmentPerTick = 0;
    private Coroutine adjustmentCoroutine;
    private AudioSource audioSource;
    private int lastIntactnessWhenCrackPlayed = 100;
    private float nextCrackAllowedTime = 0f;
    private float spawnGraceEndTime = 0f;
    private bool enableBreakage = false;
    private int healAmount = 20;
    private ForceBottleBreakage bottleBreaker;
    private enum GraspState
    {
        None,
        TooLoose,
        Ideal,
        TooTight
    }

    private GraspState currentState = GraspState.None;
    private float stateStartTime = 0f;

    private void CheckIfBreakageEnabled()
    {
        enableBreakage = conditionManager.GetBreakageEnableState();
    }

    private void CheckMaterialColor()
    {
        Color color = conditionManager.GetMaterialColor();
        glassMaterial.SetColor("_VialColor", color);
    }

    void Awake()
    {
        conditionManager = GameObject.Find("ConditionManager").GetComponent<ConditionManager>();
        CheckIfBreakageEnabled();
        CheckMaterialColor();
        baseObjectAngle = Quaternion.Euler(Vector3.forward * 0 * 0);
        audioSource = GetComponent<AudioSource>();
        objectIntactness = 100;
        spawnGraceEndTime = Time.time + spawnGracePeriod;
        glassMaterial.SetFloat("_CrackedAmount", 0f);
        UpdateGraspStatus(0f);
        adjustmentCoroutine = StartCoroutine(IntactnessAdjustmentLoop());
        bottleBreaker = FindObjectOfType<ForceBottleBreakage>();
    }

    void Update()
    {
        if(wobbleActive)
        {
            float wobbleAngle = Mathf.Sin(Time.time * wobbleSpeed) * wobbleIntensity;
            wobbleAngle = Mathf.Clamp(wobbleAngle, -80f, 80f);
            Quaternion target = baseObjectAngle * Quaternion.Euler(Vector3.forward * wobbleAngle);
            transform.rotation = Quaternion.Lerp(transform.rotation, target, Time.deltaTime * wobbleLerpSpeed);
        }
        else
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, baseObjectAngle, Time.deltaTime * wobbleLerpSpeed);
        }
    }

    public void BreakObject()
    {
        if (enableBreakage)
        {
            Instantiate(brokenObject, transform.position, transform.rotation);
        }

        InteractiveMole interactiveMole = GetComponent<InteractiveMole>();
        if (interactiveMole != null)
        {
            //interactiveMole.FailPop();
        }
            Destroy(gameObject); //or disable it, if we need it to not be destroyed.
        
        // Call any logger functions we need here.
    }
    
    private GraspState ClassifyGrasp(float graspStatus)
    {
        return graspStatus switch
        {
            > 60f and <= 100f => GraspState.TooTight,
            >= 20f and <= 60f => GraspState.Ideal,
            < 20f and > 0f    => GraspState.TooLoose,
            _                 => GraspState.None
        };
    }

    public void UpdateGraspStatus(float graspStatus)
    {
        GraspState newState = ClassifyGrasp(graspStatus);

        if (newState != currentState)
        {
            currentState = newState;
            stateStartTime = Time.time; // reset ramp
        }

        switch (currentState)
        {
            case GraspState.TooTight:
                adjustmentPerTick = -25;
                ToggleAuras(false, false, true);
                EnableObjectWobble(false);
                break;

            case GraspState.Ideal:
                adjustmentPerTick = healAmount;
                ToggleAuras(true, false, false);
                EnableObjectWobble(false);
                break;

            case GraspState.TooLoose:
                adjustmentPerTick = -10;
                ToggleAuras(false, true, false);
                EnableObjectWobble(true);
                break;

            default:
                adjustmentPerTick = -5;
                ToggleAuras(false, false, false);
                EnableObjectWobble(true);
                break;
        }
    }

    public bool IsForceBreakEnabled()
    {
        return bottleBreaker.IsForceBreakageEnabled();
    }

    private void ToggleAuras(bool good, bool low, bool high)
    {
        if (!enableAuras) return;

        auraGood.SetActive(good);
        auraBadLow.SetActive(low);
        auraBadHigh.SetActive(high);
    }

    private float GetRampMultiplier()
    {
        float duration = Time.time - stateStartTime;

        // exponential ease-in
        float ramp = 1f - Mathf.Exp(-duration * rampSpeed);

        return Mathf.Lerp(minRampMultiplier, maxRampMultiplier, ramp);
    }

    public void SetGraspStatusUpdatesEnabled(bool enabled)
    {
        enableGraspStatusUpdates = enabled;
        if (!enableGraspStatusUpdates)
        {
            adjustmentPerTick = 0;
        }
    }

    public void EnableGraspStatusUpdates() => SetGraspStatusUpdatesEnabled(true);
    public void DisableGraspStatusUpdates() => SetGraspStatusUpdatesEnabled(false);
    public bool AreGraspStatusUpdatesEnabled() => enableGraspStatusUpdates;


    private void UpdatePartialBreakage()
    {
        if (enableBreakage)
        {
            glassMaterial.SetFloat("_CrackedAmount", (100.0f-objectIntactness)/100.0f);
        }
    }

    private void UpdateAuraShader()
    {
        // Update the aura shader here based on grasp status
    }

    private void PlayCrackingSound()
    {
        if (audioSource == null || crackingSound == null)
        {
            return;
        }

        if (Time.time < nextCrackAllowedTime)
        {
            return;
        }

        float volumeScale = 1f - (objectIntactness / 100f);
        float scaledVolume = Mathf.Lerp(0.1f, crackVolume, volumeScale);
        audioSource.PlayOneShot(crackingSound, scaledVolume);
        
        nextCrackAllowedTime = Time.time + crackingSound.length;
        lastIntactnessWhenCrackPlayed = objectIntactness;
    }

    private IEnumerator IntactnessAdjustmentLoop()
    {
        if (spawnGracePeriod > 0f)
        {
            yield return new WaitForSeconds(spawnGracePeriod);
        }

        while (true)
        {
            if (Time.time < spawnGraceEndTime)
            {
                objectIntactness = 100;
                yield return null;
                continue;
            }

            if (enableGraspStatusUpdates) 
            {
                if (emgPointer == null)
                {
                    emgPointer = FindObjectOfType<EMGPointer>();
                    if (emgPointer != null)
                    {
                        Debug.Log($"[Breakable:{gameObject.name}] Found EMGPointer by type: {emgPointer.gameObject.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[Breakable:{gameObject.name}] FindObjectOfType<EMGPointer>() failed. No active EMGPointer found in scene.");
                    }
                }

                float mvcPercent = emgPointer != null ? emgPointer.GetCurrentMvcPercent() : 0f;
                UpdateGraspStatus(mvcPercent);
            }

            // Adjust the intactness based on the current grasp adjustment amount
            float rampMultiplier = GetRampMultiplier();
            int adjustedDelta = Mathf.RoundToInt(adjustmentPerTick * rampMultiplier);

            // clamp extreme spikes
            if (IsForceBreakEnabled())
            {
                adjustedDelta = -15;
            }
            else
            {
                adjustedDelta = Mathf.Clamp(adjustedDelta, -maxDeltaPerTick, maxDeltaPerTick);
            }
            
            Debug.Log("[BottleBreaker] Adjusting Bottle Health By Amount: " + adjustedDelta);
            objectIntactness += adjustedDelta;
            Debug.Log("[BottleBreaker] New Bottle Health: " + objectIntactness);

            objectIntactness = Mathf.Clamp(objectIntactness, -100, 100);

            if (enableBreakage)
            {
                UpdatePartialBreakage();

                if (enableGraspStatusUpdates && objectIntactness < lastIntactnessWhenCrackPlayed - 20 && Time.time >= nextCrackAllowedTime)
                {
                    PlayCrackingSound();
                    nextCrackAllowedTime = Time.time + 1f; // Set the next allowed crack sound time
                }
            }

                // // Check if the object should break
                // if (objectIntactness <= 0)
                // {
                //     BreakObject();
                //     yield break; // Exit the coroutine after breaking the object
                // }
                yield return new WaitForSeconds(1.0f); // Adjust the frequency of intactness updates as needed
        }
            
    }

    private void EnableObjectWobble(bool Enabled) //Calculates a randomized curve and intensity, which is used to Lerp the object's rotation for a wobble effect (see Update()).
    {
        if(Enabled)
        {
            wobbleIntensity = Random.Range(minWobble, maxWobble);
            wobbleSpeed = Random.Range(wobbleSpeedMin, wobbleSpeedMax);
            wobbleActive = true;
        }
        else
        {
            wobbleActive = false;
        }
    }

    public bool BreakIfIntactnessBelow(int threshold)
    {
        if (objectIntactness >= threshold)
        {
            return false;
        }

        BreakObject();
        return true;
    } 

    void OnDestroy()
    {
        if (adjustmentCoroutine != null) // Should be unnecessary, but just in case..
        {
            StopCoroutine(adjustmentCoroutine);
        }
    }

    public void SetRampSpeed(float value)
    {
        rampSpeed = value;
    }

    public void SetHealAmount(int value)
    {
        healAmount = value;
    }

    public void SetMinRampMultiplier(float value)
    {
        minRampMultiplier = value;
    }

    public void SetMaxRampMultiplier(float value)
    {
        maxRampMultiplier = value;
    }

    public void SetMaxDeltaPerTick(int value)
    {
        maxDeltaPerTick = value;
    }

    public int GetObjectIntactness() => objectIntactness;
}
