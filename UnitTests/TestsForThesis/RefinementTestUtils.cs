using CamAlgorithms.ImageMatching;
using CamCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static CamUnitTest.TestsForThesis.SgmTestUtils;

namespace CamUnitTest.TestsForThesis
{
    public class RefinementTestUtils
    {
        public static void PrepareImages(SteroImage caseType, out IImage left, out IImage right, out DisparityImage disp)
        {
            if(caseType == SteroImage.PipesResampled)
            {
                // Resample images: resample by skipping pixels 2 times (so final image is about 180x120)
                // In comparsion of final disparity map divide each disparity by 4
                left = LoadImage_PipesLeft();
                right = LoadImage_PipesRight();
                disp = LoadImage_PipesDisparity();
            }
            else //if(caseType == SteroImage.MotorResampled)
            {
                left = LoadImage_MotorLeft();
                right = LoadImage_MotorRight();
                disp = LoadImage_MotorDisparity();
            }
        }

        public static void LoadSgmResultMap(Context context, SteroImage caseType, out DisparityMap leftMap, out DisparityMap rightMap)
        {
            string pathLeft = context.ResultDirectory;
            string pathRight = context.ResultDirectory;
            if(caseType == SteroImage.PipesResampled)
            {
                pathLeft += "\\refiners_input_pipes\\disparity_map__pipes_left.xml";
                pathRight += "\\refiners_input_pipes\\disparity_map__pipes_right.xml";
            }
            else
            {
                pathLeft += "\\refiners_input_motor\\disparity_map__motor_left.xml";
                pathRight += "\\refiners_input_motor\\disparity_map__motor_right.xml";
            }
            leftMap = LoadMapXml(pathLeft);
            rightMap = LoadMapXml(pathRight);
        }
        
        public static DisparityMap LoadMapXml(string path)
        {
            DisparityMap map;
            using(FileStream file = new FileStream(path, FileMode.Open))
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(file);

                XmlNode mapNode = xmlDoc.GetElementsByTagName("DisparityMap")[0];
                map = DisparityMap.CreateFromNode(mapNode);
            }
            return map;
        }

        public static LimitRangeRefiner PrepareLimitRange(int dmin, int dmax)
        {
            return new LimitRangeRefiner()
            {
                MaxDisparity = dmax,
                MinDisparity = dmin
            };
        }

        public static InvalidateLowConfidenceRefiner PrepareLowConfidence(double minConf)
        {
            return new InvalidateLowConfidenceRefiner()
            {
                ConfidenceTreshold = minConf
            };
        }

        public static CrossCheckRefiner PrepareCrossCheck(double maxDiff)
        {
            return new CrossCheckRefiner()
            {
                MaxDisparityDiff = maxDiff
            };
        }

        public static MedianFilterRefiner PrepareMedianFilter()
        {
            return new MedianFilterRefiner()
            {
                
            };
        }

        public static PeakRemovalRefiner PreparePeakRemoval(double maxDiff, int minArea, bool interpolate, int iterpolateTresh)
        {
            return new PeakRemovalRefiner()
            {
                MaxDisparityDiff = maxDiff,
                MinSegmentSize = minArea,
                InterpolateInvalidated = interpolate,
                MinValidPixelsCountForInterpolation = iterpolateTresh
            };
        }
        
        public static AnisotopicDiffusionRefiner PrepareDiffusion(double kernelCoeff, double stepCoeff, int iterations)
        {
            return new AnisotopicDiffusionRefiner()
            {
                KernelType = AnisotopicDiffusionRefiner.CoeffKernelType.Exponential,
                UseEightDirections = false,
                KernelCoeff = kernelCoeff,
                StepCoeff = stepCoeff,
                Iterations = iterations,
                SmoothDisparityMap = false,
                InterpolateDisparityMap = false
            };
        }

        public static AnisotopicDiffusionRefiner PrepareSmoothing(double kernelCoeff, double stepCoeff, int iterations)
        {
            return new AnisotopicDiffusionRefiner()
            {
                KernelType = AnisotopicDiffusionRefiner.CoeffKernelType.Exponential,
                UseEightDirections = false,
                KernelCoeff = kernelCoeff,
                StepCoeff = stepCoeff,
                Iterations = iterations,
                SmoothDisparityMap = true,
                InterpolateDisparityMap = false
            };
        }

        public static AnisotopicDiffusionRefiner PrepareInterpolation(double kernelCoeff, double stepCoeff, int iterations)
        {
            return new AnisotopicDiffusionRefiner()
            {
                KernelType = AnisotopicDiffusionRefiner.CoeffKernelType.Exponential,
                UseEightDirections = false,
                KernelCoeff = kernelCoeff,
                StepCoeff = stepCoeff,
                Iterations = iterations,
                SmoothDisparityMap = false,
                InterpolateDisparityMap = true
            };
        }
    }
}
