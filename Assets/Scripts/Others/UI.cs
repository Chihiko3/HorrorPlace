using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI healthText = default;

    private void OnEnable()
    {
        // So as messenger, you should not need to add () after the method name.
        FPC.OnDamage += UpdateHealth;
        FPC.OnHeal += UpdateHealth;
    }

    private void OnDisable()
    {
        FPC.OnDamage -= UpdateHealth;
        FPC.OnHeal -= UpdateHealth;
    }

    private void Start()
    {
        UpdateHealth(100);
    }

    private void UpdateHealth(float currentHealth)
    {
        healthText.text = currentHealth.ToString("00");
    }
}
