using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BaroqueUI;
using System.Linq;

public class PongPadBuilder : MonoBehaviour
{
    public Material cellHitMaterial, unstoppableBallMaterial;
    public ParticleSystem hitPS, starPS;
    public GameObject preload;
    public PongPad padObjectPrefab;
    public Ball shotBallPrefab;
    public MeshRenderer haloPrefab;
    public Points canvasPointsPrefab;
    public GameObject[] levelPrefabs;
    public AudioClip backgroundMusic;

    public static PongPadBuilder instance { get; private set; }
    public static bool paused { get; private set; }

    Cell track_cell;
    float? level_end_time;
    GameObject levelInstance;
    int current_level = 6;

    private void Awake()
    {
        instance = this;
        Ball.start_positions = new List<Vector3>();
        foreach (var ball in FindObjectsOfType<Ball>())
            Ball.start_positions.Add(ball.transform.position);
    }

    private void Start()
    {
        level_end_time = Time.time;

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

    private void OnApplicationFocus(bool focus)
    {
        paused = !focus;
        Time.timeScale = paused ? 0f : 1f;
    }

    IEnumerator TrackPosition()
    {
        yield return new WaitForEndOfFrame();
        Destroy(preload);

        while (true)
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
            yield return new WaitForSecondsRealtime(0.45f);
        }
    }

    private void Ht_onControllersUpdate(Controller[] controllers)
    {
        if (paused)
            return;

        if (track_cell == null)
        {
            track_cell = FindObjectOfType<Cell>();
            if (track_cell == null)
            {
                if (level_end_time == null)
                {
                    level_end_time = Time.time + 1.2f;
                    Bonus.RemoveAllBonuses();
                }

                if (Time.time >= level_end_time.Value)
                {
                    level_end_time = null;
                    if (levelInstance != null)
                        Destroy(levelInstance);

                    if (current_level >= levelPrefabs.Length)
                        current_level = 0;
                    levelInstance = Instantiate(levelPrefabs[current_level++]);
                    Bonus.RemoveAllBonuses();
                }
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
