using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonClickBehavior : MonoBehaviour
{
    public string sceneToLoad; // ÇÓã ÇáÓíä (ÇáãÔåÏ) ÇáĞí ÊÑíÏ ÊÍãíáå

    // ÊäİíĞ åĞÇ ÇáßæÏ ÚäÏ ÇáäŞÑ Úáì ÇáÒÑ
    public void OnButtonClick()
    {
        // ÊÍãíá ÇáÓíä (ÇáãÔåÏ) ÇáãØáæÈ
        SceneManager.LoadScene(sceneToLoad);
    }
}
