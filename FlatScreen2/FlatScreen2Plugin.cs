using System;
using System.IO;
using System.Reflection;

using Harmony;
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

        public static string AssemblyDirectory // TODO: get mod directory for custom cursor textures
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        // This method is run once, when the Mod Loader is done initialising this game object
        public override void ModLoaded()
        {
            if (instance != null)
            {
                Write("WARNING: Tried to load another mod instance when one already exists! Destroying duplicate self.");
                Destroy(this);
                return;
            }

            instance = this;

            Write($"Mod assembly path: {AssemblyDirectory}");
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("muskit.FlatScreen2");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
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