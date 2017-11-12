using System.Collections.Generic;
using CamCore;

namespace CamAlgorithms.ImageMatching
{
    public abstract class ImageMatchingAlgorithm : IParameterizable
    {
        public DisparityMap MapLeft { get; set; }
        public DisparityMap MapRight { get; set; }

        public IImage ImageLeft { get; set; }
        public IImage ImageRight { get; set; }

        public bool Rectified { get; set; } = true;

        List<IAlgorithmParameter> _params = new List<IAlgorithmParameter>();
        public List<IAlgorithmParameter> Parameters
        {
            get { return _params; }
        }

        public abstract string Name { get; }
        public abstract void MatchImages();

        public abstract void Terminate();
        public virtual string GetStatus()
        {
            return "";
        }
        public virtual string GetProgress()
        {
            return "";
        }

        protected void ConvertImagesToGray()
        {
            if(ImageLeft.ChannelsCount == 3)
            {
                GrayScaleImage imgGray = new GrayScaleImage();
                imgGray.SetMatrix(ImageLeft.GetMatrix(), 0);

                if(ImageLeft is MaskedImage)
                {
                    (ImageLeft as MaskedImage).Image = imgGray;
                }
                else
                {
                    ImageLeft = imgGray;
                }
            }

            if(ImageRight.ChannelsCount == 3)
            {
                GrayScaleImage imgGray = new GrayScaleImage();
                imgGray.SetMatrix(ImageRight.GetMatrix(), 0);

                if(ImageRight is MaskedImage)
                {
                    (ImageRight as MaskedImage).Image = imgGray;
                }
                else
                {
                    ImageRight = imgGray;
                }
            }
        }

        public virtual void InitParameters()
        {
            _params = new List<IAlgorithmParameter>();
        }

        public virtual void UpdateParameters()
        {

        }
    }
}
