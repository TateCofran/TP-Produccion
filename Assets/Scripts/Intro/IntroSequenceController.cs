using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public sealed class IntroSequenceController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image slideImage;
    [SerializeField] private TextMeshProUGUI slideText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    public TextPopIn textPopIn;

    [Header("Slides")]
    [Tooltip("Assign the 3 intro images here in order.")]
    [SerializeField] private Sprite[] slideImages;

    [Header("Flow Settings")]
    [SerializeField] private string nextSceneName = "Menu";
    [Tooltip("If true, the sequence starts automatically on Start().")]
    [SerializeField] private bool autoStart = true;

    // Optional: if true, the intro will only be shown once.
    [SerializeField] private bool showOnlyOnce = false;
    [SerializeField] private string playerPrefsKey = "HasSeenIntro";

    private readonly List<string> slideTexts = new List<string>();
    private int currentIndex;
    private int MaxIndex => Mathf.Min(slideImages.Length, slideTexts.Count) - 1;

    private void Awake()
    {
        if (showOnlyOnce && PlayerPrefs.GetInt(playerPrefsKey, 0) == 1)
        {
            LoadNextScene();
            return;
        }

        BuildTexts();

        if (nextButton != null)
            nextButton.onClick.AddListener(ShowNext);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipIntro);
    }

    private void Start()
    {
        if (slideImages == null || slideImages.Length == 0)
        {
            Debug.LogWarning("[IntroSequenceController] No slide images assigned.");
            return;
        }

        if (autoStart)
        {
            ShowSlide(0);
        }
    }

    private void BuildTexts()
    {
        slideTexts.Clear();

        // Slide 0
        slideTexts.Add(
            "The world was shattered. Two realities now overlap, " +
            "fighting for the same space."
        );

        // Slide 1
        slideTexts.Add(
            "From the cracks between worlds, corrupted creatures emerged, " +
            "spreading chaos and destruction."
        );

        // Slide 2
        slideTexts.Add(
            "You are the last line of defense."

        );
    }

    private void ShowSlide(int index)
    {
        if (slideImages == null || slideImages.Length == 0 || slideTexts.Count == 0)
        {
            Debug.LogWarning("[IntroSequenceController] Slides not properly configured.");
            return;
        }

        currentIndex = Mathf.Clamp(index, 0, MaxIndex);

        // Image
        if (slideImage != null && slideImages.Length > currentIndex)
        {
            slideImage.sprite = slideImages[currentIndex];
        }

        // Text
        if (slideText != null && slideTexts.Count > currentIndex)
        {
            slideText.text = slideTexts[currentIndex];
            textPopIn.PlayPopIn();
        }

        // Button label (Next / Start)
        if (nextButton != null)
        {
            var label = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                bool isLast = currentIndex >= MaxIndex;
                label.text = isLast ? "Start" : "Next";
            }
        }
    }

    private void ShowNext()
    {
        if (currentIndex >= MaxIndex)
        {
            EndIntro();
        }
        else
        {
            ShowSlide(currentIndex + 1);
        }
    }

    private void SkipIntro()
    {
        EndIntro();
    }

    private void EndIntro()
    {
        if (showOnlyOnce)
        {
            PlayerPrefs.SetInt(playerPrefsKey, 1);
            PlayerPrefs.Save();
        }

        LoadNextScene();
    }

    private void LoadNextScene()
    {
        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("[IntroSequenceController] nextSceneName is empty. " +
                             "Assign a scene name in the inspector.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}
