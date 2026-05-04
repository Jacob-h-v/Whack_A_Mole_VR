using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PressureGaugeAnimator : MonoBehaviour
{
    [SerializeField] private Transform needleTransform;
    [SerializeField] private Transform handModel;
    [SerializeField] private EMGPointer emgPointer;
    [SerializeField] private float maxMVCAngle = 180.0f;
    [SerializeField] private float zeroMVCAngle = 0.0f;
    [SerializeField] private Vector3 localOffset = new Vector3(0.1f, 0.05f, 0.2f);

    private float mvc = 100.0f;
    private float currentPercentMVC = 0.0f;
    private float smoothingSpeed = 10f;


    void Awake()
    {
        emgPointer = FindObjectOfType<EMGPointer>();
    }

    // Update is called once per frame
    void Update()
    {
        // UpdateCurrentAndMaxMVC();
        // float zRotation = Mathf.Clamp(GetGaugeRotation(), -135f, 135f);
        // needleTransform.eulerAngles = new Vector3(0f, 0f, zRotation);
        UpdateCurrentAndMaxMVC();

        float safeMvc = Mathf.Max(mvc, 0.0001f);
        float mvcNormalized = currentPercentMVC / safeMvc;

        float zRotation = Mathf.Clamp(
            zeroMVCAngle - mvcNormalized * (zeroMVCAngle - maxMVCAngle),
            -135f,
            135f
        );

        needleTransform.localRotation = Quaternion.Euler(0f, 0f, zRotation);
    }

    // void LateUpdate()
    // {
    //     //transform.position = handModel.TransformPoint(localOffset); // Offset the gauge slightly from the hand model
    //     //transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position); // Make sure the gauge always faces the viewport

    //     // More advance approach, hopefully it worky
    //     Transform cam = Camera.main.transform;

    //     Vector3 basePos = handModel.TransformPoint(localOffset);
    //     Vector3 dir = basePos-cam.position;

    //     if(Physics.Raycast(cam.position, dir.normalized, out RaycastHit hit, dir.magnitude))
    //     {
    //         if(hit.transform != transform)
    //         {
    //             basePos = hit.point - dir.normalized * 0.03f;
    //         }
    //     }

    //     //transform.position = basePos;
    //     transform.position = Vector3.Lerp(transform.position, basePos, Time.deltaTime * smoothingSpeed); // smoother motion than the line above

    //     Vector3 flatDir = transform.position - cam.position;
    //     flatDir.y = 0;
    //     transform.rotation = Quaternion.LookRotation(flatDir);

    //     float vDot = Vector3.Dot(handModel.forward, cam.forward);

    //     if(vDot < 0.3f)
    //     {
    //         localOffset.z = 0.25f; // move gauge towards camera if the hand is facing away
    //     }
    //     else
    //     {
    //         localOffset.z = 0.10f;
    //     }
    // }

    private void UpdateCurrentAndMaxMVC() // Fetches an update for the current and highest measured MVC
    {
        float maxEMG = emgPointer != null ? emgPointer.GetMaxEMG() : 100f;
        float currentMVCPercent = emgPointer != null ? emgPointer.GetCurrentMvcPercent() : 0f;

        mvc = maxEMG;
        currentPercentMVC = currentMVCPercent;
    }

    // private float GetGaugeRotation() // Calculates the needle's z-axis rotation
    // {
    //     float maxAngle = zeroMVCAngle - maxMVCAngle;
    //     float mvcNormalized = currentPercentMVC / mvc;
    //     return zeroMVCAngle - mvcNormalized * maxAngle;
    // }

}
