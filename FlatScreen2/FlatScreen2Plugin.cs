using System;
using System.IO;
using System.Reflection;

using Harmony;
using UnityEngine;

namespace Triquetra.FlatScreen2
{
    public class FlatScreen2Plugin : VTOLMOD
    {
        protected static FlatScreen2Plugin instance;

        public GameObject mbInstance;
        public Preferences pref;

        /// <summary>
        /// Print to stdout with the mod name prepended.
        /// </summary>
        /// <param name="msg"></param>
        public static void Write(string msg)
        {
            instance.Log(msg);
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

            Write($"Mod folder: {ModFolder}");
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("muskit.FlatScreen2");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            InitPreferences();
            InitMonoBehaviour();

            base.ModLoaded();
        }

        public void InitPreferences()
        {
            if (pref != null)
                return;

            pref = new Preferences();
            pref.Load();
        }

        public void InitMonoBehaviour()
        {
            if (mbInstance != null)
                return;
            Log("Creating FlatScreen2MonoBehaviour");
            mbInstance = new GameObject();
            mbInstance.AddComponent<FlatScreen2MonoBehaviour>();
            GameObject.DontDestroyOnLoad(mbInstance);
        }
    }
}