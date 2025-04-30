/**
 * Manager to control display of procedures and tracking of progress/status
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Class to define groups of interface elements
public class ProcedureDisplay : MonoBehaviour
{
    // UI components for panel (title, panel, description, step number, progress)
    [Header("UI References")]
    [SerializeField] private GameObject procedurePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI stepText;
    [SerializeField] private TextMeshProUGUI progressText;

    // Navigation buttons to move next, previous, skip, or return to main menu
    [Header("Navigation Controls")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button completeButton;

    // Progress indicators (i.e. progress bar)
    [Header("Progress Indicators")]
    [SerializeField] private Transform stepIndicatorContainer;
    [SerializeField] private GameObject stepIndicatorPrefab;
    [SerializeField] private Color activeStepColor = Color.cyan;
    [SerializeField] private Color inactiveStepColor = Color.gray;
    [SerializeField] private Color completedStepColor = Color.cyan;

    // Events triggered when user interacts with UI
    public UnityEvent onProcedureCompleted;

    // current procedure state
    private ProcedureDisplay currentProcedure;
    private int currentStepIndex = 0;
    private List<GameObject> stepIndicators = new List<GameObject>(); // list of step indicators for the progress bar

}