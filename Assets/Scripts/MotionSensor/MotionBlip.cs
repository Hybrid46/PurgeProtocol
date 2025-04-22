using UnityEngine;

public class MotionBlip : MonoBehaviour
{
    private Material material;
    private Color originalColor;
    private float fadeTimer;
    private float fadeDuration;

    public void Initialize(Color color, float fadeTime, Material blipMaterial)
    {
        material = new Material(blipMaterial);
        GetComponent<Renderer>().sharedMaterial = material;

        originalColor = color;
        material.color = originalColor;
        fadeDuration = fadeTime;
        fadeTimer = fadeDuration;

        Destroy(gameObject, fadeTime);
    }

    void Update()
    {
        if (fadeTimer > 0)
        {
            fadeTimer -= Time.deltaTime;
            float alpha = Mathf.Clamp01(fadeTimer / fadeDuration);
            material.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
