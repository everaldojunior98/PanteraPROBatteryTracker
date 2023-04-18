using System.IO;
using System.Web.Script.Serialization;

namespace PanteraPRO
{
    public class AppSettings<T> where T : new()
    {
        private const string DefaultFilename = "settings.json";

        public void Save(string fileName = DefaultFilename)
        {
            File.WriteAllText(fileName, new JavaScriptSerializer().Serialize(this));
        }

        public static void Save(T pSettings, string fileName = DefaultFilename)
        {
            File.WriteAllText(fileName, new JavaScriptSerializer().Serialize(pSettings));
        }

        public static T Load(string fileName = DefaultFilename)
        {
            var t = new T();
            if (File.Exists(fileName))
                t = new JavaScriptSerializer().Deserialize<T>(File.ReadAllText(fileName));
            return t;
        }
    }
}