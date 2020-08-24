using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class FollowJoystick : MonoBehaviour
{
    float current_angle;

    IEnumerator Start()
    {
        while (true)
        {
            yield return null;

            foreach (var ctrl in Baroque.GetControllers())
            {
                if (!ctrl.isReady)
                    continue;

                Vector2 pos = ctrl.touchpadPosition;
                if (pos.sqrMagnitude < 0.6f * 0.6f || Mathf.Abs(pos.x) < 0.3f)
                    continue;

                /* moving the joystick! */
                float dir = Mathf.Sign(pos.x);
                current_angle += dir * 120f;
                if (current_angle >= 360f)
                    current_angle -= 360f;
                else if (current_angle < 0f)
                    current_angle += 360f;

                transform.rotation = Quaternion.Euler(0, -current_angle, 0);
                PongPadBuilder.instance.ChangeTrackingSpacePosition(transform.GetChild(0));

                Baroque.FadeToColor(new Color(0.5f, 0.5f, 0.5f), 0.05f);
                yield return new WaitForSeconds(0.05f);
                Baroque.FadeToColor(Color.clear, 0.05f);

                /* wait until we release the joystick */
                while (true)
                {
                    yield return null;
                    if (!ctrl.isReady)
                        break;
                    pos = ctrl.touchpadPosition;
                    if (pos.sqrMagnitude < 0.4f * 0.4f || Mathf.Abs(pos.x) < 0.14f)
                        break;
                }
                break;
            }
        }
    }
}
