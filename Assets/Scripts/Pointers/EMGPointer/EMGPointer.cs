using System;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR;

/*
Implementation of the pointer abstract class to handle the EMG Pointer.
Used to control the Pointer with EMG data from the armband.
*/
public class EMGPointer : Pointer
{
    [SerializeField]
    private GameObject virtualHandPrefab;
    private GameObject virtualHand;

    [SerializeField]
    [Tooltip("Local offset from tracker/controller pivot to the spawned virtual hand root.")]
    private Vector3 virtualHandLocalOffset = new Vector3(0f, -0.45f, 0f);

    [SerializeField] private GameObject SteamVRVisualHand;
    [SerializeField] private EMGDataProcessor emgDataProcessor;
    [SerializeField] private EMGPointerBehavior emgPointerBehavior;
    [SerializeField] private bool recordMaximumEMG = true; // If true, records the maximum EMG value reached during the session.
    [SerializeField] private float maxEMG = 0.0f;
    [SerializeField][Range(0f, 1f)] private float emgThreshold = 0.3f; // Threshold above which the EMG signal is considered as a muscle activation (0-1).
    [SerializeField] private bool followUltimateTracker = true;
    [SerializeField] private float trackerSearchInterval = 1f;
    [SerializeField] private string trackerSerialContains = "";
    [SerializeField] private bool debugTrackerAndHandCoords = false;
    [SerializeField] private float debugCoordsLogInterval = 0.2f;

    private AIServerInterface aiServerInterface;
    private EMGClassifiedGestureManager emgClassifiedGestureManager;
    private const string DEFAULT_GESTURE = "Neutral"; // Default gesture when no mole is hovered over. 
    private string moleHoveringGesture = DEFAULT_GESTURE; // Current gesture of the mole being hovered over (only in Training mode).
    private string gestureConfidence = "Uncertain";
    private string thresholdState = "below";
    private int trackerDeviceIndex = -1;
    private float nextTrackerSearchTime;
    private float nextDebugCoordsLogTime;
    private SteamVR_Behaviour_Pose steamVRPose;
    private bool steamVRPoseWasEnabled;

    private void Awake()
    {
        steamVRPose = GetComponent<SteamVR_Behaviour_Pose>();
    }

