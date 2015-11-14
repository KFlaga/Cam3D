using DirectShowLib;
using System;

namespace CamCapture
{
    public class CameraConfig
    {
        public Int32 Width { get; set; }
        public Int32 Height { get; set; }
        public Int32 BitRate { get; set; }

        // Config is created based on VideoInfoHeader and pin type obtained from
        // camera filter videoinfos enumeration
        public CameraConfig(VideoInfoHeader vinfo)
        {
            Width = vinfo.BmiHeader.Width;
            Height = vinfo.BmiHeader.Height;
            BitRate = vinfo.BitRate;
        }

        public override string ToString()
        {
            return "W: " + Width.ToString() + "  H: " + Height.ToString() +
               "  BitRate: " + (BitRate / 8000) + " KB/s";
        }

        public bool Equals(CameraConfig config)
        {
            if (config.Width == this.Width &&
                config.Height == this.Height &&
                config.BitRate == this.BitRate)
                return true;
            return false;
        }

        public bool Equals(VideoInfoHeader videoInfo)
        {
            if (videoInfo.BmiHeader.Width == this.Width &&
                videoInfo.BmiHeader.Height == this.Height &&
                videoInfo.BitRate == this.BitRate)
                return true;
            return false;
        }
    }

}
