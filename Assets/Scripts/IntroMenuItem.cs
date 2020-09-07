using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaroqueUI;


public class IntroMenuItem : MonoBehaviour
{
    public Color color;
    public int episode;
    public Text scoreText;
    public string sceneName;


    static int played_episode;
    static bool fetch_started;

    MaterialPropertyBlock pb;
    float highlight_start;
    int auto_rebump;

    private void Start()
    {
        pb = new MaterialPropertyBlock();
        SetColorForHighlight(0);

        if (episode > 0)
        {
            RefreshScores();
            Scores.GetLocalScore(episode).global_high_score_updated = RefreshScores;

            if (!fetch_started)
            {
                Scores.FetchGlobalScores();
                fetch_started = true;
            }

            if (played_episode == episode)
            {
                highlight_start = Time.time;
                auto_rebump = 4;
            }
        }
    }

    void SetColorForHighlight(float highlight)
    {
        var col1 = Color.Lerp(color, color * 2f, highlight);
        pb.SetColor("_Color", col1);
        GetComponent<MeshRenderer>().SetPropertyBlock(pb);
    }

    void RefreshScores()
    {
        if (!this || !gameObject)
            return;

        var score = Scores.GetLocalScore(episode);
        string s = string.Format("top score: {0}\nlocal best: {1}\nlatest: {2}",
            Points.FormatPoints(score.global_high_score),
            Points.FormatPoints(score.local_high_score),
            Points.FormatPoints(score.latest_run));
        scoreText.text = s;
    }

    private void LateUpdate()
    {
        if (highlight_start == 0f)
            return;

        float t = (Time.time - highlight_start) * 3f;
        SetColorForHighlight(1f - t);
        if (t >= 1f)
        {
            highlight_start = 0f;
            if (auto_rebump > 0)
            {
                auto_rebump--;
                highlight_start = Time.time;
            }
        }
    }

    public void Bump(Controller controller)
    {
        if (highlight_start == 0f)
            controller.HapticPulse(1000);
        highlight_start = Time.time;
    }

    public IEnumerator SceneChange()
    {
        FindObjectOfType<PongBaseBuilder>().FadeOutSounds(0.2f);
        Baroque.FadeToColor(Color.black, 0.2f);
        yield return new WaitForSeconds(0.2f);
        played_episode = episode;
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
