using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Windows;
using System.Reflection;

namespace CamCore
{
    public class ModuleLoader
    {
        public string ConfFilePath { get; set; }
        public List<Module> Modules { get; private set; }

        public bool LoadModules()
        {
            Modules = new List<Module>();
            FileStream file;
            try
            {
                file = new FileStream(ConfFilePath, FileMode.Open);
            }
            catch(FileNotFoundException e)
            {
                MessageBox.Show("Modules load failed - couldn't open config file", "Error");
                return false;
            }

            XmlDocument confDoc = new XmlDocument();
            confDoc.Load(file);

            XmlNodeList modulesList = confDoc.GetElementsByTagName("Module");
            List<ModuleInfo> modInfoList = new List<ModuleInfo>();
            foreach (XmlNode moduleNode in modulesList)
            {
                ModuleInfo modInfo = new ModuleInfo();
                try
                {
                    modInfo.Assembly = moduleNode["Assembly"].InnerText;
                    modInfo.Namespace = moduleNode["Namespace"].InnerText;
                    modInfo.ClassName = moduleNode["ClassName"].InnerText;
                    modInfo.ModuleName = moduleNode["ModuleName"].InnerText;
                    modInfoList.Add(modInfo);
                }
                catch(NullReferenceException e)
                {
                    // failed to load this module
                }
            }

            StringBuilder loadedMods = new StringBuilder("Loaded modules: ");
            foreach(ModuleInfo modInfo in modInfoList)
            {
                Module module = LoadModule(modInfo);
                if(module != null)
                {
                    Modules.Add(module);
                    loadedMods.Append(modInfo.ModuleName + ",");
                }
            }

            MessageBox.Show(loadedMods.ToString());
            file.Close();

            return true;
        }

        public Module LoadModule(ModuleInfo modInfo)
        {
            Assembly modAssembly = Assembly.LoadFrom(modInfo.Assembly);
            Module module = (Module)modAssembly.CreateInstance(modInfo.Namespace + "." + modInfo.ClassName);
            return module;
        }
    }
}
