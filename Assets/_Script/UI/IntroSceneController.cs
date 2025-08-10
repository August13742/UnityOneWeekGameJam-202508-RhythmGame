using UnityEngine;

public class IntroSceneController : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(ScheduleStart());
    }
    System.Collections.IEnumerator ScheduleStart()
    {
        CrossfadeManager.Instance.FadeFromBlack(1.5f);
        yield return new WaitForSeconds(2.5f);
        CrossfadeManager.Instance.FadeToBlack(1.5f);
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("TitleScene");
    }
}
