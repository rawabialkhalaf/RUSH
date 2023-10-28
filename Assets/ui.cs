using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ImageInteraction : MonoBehaviour
{
    public AudioClip soundClip; // ����� ���� ���� ������ ��� ����� ��� ������.
    public GameObject secondScene; // ����� ������ ���� ���� ������ ��� ����� ��� ������.
    public GameObject twiq; // ������ ���� ���� ������ �����.
    public float scaleFactor = 1.5f; // ���� ����� ��� ������ ��� ����� �����.
    private bool isImageZoomed = false;
    private Image imageComponent;
    private bool hasMovedToStartScenes = false;

    private void Start()
    {
        // ������ ��� ���� ������ �� ����� ��������.
        imageComponent = GetComponent<Image>();
    }

    private void Update()
    {
        // ������� ��� ����� ��� ������ ������.
        if (Input.GetMouseButtonDown(0)) // 0 ���� ��� �� ������ ������.
        {
            OnImageClick();
        }
    }

    public void OnImageClick()
    {
        if (isImageZoomed && !hasMovedToStartScenes)
        {
            // ����� ��� ������ `twiq` ��� ����� ������.
            twiq.transform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            isImageZoomed = false;

            // ����� �����.
            AudioSource.PlayClipAtPoint(soundClip, transform.position);

            // ��� ����� ������.
            secondScene.SetActive(true);

            // �� ������ ������� ������� ��� �� �������� ��� "Start Scenes" ��.
            hasMovedToStartScenes = true;
        }
        else if (hasMovedToStartScenes)
        {
            // ����� ����� "Start Scenes".
            SceneManager.LoadScene("Start Scenes");
        }
        else
        {
            // ����� ��� ������.
            imageComponent.rectTransform.localScale = new Vector3(scaleFactor, scaleFactor, 1);
            isImageZoomed = true;
        }
    }
}
