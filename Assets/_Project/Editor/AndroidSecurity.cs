#if UNITY_ANDROID
using System.IO;
using System.Xml;
using UnityEditor.Android;

namespace ZeroGCargo.Editor
{
    /// <summary>Hardens the generated manifest without replacing Unity's required launch activity.</summary>
    public sealed class AndroidSecurity : IPostGenerateGradleAndroidProject
    {
        /// <summary>Runs after Unity's default manifest generation.</summary>
        public int callbackOrder => 100;
        /// <summary>Disables application backups and cleartext networking for the offline game.</summary>
        public void OnPostGenerateGradleAndroidProject(string path)
        {
            string file = Path.Combine(path,"src/main/AndroidManifest.xml");
            if (!File.Exists(file)) throw new FileNotFoundException("Generated Android manifest not found",file);
            var document = new XmlDocument(); document.Load(file);
            XmlElement app = document.SelectSingleNode("/manifest/application") as XmlElement;
            if (app == null) throw new System.InvalidOperationException("Generated manifest has no application element");
            const string android = "http://schemas.android.com/apk/res/android";
            app.SetAttribute("allowBackup",android,"false");
            app.SetAttribute("usesCleartextTraffic",android,"false");
            document.Save(file);
        }
    }
}
#endif
