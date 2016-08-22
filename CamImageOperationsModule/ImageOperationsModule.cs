﻿using System.Windows.Controls;

namespace CamImageOperationsModule
{
    public class ImageOperationsModule : CamCore.Module
    {
        private UserControl _calibControl = null;

        public override string Name { get { return "Image Operations Module"; } } // Name which user sees
        public override UserControl MainPanel
        {
            get
            {
                if(_calibControl == null)
                    _calibControl = new ImageOperationsModePanel();
                return _calibControl;
            }
        }

        public override bool EndModule()
        {
            return true;
        }

        public override bool StartModule()
        {
            return true;
        }
    }
}
