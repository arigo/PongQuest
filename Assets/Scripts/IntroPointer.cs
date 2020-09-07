using System.Collections;
using System.Collections.Generic;
using BaroqueUI;
using UnityEngine;


public class IntroPointer : MonoBehaviour, IPongPad
{
    public const int LAYER_SELECTION = 12;

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
            z = hitInfo.distance * 0.5f;

            var menu_item = hitInfo.collider.GetComponent<IntroMenuItem>();
            if (menu_item != null)
            {
                menu_item.Bump(controller);
                if (controller.triggerPressed && !triggered)
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
}
