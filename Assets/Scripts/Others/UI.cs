using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText = default;
    [SerializeField] private TextMeshProUGUI staminaText = default;

    private void OnEnable()
    {
        // So as messenger, you should not need to add () after the method name.
        FPC.OnDamage += UpdateHealth;
        FPC.OnHeal += UpdateHealth;
        FPC.OnStaminaChange += UpdateStamina;
    }

    private void OnDisable()
    {
        FPC.OnDamage -= UpdateHealth;
        FPC.OnHeal -= UpdateHealth;
        FPC.OnStaminaChange -= UpdateStamina;
    }

    private void Start()
    {
        UpdateHealth(100);
        UpdateStamina(100);
    }

    private void UpdateHealth(float currentHealth)
    {
        healthText.text = currentHealth.ToString("00");
    }

    private void UpdateStamina(float currentStamina)
    {

        staminaText.text = currentStamina.ToString("00");

    }
}
