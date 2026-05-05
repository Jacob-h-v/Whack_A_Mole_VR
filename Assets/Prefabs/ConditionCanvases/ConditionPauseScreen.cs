using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ConditionPauseScreen : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button continueButton;

    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(HandleContinuePressed);
        }
        else
        {
            Debug.LogWarning("[ConditionPauseScreen] Continue button is not assigned.");
        }
    }

    private void OnEnable()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void OnDisable()
    {
        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(HandleContinuePressed);
        }
    }

    private void HandleContinuePressed()
    {
        Time.timeScale = previousTimeScale;
        Destroy(gameObject);
    }
}
