using System.Collections;
using UnityEngine;
using TMPro;

public enum MissionType
{
    None,
    MissionA,
    MissionB,
    MissionC
}

public class MissionMenuManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject missionPanel;
    public TextMeshProUGUI selectedMissionLabel;
    public TextMeshProUGUI missionButtonText;
    public TextMeshProUGUI statusMessageText;   // testo messaggio in game
    public float messageDuration = 2f;

    [Header("Path Guide")]
    public PathArrowGuide pathArrowGuide;

    private MissionType currentMission = MissionType.None;
    private Coroutine completionRoutine;

    void Start()
    {
        if (missionPanel != null)
            missionPanel.SetActive(true);

        if (statusMessageText != null)
            statusMessageText.gameObject.SetActive(false);

        UpdateLabel();
    }

    public void OpenMenu()
    {
        if (missionPanel != null)
            missionPanel.SetActive(true);
    }

    public void CloseMenu()
    {
        if (missionPanel != null)
            missionPanel.SetActive(false);
    }

    public void SelectMissionA()
    {
        StartMission(MissionType.MissionA);
    }

    public void SelectMissionB()
    {
        StartMission(MissionType.MissionB);
    }

    public void SelectMissionC()
    {
        StartMission(MissionType.MissionC);
    }

    private void StartMission(MissionType mission)
    {
        currentMission = mission;
        UpdateLabel();

        if (pathArrowGuide != null)
            pathArrowGuide.ShowMission(currentMission);

        if (missionPanel != null)
            missionPanel.SetActive(false);
    }

    public void OnMissionCompleted()
    {
        if (completionRoutine != null)
            StopCoroutine(completionRoutine);

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

        if (pathArrowGuide != null)
            pathArrowGuide.ClearArrows();

        yield return new WaitForSeconds(messageDuration);

        if (statusMessageText != null)
            statusMessageText.gameObject.SetActive(false);

        OpenMenu();
    }

    private void UpdateLabel()
    {
        string label;

        switch (currentMission)
        {
            case MissionType.None:
                label = "No current mission";
                break;
            case MissionType.MissionA:
                label = "Mission A - Exit";
                break;
            case MissionType.MissionB:
                label = "Mission B - Shelves";
                break;
            case MissionType.MissionC:
                label = "Mission C - Barrels";
                break;
            default:
                label = "No current mission";
                break;
        }

        if (selectedMissionLabel != null)
            selectedMissionLabel.text = label;

        if (missionButtonText != null)
            missionButtonText.text = currentMission == MissionType.None
                ? "No current mission"
                : label;
    }
}