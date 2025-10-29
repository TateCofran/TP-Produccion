using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIAudioManager : MonoBehaviour
{
    [Header("Sonido de click")]
    [SerializeField] private AudioClip clickSound;

    private AudioSource audioSource;

    //para registrar botones solo una vez, sin repetidos
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
        //botones activos
        Button[] buttons = FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Button btn in buttons)
        {
            //si no está registrado, o si estaba desactivado antes
            if (!registeredButtons.Contains(btn))
            {
                btn.onClick.AddListener(PlayClickSound);
                registeredButtons.Add(btn);
                //Debug.Log("Registrado botón: " + btn.name);
            }
            else
            {
                //si estaba en el HashSet pero estaba inactivo antes, ver si está activo ahora
                //para que los botones que se activan y reactivan vuelvan a sonar
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

