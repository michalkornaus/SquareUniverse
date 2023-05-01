using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerNeeds : MonoBehaviour
{
    [Header("Needs variables")]
    [Range(0, 100)]
    public float ThirstPoints;
    [Range(0, 100)]
    public float HungerPoints;

    private float thirstDecreseRate = 0.05f;
    private float hungerDecreseRate = 0.025f;

    [Header("Thirst UI")]
    public TMP_Text thirstText;
    public Image thirstMask;
    private readonly float thirstMaskMax = 1f;

    [Header("Hunger UI")]
    public TMP_Text hungerText;
    public Image hungerMask;
    private readonly float hungerMaskMax = 1f;
    void Update()
    {
        //Decrease the player needs
        ThirstPoints -= thirstDecreseRate * Time.deltaTime;
        HungerPoints -= hungerDecreseRate * Time.deltaTime;

        //Update thirst & hunger UI
        if (ThirstPoints >= 0)
        {
            thirstText.text = ((int)ThirstPoints).ToString();
        }
        else
        {
            thirstText.text = "0";
        }


        thirstMask.fillAmount = thirstMaskMax * (ThirstPoints / 100f);

        if (HungerPoints >= 0)
            hungerText.text = ((int)HungerPoints).ToString();
        else
            hungerText.text = "0";

        hungerMask.fillAmount = hungerMaskMax * (HungerPoints / 100f);
    }
    public void Eat(float foodValue)
    {
        HungerPoints += foodValue;
        if (HungerPoints > 100)
            HungerPoints = 100;
        Debug.Log("Player eaten " + foodValue + " food");
    }
    public void Drink(float drinkValue)
    {
        ThirstPoints += drinkValue;
        if (ThirstPoints > 100)
            ThirstPoints = 100;
        Debug.Log("Player drank " + drinkValue + " water");
    }
}
