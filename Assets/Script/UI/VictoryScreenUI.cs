using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class VictoryScreenUI : MonoBehaviour
{
    [Header("UI Text References")]
    [SerializeField] private TMP_Text currentMoneyText;
    [SerializeField] private TMP_Text enemyGoldText;
    [SerializeField] private TMP_Text abilityGoldText;
    [SerializeField] private TMP_Text upkeepText;
    [SerializeField] private TMP_Text totalMoneyText;

    [Header("Buttons")]
    [SerializeField] private Button nextButton;

    public event Action OnNextClicked;

    private void Awake()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
                OnNextClicked?.Invoke();
            });
        }
    }

    public void Show(int current, int enemy, int ability, int upk)
    {
        gameObject.SetActive(true);

        // Math preview: What your wallet will look like after upkeep is taken on the map.
        int total = current + enemy + ability - upk;

        if (currentMoneyText != null) currentMoneyText.text = $"${current}";
        if (enemyGoldText != null) enemyGoldText.text = $"${enemy}";
        if (abilityGoldText != null) abilityGoldText.text = $"${ability}";
        if (upkeepText != null) upkeepText.text = $"-${upk}";
        if (totalMoneyText != null) totalMoneyText.text = $"${total}";
    }
}