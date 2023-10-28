using UnityEngine;
using UnityEngine.SceneManagement;

public class CubeInteraction : MonoBehaviour
{
    public string sceneToLoad; // ��� ����� (������) ���� ���� ������

    // ��� ������ ��� ��������� ��� ��� ���������� Trigger ���� ��� ������ �������.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // ���� �� �� ���������� ���� ��� �� ������� �� ����� (����� ������� ������� �� ����� ������ �� ���)
        {
            // ����� ����� (������) �������
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
