using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace CamCore
{
    public static partial class XmlExtensions
    {
        public static XmlNode FirstChildWithName(this XmlNode node, string childName, bool caseSensitive = true)
        {
            if(node.HasChildNodes)
            {
                var childNodes = node.ChildNodes;
                foreach(XmlNode childNode in childNodes)
                {
                    if(childNode.Name.Equals(childName, caseSensitive ?
                        StringComparison.Ordinal :
                        StringComparison.OrdinalIgnoreCase))
                        return childNode;
                }
            }
            return null;
        }

        public static Matrix<double> MatrixFromNode(XmlNode matNode)
        {
            int rows = int.Parse(matNode.Attributes["rows"].Value);
            int cols = int.Parse(matNode.Attributes["columns"].Value);
            DenseMatrix matrix = new DenseMatrix(rows, cols);

            XmlNode rowNode = matNode.FirstChild;
            for(int row = 0; row < rows; ++row)
            {
                string rowText = rowNode.InnerText;

                string[] nums = rowText.Split('|');
                for(int num = 0; num < cols; ++num)
                {
                    double val = double.Parse(nums[num]);
                    matrix[row, num] = val;
                }

                rowNode = rowNode.NextSibling;
            }

            return matrix;
        }

        public static XmlNode CreateMatrixNode(XmlDocument xmlDoc, Matrix<double> matrix, string nodeName = "Matrix")
        {
            XmlNode matNode = xmlDoc.CreateElement(nodeName);

            XmlAttribute attRows = xmlDoc.CreateAttribute("rows");
            attRows.Value = matrix.RowCount.ToString();

            XmlAttribute attCols = xmlDoc.CreateAttribute("columns");
            attCols.Value = matrix.ColumnCount.ToString();

            matNode.Attributes.Append(attRows);
            matNode.Attributes.Append(attCols);

            for(int row = 0; row < matrix.RowCount; ++row)
            {
                StringBuilder nums = new StringBuilder();
                for(int col = 0; col < matrix.ColumnCount; ++col)
                {
                    double val = matrix[row, col];
                    nums.Append(val.ToString("F5"));
                    nums.Append('|');
                }
                nums.Remove(nums.Length - 1, 1);

                XmlNode rowNode = xmlDoc.CreateElement("Row");
                rowNode.InnerText = nums.ToString();
                matNode.AppendChild(rowNode);
            }

            return matNode;
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
    }
}
