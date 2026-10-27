using UnityEngine;
using TMPro;

public class MoneyHUD : MonoBehaviour
{
    [SerializeField] private EconomyManager economy;
    [SerializeField] private TMP_Text moneyLabel;
    [SerializeField] private string prefix = "$";

    private void Awake()
    {
        if (economy == null) economy = FindAnyObjectByType<EconomyManager>();
    }

    private void Start()
    {
        if (economy == null) { Debug.LogError("[MoneyHUD] EconomyManager not found."); return; }
        if (moneyLabel == null) { Debug.LogError("[MoneyHUD] moneyLabel not assigned."); return; }

        economy.OnMoneyChanged += HandleMoneyChanged;
        HandleMoneyChanged(economy.Money);   // initial paint
    }

    private void OnDestroy()
    {
        if (economy != null) economy.OnMoneyChanged -= HandleMoneyChanged;
    }

    private void HandleMoneyChanged(int newBalance)
    {
        moneyLabel.text = $"{prefix}{newBalance}";
    }
}