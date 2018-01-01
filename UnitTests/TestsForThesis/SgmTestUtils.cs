using CamCore;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using System.Xml;

namespace CamUnitTest.TestsForThesis
{
    public enum ResampleLevel
    {
        x4,
        x2,
        None
    }

    public class SgmTestUtils
    {
        public static ResampleLevel resampleLevel = ResampleLevel.x4;

        public static string imagesDirectory { get; set; } = @"..\..\..\test_data\matching_images\";

        public static ImageType LoadImage<ImageType>(string path) where ImageType: IImage, new()
        {
            BitmapImage bitmap = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            ImageType image = new ImageType();
            image.FromBitmapSource(bitmap);
            return image;
        }

        public static void SaveImage(IImage image, string path)
        {
            using(Stream file = new FileStream(path, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image.ToBitmapSource()));
                encoder.Save(file);
            }
        }

        public static ColorImage LoadImage_PipesLeft()
        {
            return LoadImage<ColorImage>(imagesDirectory + "pipes\\im0.png");
        }

        public static ColorImage LoadImage_PipesRight()
        {
            return LoadImage<ColorImage>(imagesDirectory + "pipes\\im1.png");
        }

        public static DisparityImage LoadImage_PipesDisparity()
        {
            return LoadImage<DisparityImage>(imagesDirectory + "pipes\\left_map.png");
        }

        public static void SaveImage_PipesDisparity(DisparityImage image)
        {
            SaveImage(image, imagesDirectory + "pipes\\left_map1.png");
        }

        public static ColorImage LoadImage_MotorLeft()
        {
            return LoadImage<ColorImage>(imagesDirectory + "motor\\im0.png");
        }

        public static ColorImage LoadImage_MotorRight()
        {
            return LoadImage<ColorImage>(imagesDirectory + "motor\\im1.png");
        }

        public static DisparityImage LoadImage_MotorDisparity()
        {
            return LoadImage<DisparityImage>(imagesDirectory + "motor\\left_map.png");
        }

        public static ImageType CropImageCentrally<ImageType>(ImageType image, IntVector2 halfSize) where ImageType : IImage, new()
        {
            ImageType cropped = new ImageType();
            IntVector2 center = new IntVector2(image.ColumnCount / 2, image.RowCount / 2);
            IntVector2 topLeft = center - halfSize;

            for(int ch = 0; ch < image.ChannelsCount; ++ch)
            {
                Matrix<double> matrix = new DenseMatrix(halfSize.Y * 2 + 1, halfSize.X * 2 + 1);
                for(int c = 0; c < matrix.ColumnCount; ++c)
                {
                    for(int r = 0; r < matrix.RowCount; ++r)
                    {
                        matrix[r, c] = image[topLeft.Y + r, topLeft.X + c];
                    }
                }
                cropped.SetMatrix(matrix, ch);
            }

            return cropped;
        }
        
        public static ImageType ResampleImage<ImageType>(ImageType image) where ImageType : IImage, new()
        {
            if(resampleLevel == ResampleLevel.None)
            {
                return image;
            }

            ImageType resampled = new ImageType();

            ImageResampler resampler = new ImageResampler();
            for(int ch = 0; ch < image.ChannelsCount; ++ch)
            {
                Matrix<double> matrix = resampler.Downsample_Skippixel(image.GetMatrix(ch));

                if(resampleLevel == ResampleLevel.x4)
                {
                    matrix = resampler.Downsample_Skippixel(matrix);
                }

                resampled.SetMatrix(matrix, ch);

            }

            return resampled;
        }

        public static DisparityImage ResampleDisparityImage(DisparityImage image)
        {
            if(resampleLevel == ResampleLevel.None)
            {
                return image;
            }

            DisparityImage resampled = new DisparityImage();
            resampled.ImageMatrix = image.ImageMatrix.Clone();
            
            for(int c = 0; c < image.ColumnCount; ++c)
            {
                for(int r = 0; r < image.RowCount; ++r)
                {
                    if(resampled.ImageMatrix[r, c] < resampled.InvalidDisparity)
                    {
                        resampled.ImageMatrix[r, c] = resampled.ImageMatrix[r, c] * 0.5;
                    }
                }
            }

            ImageResampler resampler = new ImageResampler();
            Matrix<double> matrix = resampler.Downsample_Skippixel(resampled.ImageMatrix);

            if(resampleLevel == ResampleLevel.x4)
            {
                matrix = resampler.Downsample_Skippixel(matrix);
            }

            resampled.SetMatrix(matrix, 0);

            return resampled;
        }
        
        public enum SteroImage
        {
            PipesResampled,
            MotorResampled,
        }

        public static void PrepareImages(SteroImage caseType, out IImage left, out IImage right, out DisparityImage disp)
        {
            if(caseType == SteroImage.PipesResampled)
            {
                left = ResampleImage(LoadImage_PipesLeft());
                right = ResampleImage(LoadImage_PipesRight());
                disp = ResampleDisparityImage(LoadImage_PipesDisparity());
            }
            else //if(caseType == SteroImage.MotorResampled)
            {
                left = ResampleImage(LoadImage_MotorLeft());
                right = ResampleImage(LoadImage_MotorRight());
                disp = ResampleDisparityImage(LoadImage_MotorDisparity());
            }
        }

        public static void StoreDisparityMapAsImage(Context context, DisparityMap map, string suffix = "", int invalidDisparity = 254)
        {
            DisparityImage image = new DisparityImage();
            image.InvalidDisparity = invalidDisparity;
            image.FromDisparityMap(map);

            string path = context.ResultDirectory + "\\disparity_image_" + suffix + ".png";
            SaveImage(image, path);
        }

        public static void StoreDisparityMapInXml(Context context, DisparityMap map, string suffix = "", int invalidDisparity = 254)
        {
            string path = context.ResultDirectory + "\\disparity_map_" + suffix + ".xml";
            SaveMapXml(map, path);
        }

        public static void SaveMapXml(DisparityMap map, string path)
        {
            using(FileStream file = new FileStream(path, FileMode.Create))
            {
                XmlDocument xmlDoc = new XmlDocument();

                XmlNode mapNode = map.CreateMapNode(xmlDoc);
                xmlDoc.InsertAfter(mapNode, xmlDoc.DocumentElement);

                xmlDoc.Save(file);
            }
        }

    }
}
