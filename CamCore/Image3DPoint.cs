using System.Windows;

namespace CamCore
{
    public class Camera3DPoint
    {
        public Vector2 Cam1Img { get; set; } = new Vector2();
        public Vector2 Cam2Img { get; set; } = new Vector2();
        public Vector3 Real { get; set; } = new Vector3();
    }
}
