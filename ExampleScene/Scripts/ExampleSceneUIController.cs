using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HeliumDreamsTools;


public class ExampleSceneUIController : MonoBehaviour
{
    public GameObject IntroScreen;

    public Text smoothingIterations;
    public Text chaseSpeed;
    public Text yOffsetText;

    public RectTransform settingsMenu;
    public bool settingsMenuVisible;
    public Vector2 hiddenPosition, shownPosition;

    public GameObject disableGyroButton;

    public Toggle toggleGyro;

    private void Start()
    {
        IntroScreen.SetActive(true);

        shownPosition = new Vector2(0, -200.0f);
        hiddenPosition = new Vector2(-settingsMenu.sizeDelta.x, -200.0f);

        settingsMenu.anchoredPosition = hiddenPosition;
        settingsMenuVisible = false;
    }

    private void Update()
    {
        smoothingIterations.text = GyroscopicTouchCamera.instance.smoothingSamples.ToString();
        chaseSpeed.text = GyroscopicTouchCamera.instance.chaseSpeed.ToString();
        yOffsetText.text = GyroscopicTouchCamera.instance.customYOffset.ToString();
    }

    public void SetIntroScreenActice(bool isOn)
    {
        IntroScreen.SetActive(isOn);
    }

    public void ToggleSettingsMenu()
    {
        settingsMenuVisible = !settingsMenuVisible;

        settingsMenu.anchoredPosition = settingsMenuVisible ? shownPosition : hiddenPosition;
    }

    public void ToggleTouchDisableGyro(bool isOn)
    {
        disableGyroButton.SetActive(isOn);
    }

    public void UpdateToggleGyroDisplay()
    {
        toggleGyro.isOn = GyroscopicTouchCamera.instance.isGyroActive;
    }
}
