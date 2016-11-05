using System;
using System.Xml;

namespace CamCore
{
    public class DisparityMap : ICloneable
    {
        public Disparity[,] Disparities;
        public int RowCount { get; private set; }
        public int ColumnCount { get; private set; }

        public DisparityMap(int rows, int cols)
        {
            Disparities = new CamCore.Disparity[rows, cols];
            RowCount = rows;
            ColumnCount = cols;
        }

        public Disparity this[int y, int x]
        {
            get
            {
                return Disparities[y, x];
            }
        }

        public void Set(int y, int x, Disparity d)
        {
            Disparities[y, x] = d;
        }

        public Disparity Get(int y, int x)
        {
            return Disparities[y, x];
        }

        public object Clone()
        {
            var cloned = new DisparityMap(RowCount, ColumnCount);
            for(int r = 0; r < RowCount; ++r)
            {
                for(int c = 0; c < ColumnCount; ++c)
                {
                    cloned.Set(r, c, (Disparity)Disparities[r, c].Clone());
                }
            }
            return cloned;
        }

        //<DisparityMap rows = "1" cols="2">
        //  <Row>
        //	    <Disparity dx = "1" dy="0" cost="0.04" confidence="1.0" flags="Vaild"/>
        //	    <Disparity dx = "0" dy="0" cost="1.00" confidence="0.0" flags="Invalid|Occluded"/>
        //  </Row>
        //</DisparityMap>

        public XmlNode CreateMapNode(XmlDocument xmlDoc)
        {
            XmlNode mapNode = xmlDoc.CreateElement("DisparityMap");

            int rows = Disparities.GetLength(0);
            int cols = Disparities.GetLength(1);
            for(int r = 0; r < rows; ++r)
            {
                XmlNode rowNode = xmlDoc.CreateElement("Row");
                for(int c = 0; c < cols; ++c)
                {
                    XmlNode dispNode = Disparities[r, c].CreateDisparityNode(xmlDoc);
                    rowNode.AppendChild(dispNode);
                }
                mapNode.AppendChild(rowNode);
            }

            XmlAttribute rowsAtt = xmlDoc.CreateAttribute("rows");
            rowsAtt.Value = rows.ToString();
            XmlAttribute colsAtt = xmlDoc.CreateAttribute("cols");
            colsAtt.Value = cols.ToString();
            mapNode.Attributes.Append(rowsAtt);
            mapNode.Attributes.Append(colsAtt);

            return mapNode;
        }

        public static DisparityMap CreateFromNode(XmlNode mapNode)
        {
            int rows = int.Parse(mapNode.Attributes["rows"].Value);
            int cols = int.Parse(mapNode.Attributes["cols"].Value);

            DisparityMap map = new DisparityMap(rows, cols);
            XmlNode rowNode = mapNode.FirstChildWithName("Row");
            for(int r = 0; r < rows; ++r)
            {
                XmlNode dispNode = rowNode.FirstChildWithName("Disparity");
                for(int c = 0; c < cols; ++c)
                {
                    Disparity disp = Disparity.CreateFromNode(dispNode);
                    map.Disparities[r, c] = disp;
                    dispNode = dispNode.NextSibling;
                }
                rowNode = rowNode.NextSibling;
            }

            return map;
        }
    }
}
