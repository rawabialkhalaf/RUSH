using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(AudioSource))]
public class PlayerSoundController : MonoBehaviour
{
    private AudioSource audioSource;
    private bool isMoving = false;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Update()
    {
        // Â–« „Ã—œ „À«· ·· Õﬁﬁ „‰  Õ—ﬂ «··«⁄» »«” Œœ«„ „ÕÊ—Ì «·Õ—ﬂ…
        // ﬁœ  Õ «Ã ≈·Ï  ⁄œÌ· Â–Â «·ÿ—Ìﬁ… »‰«¡ ⁄·Ï ‰Ÿ«„ «·Õ—ﬂ… «·Œ«’ »ﬂ
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (horizontal != 0 || vertical != 0)
        {
            if (!isMoving)
            {
                isMoving = true;
                audioSource.Play();
            }
        }
        else
        {
            if (isMoving)
            {
                isMoving = false;
                audioSource.Stop();
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        audioSource.Stop();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
