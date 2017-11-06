using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Schema;
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

        private static bool Implements<T>(Type tp) where T : class
        {
            return tp.GetInterfaces().Any((t) => { return t == typeof(IXmlSerializable); });
        }

        // Reads all properties (including Matrix/Vector)
        public static void ReadXmlAllProperties(XmlReader reader, object obj)
        {
            reader.MoveToContent();

            reader.ReadStartElement();
            while(reader.NodeType != XmlNodeType.EndElement)
            {
                string nodeName = reader.Name;

                var propertyInfo = obj.GetType().GetProperty(nodeName);
                if(propertyInfo != null)
                {
                    if(propertyInfo.PropertyType == typeof(Matrix<double>))
                    {
                        MatrixXmlSerializer serializer = new MatrixXmlSerializer();
                        serializer.ReadXml(reader);
                        propertyInfo.SetValue(obj, serializer.Mat);
                    }
                    else if(propertyInfo.PropertyType == typeof(Vector<double>))
                    {
                        VectorXmlSerializer serializer = new VectorXmlSerializer();
                        serializer.ReadXml(reader);
                        propertyInfo.SetValue(obj, serializer.Vec);
                    }
                    else if(Implements<IXmlSerializable>(propertyInfo.PropertyType))
                    {
                        IXmlSerializable serializer = propertyInfo.GetValue(obj) as IXmlSerializable;
                        serializer.ReadXml(reader);
                    }
                    else
                    {
                        object val = reader.ReadElementContentAs(propertyInfo.PropertyType, null);
                        propertyInfo.SetValue(obj, val);
                    }
                }
                else
                    reader.Read();
            }
        }

        public static void WriteXmlProperty(XmlWriter writer, object obj, System.Reflection.PropertyInfo propertyInfo)
        {
            if(propertyInfo.PropertyType == typeof(Matrix<double>))
            {
                writer.WriteStartElement(propertyInfo.Name);
                new MatrixXmlSerializer(propertyInfo.GetValue(obj) as Matrix<double>).WriteXml(writer);
                writer.WriteEndElement();
            }
            else if(propertyInfo.PropertyType == typeof(Vector<double>))
            {
                writer.WriteStartElement(propertyInfo.Name);
                new VectorXmlSerializer(propertyInfo.GetValue(obj) as Vector<double>).WriteXml(writer);
                writer.WriteEndElement();
            }
            else if(Implements<IXmlSerializable>(propertyInfo.PropertyType))
            {
                writer.WriteStartElement(propertyInfo.Name);
                IXmlSerializable serializable = propertyInfo.GetValue(obj) as IXmlSerializable;
                serializable.WriteXml(writer);
                writer.WriteEndElement();
            }
            else
            {
                writer.WriteElementString(propertyInfo.Name, propertyInfo.GetValue(obj).ToString());
            }
        }

        // Writes all properties (including Matrix/Vector) not marked as XmlIgnore
        public static void WriteXmlNonIgnoredProperties(XmlWriter writer, object obj)
        {
            foreach(var prop in obj.GetType().GetProperties())
            {
                bool isIgnored = false;
                foreach(var att in prop.GetCustomAttributesData())
                {
                    if(att.AttributeType == typeof(XmlIgnoreAttribute))
                    {
                        isIgnored = true;
                        break;
                    }
                }
                if(isIgnored) { continue; }

                WriteXmlProperty(writer, obj, prop);
            }
        }
    }


    public class MatrixXmlSerializer : IXmlSerializable
    {
        public Matrix<double> Mat { get; set; }

        public MatrixXmlSerializer()
        {
            Mat = null;
        }

        public MatrixXmlSerializer(Matrix<double> matrix)
        {
            Mat = matrix;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent(); // Should move to begining of <Matrix>
            int rows = int.Parse(reader.GetAttribute("rows"));
            int cols = int.Parse(reader.GetAttribute("columns"));

            Mat = new DenseMatrix(rows, cols);
            reader.ReadStartElement(); // Moves to first <Row>

            for(int row = 0; row < Mat.RowCount; ++row)
            {
                string rowString = reader.ReadElementContentAsString(); // Reads from <Row> and moves to next one

                string[] nums = rowString.Split('|');
                for(int num = 0; num < cols; ++num)
                {
                    double val = double.Parse(nums[num]);
                    Mat.At(row, num, val);
                }
            }
            reader.ReadEndElement(); // Should read </Matrix>
        }

        public void WriteXml(XmlWriter writer)
        {
            // writer is on 'Matrix' node
            // write size attributes first
            writer.WriteAttributeString("rows", Mat.RowCount.ToString());
            writer.WriteAttributeString("columns", Mat.ColumnCount.ToString());

            // For each row write 'Row' element with row contents
            for(int row = 0; row < Mat.RowCount; ++row)
            {
                StringBuilder nums = new StringBuilder();
                for(int col = 0; col < Mat.ColumnCount; ++col)
                {
                    double val = Mat.At(row, col);
                    nums.Append(val.ToString("F5"));
                    nums.Append('|');
                }
                nums.Remove(nums.Length - 1, 1);

                writer.WriteElementString("Row", nums.ToString());
            }
        }
    }

    public class VectorXmlSerializer : IXmlSerializable
    {
        public Vector<double> Vec { get; set; }

        public VectorXmlSerializer()
        {
            Vec = null;
        }

        public VectorXmlSerializer(Vector<double> v)
        {
            Vec = v;
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            reader.MoveToContent(); // Should move to begining of <Matrix>
            int size = int.Parse(reader.GetAttribute("size"));

            Vec = new DenseVector(size);
            reader.ReadStartElement(); // Moves to first <Row>

            string content = reader.ReadElementContentAsString(); // Reads from <Row> and moves to next one

            string[] nums = content.Split('|');
            for(int num = 0; num < size; ++num)
            {
                double val = double.Parse(nums[num]);
                Vec.At(num, val);
            }

            reader.ReadEndElement(); // Should read </Matrix>
        }

        public void WriteXml(XmlWriter writer)
        {
            // writer is on 'Matrix' node
            // write size attributes first
            writer.WriteAttributeString("size", Vec.Count.ToString());

            // For each row write 'Row' element with row contents
            StringBuilder nums = new StringBuilder();
            for(int n = 0; n < Vec.Count; ++n)
            {
                double val = Vec.At(n);
                nums.Append(val.ToString("F5"));
                nums.Append('|');
            }
            nums.Remove(nums.Length - 1, 1);

            writer.WriteElementString("Value", nums.ToString());
        }
    }
}
