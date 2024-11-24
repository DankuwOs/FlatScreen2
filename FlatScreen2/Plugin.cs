using System.IO;
using System.Reflection;
using ModLoader.Framework;
using ModLoader.Framework.Attributes;
using UnityEngine;

namespace Triquetra.FlatScreen2
{
    [ItemId("danku-flatscreen2")]
    public class Plugin : VtolMod
    {
        protected static Plugin instance;

        public GameObject mbInstance;
        public Preferences pref;

        /// <summary>
        /// Print to stdout with the mod name prepended.
        /// </summary>
        /// <param name="msg"></param>
        public static void Write(object msg)
        {
            Debug.Log(msg);
        }

        // This method is run once, when the Mod Loader is done initialising this game object
        public void Awake()
        {
            if (instance != null)
            {
                Write("WARNING: Tried to load another mod instance when one already exists! Destroying duplicate self.");
                Destroy(this);
                return;
            }

            instance = this;

            InitPreferences();
            InitMonoBehaviour();
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
            Debug.Log("Creating FlatScreen2MonoBehaviour");
            mbInstance = new GameObject();
            mbInstance.AddComponent<FlatScreen2MonoBehaviour>();
            GameObject.DontDestroyOnLoad(mbInstance);
        }

        public override void UnLoad()
        {
            Destroy(mbInstance);
        }
    }
}