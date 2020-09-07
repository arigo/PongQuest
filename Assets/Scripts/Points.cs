using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaroqueUI;


public class Points : MonoBehaviour
{
    static Color total_points_color;

    public Text text;

    public static string FormatPoints(int score)
    {
        if (score < 0)
            return "---";
        return score.ToString("###\\'###\\'##0").TrimStart('\'');
    }

    public static void UpdateTotalPoints(int count)
    {
        var b = PongPadBuilder.instance;
        b._total_points += count;
        if (b._total_points < 0)
            b._total_points = 0;
        b.totalPointsText.text = FormatPoints(b._total_points);
    }

    public static void AddPoints(Vector3 position, Color color, int count, float points_size = 0f)
    {
        Quaternion rot = Quaternion.LookRotation(position - Baroque.GetHeadTransform().position);
        rot.eulerAngles += Random.insideUnitSphere * 10f;

        var b = PongPadBuilder.instance;
        var p = Instantiate(b.canvasPointsPrefab, position, rot);
        p.text.text = count.ToString();
        p.text.color = color;
        if (points_size > 1f)
            p.transform.localScale *= points_size;

        if (total_points_color == new Color())
            total_points_color = b.totalPointsText.color;

        UpdateTotalPoints(count);
        b.StartCoroutine(_Blink());

        IEnumerator _Blink()
        {
            float fraction = 0.75f;
            while (true)
            {
                b.totalPointsText.color = Color.Lerp(total_points_color, color, fraction);
                if (fraction <= 0f)
                    break;
                yield return null;
                fraction -= Time.deltaTime;
            }
        }
    }

    IEnumerator Start()
    {
        float delta_y = 0;
        float vy = 1.5f;
        while (vy > 0f)
        {
            yield return null;

            float delta = Time.deltaTime * vy * 100f;
            delta_y += delta;
            transform.position += transform.TransformVector(new Vector3(0, delta, 0));
            vy -= Time.deltaTime;
        }
        Destroy((GameObject)gameObject, 0.1f);
    }
}
