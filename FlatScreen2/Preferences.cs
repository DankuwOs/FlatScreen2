using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Triquetra.FlatScreen2
{
    public class Preferences
    {
        public static Preferences instance { get; private set; }

        public bool zoomReqCtrlRMB = false;
        public bool limitXRot = true;
        public bool limitYRot = true;
        public int mouseSensitivity = 2;

        public bool useSphere = true;
        public float sphereSize = 0.015f;

        [XmlIgnore]
        public string filePath;

        public Preferences()
        {
            instance = this;
            filePath = PilotSaveManager.saveDataPath + "/flatscreen2.xml";
        }

        public void Save()
        {
            Plugin.Write("Saving preferences...");
            var serializer = new XmlSerializer(typeof(Preferences));
            using (TextWriter writer = new StreamWriter(filePath))
            {
                serializer.Serialize(writer, this);
            }
            Plugin.Write("Preferences saved!");
        }

        public void Load()
        {
            if (File.Exists(filePath))
            {
                Plugin.Write("Loading preferences...");
                XmlSerializer serializer = new XmlSerializer(typeof(Preferences));
                using (Stream reader = new FileStream(filePath, FileMode.Open))
                {
                    var load = (Preferences)serializer.Deserialize(reader);

                    // add more settings here as needed
                    zoomReqCtrlRMB = load.zoomReqCtrlRMB;
                    limitXRot = load.limitXRot;
                    limitYRot = load.limitYRot;
                    mouseSensitivity = load.mouseSensitivity;

                    useSphere = load.useSphere;
                }
                Plugin.Write("Preferences loaded!");
            }
            else
            {
                Plugin.Write("Could not find preferences file.");
                Save();
            }
        }

        private void OnDestroy()
        {
            Save();
        }
    }
}

