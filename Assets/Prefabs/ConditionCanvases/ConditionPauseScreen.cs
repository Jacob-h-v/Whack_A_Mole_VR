using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConditionPauseScreen : MonoBehaviour
{
    [Header("Controls")]
    [SerializeField] private KeyCode continueKey = KeyCode.Space;

    private float previousTimeScale = 1f;
    private bool despawnedByContinue = false;
    private bool loggedDespawn = false;

    private void Awake()
    {
        Debug.Log("[ConditionPauseScreen] I have spawned.");
    }

    private void OnEnable()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void Update()
    {
        if (Input.GetKeyDown(continueKey))
        {
            HandleContinuePressed();
        }
    }

    private void OnDisable()
    {
        if (loggedDespawn)
        {
            return;
        }

        Debug.Log($"[Canvas Disabled] {name} | activeSelf: {gameObject.activeSelf} | activeInHierarchy: {gameObject.activeInHierarchy}", this);


        string reason = despawnedByContinue ? "Continue pressed" : "Disabled externally";
        LogDespawn(reason);
    }

    private void OnDestroy()
    {
        if (loggedDespawn)
        {
            return;
        }

        string reason = despawnedByContinue ? "Continue pressed" : "Destroyed externally";
        LogDespawn(reason);
    }

    private void HandleContinuePressed()
    {
        despawnedByContinue = true;
        Time.timeScale = previousTimeScale;
        Destroy(gameObject);
    }

    private void LogDespawn(string reason)
    {
        loggedDespawn = true;
        Debug.Log($"[ConditionPauseScreen] Despawning. Reason: {reason}.");
    }
}
