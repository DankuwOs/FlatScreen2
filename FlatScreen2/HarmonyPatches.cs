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
                FlatScreen2Plugin.Write("FlatScreen is enabled! Enabling XR to skip scene loader's VR check...");
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

    [HarmonyPatch(typeof(EjectionSeat), nameof(EjectionSeat.Start))]
    internal class ResetStateOnPlayerSpawn
    {
        static void Postfix()
        {
            if (FlatScreen2MonoBehaviour.instance.flatScreenEnabled)
            {
                FlatScreen2Plugin.Write("Player spawn! Resetting state...");
                FlatScreen2MonoBehaviour.instance?.ResetState();
            }
        }
    }

    [HarmonyPatch(typeof(LoadingSceneHelmet), nameof(LoadingSceneHelmet.Start))]
    internal class LoadingHelmetEquipper
    {
        static void Postfix(LoadingSceneHelmet __instance)
        {
            if (FlatScreen2MonoBehaviour.instance.flatScreenEnabled)
            {
                FlatScreen2Plugin.Write("Teleporting loading helmet to equip...");

                var traverse = new Traverse(__instance);
                traverse.Field("grabbed").SetValue(true);
                __instance.transform.position = __instance.headHelmet.transform.position;
            }
        }
    }
}
