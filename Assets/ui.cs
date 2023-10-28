using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ImageInteraction : MonoBehaviour
{
    public AudioClip soundClip; // «·’Ê  «·–Ì ”Ì „  ‘€Ì·Â ⁄‰œ «·‰ﬁ— ⁄·Ï «·’Ê—….
    public GameObject secondScene; // «·”Ì‰ «·À«‰Ì «·–Ì ”Ì „  ‘€Ì·Â ⁄‰œ «·‰ﬁ— ⁄·Ï «·’Ê—….
    public GameObject twiq; // «·⁄‰’— «·–Ì ”Ì „  ﬂ»Ì—Â Ê‰ﬁ·Â.
    public float scaleFactor = 1.5f; // ⁄«„·  ﬂ»Ì— ÕÃ„ «·’Ê—… ⁄‰œ «·‰ﬁ— ⁄·ÌÂ«.
    private bool isImageZoomed = false;
    private Image imageComponent;
    private bool hasMovedToStartScenes = false;

    private void Start()
    {
        // «·Õ’Ê· ⁄·Ï „ﬂÊ‰ «·’Ê—… ›Ì Ê«ÃÂ… «·„” Œœ„.
        imageComponent = GetComponent<Image>();
    }

    private void Update()
    {
        // «· ›«⁄· ⁄‰œ «·‰ﬁ— »“— «·›√—… «·√Ì”—.
        if (Input.GetMouseButtonDown(0)) // 0 Ì‘Ì— ≈·Ï “— «·›√—… «·√Ì”—.
        {
            OnImageClick();
        }
    }

    public void OnImageClick()
    {
        if (isImageZoomed && !hasMovedToStartScenes)
        {
            //  ⁄ÌÌ‰ ÕÃ„ «·⁄‰’— `twiq` ≈·Ï «·ÕÃ„ «·„ﬂ»—.
            twiq.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            isImageZoomed = false;

            //  ‘€Ì· «·’Ê .
            AudioSource.PlayClipAtPoint(soundClip, transform.position);

            // »œ¡ «·”Ì‰ «·À«‰Ì.
            secondScene.SetActive(true);

            // ﬁ„ » ⁄ÌÌ‰ «·⁄·«„… ··≈‘«—… ≈·Ï √‰ «·«‰ ﬁ«· ≈·Ï "Start Scenes"  „.
            hasMovedToStartScenes = true;
        }
        else if (hasMovedToStartScenes)
        {
            //  Õ„Ì· «·”Ì‰ "Start Scenes".
            SceneManager.LoadScene("Start Scenes");
        }
        else
        {
            //  ﬂ»Ì— ÕÃ„ «·’Ê—….
            imageComponent.rectTransform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            isImageZoomed = true;
        }
    }
}
