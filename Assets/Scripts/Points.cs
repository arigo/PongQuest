using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BaroqueUI;


public class Points : MonoBehaviour
{
    public Text text;

    public static void AddPoints(Vector3 position, Color color, int count, float points_size = 0f)
    {
        Quaternion rot = Quaternion.LookRotation(position - Baroque.GetHeadTransform().position);
        rot.eulerAngles += Random.insideUnitSphere * 10f;

        var p = Instantiate(PongPadBuilder.instance.canvasPointsPrefab, position, rot);
        p.text.text = count.ToString();
        p.text.color = color;
        if (points_size > 1f)
            p.transform.localScale *= points_size;
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
