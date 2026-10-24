using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    public static UIAudioManager instance;

    [Header("Click Sound")]
    [SerializeField] private AudioClip clickSound;

    private AudioSource audioSource;

    private readonly HashSet<Button> registeredButtons = new();

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        RegisterActiveButtons();
    }

    private void RegisterActiveButtons()
    {
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Button btn in buttons)
        {
            if (!registeredButtons.Contains(btn))
            {
                btn.onClick.AddListener(PlayClickSound);
                registeredButtons.Add(btn);
            }
            else
            {
                btn.onClick.RemoveListener(PlayClickSound);
                btn.onClick.AddListener(PlayClickSound);
            }
        }
    }

    private void PlayClickSound()
    {
        if (clickSound && audioSource)
            audioSource.PlayOneShot(clickSound);
    }
}
