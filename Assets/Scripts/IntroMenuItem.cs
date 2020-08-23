using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class IntroMenuItem : MonoBehaviour
{
    public string sceneName;


    public IEnumerator SceneChange()
    {
        FindObjectOfType<PongBaseBuilder>().FadeOutSounds(0.2f);
        Baroque.FadeToColor(Color.black, 0.2f);
        yield return new WaitForSeconds(0.2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
