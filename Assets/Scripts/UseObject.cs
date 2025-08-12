using System;
using System.Collections;
using Unity.Burst.CompilerServices;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;


public class UseObject : MonoBehaviour
{
    public enum UseType { switchOnOff, button, slider };

    public UseType useType;
    public GameObject ship;
    public bool isActive = false;
    public GameObject lightBulb;

    [SerializeField] private Vector3 minRotation = Vector3.zero;
    [SerializeField] private Vector3 maxRotation = Vector3.zero;
    [SerializeField] private AudioClip useSoundClip;

    private Transform player;
    private AudioSource useAudioSource;


    private void Start()
    {
        player = GameObject.Find("Player").transform;
        useAudioSource = player.GetComponent<MouseLook>().playerAudioSource;
    }

    public void Use()
    {
        // state toggle
        if (isActive) isActive = false;
        else isActive = true;

        if (useType == UseType.switchOnOff)
        {
            if (!isActive)
            {
                transform.localRotation = Quaternion.Euler(maxRotation);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(minRotation);
            }

            if (lightBulb)
            {
                Light lightSource = lightBulb.GetComponent<Light>();

                if (!lightSource.enabled)
                {
                    lightBulb.GetComponent<MeshRenderer>().material.EnableKeyword("_EMISSION");
                    lightSource.enabled = true;
                }
                else
                {
                    lightBulb.GetComponent<MeshRenderer>().material.DisableKeyword("_EMISSION");
                    lightSource.enabled = false;
                }
            }
        }
        else if (useType == UseType.button)
        {
            // push button functions
        }
        else if (useType == UseType.slider)
        {
            // turn knob functions
        }

        if (useAudioSource != null && useSoundClip != null)
        {
            useAudioSource.PlayOneShot(useSoundClip);
        }
    }
}