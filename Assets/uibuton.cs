using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClickBehavior : MonoBehaviour
{
    public string sceneToLoad; // ��� ����� (������) ���� ���� ������

    // ����� ��� ����� ��� ����� ��� ����
    public void OnButtonClick()
    {
        // ����� ����� (������) �������
        SceneManager.LoadScene(sceneToLoad);
    }
}
