using System.Reflection;

using UnityEngine;

namespace muskit.FlatScreen2
{
    public class FlatScreen2Plugin : VTOLMOD
    {
        private GameObject monoBehaviour;

        protected static FlatScreen2Plugin instance;

        public static void Write(string msg)
        {
            instance.Log(msg);
        }

        public FlatScreen2Plugin() : base()
        {
            instance = this;
        }

        // This method is run once, when the Mod Loader is done initialising this game object
        public override void ModLoaded()
        {
            EnableFlatScreen();
            base.ModLoaded();
        }

        public void EnableFlatScreen()
        {
            if (monoBehaviour != null)
                return;
            Log("Creating FlatScreen2MonoBehaviour");
            monoBehaviour = new GameObject();
            monoBehaviour.AddComponent<FlatScreen2MonoBehaviour>();
            GameObject.DontDestroyOnLoad(monoBehaviour);
        }

        private void DisableFlatScreen()
        {
            if (monoBehaviour == null)
                return;
            Log("Destroying FlatScreen2MonoBehaviour");
            GameObject.Destroy(monoBehaviour);
        }
    }
}