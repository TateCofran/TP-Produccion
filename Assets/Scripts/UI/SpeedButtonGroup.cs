using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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

    void Start()
    {
        foreach (var btn in buttons)
        {
            float speed = btn.speedValue;
            btn.button.onClick.AddListener(() => OnSpeedButtonClicked(speed));
        }

        // Inicializa en velocidad x1
        OnSpeedButtonClicked(1f);
    }

    void OnSpeedButtonClicked(float selectedSpeed)
    {
        Time.timeScale = selectedSpeed;

        foreach (var btn in buttons)
        {
            if (btn.speedValue == selectedSpeed)
                btn.targetImage.sprite = btn.pressedSprite;
            else
                btn.targetImage.sprite = btn.normalSprite;
        }
    }
}
