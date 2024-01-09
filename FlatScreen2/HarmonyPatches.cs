using Harmony;

using UnityEngine.XR;

namespace muskit.FlatScreen2
{
    [HarmonyPatch(typeof(XRLoaderSelector), nameof(XRLoaderSelector._StartXR))]
    internal class StartXRSkipper
    {
        static bool Prefix()
        {
            if (FlatScreen2MonoBehaviour.instance.flatScreenEnabled)
            {
                FlatScreen2Plugin.Write("FlatScreen is enabled! Skipping attempt to start XR...");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(
        typeof(LoadingSceneController.StandardToVRSwitcher),
        nameof(LoadingSceneController.StandardToVRSwitcher.SwitchRoutine)
    )]
    internal class LoadingSceneVRSkipper
    {
        static void Prefix()
        {
            if (FlatScreen2MonoBehaviour.instance.flatScreenEnabled)
            {
                FlatScreen2Plugin.Write("FlatScreen is enabled! Enabling XR for scene loader's VR check...");
                XRSettings.enabled = true;
            }
        }

        static void Postfix()
        {
            if (FlatScreen2MonoBehaviour.instance.flatScreenEnabled)
            {
                FlatScreen2Plugin.Write("Done loading scene, disabling XR.");
                XRSettings.enabled = false;
            }
        }
    }
}
