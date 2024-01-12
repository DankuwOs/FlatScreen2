using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEngine.SceneManagement;

using VTOLVR.Multiplayer;

namespace Triquetra.FlatScreen2
{
    public static class Util
    {
        public static bool IsFlyingScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 7 || buildIndex == 11;
        }

        public static bool IsReadyRoomScene()
        {
            int buildIndex = SceneManager.GetActiveScene().buildIndex;
            return buildIndex == 2;
        }

        public static Camera GetEyeCamera()
        {
            if (VRHead.instance != null)
                return VRHead.instance.cam;

            IEnumerable<Camera> cameras = GameObject.FindObjectsOfType<Camera>(false)
                .Where(c => c.name == "Camera (eye)" && c.isActiveAndEnabled)
                .OrderByDescending(c => c.depth);

            if (cameras.Any(x => x.gameObject?.layer == LayerMask.NameToLayer("MPBriefing")))
            {
                if (VTOLMPSceneManager.instance.localPlayer.chosenTeam)
                {
                    GameObject localAvatarObject = typeof(VTOLMPSceneManager)
                        .GetField("localAvatarObj", BindingFlags.Instance | BindingFlags.NonPublic)?
                        .GetValue(VTOLMPSceneManager.instance) as GameObject;
                    if (localAvatarObject != null)
                    {
                        Camera localAvatarCam = localAvatarObject?.GetComponentInChildren<Camera>(false);
                        if (localAvatarCam != null)
                        {
                            return localAvatarCam;
                        }
                    }
                }
            }
            return cameras.FirstOrDefault();
        }

        public static IEnumerable<Camera> GetSpectatorCameras()
        {
            return GameObject.FindObjectsOfType<Camera>(true).Where(c => c.name == "FlybyCam" || c.name == "flybyHMCScam");
        }
    }
}
