using System.IO;
using System.Reflection;
using ModLoader.Framework;
using ModLoader.Framework.Attributes;
using UnityEngine;
using VTOLAPI;

namespace Triquetra.FlatScreen2
{
    [ItemId("danku-flatscreen2")]
    public class Plugin : VtolMod
    {
        public static Plugin instance;
        public static bool IsEditor;

        public GameObject mbInstance;
        public Preferences pref;

        public static Texture2D cursorTex;

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

            VTAPI.SceneLoaded += SceneLoaded;
            
            string modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (modDirectory != null)
            {
                string cursorPath = Path.Combine(modDirectory, "cursorTex.png");
                if (File.Exists(cursorPath))
                {
                    Debug.Log($"Loading cursor texture from {cursorPath}");
                    cursorTex = new Texture2D(2, 2);
                    byte[] textureData = File.ReadAllBytes(cursorPath);
                    cursorTex.LoadImage(textureData);
                }
                else
                {
                    Debug.Log($"Couldn't load cursor texture from {cursorPath}");
                }
            }
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
            VTAPI.SceneLoaded -= SceneLoaded;
            
            Destroy(mbInstance);
        }

        private void SceneLoaded(VTScenes scene)
        {
            switch (scene)
            {
                case VTScenes.VTEditMenu:
                case VTScenes.VTMapEditMenu:
                    IsEditor = true;
                    break;
                case VTScenes.ReadyRoom:
                case VTScenes.SamplerScene:
                case VTScenes.LaunchSplashScene:
                    IsEditor = false;
                    break;
            }
        }
    }
}