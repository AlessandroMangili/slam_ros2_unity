using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum MissionType { None, MissionA, MissionB, MissionC }

/// <summary>
/// Manages mission selection, autonomous/teleoperation mode switching,
/// and mission completion flow.
///
/// Missions can be run in two modes:
///   - Teleoperation: static arrow guides are shown and the visual path
///     is computed via ComputePathToPose, but the robot is driven manually.
///   - Autonomous: Nav2 NavigateToPose is triggered once to drive the robot
///     to the goal automatically. The visual path uses a separate topic
///     so it does not interfere with Nav2.
///
/// The autonomous toggle can be flipped at any time, even mid-mission,
/// to switch between the two modes on the fly.
/// </summary>
public class MissionMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject      missionPanel;
    public TextMeshProUGUI selectedMissionLabel;
    public TextMeshProUGUI missionButtonText;
    public TextMeshProUGUI statusMessageText;

    [Tooltip("Seconds the completion message is shown before the menu reopens")]
    public float messageDuration = 2f;

    [Header("Autonomous Mode Toggle")]
    public Toggle          autonomousToggle;
    public TextMeshProUGUI toggleLabel;

    [Header("Path Guide")]
    public PathArrowGuide pathArrowGuide;

    [Header("ROS Path")]
    public ROSPathRequester rosPathRequester;

    [Header("ROS Navigation")]
    public ROSNavigator rosNavigator;

    private MissionType currentMission   = MissionType.None;
    private Coroutine   completionRoutine;

    // Convenience property — reads the toggle state directly
    private bool autonomousMode => autonomousToggle != null && autonomousToggle.isOn;

    // ─── Unity lifecycle ─────────────────────────────────────────────────────

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

    // ─── Autonomous toggle ───────────────────────────────────────────────────

    /// <summary>
    /// Called when the autonomous mode toggle changes value.
    /// If a mission is already active, immediately switches the robot's
    /// operating mode without restarting the mission.
    /// </summary>
    private void OnToggleChanged(bool value)
    {
        UpdateToggleLabel();

        if (currentMission == MissionType.None) return;

        if (value)
        {
            // Switched to autonomous mid-mission → start Nav2 navigation
            if (rosPathRequester != null)
                rosPathRequester.SetAutonomousMode(true);

            if (rosNavigator != null)
                rosNavigator.StartNavigation(currentMission);
        }
        else
        {
            // Switched to teleoperation → cancel Nav2 and resume manual driving
            if (rosPathRequester != null)
                rosPathRequester.SetAutonomousMode(false);

            if (rosNavigator != null)
                rosNavigator.CancelNavigation();
        }
    }

    private void UpdateToggleLabel()
    {
        if (toggleLabel == null) return;
        toggleLabel.text = autonomousMode ? "Auto Navigation: ON" : "Auto Navigation: OFF";
    }

    // ─── Menu ────────────────────────────────────────────────────────────────

    public void OpenMenu()  { if (missionPanel != null) missionPanel.SetActive(true);  }
    public void CloseMenu() { if (missionPanel != null) missionPanel.SetActive(false); }

    // Wired to the three mission buttons in the Inspector
    public void SelectMissionA() => StartMission(MissionType.MissionA);
    public void SelectMissionB() => StartMission(MissionType.MissionB);
    public void SelectMissionC() => StartMission(MissionType.MissionC);

    // ─── Mission start ───────────────────────────────────────────────────────

    /// <summary>
    /// Starts the selected mission:
    ///   - Always spawns static arrow guides.
    ///   - In autonomous mode: sends the Nav2 goal (once) and starts the
    ///     visual path on a separate topic.
    ///   - In teleoperation mode: only starts the visual path via
    ///     ComputePathToPose (no Nav2 goal is sent).
    /// </summary>
    private void StartMission(MissionType mission)
    {
        currentMission = mission;
        UpdateLabel();

        // Static arrow guides are always active regardless of mode
        if (pathArrowGuide != null)
            pathArrowGuide.ShowMission(currentMission);

        if (autonomousMode)
        {
            // Visual path uses a dedicated topic so it does not interfere
            // with Nav2's own ComputePathToPose requests
            if (rosPathRequester != null)
            {
                rosPathRequester.SetAutonomousMode(true);
                rosPathRequester.StartPathUpdates(currentMission);
            }

            // Send the NavigateToPose goal exactly once
            if (rosNavigator != null)
                rosNavigator.StartNavigation(currentMission);
        }
        else
        {
            // Teleoperation: compute visual path only, no autonomous goal
            if (rosPathRequester != null)
            {
                rosPathRequester.SetAutonomousMode(false);
                rosPathRequester.StartPathUpdates(currentMission);
            }
        }

        if (missionPanel != null) missionPanel.SetActive(false);
    }

    // ─── Mission completion ──────────────────────────────────────────────────

    /// <summary>
    /// Called by PathArrowGuide when the robot reaches the goal proximity threshold.
    /// Shows a completion message, clears all guides, and reopens the mission menu.
    /// </summary>
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

        if (pathArrowGuide   != null) pathArrowGuide.ClearArrows();
        if (rosPathRequester != null) rosPathRequester.StopPathUpdates();
        if (rosNavigator     != null) rosNavigator.CancelNavigation();

        yield return new WaitForSeconds(messageDuration);

        if (statusMessageText != null)
            statusMessageText.gameObject.SetActive(false);

        OpenMenu();
    }

    // ─── Label helpers ───────────────────────────────────────────────────────

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
        if (missionButtonText    != null)
            missionButtonText.text = currentMission == MissionType.None
                ? "No current mission"
                : label;
    }
}