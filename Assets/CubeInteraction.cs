using UnityEngine;
using UnityEngine.SceneManagement;

public class CubeInteraction : MonoBehaviour
{
    public string sceneToLoad; // ÇÓã ÇáÓíä (ÇáãÔåÏ) ÇáĞí ÊÑíÏ ÊÍãíáå

    // åĞå ÇáÏÇáÉ ÊÊã ÇÓÊÏÚÇÄåÇ ÚäÏ áãÓ ÇáßæãÈæääÊ Trigger ÇáĞí íÊã ÊæÕíáå ÈÇáßíæÈ.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // ÊÍŞŞ ãä Ãä ÇáßæãÈæääÊ ÇáĞí ÏÎá İí ÇáãäØŞÉ åæ áÇÚÈß (íãßäß ÇÓÊÎÏÇã ÊæÓíãÇÊ Ãæ ØÈŞÇÊ ááÊÍŞŞ ãä Ğáß)
        {
            // ÊÍãíá ÇáÓíä (ÇáãÔåÏ) ÇáãØáæÈ
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
