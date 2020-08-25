using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class FollowJoystick : MonoBehaviour
{
    public GameObject canvasInfo;

    float current_angle;
    bool[] stop_instruction;

    void Start()
    {
        foreach (var ctrl in Baroque.GetControllers())
            StartCoroutine(_FollowJoystick(ctrl));
        StartCoroutine(_BlinkCanvasInfo());
    }

    IEnumerator _BlinkCanvasInfo()
    {
        bool active = true;
        while (true)
        {
            yield return new WaitForSeconds(0.4f);
            if (canvasInfo == null)
                break;
            active = !active;
            canvasInfo.SetActive(active);
        }
    }

    bool _JoystickReleased(Controller ctrl)
    {
        if (!ctrl.isReady)
            return true;
        var pos = ctrl.touchpadPosition;
        return pos.sqrMagnitude < 0.4f * 0.4f || Mathf.Abs(pos.x) < 0.14f;
    }

    IEnumerator _FollowJoystick(Controller ctrl)
    {
        while (!_JoystickReleased(ctrl))
            yield return null;

        while (true)
        {
            yield return null;

            if (!ctrl.isReady)
                continue;

            Vector2 pos = ctrl.touchpadPosition;
            if (pos.sqrMagnitude < 0.6f * 0.6f || Mathf.Abs(pos.x) < 0.3f)
                continue;

            /* moving the joystick! */
            if (canvasInfo != null)
            {
                Destroy((GameObject)canvasInfo);
                canvasInfo = null;
            }
            if (stop_instruction != null)
                stop_instruction[0] = true;
            stop_instruction = new bool[1];
            StartCoroutine(_FadeOutPadsAndHeadset(stop_instruction));

            float dir = Mathf.Sign(pos.x);
            current_angle += dir * 120f;
            if (current_angle >= 360f)
                current_angle -= 360f;
            else if (current_angle < 0f)
                current_angle += 360f;

            transform.rotation = Quaternion.Euler(0, -current_angle, 0);
            PongPadBuilder.instance.UpdateTrackingSpacePosition();
            PongPad.RestartFollowingAll();

            Baroque.FadeToColor(new Color(0.5f, 0.5f, 0.5f), 0.05f);
            yield return new WaitForSeconds(0.05f);
            Baroque.FadeToColor(Color.clear, 0.05f);

            /* wait until we release the joystick */
            while (!_JoystickReleased(ctrl))
                yield return null;
        }
    }

    IEnumerator _FadeOutPadsAndHeadset(bool[] stop_instruction)
    {
        var mat = new Material(PongPadBuilder.instance.padsAndHeadsetTransparencyMaterial);

        void FixMat(Transform toplevel)
        {
            foreach (var rend in toplevel.GetComponentsInChildren<MeshRenderer>())
                rend.sharedMaterial = mat;
        }

        var headset = Instantiate(PongPadBuilder.instance.headsetPrefab);
        var head_tr = Baroque.GetHeadTransform();
        headset.SetPositionAndRotation(head_tr.position, head_tr.rotation);
        FixMat(headset);

        foreach (var ctrl in Baroque.GetControllers())
        {
            var pad = ctrl.GetComponentInChildren<PongPad>();
            if (pad != null)
            {
                pad = Instantiate(pad, headset, worldPositionStays: true);
                Destroy((PongPad)pad.GetComponent<PongPad>());
                FixMat(pad.transform);
            }
        }

        while (!stop_instruction[0])
        {
            yield return null;

            var col = mat.color;
            col.a -= Time.deltaTime * 0.75f;
            if (col.a <= 0f)
                break;
            mat.color = col;
        }

        Destroy((GameObject)headset.gameObject);
        Destroy(mat);
    }
}
