using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/*
MVC Bar Widget - Displays current MVC as a vertical bar anchored to the virtual hand.
- Green core (20-60% ideal range)
- Blue gradient (0-20% too loose)
- Red gradient (60-100% too tight)
- Moving notch indicator for current MVC
- Updates every 0.3 seconds
*/

public class MvcBar : MonoBehaviour
{
    [SerializeField] private Image notchImage;
    [SerializeField] private Image redZoneImage;
    [SerializeField] private Image blueZoneImage;
    [SerializeField] private Image greenZoneImage;
    [SerializeField] [Range(0.1f, 1f)] private float updateInterval = 0.3f;
    
    private EMGPointer emgPointer;
    private float minMvcSeen = 100f;
    private float maxMvcSeen = 0f;
    private float nextUpdateTime = 0f;

    private void Awake()
    {
        // Auto-find EMGPointer in scene (it will exist by the time hand is instantiated)
        emgPointer = FindObjectOfType<EMGPointer>();
        if (emgPointer == null)
        {
            Debug.LogWarning($"[MvcBar] EMGPointer not found in scene. MVC bar will not update.");
        }

        // Initialize zone colors
        InitializeZoneColors();
    }

    private void InitializeZoneColors()
    {
        // Set static zone colors
        if (greenZoneImage != null)
        {
            greenZoneImage.color = new Color(0f, 1f, 0f, 1f);
        }
    }

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            UpdateBar();
            nextUpdateTime = Time.time + updateInterval;
        }
    }

    private void UpdateBar()
    {
        if (emgPointer == null || emgPointer.GetCurrentMvcRatio() < 0)
        {
            return;
        }

        float currentMvcPercent = emgPointer.GetCurrentMvcPercent();
        float maxEMG = emgPointer.GetMaxEMG();

        if (maxEMG <= 0)
        {
            return;
        }

        // Track min/max seen during this session
        minMvcSeen = Mathf.Min(minMvcSeen, currentMvcPercent);
        maxMvcSeen = Mathf.Max(maxMvcSeen, currentMvcPercent);

        // Update zone gradients
        UpdateZoneGradients();

        // Update notch position based on MVC
        UpdateNotchPosition(currentMvcPercent);
    }

    private void UpdateZoneGradients()
    {
        // Red zone: gradient from white (at 60%) to red (at 100%)
        if (redZoneImage != null)
        {
            redZoneImage.color = new Color(1f, 1f, 1f, 1f);
        }

        // Blue zone: gradient from white (at 20%) to blue (at 0%)
        if (blueZoneImage != null)
        {
            blueZoneImage.color = new Color(1f, 1f, 1f, 1f);
        }

        // Green zone: solid green
        if (greenZoneImage != null)
        {
            greenZoneImage.color = new Color(0f, 1f, 0f, 1f);
        }
    }

    private void UpdateNotchPosition(float mvcPercent)
    {
        if (notchImage == null)
        {
            return;
        }

        // Normalize MVC to 0-1 range (0-100%)
        float normalizedMvc = Mathf.Clamp01(mvcPercent / 100f);

        // Position notch vertically (0 = bottom, 1 = top)
        RectTransform notchRect = notchImage.GetComponent<RectTransform>();
        notchRect.anchorMin = new Vector2(0.5f, normalizedMvc);
        notchRect.anchorMax = new Vector2(0.5f, normalizedMvc);
        notchRect.offsetMin = Vector2.zero;
        notchRect.offsetMax = Vector2.zero;
    }

    public float GetMinMvcSeen() => minMvcSeen;
    public float GetMaxMvcSeen() => maxMvcSeen;

    public void ResetMinMaxTracking()
    {
        minMvcSeen = 100f;
        maxMvcSeen = 0f;
    }
}
