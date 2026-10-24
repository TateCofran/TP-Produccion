using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SpeedButtonGroup : MonoBehaviour
{
    [System.Serializable]
    public class SpeedButton
    {
        public Button button;
        public Image targetImage;
        public Sprite normalSprite;
        public Sprite pressedSprite;
        public float speedValue;
    }

    public List<SpeedButton> buttons;

    private void Start()
    {
        foreach (var btn in buttons)
        {
            float speed = btn.speedValue;
            btn.button.onClick.AddListener(() => OnSpeedButtonClicked(speed));
        }

        OnSpeedButtonClicked(1f);
    }

    private void OnEnable()
    {
        GameTime.OnSpeedChanged += RefreshVisuals;
    }

    private void OnDisable()
    {
        GameTime.OnSpeedChanged -= RefreshVisuals;
    }

    private void OnSpeedButtonClicked(float selectedSpeed)
    {
        GameTime.SetSpeed(selectedSpeed);
    }

    private void RefreshVisuals(float currentSpeed)
    {
        foreach (var btn in buttons)
        {
            if (btn.targetImage == null) continue;
            btn.targetImage.sprite = Mathf.Approximately(btn.speedValue, currentSpeed)
                ? btn.pressedSprite
                : btn.normalSprite;
        }
    }
}
