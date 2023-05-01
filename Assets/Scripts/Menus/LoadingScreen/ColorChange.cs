using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ColorChange : MonoBehaviour
{
    private Image image;
    private GameObject loadingPanel;
    private bool isDisappearing = false;
    
    void Awake()
    {
        image = GetComponent<Image>();
        loadingPanel = GetComponentInParent<LoadingCircle>().gameObject;
    }
    public void StartChange()
    {
        if (loadingPanel.activeSelf)
            StartCoroutine(ChangeDirection());
    }
    void Update()
    {
        if (isDisappearing)
        {
            image.color = Color.Lerp(image.color, Color.clear, Time.deltaTime * 6f);
        }
        else
        {
            image.color = Color.Lerp(image.color, Color.white, Time.deltaTime * 6f);
        }

    }
    private IEnumerator ChangeDirection()
    {
        isDisappearing = !isDisappearing;
        yield return new WaitForSeconds(3f);
        StartCoroutine(ChangeDirection());
    }
}
