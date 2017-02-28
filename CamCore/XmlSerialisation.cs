using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CamCore
{
    public static class XmlSerialisation
    {
        public static T CreateFromFile<T>(Stream file)
        {
            TextReader reader = new StreamReader(file);
            return CreateObject<T>(reader);
        }

        public static T CreateFromFile<T>(string path)
        {
            TextReader reader = new StreamReader(path);
            return CreateObject<T>(reader);
        }

        public static T CreateFromNode<T>(XmlNode node)
        {
            TextReader reader = new StringReader(node.OuterXml);
            return CreateObject<T>(reader);
        }

        private static T CreateObject<T>(TextReader reader)
        {
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            object obj = deserializer.Deserialize(reader);
            T loadedObj = (T)obj;
            reader.Close();

            return loadedObj;
        }

        public static void SaveToFile<T>(T obj, Stream file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            TextWriter writer = new StreamWriter(file);
            serializer.Serialize(writer, obj);
            writer.Close();
        }

        public static void SaveToFile<T>(T obj, string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            TextWriter writer = new StreamWriter(path, false);
            serializer.Serialize(writer, obj);
            writer.Close();
        }
    }
}
