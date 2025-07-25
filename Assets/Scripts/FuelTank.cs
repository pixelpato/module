using System.Collections;
using UnityEditor;
using UnityEngine;


public class FuelTank : MonoBehaviour
{
    public float increaseRate = 1.5f; // Rate at which the counter increases
    public GameObject car;
    public GameObject meshToHide;
    public float currentLiquid;
    public float maxLiquid;

    public AudioSource audioSource;

    public AudioClip audioClip;

    private Coroutine fuelCoroutine;
    private GameObject collidingObject;
    private string startName;


    private void Start ()
    {
        startName = gameObject.name;
        gameObject.name = startName + " " + Mathf.Floor(currentLiquid) + "/" + maxLiquid + "L";
    }

    private void OnTriggerEnter (Collider collision)
    {
        collidingObject = collision.gameObject;

        if (
            collision.CompareTag("Fuel") &&
            collidingObject.GetComponent<UseObject>() != null &&
            collidingObject.GetComponent<UseObject>().isActive == true &&
            collidingObject.GetComponent<FuelTank>().currentLiquid > 0f &&
            meshToHide.GetComponent<MeshRenderer>().enabled == false
            )
        {
            RefuelTank();

            if (collidingObject.GetComponent<UseObject>().meshToHide != null)
            {
                collidingObject.GetComponent<UseObject>().meshToHide.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    private void RefuelTank()
    {
        if (fuelCoroutine == null)
        {
            fuelCoroutine = StartCoroutine(IncreaseCounter());
        }

        FuelTank script = collidingObject.GetComponent<FuelTank>();

        if (script)
        {
            script.audioSource.clip = script.audioClip;
            script.audioSource.loop = true;
            script.audioSource.Play();
        }
    }

    private void OnTriggerExit (Collider collision)
    {
        collidingObject = collision.gameObject;
        FuelTank script = collidingObject.GetComponent<FuelTank>();

        if (collision.CompareTag("Fuel") &&
            collidingObject.GetComponent<UseObject>()
            )
        {
            if (fuelCoroutine != null)
            {
                StopCoroutine(fuelCoroutine);
                fuelCoroutine = null;
            }

            if (collidingObject.GetComponent<UseObject>().meshToHide != null)
            {
                collidingObject.GetComponent<UseObject>().meshToHide.GetComponent<MeshRenderer>().enabled = true;
            }

            if (script.audioSource) script.audioSource.Stop();
        }
    }

    private IEnumerator IncreaseCounter ()
    {
        while (true)
        {
            if (currentLiquid < maxLiquid && collidingObject.GetComponent<FuelTank>().currentLiquid > 0f)
            {
                currentLiquid += increaseRate * Time.deltaTime;
                collidingObject.GetComponent<FuelTank>().currentLiquid -= increaseRate * Time.deltaTime;

                if (collidingObject.GetComponent<FuelTank>().currentLiquid < 0f) collidingObject.GetComponent<FuelTank>().currentLiquid = 0f;

                gameObject.name = startName + " " + Mathf.Floor(currentLiquid) + "/" + maxLiquid + "L";
                collidingObject.name = collidingObject.GetComponent<FuelTank>().startName + " " + Mathf.Floor(collidingObject.GetComponent<FuelTank>().currentLiquid) + "/" + collidingObject.GetComponent<FuelTank>().maxLiquid + "L";
            }

            yield return null; // Wait for the next frame
        }
    }
}