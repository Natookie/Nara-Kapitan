using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UILightningController : MonoBehaviour
{
    [Header("LIGHTNING TWEAKS")]
    [SerializeField] private RectTransform lightningPrefab;
    [SerializeField] private Canvas canvas;
    [SerializeField] private int lightningCount = 3;

    [Header("TIMING")]
    [SerializeField] private float minDelay = 1f;
    [SerializeField] private float maxDelay = 3f;
    [SerializeField] private float flashDelay = 0.05f;
    [SerializeField] private float lightningLifetime = 0.15f;

    [Header("FLASH UI")]
    [SerializeField] private Image flashImage;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float maxFlashIntensity = 5f;

    private RectTransform canvasRect;
    private Material flashMaterial;

    void Awake(){
        canvasRect = canvas.GetComponent<RectTransform>();

        if(flashImage != null){
            flashImage.gameObject.SetActive(false);

            flashMaterial = Instantiate(flashImage.material);
            flashImage.material = flashMaterial;

            flashMaterial.SetFloat("_Intensity", 0f);
        }
    }

    void Start(){
        StartCoroutine(LightningRoutine());
    }

    IEnumerator LightningRoutine(){
        while(true){
            yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));
            for(int i = 0; i < lightningCount; i++) SpawnLightning();
            yield return new WaitForSeconds(flashDelay);

            if(flashImage != null) StartCoroutine(Flash());
        }
    }

    void SpawnLightning(){
        RectTransform lightning = Instantiate(lightningPrefab, canvas.transform);

        Vector2 randomPos = new Vector2(
            Random.Range(0, canvasRect.rect.width),
            Random.Range(0, canvasRect.rect.height)
        );

        lightning.anchoredPosition = randomPos;
        lightning.rotation = Quaternion.Euler(0, 0, Random.Range(-20f, 20f));

        float height = Random.Range(200f, 600f);
        lightning.sizeDelta = new Vector2(20f, height);

        Destroy(lightning.gameObject, lightningLifetime);
    }

    IEnumerator Flash(){
        flashImage.gameObject.SetActive(true);

        float halfDuration = flashDuration * 0.5f;
        float t = 0f;

        while(t < halfDuration){
            t += Time.deltaTime;
            float normalized = t / halfDuration;
            float intensity = Mathf.Lerp(0f, maxFlashIntensity, normalized);
            flashMaterial.SetFloat("_Intensity", intensity);
            yield return null;
        }

        t = 0f;

        while(t < halfDuration){
            t += Time.deltaTime;
            float normalized = t / halfDuration;
            float intensity = Mathf.Lerp(maxFlashIntensity, 0f, normalized);
            flashMaterial.SetFloat("_Intensity", intensity);
            yield return null;
        }

        flashMaterial.SetFloat("_Intensity", 0f);
        flashImage.gameObject.SetActive(false);
    }
}