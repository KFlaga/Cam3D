using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Text;
using System.Xml;

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
    }
}

