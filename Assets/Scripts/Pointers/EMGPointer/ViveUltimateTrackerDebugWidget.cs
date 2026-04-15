using System.Text;
using UnityEngine;
using Valve.VR;

public class ViveUltimateTrackerDebugWidget : MonoBehaviour
{
    [SerializeField] private EMGPointer emgPointer;
    [SerializeField] private bool visible = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F9;
    [SerializeField] private float refreshInterval = 0.25f;
    [SerializeField] private Vector2 widgetPosition = new Vector2(20f, 20f);
    [SerializeField] private Vector2 widgetSize = new Vector2(650f, 330f);

    private Rect windowRect;
    private Vector2 scrollPosition;
    private float nextRefreshTime;
    private int trackerDeviceIndex = -1;
    private string trackerLabel = "No tracker detected";
    private SteamVR_Behaviour_Pose trackerPoseComponent;
    private readonly StringBuilder debugText = new StringBuilder(2048);

    private void Start()
    {
        windowRect = new Rect(widgetPosition.x, widgetPosition.y, widgetSize.x, widgetSize.y);
        if (emgPointer == null)
        {
            emgPointer = FindObjectOfType<EMGPointer>();
        }

        RefreshData();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }

        if (!visible)
        {
            return;
        }

        if (Time.unscaledTime >= nextRefreshTime)
        {
            RefreshData();
        }
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, "VIVE Ultimate Tracker Debug");
    }

    private void DrawWindow(int windowId)
    {
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
        GUILayout.Label(debugText.ToString());
        GUILayout.EndScrollView();

        GUILayout.Label("Toggle key: " + toggleKey);
        GUI.DragWindow(new Rect(0f, 0f, 10000f, 20f));
    }

    private void RefreshData()
    {
        nextRefreshTime = Time.unscaledTime + refreshInterval;
        debugText.Length = 0;

        SteamVR.Initialize();
        if (!SteamVR.active || OpenVR.System == null)
        {
            debugText.AppendLine("SteamVR is not active.");
            return;
        }

        trackerDeviceIndex = FindUltimateOrFallbackTracker(out trackerLabel);
        trackerPoseComponent = FindPoseComponentForDevice(trackerDeviceIndex);

        debugText.AppendLine("Tracker selection");
        debugText.AppendLine("- Device index: " + trackerDeviceIndex);
        debugText.AppendLine("- Device info: " + trackerLabel);
        debugText.AppendLine();

        AppendTrackedPoseFromSteamVRRender();
        AppendBoundPoseInfo();
        AppendEMGPointerInfo();
    }

    private int FindUltimateOrFallbackTracker(out string selectedLabel)
    {
        selectedLabel = "No connected generic tracker";

        int fallbackTrackerIndex = -1;
        string fallbackTrackerLabel = "";

        CVRSystem system = OpenVR.System;
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
            string deviceInfo = deviceClass + " | serial=" + serial + " | model=" + model + " | type=" + controllerType + " | render=" + renderModel;

            string searchable = (serial + " " + model + " " + controllerType + " " + renderModel).ToLowerInvariant();
            bool looksLikeUltimateTracker = searchable.Contains("ultimate") || searchable.Contains("vive_tracker_ultimate");

            if (looksLikeUltimateTracker)
            {
                selectedLabel = deviceInfo;
                return (int)i;
            }

            if (deviceClass == ETrackedDeviceClass.GenericTracker && fallbackTrackerIndex < 0)
            {
                fallbackTrackerIndex = (int)i;
                fallbackTrackerLabel = deviceInfo;
            }
        }

        if (fallbackTrackerIndex >= 0)
        {
            selectedLabel = fallbackTrackerLabel;
        }

        return fallbackTrackerIndex;
    }

    private SteamVR_Behaviour_Pose FindPoseComponentForDevice(int deviceIndex)
    {
        if (deviceIndex < 0)
        {
            return null;
        }

        SteamVR_Behaviour_Pose[] poses = FindObjectsOfType<SteamVR_Behaviour_Pose>();
        for (int i = 0; i < poses.Length; i++)
        {
            SteamVR_Behaviour_Pose pose = poses[i];
            if (pose != null && pose.GetDeviceIndex() == deviceIndex)
            {
                return pose;
            }
        }

        return null;
    }

    private void AppendTrackedPoseFromSteamVRRender()
    {
        debugText.AppendLine("Raw tracked pose (OpenVR)");
        if (trackerDeviceIndex < 0)
        {
            debugText.AppendLine("- Not available (no tracker index found)");
            debugText.AppendLine();
            return;
        }

        SteamVR_Render render = SteamVR_Render.instance;
        if (render == null || render.poses == null || trackerDeviceIndex >= render.poses.Length)
        {
            debugText.AppendLine("- SteamVR render poses are not available yet");
            debugText.AppendLine();
            return;
        }

        TrackedDevicePose_t pose = render.poses[trackerDeviceIndex];
        debugText.AppendLine("- Connected: " + pose.bDeviceIsConnected);
        debugText.AppendLine("- Pose valid: " + pose.bPoseIsValid);

        if (pose.bPoseIsValid)
        {
            SteamVR_Utils.RigidTransform rigid = new SteamVR_Utils.RigidTransform(pose.mDeviceToAbsoluteTracking);
            debugText.AppendLine("- Position: " + FormatVector3(rigid.pos));
            debugText.AppendLine("- Rotation: " + FormatVector3(rigid.rot.eulerAngles));
        }

        debugText.AppendLine();
    }

    private void AppendBoundPoseInfo()
    {
        debugText.AppendLine("Scene binding (SteamVR_Behaviour_Pose)");
        if (trackerPoseComponent == null)
        {
            debugText.AppendLine("- No scene object is currently bound to this tracker index");
            debugText.AppendLine();
            return;
        }

        Transform t = trackerPoseComponent.transform;
        debugText.AppendLine("- Object: " + GetHierarchyPath(t));
        debugText.AppendLine("- Input source: " + trackerPoseComponent.inputSource);
        debugText.AppendLine("- World position: " + FormatVector3(t.position));
        debugText.AppendLine("- World rotation: " + FormatVector3(t.eulerAngles));
        debugText.AppendLine("- Local position: " + FormatVector3(t.localPosition));
        debugText.AppendLine("- Local rotation: " + FormatVector3(t.localEulerAngles));
        debugText.AppendLine();
    }

    private void AppendEMGPointerInfo()
    {
        debugText.AppendLine("EMG pointer / virtual hand");
        if (emgPointer == null)
        {
            emgPointer = FindObjectOfType<EMGPointer>();
        }

        if (emgPointer == null)
        {
            debugText.AppendLine("- EMGPointer not found in scene");
            return;
        }

        Transform pointerTransform = emgPointer.transform;
        debugText.AppendLine("- EMGPointer object: " + GetHierarchyPath(pointerTransform));
        debugText.AppendLine("- EMGPointer position: " + FormatVector3(pointerTransform.position));
        debugText.AppendLine("- EMGPointer rotation: " + FormatVector3(pointerTransform.eulerAngles));

        Transform virtualHand = emgPointer.GetVirtualHandTransform();
        if (virtualHand == null)
        {
            debugText.AppendLine("- VirtualHand: not instantiated (EMG pointer may be disabled)");
            return;
        }

        debugText.AppendLine("- VirtualHand object: " + GetHierarchyPath(virtualHand));
        debugText.AppendLine("- VirtualHand world position: " + FormatVector3(virtualHand.position));
        debugText.AppendLine("- VirtualHand world rotation: " + FormatVector3(virtualHand.eulerAngles));
        debugText.AppendLine("- VirtualHand local position: " + FormatVector3(virtualHand.localPosition));
        debugText.AppendLine("- VirtualHand local rotation: " + FormatVector3(virtualHand.localEulerAngles));
    }

    private static string GetHierarchyPath(Transform transformTarget)
    {
        if (transformTarget == null)
        {
            return "<null>";
        }

        string path = transformTarget.name;
        Transform current = transformTarget.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return path;
    }

    private static string FormatVector3(Vector3 value)
    {
        return "(" + value.x.ToString("F3") + ", " + value.y.ToString("F3") + ", " + value.z.ToString("F3") + ")";
    }

    private static string GetDeviceProperty(uint deviceIndex, ETrackedDeviceProperty property)
    {
        if (SteamVR.instance == null)
        {
            return "<unavailable>";
        }

        return SteamVR.instance.GetStringProperty(property, deviceIndex);
    }
}
