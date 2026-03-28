using UnityEngine;
using TMPro;

public class CommandFeedbackUI : MonoBehaviour
{
    [SerializeField] private CommandSystem commandSystem;
    [SerializeField] private TMP_Text selectedGroupText;
    [SerializeField] private TMP_Text commandText;
    [SerializeField] private GameObject focusTargetMarker;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 0.8f, 0f);

    private Transform currentMarkedTarget;

    void OnEnable()
    {
        if (commandSystem != null)
        {
            commandSystem.OnGroupSelectionChanged += HandleGroupSelectionChanged;
            commandSystem.OnCommandIssued += HandleCommandIssued;
        }
    }

    void OnDisable()
    {
        if (commandSystem != null)
        {
            commandSystem.OnGroupSelectionChanged -= HandleGroupSelectionChanged;
            commandSystem.OnCommandIssued -= HandleCommandIssued;
        }
    }

    void Update()
    {
        if (focusTargetMarker == null)
        {
            return;
        }

        if (currentMarkedTarget == null)
        {
            focusTargetMarker.SetActive(false);
            return;
        }

        focusTargetMarker.SetActive(true);
        focusTargetMarker.transform.position = currentMarkedTarget.position + markerOffset;
    }

    private void HandleGroupSelectionChanged(AllySelectionGroup group, int count)
    {
        if (selectedGroupText != null)
        {
            selectedGroupText.text = "Group: " + group + " (" + count + ")";
        }
    }

    private void HandleCommandIssued(string commandName, Transform target)
    {
        if (commandText != null)
        {
            commandText.text = "Command: " + commandName;
        }

        currentMarkedTarget = target;

        if (commandName != "Focus Target")
        {
            currentMarkedTarget = null;
        }
    }
}
