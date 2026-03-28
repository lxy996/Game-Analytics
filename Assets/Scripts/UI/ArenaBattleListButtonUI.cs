using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ArenaBattleListButtonUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subtitleText;
    [SerializeField] private TMP_Text statusText;

    private ArenaRunController runController;
    private int entryIndex;
    private bool isNormalBattle;

    public void SetupNormal(
        ArenaRunController controller,
        int index,
        string title,
        string subtitle,
        string status,
        bool interactable
    )
    {
        runController = controller;
        entryIndex = index;
        isNormalBattle = true;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    public void SetupChallenge(
        ArenaRunController controller,
        int index,
        string title,
        string subtitle,
        string status,
        bool interactable
    )
    {
        runController = controller;
        entryIndex = index;
        isNormalBattle = false;

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (subtitleText != null)
        {
            subtitleText.text = subtitle;
        }

        if (statusText != null)
        {
            statusText.text = status;
        }

        if (button != null)
        {
            button.interactable = interactable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (runController == null)
        {
            return;
        }

        if (isNormalBattle)
        {
            runController.SelectNormalBattle(entryIndex);
        }
        else
        {
            runController.SelectFamilyChallenge(entryIndex);
        }
    }
}
