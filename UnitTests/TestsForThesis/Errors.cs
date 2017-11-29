using System;
using System.Collections.Generic;
using System.Linq;

namespace CamUnitTest.TestsForThesis
{
    public class Deviation
    {
        public List<double> variations { get; set; }
        public double sum { get; set; } 
        public double mean { get; set; }
        public double max { get; set; }
        public double most { get; set; }
        public int pointCount { get; set; }
        public string name { get; set; }

        public Deviation(List<double> variations, string name = "")
        {
            SetErrors(variations);
            this.name = name;
        }

        public delegate List<double> GetVariationsList();
        public Deviation(GetVariationsList variationsListGetter, string name = "")
        {
            SetErrors(variationsListGetter());
            this.name = name;
        }

        void SetErrors(List<double> variations)
        {
            this.variations = variations;
            pointCount = variations.Count;
            sum = Math.Sqrt(variations.Sum());
            mean = sum / Math.Sqrt(pointCount);
            variations.Sort((a, b) => { return -a.CompareTo(b); });
            max = Math.Sqrt(variations[0]);
            most = Math.Sqrt(variations[(int)(pointCount * 0.05) + 1]);
        }
        
        public static void Store(Context context, Deviation deviation, string info = "", bool shortVer = false)
        {
            if(shortVer)
            {
                context.Output.AppendLine(deviation.mean.ToString("E3"));
                context.Output.AppendLine(deviation.most.ToString("E3"));
                context.Output.AppendLine(deviation.max.ToString("E3"));
            }
            else
            {
                if(info.Length > 0)
                {
                    context.Output.AppendLine("Case: " + info);
                }
                context.Output.AppendLine(deviation.name + " Mean: " + deviation.mean.ToString("E3"));
                context.Output.AppendLine(deviation.name + " 95 %: " + deviation.most.ToString("E3"));
                context.Output.AppendLine(deviation.name + " Max: " + deviation.max.ToString("E3"));
            }
            context.Output.AppendLine();
        }
    }
}
