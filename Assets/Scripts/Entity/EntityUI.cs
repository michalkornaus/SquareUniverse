using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EntityUI : MonoBehaviour
{
    public Entity entity;
    public Slider hpSlider;
    public Image hpSliderFill;
    public TMP_Text nameText;
    public TMP_Text hpText;
    public TMP_Text lvlText;
    private int currentHP;
    private int maxHP;

    private Color greenColor = new(0.5254902f, 1f, 0.5254902f);
    private Color redColor = new(1f, 0.4588236f, 0.4698749f);
    void Start()
    {
        nameText.text = entity.Name;
        lvlText.text = entity.Level.ToString() + " LVL";
        currentHP = entity.HealthPoints;
        maxHP = entity._MaxHealthPoints;
        hpText.text = entity.HealthPoints.ToString() + " HP";
        //slider setup
        if (entity.isHostile)
            hpSliderFill.color = redColor;
        else
            hpSliderFill.color = greenColor;
        hpSlider.maxValue = maxHP;
        hpSlider.value = currentHP;
    }
    void Update()
    {
        if (entity.HealthPoints != currentHP)
        {
            hpSlider.value = entity.HealthPoints;
            if (entity.HealthPoints >= 0)
                hpText.text = entity.HealthPoints.ToString() + " HP";
            else
                hpText.text = "0 HP";
        }
    }
}
