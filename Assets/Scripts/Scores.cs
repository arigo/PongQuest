using System;
using System.Collections;
using System.IO;
using UnityEngine;


public static class Scores
{
    const int EPISODES = 3;

    [Serializable]
    public class Score
    {
        public int global_high_score = -1;
        public int local_high_score = -1;
        public int latest_run = -1;

        internal Action global_high_score_updated;
    }

    [Serializable]
    public class AllScores
    {
        public Score[] scores;
    }

    static Score[] _scores;


    const string BASE_URL = "https://vrsketch.eu/minesweeper/questanoid/score";

    static Score[] LocalScores()
    {
        if (_scores == null)
            _scores = LoadScoresFromDisk();
        return _scores;
    }

    public static Score GetLocalScore(int episode)
    {
        return LocalScores()[episode - 1];
    }

    static GlobalScoreFetcher gsf;

    public static void FetchGlobalScores(int just_played = 0)
    {
        if (gsf == null)
        {
            var go = new GameObject("global scores fetcher");
            UnityEngine.Object.DontDestroyOnLoad(go);
            gsf = go.AddComponent<GlobalScoreFetcher>();
        }
        gsf.StartCoroutine(gsf.FetchGlobalScores(just_played));
    }

    class GlobalScoreFetcher : MonoBehaviour
    {
        public IEnumerator FetchGlobalScores(int just_played)
        {
            yield return new WaitForSeconds(0.1f);

            string url = BASE_URL;
            if (just_played > 0)
            {
                int hs = GetLocalScore(just_played).local_high_score;
                url = string.Format("{0}?episode={1}&hs={2}", url, just_played, hs);
            }
            WWW www = new WWW(url);
            yield return www;

            if (string.IsNullOrEmpty(www.error))
            {
                var scores = LocalScores();
                bool changes = false;
                string[] incoming = www.text.Split(' ', '\n');
                for (int i = 0; i < scores.Length && i < incoming.Length; i++)
                    if (int.TryParse(incoming[i], out var s) && s != scores[i].global_high_score)
                    {
                        scores[i].global_high_score = s;
                        scores[i].global_high_score_updated?.Invoke();
                        changes = true;
                    }
                if (changes)
                    SaveScoresToDisk();
            }
            else
                Debug.LogError(url + " ===> " + www.error);
        }
    }

    static T LoadJson<T>(string filename) where T : class
    {
        if (!File.Exists(filename))
            return null;
        try
        {
            var json_str = File.ReadAllText(filename, System.Text.Encoding.UTF8);
            return JsonUtility.FromJson<T>(json_str);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return null;
        }
    }

    static bool SaveJson<T>(string filename, T obj, bool erase = false)
    {
        try
        {
            if (erase)
            {
                if (File.Exists(filename))
                {
                    if (File.Exists(filename + ".bak"))
                        File.Delete(filename + ".bak");
                    File.Move(filename, filename + ".bak");
                }
            }
            else
            {
                var json_str = JsonUtility.ToJson(obj);
                File.WriteAllText(filename + "~", json_str, System.Text.Encoding.UTF8);
                if (File.Exists(filename))
                    File.Delete(filename);
                File.Move(filename + "~", filename);
            }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            return false;
        }
    }

    static string FileName => Application.persistentDataPath + "/scores.json";

    static Score[] LoadScoresFromDisk()
    {
        var all_scores = LoadJson<AllScores>(FileName);
        var scores = all_scores?.scores;
        if (scores == null)
            scores = new Score[0];
        if (scores.Length < EPISODES)
            Array.Resize(ref scores, EPISODES);

        for (int i = 0; i < EPISODES; i++)
            if (scores[i] == null)
                scores[i] = new Score();

        return scores;
    }

    static void SaveScoresToDisk()
    {
        if (_scores != null)
        {
            var all_scores = new AllScores { scores = _scores };
            SaveJson(FileName, all_scores);
        }
    }

    public static void NewScore(int episode, int points)
    {
        var score = GetLocalScore(episode);
        score.latest_run = points;
        if (points > score.local_high_score)
        {
            score.local_high_score = points;
            if (points > score.global_high_score)
                score.global_high_score = points;
            FetchGlobalScores(episode);
        }
        SaveScoresToDisk();
    }
}
