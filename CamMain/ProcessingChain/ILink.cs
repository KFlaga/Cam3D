using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace CamMain.ProcessingChain
{
    /// <summary>
    /// Types of processing links can do.
    /// All links should be added in predefined order, specified in this enum
    /// </summary>
    public enum LinkType
    {
        Configuration = 0,
        RawCalibrationImagesExtraction,
        DistortionModelComputation,
        UndistortedPointsExtraction,
        CalibrationImagesUndistortion,
        OneCameraCalibration,
        CrossCalibration,
        RectificationComputation,
        CalibrationImagesRectification,
        MatchedImagesPrepare,
        ImageMatching,
        DisparityRefinement,
        Triangulation
    }

    /// <summary>
    /// Provides one processing link in chain.
    /// Each link have some input data, loaded from files or global data (output from previous links)
    /// and after processing produces output data stored in global data and
    /// optionaly in files. Links are chained in predefined order and may depend
    /// on previous ones outputs.
    /// </summary>
    public interface ILink
    {
        LinkType LinkType { get; }
        
        /// <summary>
        /// Indicates if output data should be stored on disc
        /// </summary>
        bool StoreDataOnDisc { get; set; }

        /// <summary>
        /// Indicates if previously saved processed data should be loaded
        /// from disc rather than processed again.
        /// If true no Process step occurs and loading of some internal config etc. is ommited.
        /// Also input file should contain output of this Link.
        /// After 'Save' GlobalData should contain same data as if it would be processed again.
        /// </summary>
        bool LoadDataFromDisc { get; set; }

        /// <summary>
        /// Loads necessary data from Xml config, other files or global data
        /// </summary>
        void Load();
        
        /// <summary>
        /// Perform processing step
        /// </summary>
        void Process();

        /// <summary>
        /// Stores link data to Xml config, other files or global data
        /// </summary>
        void Save();
    }
}
