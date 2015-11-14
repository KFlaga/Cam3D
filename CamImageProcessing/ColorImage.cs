using MathNet.Numerics.LinearAlgebra.Single;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CamImageProcessing
{
    public class ColorImage
    {
        public Matrix[] ImageMatrix { get; set; }
        public int SizeX { get { return ImageMatrix[0].ColumnCount; } }
        public int SizeY { get { return ImageMatrix[0].RowCount; } }

        public float this[int y, int x, int channel]
        {
            get
            {
                return (ImageMatrix[channel])[y, x];
            }
            set
            {
                (ImageMatrix[channel])[y, x] = value;
            }
        }
    }
}
