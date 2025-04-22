using System.Collections.Generic;
using UnityEngine;

public class MotionDetector : MonoBehaviour
{
    public enum Mode { ClosestOnly, Full }

    [Header("Settings")]
    public float maxRange = 10f;
    public float pulseSpeed = 5f;
    public Mode mode = Mode.Full;
    public bool isActive = true;

    [Header("Blip Settings")]
    public float blipLifetime = 2f;
    public Color blipColor = Color.red;

    [Header("References")]
    public Color pulseColor = Color.green;
    public GameObject blipPrefab;
    public Material blipMaterial;
    public Material pulseMaterial;

    [Header("Debug")]
    [Space(20)]
    [SerializeField] private float currentScale = 0f;
    [SerializeField] private List<GameObject> detectedObjects = new List<GameObject>();

    void Update()
    {
        if (!isActive) return;

        currentScale += pulseSpeed * Time.deltaTime;

        if (currentScale >= maxRange)
        {
            currentScale = 0f;
            detectedObjects.Clear();
        }

        transform.localScale = Vector3.one * currentScale;

        float alpha = 1 - (currentScale / maxRange);
        pulseMaterial.color = new Color(pulseColor.r, pulseColor.g, pulseColor.b, alpha);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        if (other.gameObject.layer == gameObject.layer && !detectedObjects.Contains(other.gameObject))
        {
            detectedObjects.Add(other.gameObject);
            CreateBlip(other.transform.position);
            if (mode == Mode.ClosestOnly) currentScale = 0f;
        }
    }

    private void CreateBlip(Vector3 position)
    {
        GameObject blip = Instantiate(blipPrefab, position, Quaternion.identity);

        blip.transform.localScale = Vector3.one * 0.5f;

        MotionBlip blipScript = blip.GetComponent<MotionBlip>();
        blipScript.Initialize(blipColor, blipLifetime, blipMaterial);
    }

    public void ToggleRadar(bool state)
    {
        isActive = state;

        if (!isActive)
        {
            currentScale = 0f;
            detectedObjects.Clear();
        }
    }
}
