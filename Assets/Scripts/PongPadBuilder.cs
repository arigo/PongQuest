using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System.Linq;

public class PongPadBuilder : MonoBehaviour
{
    public Material cellHitMaterial, unstoppableBallMaterial;
    public ParticleSystem hitPS, starPS;
    public PongPad padObjectPrefab;
    public Ball shotBallPrefab;
    public GameObject levelPrefab;
    public AudioClip backgroundMusic;

    Cell track_cell;
    GameObject levelInstance;

    private void Awake()
    {
        Ball.start_positions = new List<Vector3>();
        foreach (var ball in FindObjectsOfType<Ball>())
            Ball.start_positions.Add(ball.transform.position);
    }

    private void Start()
    {
        var ht = Controller.GlobalTracker(this);
        ht.onControllersUpdate += Ht_onControllersUpdate;
        StartCoroutine(TrackPosition());

        if (backgroundMusic != null)
        {
            var go = new GameObject("music");
            var asrc = go.AddComponent<AudioSource>();
            asrc.clip = backgroundMusic;
            asrc.loop = true;
            asrc.priority = 0;
            asrc.volume = 0.65f;
            //DontDestroyOnLoad(go);
            //ChangedMusicVolume();
            asrc.Play();
        }
    }

    IEnumerator TrackPosition()
    {
        while (true)
        {
            Vector2 size;
            if (Baroque.TryGetPlayAreaSize(out size))
            {
                var boundary = OVRManager.boundary;
                Vector3[] geometry = boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
                if (geometry.Length == 4 && OVRManager.instance != null)
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
            }
            yield return new WaitForSeconds(0.95f);
        }
    }

    private void Ht_onControllersUpdate(Controller[] controllers)
    {
        if (track_cell == null)
        {
            track_cell = FindObjectOfType<Cell>();
            if (track_cell == null)
            {
                if (levelInstance != null)
                    Destroy(levelInstance);
                levelInstance = Instantiate(levelPrefab);
            }
        }

        Physics.SyncTransforms();

        PongPad.UpdateAllBalls();

        foreach (var ctrl in controllers)
            if (ctrl.isActiveAndEnabled)
            {
                var pad = ctrl.GetComponentInChildren<PongPad>();
                if (pad == null)
                {
                    pad = Instantiate(padObjectPrefab, ctrl.transform);
                    pad.StartFollowing(ctrl);
                }
                pad.FollowController();
            }
    }
}
