using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Harmony;

namespace Triquetra.FlatScreen2
{
    [HarmonyPatch(typeof(FlightSceneManager), "InstantScenarioRestartRoutine")]
    internal class FlightSceneManagerRestart
    {
        static void Postfix()
        {
            FlatScreen2MonoBehaviour.Instance?.Reclean();
        }
    }
}
