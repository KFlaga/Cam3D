﻿using CamCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

namespace CamAutomatization
{
    public class ProcessingChain
    {
        private XmlDocument _xmlDoc;

        private GlobalData _globalData;
        private List<ILink> _links;

        public ProcessingChain()
        {
            _globalData = new GlobalData();
        }

        public void OpenChainFile()
        {
            FileOperations.LoadFromFile(OpenChainFile, "Xml File|*.xml");
        }

        private void OpenChainFile(Stream file, string path)
        {
            _xmlDoc = new XmlDocument();
            _xmlDoc.Load(file);
        }

        public void Process()
        {
            OpenChainFile();
            AddStandardLinks();

            try
            {
                foreach(var link in _links)
                {
                    link.Load();
                    link.Process();
                    link.Save();
                }
            }
            catch(Exception e)
            {

            }
            
            using(Stream outFile = new FileStream(_globalData.Get<ConfigurationLinkData>().
                WorkingDirectory + "chain_output.xml", FileMode.Create))
            {
                _xmlDoc.Save(outFile);
            }
        }

        private void AddStandardLinks()
        {
            _links = new List<ILink>();
            _links.Add(new ConfigurationLink(_globalData, _xmlDoc));
            _links.Add(new RawCalibrationImagesLink(_globalData));
            _links.Add(new DistortionModelLink(_globalData));
            _links.Add(new UndistortPointsLink(_globalData));
            _links.Add(new UndistortImagesLink(_globalData));
            _links.Add(new CalibrationLink(_globalData));
            _links.Add(new RectificationLink(_globalData));
            _links.Add(new MatchedImagesLink(_globalData));
            _links.Add(new ImageMatchingLink(_globalData));
            _links.Add(new DisparityRefinementLink(_globalData));
            _links.Add(new TriangulationLink(_globalData));
        }
    }
}