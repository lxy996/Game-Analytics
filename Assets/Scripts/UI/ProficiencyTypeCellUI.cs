using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ProficiencyTypeCellUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;

    private ProficiencyPanelController panelController;
    private GladiatorProficiencyType proficiencyType;

    public void Setup(
        ProficiencyPanelController controller,
        GladiatorProficiencyType type,
        string label
    )
    {
        panelController = controller;
        proficiencyType = type;

        if (labelText != null)
        {
            labelText.text = label;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }
    }

    private void HandleClick()
    {
        if (panelController == null)
        {
            return;
        }

        panelController.SelectProficiencyType(proficiencyType);
    }
}
