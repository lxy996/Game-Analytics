using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ProficiencyPerkChoiceColumnUI : MonoBehaviour
{
    [SerializeField] private TMP_Text tierText;

    [Header("Top")]
    [SerializeField] private Button topButton;
    [SerializeField] private Image topIcon;
    [SerializeField] private TMP_Text topNameText;
    [SerializeField] private Image topSelectionFrame;

    [Header("Bottom")]
    [SerializeField] private Button bottomButton;
    [SerializeField] private Image bottomIcon;
    [SerializeField] private TMP_Text bottomNameText;
    [SerializeField] private Image bottomSelectionFrame;

    [Header("Locked")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private TMP_Text lockedText;

    public void Setup(
        int tier,
        string topName,
        string bottomName,
        Sprite icon,
        bool unlocked,
        bool hasChoice,
        bool choseTop,
        Action onTopClick,
        Action onBottomClick
    )
    {
        if (tierText != null)
        {
            tierText.text = tier.ToString();
        }

        if (topIcon != null)
        {
            topIcon.sprite = icon;
        }

        if (bottomIcon != null)
        {
            bottomIcon.sprite = icon;
        }

        if (topNameText != null)
        {
            topNameText.text = topName;
        }

        if (bottomNameText != null)
        {
            bottomNameText.text = bottomName;
        }

        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(!unlocked);
        }

        if (lockedText != null)
        {
            lockedText.text = unlocked ? "" : "Need Lv." + tier;
        }

        if (topButton != null)
        {
            topButton.interactable = unlocked;
            topButton.onClick.RemoveAllListeners();

            if (unlocked)
            {
                topButton.onClick.AddListener(delegate
                {
                    if (onTopClick != null)
                    {
                        onTopClick();
                    }
                });
            }
        }

        if (bottomButton != null)
        {
            bottomButton.interactable = unlocked;
            bottomButton.onClick.RemoveAllListeners();

            if (unlocked)
            {
                bottomButton.onClick.AddListener(delegate
                {
                    if (onBottomClick != null)
                    {
                        onBottomClick();
                    }
                });
            }
        }

        RefreshChoiceVisual(unlocked, hasChoice, choseTop);
    }

    private void RefreshChoiceVisual(bool unlocked, bool hasChoice, bool choseTop)
    {
        Color selectedColor;
        Color dimColor;
        Color normalColor;

        selectedColor = Color.white;
        normalColor = Color.white;
        dimColor = new Color(1f, 1f, 1f, 0.35f);

        if (!unlocked)
        {
            SetButtonVisual(topIcon, topNameText, dimColor);
            SetButtonVisual(bottomIcon, bottomNameText, dimColor);

            SetFrame(topSelectionFrame, false);
            SetFrame(bottomSelectionFrame, false);
            return;
        }

        if (!hasChoice)
        {
            SetButtonVisual(topIcon, topNameText, normalColor);
            SetButtonVisual(bottomIcon, bottomNameText, normalColor);

            SetFrame(topSelectionFrame, false);
            SetFrame(bottomSelectionFrame, false);
            return;
        }

        if (choseTop)
        {
            SetButtonVisual(topIcon, topNameText, selectedColor);
            SetButtonVisual(bottomIcon, bottomNameText, dimColor);

            SetFrame(topSelectionFrame, true);
            SetFrame(bottomSelectionFrame, false);
        }
        else
        {
            SetButtonVisual(topIcon, topNameText, dimColor);
            SetButtonVisual(bottomIcon, bottomNameText, selectedColor);

            SetFrame(topSelectionFrame, false);
            SetFrame(bottomSelectionFrame, true);
        }
    }

    private void SetButtonVisual(Image icon, TMP_Text text, Color color)
    {
        if (icon != null)
        {
            icon.color = color;
        }

        if (text != null)
        {
            text.color = color;
        }
    }

    private void SetFrame(Image frame, bool active)
    {
        if (frame != null)
        {
            frame.enabled = active;
        }
    }
}
