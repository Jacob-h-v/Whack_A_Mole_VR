using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressureGaugeAnimator : MonoBehaviour
{
    [SerializeField] private Transform needleTransform;
    [SerializeField] private EMGPointer emgPointer;
    [SerializeField] private float maxMVCAngle = 180.0f;
    [SerializeField] private float zeroMVCAngle = 0.0f;

    private float mvc = 100.0f;
    private float currentPercentMVC = 0.0f;


    // Update is called once per frame
    void Update()
    {
        UpdateCurrentAndMaxMVC();
        needleTransform.eulerAngles = new Vector3(0,0,GetGaugeRotation());
    }

    private void UpdateCurrentAndMaxMVC() // Fetches an update for the current and highest measured MVC
    {
        float maxEMG = emgPointer != null ? emgPointer.GetMaxEMG() : 100f;
        float currentMVCPercent = emgPointer != null ? emgPointer.GetCurrentMvcPercent() : 0f;

        mvc = maxEMG;
        currentPercentMVC = currentMVCPercent;
    }

    private float GetGaugeRotation() // Calculates the needle's z-axis rotation
    {
        float maxAngle = zeroMVCAngle - maxMVCAngle;
        float mvcNormalized = currentPercentMVC / mvc;
        return zeroMVCAngle - mvcNormalized * maxAngle;
    }

}
