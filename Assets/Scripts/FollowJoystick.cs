using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;


public class FollowJoystick : MonoBehaviour
{
    public GameObject canvasInfo;
    public GameObject warpWalls;
    public Transform ballStartTr;

    float current_angle;
    bool[] stop_instruction;
    float wrap_walls_timeout;
    int wrap_discontinuity;

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

            if (PongPadBuilder.paused)
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

    public void StartWarpWallsBonus()
    {
        bool was_inactive = wrap_walls_timeout == 0f;
        wrap_walls_timeout = Time.time + 60f;
        if (was_inactive)
            StartCoroutine(_AnimateWarpWalls());
    }

    IEnumerator _AnimateWarpWalls()
    {
        const float ALPHA_MAX = 0.4f;
        const float STANDARD_FORMUPARAM = 0.35295f;

        warpWalls.SetActive(true);

        var pb = new MaterialPropertyBlock();
        int name_id = Shader.PropertyToID("_Parameters");
        var walls = warpWalls.GetComponentsInChildren<MeshRenderer>();
        float alpha = 0f;
        float formu_delta = 0f;

        while (true)
        {
            float target_alpha;
            float remaining = wrap_walls_timeout - Time.time;
            if (remaining < 0f)
                break;
            if (remaining >= 2.8f || (remaining % 0.8f) >= 0.4f)
                target_alpha = ALPHA_MAX;
            else
                target_alpha = 0f;

            alpha = Mathf.MoveTowards(alpha, target_alpha, 3.2f * Time.deltaTime);
            formu_delta *= Mathf.Exp(-Time.deltaTime);
            if (wrap_discontinuity != 0)
            {
                alpha = ALPHA_MAX * 2.0f;
                formu_delta = wrap_discontinuity * 0.1f;
                wrap_discontinuity = 0;
            }

            pb.SetVector(name_id, new Vector4(alpha, 1f, STANDARD_FORMUPARAM + formu_delta));
            walls[0].SetPropertyBlock(pb);
            pb.SetVector(name_id, new Vector4(alpha, -1f, STANDARD_FORMUPARAM - formu_delta));
            walls[1].SetPropertyBlock(pb);

            yield return null;
        }

        wrap_walls_timeout = 0f;
        warpWalls.SetActive(false);
    }

    public void WarpBall(Ball ball, ref Vector3 velocity)
    {
        var tr1 = warpWalls.transform.GetChild(0);
        var tr2 = warpWalls.transform.GetChild(1);
        var ball_pos = ball.transform.position;

        int direction = 1;
        if ((ball_pos - tr1.position).sqrMagnitude > (ball_pos - tr2.position).sqrMagnitude)
        {
            var tr_swap = tr1;
            tr1 = tr2;
            tr2 = tr_swap;
            direction = -1;
        }

        /* warping from tr1 to tr2 */
        var rel_velocity = tr1.InverseTransformVector(velocity);
        if (rel_velocity.z > 0.001f)
        {
            rel_velocity.z = -Mathf.Max(rel_velocity.z, 0.02f);
            rel_velocity.x = -rel_velocity.x;

            var rel_position = tr1.InverseTransformPoint(ball_pos);
            if (rel_position.z > 0f)
                rel_position.z = 0f;

            ball.transform.position = tr2.TransformPoint(rel_position);
            velocity = tr2.TransformVector(rel_velocity);

            wrap_discontinuity = direction;
        }
    }
}
