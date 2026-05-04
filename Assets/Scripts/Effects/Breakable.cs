using System.Collections;
using UnityEngine;


public class Breakable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GameObject brokenObject;
    [SerializeField] Material glassMaterial;
    [SerializeField] private GameObject auraGood;
    [SerializeField] private GameObject auraBadLow;
    [SerializeField] private GameObject auraBadHigh;
    [SerializeField] EMGPointer emgPointer;
    [SerializeField] private AudioClip crackingSound;

    [Header("DebugIndicators")]
    [SerializeField] private int objectIntactness = 100;

    [Header("Settings")]
    [SerializeField] private bool enableGraspStatusUpdates = false;
    [SerializeField] private bool enableAuras = false;
    [SerializeField] [Range(0f, 2f)] private float crackVolume = 1f;
    [SerializeField] private bool enableWobbles = true;
    [SerializeField] [Range(0.1f, 2.0f)] private float wobbleDelay = 0.5f;
    [SerializeField] [Range(1f, 100f)] private float maxWobble = 10f;
    [SerializeField] bool enableBreakage = false;
    [SerializeField] [Range(0f, 1f)] private float spawnGracePeriod = 0.3f;

    private Quaternion wobbleTargetAngle;
    private Quaternion baseObjectAngle;
    private bool wobbleActive = false;
    private int adjustmentPerTick = 0;
    private Coroutine adjustmentCoroutine;
    private AudioSource audioSource;
    private int lastIntactnessWhenCrackPlayed = 100;
    private float nextCrackAllowedTime = 0f;
    private float spawnTime;

    public void SetBreakageActive(bool active)
    {
        enableBreakage = active;
    }

    void Awake()
    {
        baseObjectAngle = Quaternion.Euler(Vector3.forward * 0 * 0);
        audioSource = GetComponent<AudioSource>();
        spawnTime = Time.time;
        objectIntactness = 100;
        UpdateGraspStatus(0f);
        adjustmentCoroutine = StartCoroutine(IntactnessAdjustmentLoop());
    }

    void Update()
    {
        if(wobbleActive)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, wobbleTargetAngle, Time.deltaTime);
        }
    }

    public void BreakObject()
    {
        Instantiate(brokenObject, transform.position, transform.rotation);

        InteractiveMole interactiveMole = GetComponent<InteractiveMole>();
        if (interactiveMole != null)
        {
            interactiveMole.FailPop();
        }
            Destroy(gameObject); //or disable it, if we need it to not be destroyed.
        
        // Call any logger functions we need here.
    }

    public void UpdateGraspStatus(float graspStatus)
    {
        switch (graspStatus)
        {
            case >60f and <=100f: // Too tight
                adjustmentPerTick = -25;
                if(enableAuras)
                {
                    auraGood.SetActive(false);
                    auraBadLow.SetActive(false);
                    auraBadHigh.SetActive(true);
                }
                if(enableWobbles)
                {
                    EnableObjectWobble(false);
                }
                break;
            case >=20f and <=60f: // Ideal grasp strength
                adjustmentPerTick = 25;
                if(enableAuras)
                {
                    auraGood.SetActive(true);
                    auraBadLow.SetActive(false);
                    auraBadHigh.SetActive(false);
                }
                if(enableWobbles)
                {
                    EnableObjectWobble(false);
                }
                break;
            case <20f and >0f: // Too Loose
                adjustmentPerTick = -10;
                if(enableAuras)
                {
                    auraGood.SetActive(false);
                    auraBadLow.SetActive(true);
                    auraBadHigh.SetActive(false);
                }
                if(enableWobbles)
                {
                    EnableObjectWobble(true);
                }
                break;
            case <=0 or >100f: // No signal or invalid value
                adjustmentPerTick = 0;
                if(enableAuras)
                {
                    auraGood.SetActive(false);
                    auraBadLow.SetActive(false);
                    auraBadHigh.SetActive(false);
                }
                if(enableWobbles)
                {
                    EnableObjectWobble(false);
                }
                break;
            default:
                adjustmentPerTick = 0;
                if(enableAuras)
                {
                    auraGood.SetActive(false);
                    auraBadLow.SetActive(false);
                    auraBadHigh.SetActive(false);
                }
                if(enableWobbles)
                {
                    EnableObjectWobble(false);
                }
                break;
        }
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
        glassMaterial.SetFloat("_CrackedAmount", (100.0f-objectIntactness)/100.0f);
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
        while (true)
        {
            if (Time.time - spawnTime < spawnGracePeriod)
            {
                objectIntactness = 100;
                lastIntactnessWhenCrackPlayed = objectIntactness;

                if (enableBreakage)
                {
                    UpdatePartialBreakage();
                    float remainingGraceTime = Mathf.Max(0f, spawnGracePeriod - (Time.time - spawnTime));
                    yield return new WaitForSeconds(remainingGraceTime);
                }

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
            objectIntactness += adjustmentPerTick;
            objectIntactness = Mathf.Clamp(objectIntactness, 0, 100);

            if (enableBreakage)
            {
                UpdatePartialBreakage();

                if (enableGraspStatusUpdates && objectIntactness < lastIntactnessWhenCrackPlayed - 20 && Time.time >= nextCrackAllowedTime)
                {
                    PlayCrackingSound();
                    nextCrackAllowedTime = Time.time + 1f; // Set the next allowed crack sound time
                }

                // Check if the object should break
                if (objectIntactness <= 0)
                {
                    BreakObject();
                    yield break; // Exit the coroutine after breaking the object
                }

                yield return new WaitForSeconds(1.0f); // Adjust the frequency of intactness updates as needed
            }
        }
            
    }

    private void EnableObjectWobble(bool Enabled) //Calculates a randomized curve and intensity, which is used to Lerp the object's rotation for a wobble effect (see Update()).
    {
        if(Enabled)
        {
            float intensity = Random.Range(0.1f, maxWobble);
            float curve = Mathf.Sin(Random.Range(0, Mathf.PI * 2));
            wobbleTargetAngle = Quaternion.Euler(Vector3.forward * curve * intensity);
            wobbleActive = true;
        }
        else
        {
            wobbleActive = false;
            transform.rotation = Quaternion.Lerp(transform.rotation, baseObjectAngle, Time.deltaTime);
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
