using UnityEngine;
using TMPro;

public class CommandFeedbackUI : MonoBehaviour
{
    [SerializeField] private CommandSystem commandSystem;
    [SerializeField] private TMP_Text selectedGroupText;
    [SerializeField] private TMP_Text commandText;
    [SerializeField] private GameObject focusTargetMarker;
    [SerializeField] private Vector3 markerOffset = new Vector3(0f, 0.8f, 0f);

    void LateUpdate()
    {
        ResolveCommandSystem();
        RefreshTexts();
        RefreshMarker();
    }

    private void ResolveCommandSystem()
    {
        if (commandSystem == null || !commandSystem.gameObject.activeInHierarchy)
        {
            commandSystem = Object.FindFirstObjectByType<CommandSystem>();
        }
    }

    private void RefreshTexts()
    {
        if (commandSystem == null)
        {
            return;
        }

        if (selectedGroupText != null)
        {
            selectedGroupText.text =
                "Group: " + commandSystem.GetCurrentGroup() +
                " (" + commandSystem.GetSelectedCount() + ")";
        }

        if (commandText != null)
        {
            commandText.text = "Command: " + commandSystem.GetLastIssuedCommandName();
        }
    }

    private void RefreshMarker()
    {
        Transform target;

        if (focusTargetMarker == null || commandSystem == null)
        {
            return;
        }

        target = commandSystem.GetLastIssuedCommandTarget();

        if (commandSystem.GetLastIssuedCommandName() != "Focus Target" || target == null)
        {
            focusTargetMarker.SetActive(false);
            return;
        }

        focusTargetMarker.SetActive(true);
        focusTargetMarker.transform.position = target.position + markerOffset;
    }
}