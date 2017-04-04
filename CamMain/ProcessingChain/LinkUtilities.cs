﻿using CamCore;
using CamImageProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;

namespace CamMain.ProcessingChain
{
    public static class LinkUtilities
    {
        public static void SaveImages(Dictionary<int, ImagesPair> images, 
            ConfigurationLinkData config, string nodeName, string imageBaseName)
        {
            //< nodeName >
            //  < Image id = "0" cam = "left" path = "imageBaseName..." />              
            //</ nodeName >

            XmlNode oldNode = config.RootNode.FirstChildWithName(nodeName);
            bool oldNodeExists = null != oldNode;

            XmlNode node = config.ConfigDoc.CreateElement(nodeName);

            foreach(var entry in images)
            {
                ImagesPair imgPair = entry.Value;
                if(imgPair.Left != null)
                {
                    XmlNode nodeImage = config.ConfigDoc.CreateElement("Image");
                    SaveImage(nodeImage, config, imgPair.Left, entry.Key, CameraIndex.Left, imageBaseName);
                    node.AppendChild(nodeImage);
                }

                if(imgPair.Right != null)
                {
                    XmlNode nodeImage = config.ConfigDoc.CreateElement("Image");
                    SaveImage(nodeImage, config, imgPair.Right, entry.Key, CameraIndex.Right, imageBaseName);
                    node.AppendChild(nodeImage);
                }
            }

            if(oldNodeExists)
            {
                config.RootNode.ReplaceChild(node, oldNode);
            }
            else
            {
                config.RootNode.AppendChild(node);
            }
        }

        private static void SaveImage(XmlNode nodeImage, ConfigurationLinkData config, 
            IImage image, int id, CameraIndex idx, string imageBaseName)
        {
            XmlAttribute attId = config.ConfigDoc.CreateAttribute("id");
            XmlAttribute attCam = config.ConfigDoc.CreateAttribute("cam");
            XmlAttribute attPath = config.ConfigDoc.CreateAttribute("path");

            attId.Value = id.ToString();
            attCam.Value = idx == CameraIndex.Left ? "left" : "right";
            attPath.Value = imageBaseName + "_" + attCam.Value + "_" + attId.Value + ".png";
            string path = config.WorkingDirectory + attPath.Value;

            nodeImage.Attributes.Append(attId);
            nodeImage.Attributes.Append(attCam);
            nodeImage.Attributes.Append(attPath);

            using(Stream file = new FileStream(path, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image.ToBitmapSource()));
                encoder.Save(file);
            }
        }

        public static void UndistortImages(Dictionary<int, ImagesPair> images, 
            Dictionary<int, ImagesPair> undistortedImages, 
            DistortionModelLinkData distortion)
        {
            ImageTransformer undistort = new ImageTransformer(
                ImageTransformer.InterpolationMethod.Quadratic, 1);

            foreach(var entry in images)
            {
                ImagesPair rawPair = entry.Value;
                ImagesPair undistorted = new ImagesPair();

                undistorted.Left = UndistortImage(rawPair.Left, distortion.DistortionLeft, undistort);
                undistorted.Right = UndistortImage(rawPair.Right, distortion.DistortionRight, undistort);

                undistortedImages.Add(entry.Key, undistorted);
            }
        }

        private static IImage UndistortImage(IImage imgRaw, 
            RadialDistortionModel distortion, ImageTransformer undistort)
        {
            undistort.Transformation =
                new RadialDistortionTransformation(distortion);

            MaskedImage img = new MaskedImage(imgRaw);
            MaskedImage imgFinal = undistort.TransfromImageBackwards(img, true);
            return imgFinal;
        }

        public static void RectifyImages(Dictionary<int, ImagesPair> images,
            Dictionary<int, ImagesPair> rectifiedImages,
            RectificationLinkData rectification)
        {
            ImageTransformer rectifier = new ImageTransformer(
                ImageTransformer.InterpolationMethod.Quadratic, 1);

            foreach(var entry in images)
            {
                ImagesPair undistorted = entry.Value;
                ImagesPair rectified = new ImagesPair();

                rectifier.Transformation = new RectificationTransformation()
                {
                    RectificationMatrix = rectification.Rectification.RectificationLeft,
                    RectificationMatrixInverse = rectification.Rectification.RectificationLeft_Inverse
                };
                rectified.Left = rectifier.TransfromImageBackwards(undistorted.Left, true);

                rectifier.Transformation = new RectificationTransformation()
                {
                    RectificationMatrix = rectification.Rectification.RectificationRight,
                    RectificationMatrixInverse = rectification.Rectification.RectificationRight_Inverse
                };
                rectified.Right = rectifier.TransfromImageBackwards(undistorted.Right, true);

                rectifiedImages.Add(entry.Key, rectified);
            }
        }

        public static void LoadImages<ImageType>(Dictionary<int, ImagesPair> images, 
            ConfigurationLinkData config, string nodeName) where ImageType : IImage, new()
        {
            //<nodeName>
            //  <Image id="1" cam="left" path=""/>
            //</nodeName>

            XmlNode imgsNode = config.RootNode.FirstChildWithName(nodeName);

            foreach(XmlNode imgNode in imgsNode.ChildNodes)
            {
                int id = int.Parse(imgNode.Attributes["id"].Value);
                CameraIndex idx = imgNode.Attributes["cam"].Value.CompareTo("right") == 0 ?
                       CameraIndex.Right : CameraIndex.Left;

                string imgPath = config.WorkingDirectory + imgNode.Attributes["path"].Value;
                BitmapImage bitmap = new BitmapImage(new Uri(imgPath, UriKind.RelativeOrAbsolute));
                ImageType image = new ImageType();
                image.FromBitmapSource(bitmap);

                images.SetImage(image, id, idx);
            }
        }

        public static void SetImage(this Dictionary<int, ImagesPair> images, IImage image, int id, CameraIndex idx)
        {
            ImagesPair imgs;
            if(images.TryGetValue(id, out imgs))
            {
                imgs.SetImage(idx, image);
            }
            else
            {
                imgs = new ImagesPair();
                imgs.SetImage(idx, image);
                images.Add(id, imgs);
            }
        }

        public static IImage GetImage(this Dictionary<int, ImagesPair> images, IImage image, int id, CameraIndex idx)
        {
            ImagesPair imgs;
            if(images.TryGetValue(id, out imgs))
            {
                return imgs.GetImage(idx);
            }
            else
                throw new KeyNotFoundException("Image with id: " + id.ToString() + " not found.");
        }
    }
}
