using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MissionType { None, MissionA, MissionB, MissionC }

public class MissionMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject      missionPanel;
    public TextMeshProUGUI selectedMissionLabel;
    public TextMeshProUGUI missionButtonText;
    public TextMeshProUGUI statusMessageText;
    public float           messageDuration = 2f;

    [Header("Autonomous Mode Toggle")]
    public Toggle          autonomousToggle;
    public TextMeshProUGUI toggleLabel;

    [Header("Path Guide")]
    public PathArrowGuide pathArrowGuide;

    [Header("ROS Path")]
    public ROSPathRequester rosPathRequester;

    [Header("ROS Navigation")]
    public ROSNavigator rosNavigator;

    private MissionType currentMission    = MissionType.None;
    private Coroutine   completionRoutine;
    private bool        autonomousMode => autonomousToggle != null && autonomousToggle.isOn;

    void Start()
    {
        if (missionPanel != null)
            missionPanel.SetActive(true);

        if (statusMessageText != null)
            statusMessageText.gameObject.SetActive(false);

        if (autonomousToggle != null)
            autonomousToggle.onValueChanged.AddListener(OnToggleChanged);

        UpdateLabel();
        UpdateToggleLabel();
    }

    // -------------------------------------------------------
    // Toggle
    // -------------------------------------------------------

    private void OnToggleChanged(bool value)
    {
        UpdateToggleLabel();

        if (currentMission == MissionType.None) return;

        if (value)
        {
            // Toggle attivato durante missione → avvia navigazione autonoma
            if (rosPathRequester != null)
                rosPathRequester.SetAutonomousMode(true);

            if (rosNavigator != null)
                rosNavigator.StartNavigation(currentMission);
        }
        else
        {
            // Toggle disattivato → ferma il robot, torna a teleoperazione
            if (rosPathRequester != null)
                rosPathRequester.SetAutonomousMode(false);

            if (rosNavigator != null)
                rosNavigator.CancelNavigation();
        }
    }

    private void UpdateToggleLabel()
    {
        if (toggleLabel == null) return;
        toggleLabel.text = autonomousMode
            ? "Auto Navigation: ON"
            : "Auto Navigation: OFF";
    }

    // -------------------------------------------------------
    // Menu
    // -------------------------------------------------------

    public void OpenMenu()
    {
        if (missionPanel != null) missionPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        if (missionPanel != null) missionPanel.SetActive(false);
    }

    public void SelectMissionA() => StartMission(MissionType.MissionA);
    public void SelectMissionB() => StartMission(MissionType.MissionB);
    public void SelectMissionC() => StartMission(MissionType.MissionC);

    // -------------------------------------------------------
    // Missione
    // -------------------------------------------------------

    private void StartMission(MissionType mission)
    {
        currentMission = mission;
        UpdateLabel();

        // Frecce statiche sempre attive
        if (pathArrowGuide != null)
            pathArrowGuide.ShowMission(currentMission);

        if (autonomousMode)
        {
            // Modalità autonoma:
            // ROSPathRequester usa topic visivo separato → non interferisce con Nav2
            if (rosPathRequester != null)
            {
                rosPathRequester.SetAutonomousMode(true);
                rosPathRequester.StartPathUpdates(currentMission);
            }

            // Nav2 NavigateToPose — pubblicato UNA SOLA VOLTA
            if (rosNavigator != null)
                rosNavigator.StartNavigation(currentMission);
        }
        else
        {
            // Modalità teleoperazione: solo ComputePathToPose per il visivo
            if (rosPathRequester != null)
            {
                rosPathRequester.SetAutonomousMode(false);
                rosPathRequester.StartPathUpdates(currentMission);
            }
        }

        if (missionPanel != null) missionPanel.SetActive(false);
    }

    // -------------------------------------------------------
    // Completamento missione
    // -------------------------------------------------------

    public void OnMissionCompleted()
    {
        if (completionRoutine != null) StopCoroutine(completionRoutine);
        completionRoutine = StartCoroutine(MissionCompletedRoutine());
    }

    private IEnumerator MissionCompletedRoutine()
    {
        if (statusMessageText != null)
        {
            statusMessageText.text = "Robot has reached the goal!";
            statusMessageText.gameObject.SetActive(true);
        }

        currentMission = MissionType.None;
        UpdateLabel();

        if (pathArrowGuide != null)    pathArrowGuide.ClearArrows();
        if (rosPathRequester != null)  rosPathRequester.StopPathUpdates();
        if (rosNavigator != null)      rosNavigator.CancelNavigation();

        yield return new WaitForSeconds(messageDuration);

        if (statusMessageText != null)
            statusMessageText.gameObject.SetActive(false);

        OpenMenu();
    }

    // -------------------------------------------------------
    // Label
    // -------------------------------------------------------

    private void UpdateLabel()
    {
        string label = currentMission switch
        {
            MissionType.MissionA => "Mission A - Exit",
            MissionType.MissionB => "Mission B - Shelves",
            MissionType.MissionC => "Mission C - Barrels",
            _                    => "No current mission"
        };

        if (selectedMissionLabel != null) selectedMissionLabel.text = label;
        if (missionButtonText != null)
            missionButtonText.text = currentMission == MissionType.None
                ? "No current mission"
                : label;
    }
}