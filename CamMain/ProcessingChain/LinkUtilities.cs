using CamCore;
using CamAlgorithms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using CamAlgorithms.Calibration;

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
                    SaveImage(nodeImage, config, imgPair.Left, entry.Key, SideIndex.Left, imageBaseName);
                    node.AppendChild(nodeImage);
                }

                if(imgPair.Right != null)
                {
                    XmlNode nodeImage = config.ConfigDoc.CreateElement("Image");
                    SaveImage(nodeImage, config, imgPair.Right, entry.Key, SideIndex.Right, imageBaseName);
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
            IImage image, int id, SideIndex idx, string imageBaseName)
        {
            XmlAttribute attId = config.ConfigDoc.CreateAttribute("id");
            XmlAttribute attCam = config.ConfigDoc.CreateAttribute("cam");
            XmlAttribute attPath = config.ConfigDoc.CreateAttribute("path");

            attId.Value = id.ToString();
            attCam.Value = idx == SideIndex.Left ? "left" : "right";
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
            RadialDistortion distortion, ImageTransformer undistort)
        {
            undistort.Transformation =
                new RadialDistortionTransformation(distortion.Model);

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
                SideIndex idx = imgNode.Attributes["cam"].Value.CompareTo("right") == 0 ?
                       SideIndex.Right : SideIndex.Left;

                string imgPath = config.WorkingDirectory + imgNode.Attributes["path"].Value;
                BitmapImage bitmap = new BitmapImage(new Uri(imgPath, UriKind.RelativeOrAbsolute));
                ImageType image = new ImageType();
                image.FromBitmapSource(bitmap);

                images.SetImage(image, id, idx);
            }
        }

        public static void SetImage(this Dictionary<int, ImagesPair> images, IImage image, int id, SideIndex idx)
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

        public static IImage GetImage(this Dictionary<int, ImagesPair> images, IImage image, int id, SideIndex idx)
        {
            ImagesPair imgs;
            if(images.TryGetValue(id, out imgs))
            {
                return imgs.GetImage(idx);
            }
            else
                throw new KeyNotFoundException("Image with id: " + id.ToString() + " not found.");
        }

        public static void SaveDisparityMaps(Dictionary<int, DisparityMap> maps,
           ConfigurationLinkData config, string nodeName, string mapBaseName)
        {
            //< nodeName >
            //  < Map id = "0" path = "mapBaseName..." />              
            //</ nodeName >

            XmlNode oldNode = config.RootNode.FirstChildWithName(nodeName);
            bool oldNodeExists = null != oldNode;

            XmlNode node = config.ConfigDoc.CreateElement(nodeName);

            foreach(var entry in maps)
            {
                DisparityMap map = entry.Value;
                if(map != null)
                {
                    XmlNode dispMapNode = config.ConfigDoc.CreateElement("Map");
                    SaveDisparityMap(dispMapNode, config, map, entry.Key, mapBaseName);
                    node.AppendChild(dispMapNode);
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

        private static void SaveDisparityMap(XmlNode dispMapNode, ConfigurationLinkData config,
            DisparityMap map, int id, string mapBaseName)
        {
            XmlAttribute attId = config.ConfigDoc.CreateAttribute("id");
            XmlAttribute attPath = config.ConfigDoc.CreateAttribute("path");

            attId.Value = id.ToString();
            attPath.Value = mapBaseName + "_" + attId.Value + ".xml";
            string path = config.WorkingDirectory + attPath.Value;

            dispMapNode.Attributes.Append(attId);
            dispMapNode.Attributes.Append(attPath);

            XmlDocument dispDoc = new XmlDocument();
            XmlNode mapNode = map.CreateMapNode(dispDoc);

            dispDoc.InsertAfter(mapNode, dispDoc.DocumentElement);

            using(FileStream file = new FileStream(path, FileMode.Create))
            {
                dispDoc.Save(file);
            }
        }


        public static void LoadDisparityMaps(Dictionary<int, DisparityMap> maps,
            ConfigurationLinkData config, string nodeName)
        {
            //<nodeName>
            //  <Map id="1" path=""/>
            //</nodeName>

            XmlNode mapListNode = config.RootNode.FirstChildWithName(nodeName);

            foreach(XmlNode mapNode in mapListNode.ChildNodes)
            {
                int id = int.Parse(mapNode.Attributes["id"].Value);

                string path = config.WorkingDirectory + mapNode.Attributes["path"].Value;

                XmlDocument mapDoc = new XmlDocument();
                using(FileStream file = new FileStream(path, FileMode.Open))
                {
                    mapDoc.Load(file);
                }

                XmlNode mapContentsNode = mapDoc.GetElementsByTagName("DisparityMap")[0];
                DisparityMap map = DisparityMap.CreateFromNode(mapContentsNode);
                maps.Add(id, map);
            }
        }
    }
}

