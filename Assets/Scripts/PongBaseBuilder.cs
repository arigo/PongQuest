using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System.Linq;


public interface IPongPad
{
    void StartFollowing(Controller ctrl);
    void FollowController();
    void DestroyPad();
}


public abstract class PongBaseBuilder : MonoBehaviour
{
    public GameObject padObjectPrefab;
    public GameObject preloadGameObject;

    public static bool paused { get => paused_no_focus || paused_no_ctrl; }
    static bool paused_no_focus, paused_no_ctrl;

    protected virtual void Start()
    {
        Baroque.FadeToColor(Color.clear, 0.2f);
        SilenceNow();
        FadeInSounds(0.5f);

        var ht = Controller.GlobalTracker(this);
        ht.onControllersUpdate += Ht_onControllersUpdate;
        StartCoroutine(TrackPosition());
    }

    private void OnApplicationFocus(bool focus)
    {
#if !UNITY_EDITOR
        paused_no_focus = !focus;
        Time.timeScale = paused ? 0f : 1f;
#endif
    }

    bool _was_paused;

    protected virtual void Update()
    {
#if !UNITY_EDITOR
        bool any_ctrl = Baroque.GetControllers().Where(ctrl => ctrl.isReady).Any();
        if (any_ctrl == paused_no_ctrl)
        {
            paused_no_ctrl = !any_ctrl;
            Time.timeScale = paused ? 0f : 1f;
        }
#endif
        if (paused)
        {
            foreach (var ctrl in Baroque.GetControllers())
            {
                var pad = ctrl.GetComponentInChildren<IPongPad>();
                if (pad != null)
                    pad.DestroyPad();
            }
            SetPaused(true);
        }
        else if (_was_paused)
            SetPaused(false);

        _was_paused = paused;
    }

    protected abstract void SetPaused(bool paused);

    IEnumerator TrackPosition()
    {
        while (true)
        {
            var boundary = OVRManager.boundary;
            Vector3[] geometry = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
            if (geometry != null && geometry.Length == 4 && OVRManager.instance != null)
            {
                Transform tracking_space = OVRManager.instance.transform;

                /*string x(Vector3 v)
                {
                    return v.x + "," + v.z;
                }
                Debug.LogError("size: " + x(size) + "   geometry: " + string.Join(" ", geometry.Select(x)));*/

                /* assume that geometry returned a perfect rectangle, but with a random center and orientation. */
                Vector3 GlobalVec2(Vector3 local_v)
                {
                    /* the documentation for GetGeometry() implies we need a
                        * TransformPoint() to get global coordinates, but it seems we don't
                        */
                    Vector3 glob = local_v;
                    return new Vector3(glob.x, 0, glob.z);
                }
                Vector3 p0 = GlobalVec2(geometry[0]);
                Vector3 p1 = GlobalVec2(geometry[1]);
                Vector3 p2 = GlobalVec2(geometry[2]);
                Vector3 p3 = GlobalVec2(geometry[3]);
                //string s(Vector3 p) => p.x + ", " + p.z + " / ";
                //Debug.Log("Recentered! " + s(p0) + s(p1) + s(p2) + s(p3));

                float length = Mathf.Min(Vector3.Distance(p0, p1), Vector3.Distance(p2, p3));
                float width = Mathf.Min(Vector3.Distance(p1, p2), Vector3.Distance(p3, p0));
                if (width > length)
                {
                    Vector3 t1 = p0; p0 = p1; p1 = p2; p2 = p3; p3 = t1;
                }
                Vector3 center = (p0 + p1 + p2 + p3) * 0.25f;

                tracking_space.position = center;
                tracking_space.rotation = Quaternion.Inverse(Quaternion.LookRotation(
                    (p2 + p3) - (p1 + p0)));
            }

            if (preloadGameObject != null)
            {
                yield return new WaitForEndOfFrame();
                Destroy((GameObject)preloadGameObject);
                preloadGameObject = null;
            }

            yield return new WaitForSecondsRealtime(0.45f);
        }
    }

    protected abstract void FrameByFrameUpdate();

    private void Ht_onControllersUpdate(Controller[] controllers)
    {
        if (paused)
            return;

        FrameByFrameUpdate();

        foreach (var ctrl in controllers)
            if (ctrl.isReady)
            {
                var pad = ctrl.GetComponentInChildren<IPongPad>();
                if (pad == null)
                {
                    pad = Instantiate(padObjectPrefab, ctrl.transform).GetComponent<IPongPad>();
                    pad.StartFollowing(ctrl);
                }
                pad.FollowController();
            }
    }

    private void OnDestroy()
    {
        /* scene change */
        AudioListener.volume = 1;
        foreach (var ctrl in Baroque.GetControllers())
        {
            var pad = ctrl.GetComponentInChildren<IPongPad>();
            if (pad != null)
                pad.DestroyPad();
        }
    }

    float _volume = 1;
    Coroutine _volume_changer;

    void _StopVolumeChanger()
    {
        if (_volume_changer != null)
        {
            StopCoroutine(_volume_changer);
            _volume_changer = null;
        }
    }

    public void FadeOutSounds(float delay)
    {
        _StopVolumeChanger();
        StartCoroutine(_FadeOutSounds(delay));
    }

    public void FadeInSounds(float delay)
    {
        _StopVolumeChanger();
        StartCoroutine(_FadeInSounds(delay));
    }

    public void SilenceNow()
    {
        _StopVolumeChanger();
        _volume = 0;
        AudioListener.volume = 0;
    }

    IEnumerator _FadeOutSounds(float delay)
    {
        while (true)
        {
            _volume -= Time.unscaledDeltaTime / delay;
            if (_volume < 0f)
                _volume = 0f;
            AudioListener.volume = _volume;
            if (_volume == 0f)
                break;
            yield return null;
        }
    }

    IEnumerator _FadeInSounds(float delay)
    {
        while (true)
        {
            _volume += Time.unscaledDeltaTime / delay;
            if (_volume > 1f)
                _volume = 1f;
            AudioListener.volume = _volume;
            if (_volume == 1f)
                break;
            yield return null;
        }
    }
}