    private void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
    }

    void Update()
    {
        // Update max EMG if recording is enabled
        if (recordMaximumEMG) maxEMG = Mathf.Max(maxEMG, (float)emgDataProcessor.GetSmoothedAbsAverage());
        thresholdState = IsAboveThreshold(emgDataProcessor.GetSmoothedAbsAverage()) ? "above" : "below";

        // Disable default visual hand when EMG pointer enabled
        if (SteamVRVisualHand != null && SteamVRVisualHand.activeSelf) SteamVRVisualHand.SetActive(false);

        // Update Gesture Visual based on current behavior (only if gesture manager is ready)
        if (emgClassifiedGestureManager != null && emgClassifiedGestureManager.IsInitialized)
        {
            HandGestureState currentGesture = GetCurrentGesture();
            emgClassifiedGestureManager.SetPose(currentGesture);
        }
    }

    public float GetCurrentEMGSmoothedAverage() => (float)emgDataProcessor.GetSmoothedAbsAverage();

    public override void Enable()
    {
        if (active) return;

        if (followUltimateTracker)
        {
            SteamVR.Initialize();
            ResolveTrackerDeviceIndex(force: true);
            if (steamVRPose != null)
            {
                steamVRPoseWasEnabled = steamVRPose.enabled;
                steamVRPose.enabled = false;
            }
        }

        if (laserMapper != null) laserMapper.GetComponentInChildren<Canvas>().enabled = false; //Disable visual components of laserMapper only for EMG pointer
        if (virtualHand != null) Destroy(virtualHand);
        if (virtualHandPrefab != null)
        {
            virtualHand = Instantiate(virtualHandPrefab, transform);
            virtualHand.transform.localPosition = virtualHandLocalOffset;
            emgClassifiedGestureManager = virtualHand.GetComponent<EMGClassifiedGestureManager>();

            Transform visualStick = virtualHand.transform.Find("VisualStick");
            if (visualStick != null)
            {
                Renderer renderer = visualStick.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    renderer.material = laserMaterial;
                    renderer.material.color = startLaserColor;
                }
            }
            else Debug.LogError("No 'VisualStick' found in the virtual hand prefab.");

            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleEntered += OnHoverEnter;
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleExited += OnHoverExit;
            virtualHand.GetComponent<VirtualHandTrigger>().TriggerOnMoleStay += OnHoverStay;
        }
        else Debug.LogError("No virtual hand prefab assigned to the EMG Pointer.");

        if (aiServerInterface == null) aiServerInterface = new AIServerInterface(emgDataProcessor.thalmicMyo);
        ChangeBehavior(emgPointerBehavior);

        base.Enable();
    }

    public override void Disable()
    {
        if (!active) return;

        if (steamVRPose != null)
        {
            steamVRPose.enabled = steamVRPoseWasEnabled;
        }

        if (virtualHand != null)
        {
            Destroy(virtualHand);
            virtualHand = null;
        }

        CancelInvoke(nameof(StartPredictionRequestCoroutine));

        base.Disable();

        if (SteamVRVisualHand != null)
        {
            SteamVRVisualHand.SetActive(true); // Re-enable default visual hand when EMG pointer disabled
        }
    }

    private void OnHoverEnter(Mole mole)
    {
        Debug.Log($"[EMGPointer:{gameObject.name}] Hover enter detected on mole '{mole.gameObject.name}' (id={mole.GetId()}, state={mole.GetState()}).");
        mole.OnHoverEnter();
        dwellStartTimer = Time.time;
        if (mole.GetState() == Mole.States.Enabled)
        {
            moleHoveringGesture = mole.GetValidationArg();

            loggerNotifier.NotifyLogger("Pointer Hover Begin", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
            {
                {"ControllerHover", mole.GetId().ToString()},
                {"ControllerName", gameObject.name}
            });
        }
    }

    private void OnHoverExit(Mole mole)
    {
        mole.SetLoadingValue(0);
        mole.OnHoverLeave();

        moleHoveringGesture = DEFAULT_GESTURE;

        loggerNotifier.NotifyLogger("Pointer Hover End", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
        {
            {"ControllerHover", mole.name},
            {"ControllerName", gameObject.name}
        });
    }

    private void OnHoverStay(Mole mole)
    {
        if (mole.checkShootingValidity(GetCurrentGesture().ToString()))
        {
            // If the EMG signal is below the threshold, reset the dwell timer.
            if (emgDataProcessor.GetSmoothedAbsAverage() < (emgThreshold * maxEMG)) dwellStartTimer = Time.time;

            mole.SetLoadingValue((Time.time - dwellStartTimer) / dwellTime);
            if ((Time.time - dwellStartTimer) > dwellTime)
            {
                pointerShootOrder++;
                loggerNotifier.NotifyLogger(overrideEventParameters: new Dictionary<string, object>(){
                                {"ControllerSmoothed", directionSmoothed},
                                {"ControllerAimAssistState", System.Enum.GetName(typeof(Pointer.AimAssistStates), aimAssistState)},
                                {"LastShotControllerRawPointingDirectionX", transform.forward.x},
                                {"LastShotControllerRawPointingDirectionY", transform.forward.y},
                                {"LastShotControllerRawPointingDirectionZ", transform.forward.z}
                            });

                loggerNotifier.NotifyLogger("Pointer Shoot", EventLogger.EventType.PointerEvent, new Dictionary<string, object>()
                            {
                                {"PointerShootOrder", pointerShootOrder},
                                {"ControllerName", gameObject.name}
                            });
                OnHoverExit(mole);
                Shoot(mole);
            }
        }
    }

    public void ResetMaxEMG() // Called by CALIBRATION:(TYPE=MAXEMG)
    {
        maxEMG = 0.0f;
        recordMaximumEMG = true;
    }

    public void StopRecordingMaxEMG() // Called by CALIBRATION:(TYPE=STOPEMG)
    {
        recordMaximumEMG = false;
    }

    public float GetCurrentMvcRatio()
    {
        if (emgDataProcessor == null || maxEMG <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)(emgDataProcessor.GetSmoothedAbsAverage() / maxEMG));
    }

    public float GetCurrentMvcPercent() => GetCurrentMvcRatio() * 100f;

    public float GetMaxEMG() => maxEMG;

    private bool IsAboveThreshold(float emgIntensity) => (emgIntensity >= (emgThreshold * maxEMG));
    
    public string getThresholdState()
    {
        if (!active)
        {
            return "inactive";
        }
        return thresholdState;
    }

    public void ChangeBehavior(EMGPointerBehavior newBehavior)
    {
        // Always cancel the previous behavior
        CancelInvoke(nameof(StartPredictionRequestCoroutine));
        Debug.Log("EMG Pointer behavior changed to: " + newBehavior);

        switch (newBehavior)
        {
            case EMGPointerBehavior.LivePrediction:
                InvokeRepeating(nameof(StartPredictionRequestCoroutine), 0f, 0.004f);
                break;

            case EMGPointerBehavior.Training:
                gestureConfidence = "Training"; // No confidence in training mode
                break;

            default:
                Debug.LogError("Unknown EMG Pointer behavior: " + newBehavior);
                break;
        }

        emgPointerBehavior = newBehavior;
    }

    private void StartPredictionRequestCoroutine() => aiServerInterface.StartPredictionRequestCoroutine();

    public HandGestureState GetCurrentGesture()
    {
        // Return Unknown if not fully initialized
        if (!active || aiServerInterface == null)
        {
            return HandGestureState.Unknown;
        }

        string currentGestureString;

        switch (emgPointerBehavior)
        {
            case EMGPointerBehavior.LivePrediction:
                currentGestureString = IsAboveThreshold(emgDataProcessor.GetSmoothedAbsAverage()) ? aiServerInterface.GetCurrentGesture() : DEFAULT_GESTURE;
                gestureConfidence = aiServerInterface.GetCurrentGestureProb();
                break;

            case EMGPointerBehavior.Training:
                currentGestureString = IsAboveThreshold(emgDataProcessor.GetSmoothedAbsAverage()) ? moleHoveringGesture : DEFAULT_GESTURE;
                gestureConfidence = "Training";
                break;

            default:
                Debug.LogError("Unknown EMG Pointer behavior: " + emgPointerBehavior);
                gestureConfidence = "Uncertain";
                return HandGestureState.Unknown;
        }

        // Try to get gesture enum from string, ignore case
        if (!Enum.TryParse(currentGestureString, true, out HandGestureState handGestureState))
        {
            Debug.LogWarning("/!\\ Unrecognized gesture: " + currentGestureString);
            gestureConfidence = "Uncertain";
            return HandGestureState.Unknown; // Return Unknown if parsing fails
        }
        else return handGestureState; // Return parsed gesture
    }

    public string GetCurrentGestureConfidence()
    {
        if (!active || aiServerInterface == null)
        {
            return "Uninitialized";
        }
        return gestureConfidence;
    }

    public Transform GetVirtualHandTransform()
    {
        return virtualHand != null ? virtualHand.transform : null;
    }

    private void LateUpdate()
    {
        if (followUltimateTracker && active)
        {
            UpdateTrackerPoseOverride();
        }
    }

    private void OnBeforeRender()
    {
        if (followUltimateTracker && active)
        {
            UpdateTrackerPoseOverride();
        }
    }

    private void UpdateTrackerPoseOverride()
    {
        if (Time.unscaledTime >= nextTrackerSearchTime || trackerDeviceIndex < 0)
        {
            ResolveTrackerDeviceIndex(force: false);
        }

        if (trackerDeviceIndex < 0)
        {
            return;
        }

        SteamVR_Render render = SteamVR_Render.instance;
        if (render == null || render.poses == null || trackerDeviceIndex >= render.poses.Length)
        {
            return;
        }

        TrackedDevicePose_t pose = render.poses[trackerDeviceIndex];
        if (!pose.bDeviceIsConnected || !pose.bPoseIsValid)
        {
            return;
        }

        SteamVR_Utils.RigidTransform trackedPose = new SteamVR_Utils.RigidTransform(pose.mDeviceToAbsoluteTracking);

        transform.position = trackedPose.pos;
        transform.rotation = trackedPose.rot;

        if (debugTrackerAndHandCoords && Time.unscaledTime >= nextDebugCoordsLogTime)
        {
            nextDebugCoordsLogTime = Time.unscaledTime + debugCoordsLogInterval;
            Vector3 trackerCoords = trackedPose.pos;
            Vector3 virtualHandCoords = virtualHand != null ? virtualHand.transform.position : Vector3.zero;
            //Debug.Log($"[TrackerCoords] : {trackerCoords.x:F3},{trackerCoords.y:F3},{trackerCoords.z:F3}, Virtual Hand Coords : {virtualHandCoords.x:F3},{virtualHandCoords.y:F3},{virtualHandCoords.z:F3}");
        }

        PositionUpdated();
    }

    private void ResolveTrackerDeviceIndex(bool force)
    {
        if (!force && Time.unscaledTime < nextTrackerSearchTime)
        {
            return;
        }

        nextTrackerSearchTime = Time.unscaledTime + trackerSearchInterval;

        if (!SteamVR.active || OpenVR.System == null)
        {
            trackerDeviceIndex = -1;
            return;
        }

        trackerDeviceIndex = FindUltimateOrFallbackTracker();
    }

    private int FindUltimateOrFallbackTracker()
    {
        CVRSystem system = OpenVR.System;
        int fallbackTrackerIndex = -1;

        for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++)
        {
            if (!system.IsTrackedDeviceConnected(i))
            {
                continue;
            }

            ETrackedDeviceClass deviceClass = system.GetTrackedDeviceClass(i);
            bool classCanBeTracker = deviceClass == ETrackedDeviceClass.GenericTracker || deviceClass == ETrackedDeviceClass.Controller;
            if (!classCanBeTracker)
            {
                continue;
            }

            string serial = GetDeviceProperty(i, ETrackedDeviceProperty.Prop_SerialNumber_String);
            string model = GetDeviceProperty(i, ETrackedDeviceProperty.Prop_ModelNumber_String);
            string controllerType = GetDeviceProperty(i, ETrackedDeviceProperty.Prop_ControllerType_String);
            string renderModel = GetDeviceProperty(i, ETrackedDeviceProperty.Prop_RenderModelName_String);
            string searchable = (serial + " " + model + " " + controllerType + " " + renderModel).ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(trackerSerialContains) && !serial.ToLowerInvariant().Contains(trackerSerialContains.ToLowerInvariant()))
            {
                continue;
            }

            bool looksLikeUltimateTracker = searchable.Contains("ultimate") || searchable.Contains("vive_tracker_ultimate");
            if (looksLikeUltimateTracker)
            {
                return (int)i;
            }

            if (deviceClass == ETrackedDeviceClass.GenericTracker && fallbackTrackerIndex < 0)
            {
                fallbackTrackerIndex = (int)i;
            }
        }

        return fallbackTrackerIndex;
    }

    private static string GetDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
    {
        if (SteamVR.instance == null)
        {
            return string.Empty;
        }

        return SteamVR.instance.GetStringProperty(property, deviceIndex);
    }




}



public enum EMGPointerBehavior
{
    LivePrediction,
    Training
}
