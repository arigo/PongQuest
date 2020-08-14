using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace BaroqueUI
{
    public class OVRCameraRigBuilder : MonoBehaviour
    {
        /* This file contains the changes from the standard Oculus prefabs to the actual
         * settings that we need in VRSketch.
         */
        const int UI_layer = 29;   /* and the next one */

        public GameObject ovrCameraRigPrefab, localAvatarPrefab;
        //public UnityEngine.PostProcessing.PostProcessingProfile postProcessingProfile;


        public GameObject Build()
        {
            throw new System.NotImplementedException();
#if false
            GameObject ovrCameraRig = Instantiate(ovrCameraRigPrefab);
            ovrCameraRig.name = "OVRCameraRig";

            var mgr = ovrCameraRig.GetComponent<OVRManager>();
            mgr.trackingOriginType = OVRManager.TrackingOrigin.FloorLevel;

            var ts = ovrCameraRig.transform.Find("TrackingSpace");

            var cam = ts.Find("LeftEyeAnchor").GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 5000f;

            cam = ts.Find("RightEyeAnchor").GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 5000f;

            cam = ts.Find("CenterEyeAnchor").GetComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 5000f;
            cam.cullingMask &= ~(3 << UI_layer);
            //var pp = cam.gameObject.AddComponent<UnityEngine.PostProcessing.PostProcessingBehaviour>();
            //pp.profile = postProcessingProfile;

            var loc = Instantiate(localAvatarPrefab, ts);
            loc.name = "LocalAvatar";
            var avt = loc.GetComponent<OvrAvatar>();
            avt.StartWithControllers = true;

            return ovrCameraRig;
#endif
        }
    }
}
