using System.Collections;
using System.Collections.Generic;
using BaroqueUI;
using UnityEngine;
using UnityEngine.UI;


public class IntroPointer : MonoBehaviour, IPongPad
{
    const int LAYER_SELECTION = 12;

    Controller controller;
    bool triggered;
    Transform cylinder_tr;
    float z_max;

    public void StartFollowing(Controller ctrl)
    {
        controller = ctrl;
        triggered = true;
        cylinder_tr = transform.GetChild(0);
        if (z_max == 0f)
            z_max = cylinder_tr.localScale.y;
    }

    public void FollowController()
    {
        float z = z_max;

        if (Physics.Raycast(transform.position, transform.forward, out var hitInfo, 10f,
            layerMask: 1 << LAYER_SELECTION) && hitInfo.collider != null)
        {
            var fadeout = hitInfo.collider.GetComponent<FadeOut>();
            if (fadeout == null)
            {
                fadeout = hitInfo.collider.gameObject.AddComponent<FadeOut>();
                controller.HapticPulse(1000);
            }
            fadeout.Bump();
            z = hitInfo.distance * 0.5f;

            if (controller.triggerPressed && !triggered)
            {
                var menu_item = hitInfo.collider.GetComponent<IntroMenuItem>();
                if (menu_item != null)
                    StartCoroutine(menu_item.SceneChange());
            }
        }
        var v = cylinder_tr.localScale; v.y = z; cylinder_tr.localScale = v;
        v = cylinder_tr.localPosition; v.z = z; cylinder_tr.localPosition = v;
        triggered = controller.triggerPressed;
    }

    void IPongPad.DestroyPad()
    {
        DestroyImmediate((GameObject)gameObject);
    }


    class FadeOut : MonoBehaviour
    {
        float highlight_start;

        public void Bump() { highlight_start = Time.time; }

        private void Update()
        {
            var text = GetComponentInChildren<Text>();
            var t = (Time.time - highlight_start) * 5f;
            if (text != null)
            {
                text.color = Color.Lerp(
                    new Color(0.25f, 0, 0, 1),
                    new Color(0.1960784f, 0.1960784f, 0.1960784f, 0.6705883f),
                    t);
            }
            if (t >= 1f)
                Destroy((Object)this);
        }
    }
}
